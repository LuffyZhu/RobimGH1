using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Windows.Forms;
using Eto.Forms;
using System.ComponentModel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Parameters;
using Eto.Drawing;
using Grasshopper.Kernel.Data;
using GH = Grasshopper;
using System.Threading.Tasks;
using Rhino;
using Emgu.CV.Dnn;
using System.Collections;
using Rhino.Render.ChangeQueue;
using System.Threading;
using Mesh = Rhino.Geometry.Mesh;
using GH_IO.Serialization;
using System.Runtime.Remoting.Channels;
using GrasshopperAsyncComponent;
using System.Windows.Forms;
using CheckBox = Eto.Forms.CheckBox;
using System.Windows.Documents;
namespace Robim.Grasshopper
{
    public class Kinematics : GH_Component
    {
        public Kinematics() : base("Kinematics", "K", "Inverse and forward kinematics for a single target (or group of targets in a robot cell with coord", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override Guid ComponentGuid => new Guid("{EFDA05EB-B281-4703-9C9E-B5F98A9B2E1D}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconKinematics;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
            pManager.AddParameter(new TargetParameter(), "Target", "T", "One target per robot", GH_ParamAccess.list);
            pManager.AddTextParameter("Previous joints", "J", "Optional previous joint values. If the pose is ambigous is will select one based on this previous position.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Display geometry", "M", "Display mesh geometry of the robot", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "Robot system's meshes", GH_ParamAccess.list);
            pManager.AddTextParameter("Joints", "J", "Robot system's joint rotations as a string of numbers separated by commas.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Planes", "P", "Robot system's joint lanes", GH_ParamAccess.list);
            pManager.AddTextParameter("Errors", "E", "Errors in kinematic solution", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_RobotSystem robotSystem = null;
            var targets = new List<GH_Target>();
            var prevJointsText = new List<GH_String>();
            bool drawMeshes = false;

            if (!DA.GetData(0, ref robotSystem)) { return; }
            if (!DA.GetDataList(1, targets)) { return; }
            DA.GetDataList(2, prevJointsText);
            if (!DA.GetData(3, ref drawMeshes)) { return; }

            List<double[]> prevJoints = null;

            if (prevJointsText.Count > 0)
            {
                prevJoints = new List<double[]>();

                foreach (var text in prevJointsText)
                {
                    if (text != null)
                    {
                        string[] jointsText = text.Value.Split(',');
                        var prevJoint = new double[jointsText.Length];

                        for (int i = 0; i < jointsText.Length; i++)
                            if (!GH_Convert.ToDouble_Secondary(jointsText[i], ref prevJoint[i])) throw new Exception(" Previous joints not formatted correctly.");

                        prevJoints.Add(prevJoint);
                    }
                }
            }

            var kinematics = robotSystem.Value.Kinematics(targets.Select(x => x.Value), prevJoints);

            var errors = kinematics.SelectMany(x => x.Errors);
            if (errors.Count() > 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Errors in solution");
            }

            var strings = kinematics.SelectMany(x => x.Joints).Select(x => new GH_Number(x).ToString());
            var joints = string.Join(",", strings);

            var planes = kinematics.SelectMany(x => x.Planes);

            if (drawMeshes)
            {
                var meshes = GeometryUtil.PoseMeshes(robotSystem.Value, kinematics, targets.Select(t => t.Value.Tool.Mesh_).ToList());
                DA.SetDataList(0, meshes.Select(x => new GH_Mesh(x)));
            }

            DA.SetData(1, joints);
            DA.SetDataList(2, planes.Select(x => new GH_Plane(x)));
            DA.SetDataList(3, errors);
        }
    }

    public sealed class Simulation : GH_Component, IDisposable, IGH_VariableParameterComponent
    {
        public Simulation() : base("Program simulation", "Sim", "Rough simulation of the robot program, right click for playback controls", "Robim", "Components")
        {
            //form = new AnimForm(this)
            //{
            //    Owner = Rhino.UI.RhinoEtoApp.MainWindow
            //};
        }

        double time = 0;
        double sliderTime = 0;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override Guid ComponentGuid => new Guid("{6CE35140-A625-4686-B8B3-B734D9A36CFC}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconSimulation;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program to simulate", GH_ParamAccess.item);
            pManager.AddNumberParameter("Time", "T", "Advance the simulation to this time", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Normalized", "N", "Time value is normalized (from 0 to 1)", GH_ParamAccess.item, true);
            //pManager.AddGeometryParameter("Geometry", "G", "Geometry to simulate", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("System meshes", "M", "System meshes", GH_ParamAccess.list);
            pManager.AddNumberParameter("Joint rotations", "J", "Joint rotations", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "P", "TCP position", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Index", "I", "Current target index", GH_ParamAccess.item);
            pManager.AddNumberParameter("Time", "T", "Current time in seconds", GH_ParamAccess.item);
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "This is the same program as the input program. Use this output to update other visualization components along with the simulation.", GH_ParamAccess.item);
            pManager.AddTextParameter("Errors", "E", "Errors", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Target Plane", "T.P", "Target Plane when has coupled plane", GH_ParamAccess.list);
            //pManager.AddGeometryParameter("Target Geometry", "T.G", "Target Geometry when has input", GH_ParamAccess.list);
            pManager.AddMeshParameter("Workpiece mesh", "W.Mesh", "Workpiece mesh", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Coupled Plane", "C", "Coupled Plane", GH_ParamAccess.list);
        }

        IGH_Param[] parameters = new IGH_Param[1]
        {
            new Param_Geometry() { Name = "Geometry", NickName = "G", Description = "Geometry to simulate", Optional = true}
        };

        private void AddParam(int index)
        {
            IGH_Param parameter = parameters[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        private void AddGeometry(object sender, EventArgs e) => AddParam(0);

        List<Plane> plane;
        protected override void BeforeSolveInstance()
        {
            plane = new List<Plane>();
        }

        //[STAThread]
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasGeometry = Params.Input.Any(x => x.Name == "Geometry");
            GH_Program program = null;
            GH_Number sliderTimeGH = null;
            GH_Boolean isNormalized = null;
            GeometryBase geometry = null;
            Mesh workpiece = null;
            if (!DA.GetData(0, ref program)) { return; }
            if (!DA.GetData(1, ref sliderTimeGH)) { return; }
            if (!DA.GetData(2, ref isNormalized)) { return; }
            if (hasGeometry) DA.GetData("Geometry", ref geometry);

            sliderTime = (isNormalized.Value) ? sliderTimeGH.Value * program.Value.Duration : sliderTimeGH.Value;
            //if (GH.Instances.DocumentEditor==null || !form.Visible) 
            time = sliderTime;

            program.Value.Animate(time, false);
            var currentTarget = program.Value.CurrentSimulationTarget;

            var errors = currentTarget.ProgramTargets.SelectMany(x => x.Kinematics.Errors);
            var joints = currentTarget.ProgramTargets.SelectMany(x => x.Kinematics.Joints);
            var planes = currentTarget.ProgramTargets.SelectMany(x => x.Kinematics.Planes).ToList();//外部轴耦合面旋转面
            var meshes = GeometryUtil.PoseMeshes(program.Value.RobotSystem, currentTarget.ProgramTargets.Select(p => p.Kinematics).ToList(), currentTarget.ProgramTargets.Select(p => p.Target.Tool.Mesh_).ToList());
            //GH_Plane plane = new GH_Plane(CreateTarget.plane);
            //plane = CreateTarget.curveplane;
            var targets = program.Value.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                //目标plane
                plane.Add(targets[i].ProgramTargets[0].Plane);
            }
            Plane[] planes1 = new Plane[plane.Count];
            Frame frame = targets[0].ProgramTargets[0].Target.Frame;
            if (frame != null && frame.IsCoupled)//有外部轴才有coupled plane
            {
                string[] externaltype = program.Value.RobotSystem.RobimFormSystem.External_Type;
                int j = 0;
                foreach (string str in externaltype)
                {
                    if (str.Contains("Track"))
                        j += 2;
                    if (str.Contains("Platform"))//一般只有1个，变位机有2个
                    {
                        j += 1;
                    }
                }
                Plane rotateplane = DigitalCoupledPlane.DCP.CustomPlane;
                rotateplane.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.P_CoupledPlane, planes[j]));//Positioner.coupledplane == 初始耦合面
                //if (geometry != null)
                //{
                //    geometry.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, rotateplane));
                //}
                if (currentTarget.ProgramTargets[0].Target.Workpiece != null)
                {
                    workpiece = currentTarget.ProgramTargets[0].Target.Workpiece.DuplicateMesh();
                    workpiece.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, rotateplane));
                }
                //Couple
                for (int i = 0; i < plane.Count; i++)
                {
                    Plane plane1 = plane[i];
                    plane1.Transform(Transform.PlaneToPlane(Plane.WorldXY, rotateplane));
                    //plane1.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, rotateplane));
                    planes1.SetValue(plane1, i);
                }
            }

            if (errors.Count() > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Errors in solution");

            DA.SetDataList(0, meshes);
            DA.SetDataList(1, joints);
            DA.SetDataList(2, planes.Select(x => new GH_Plane(x)));
            DA.SetData(3, currentTarget.Index);
            DA.SetData(4, program.Value.CurrentSimulationTime);
            DA.SetData(5, new GH_Program(program.Value));
            DA.SetDataList(6, errors);
            DA.SetDataList(7, planes1);
            //DA.SetData(8, geometry);
            DA.SetData(8, workpiece);
            DA.SetData(9, DigitalCoupledPlane.DCP.P_CoupledPlane);

            //if (GH.Instances.DocumentEditor != null && (form.Visible && form.play.Checked.Value))
            if (GH.Instances.DocumentEditor == null)
            {
                var currentTime = DateTime.Now;
                TimeSpan delta = currentTime - lastTime;
                time += delta.TotalSeconds * speed;
                lastTime = currentTime;
                ExpireSolution(true);
            }
        }

        // Form
        //AnimForm form;
        double speed = 1;
        DateTime lastTime;

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            //Menu_AppendItem(menu, "Open controls", OpenForm, true, form.Visible);
            //Menu_AppendSeparator(menu);
            //Menu_AppendItem(menu, "Geometry Simulation", AddGeometry, true, Params.Input.Any(x => x.Name == "Geometry"));
        }

        void OpenForm(object sender, EventArgs e)
        {
            //if (form.Visible)
            //{
            //    form.play.Checked = false;
            //    form.Visible = false;
            //}
            //else
            //{
            //    var mousePos = Mouse.Position;
            //    int x = (int)mousePos.X + 20;
            //    int y = (int)mousePos.Y - 160;

            //    form.Location = new Eto.Drawing.Point(x, y);
            //    form.Show();
            //}
        }

        void ClickPlay(object sender, EventArgs e)
        {
            //lastTime = DateTime.Now;
            //ExpireSolution(true);
        }

        void ClickStop(object sender, EventArgs e)
        {
            //form.play.Checked = false;
            //time = sliderTime;
            //ExpireSolution(true);
        }

        void ClickScroll(object sender, EventArgs e)
        {
            //speed = (double)form.slider.Value / 100.0;
        }

        public void Dispose()
        {
            //form.Dispose();
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
        //class AnimForm : Form
        class AnimForm : Eto.Forms.Form
        {
            Simulation _component;

            internal CheckBox play;
            internal Slider slider;

            public AnimForm(Simulation component)
            {
                _component = component;

                Maximizable = false;
                Minimizable = false;
                Padding = new Eto.Drawing.Padding(5);
                Resizable = false;
                ShowInTaskbar = true;
                Topmost = true;
                Title = "Playback";
                WindowStyle = WindowStyle.Default;

                var font = new Font(FontFamilies.Sans, 12, FontStyle.None, FontDecoration.None);
                var size = new Size(35, 35);

                play = new CheckBox() {
                    Text = "\u25B6",
                    Size = size,
                    Font = font,
                    Checked = false,
                    TabIndex = 0
                };
                play.CheckedChanged += component.ClickPlay;

                var stop = new Eto.Forms.Button() {
                    Text = "\u25FC",
                    Size = size,
                    Font = font,
                    TabIndex = 1
                };
                stop.Click += component.ClickStop;

                slider = new Slider() {
                    Orientation = Eto.Forms.Orientation.Vertical,
                    Size = new Size(45, 200),
                    TabIndex = 2,
                    MaxValue = 400,
                    MinValue = -200,
                    TickFrequency = 100,
                    SnapToTick = true,
                    Value = 100,
                };
                slider.ValueChanged += _component.ClickScroll;

                var speedLabel = new Eto.Forms.Label() {
                    Text = "100%",
                    VerticalAlignment = VerticalAlignment.Center,
                };

                var layout = new DynamicLayout();
                layout.BeginVertical(new Eto.Drawing.Padding(2), Size.Empty);
                layout.AddSeparateRow(padding: new Eto.Drawing.Padding(10), spacing: new Size(10, 0), controls: new Eto.Forms.Control[] { play, stop });
                layout.BeginGroup("Speeds");
                layout.AddSeparateRow(slider, speedLabel);
                layout.EndGroup();
                layout.EndVertical();

                Content = layout;
            }

            protected override void OnClosing(CancelEventArgs e)
            {
                //base.OnClosing(e);
                e.Cancel = true;
                play.Checked = false;
                Visible = false;
            }
        }
    }

    public sealed class CollisionCheck : GH_AsyncComponent
    {
        public CollisionCheck() : base("Collision Test", "CT", "Detect possible collisions in program", "Robim", "Components")
        {
            //form = new AnimForm(this)
            //{
            //    Owner = Rhino.UI.RhinoEtoApp.MainWindow
            //};
            BaseWorker = new LoopWorker();
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override Guid ComponentGuid => new Guid("{BF09B895-72A9-46B4-92E6-3BBFDB024438}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.collision;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Cancel", (s, e) =>
            {
                RequestCancellation();
            });
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program to ckeck", GH_ParamAccess.item);
            //pManager.AddBooleanParameter("Start", "S", "Set to true when you want to check collisions", GH_ParamAccess.item, false);
            pManager.AddMeshParameter("Environment", "E", "Optional environment mesh you want to check", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Result of oollision checking", GH_ParamAccess.item);
            pManager.AddNumberParameter("Time", "T", "Time points wehen collisions happen", GH_ParamAccess.list);
            pManager.AddNumberParameter("Index", "I", "indices of targets which cause collision", GH_ParamAccess.list);
        }

        private class LoopWorker : WorkerInstance
        {
            GH_Program _loopProgram = new GH_Program();
            List<Mesh> envMeshList = new List<Mesh>();
            List<double> finaltList = new List<double>();
            List<int> finaliList = new List<int>();
            public LoopWorker() : base(null) { }
            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                GH_Program _program = new GH_Program();
                List<Mesh> _envMeshList = new List<Mesh>();
                DA.GetData(0, ref _program);
                //if (!DA.GetData(1, ref _envMeshList)) { return; }
                DA.GetDataList(1,  _envMeshList);
                _loopProgram = _program;
                envMeshList= _envMeshList;
            }

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                List<double> tList = new List<double>();
                double tTime = 0;
                List<int> iList = new List<int>();
                var targets = _loopProgram.Value.Targets;
                // 👉 Checking for cancellation!
                if (CancellationToken.IsCancellationRequested) { return; }
                
                for (int j = 0; j < targets.Count; j++)
                {
                    if (CancellationToken.IsCancellationRequested) { return; }
                    ReportProgress(Id, (double)j / (targets.Count - 1));
                    tTime += targets[j].DeltaTime;
                    _loopProgram.Value.Animate(tTime, false);
                    var curTarget = _loopProgram.Value.CurrentSimulationTarget;
                    var coMeshes = GeometryUtil.PoseMeshes(_loopProgram.Value.RobotSystem, curTarget.ProgramTargets.Select(p => p.Kinematics).ToList(), curTarget.ProgramTargets.Select(p => p.Target.Tool.Mesh_).ToList());
                    Mesh C = coMeshes[coMeshes.Count - 1];
                    //A = C;
                    coMeshes.RemoveAt(coMeshes.Count - 1);
                    coMeshes.RemoveAt(coMeshes.Count - 1);
                    coMeshes.Concat(envMeshList);
                    /*for (int k = 0; k < envMeshList.Count; k++)
                    {
                        coMeshes.Add(envMeshList[k]);
                    }*/
                    List<Mesh> O = coMeshes;
                    //B = O;
                    //Define boolean to check if there is a collision
                    bool isColliding = false;
                    //Define integer to hold the index of the colliding geometry
                    int collidingIndex = -1;

                    //Define tolerance as the model space absolute tolerance
                    double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

                    //Go through the list of incoming obstacles
                    for (int i = 0; i < O.Count; i++)
                    {
                        //Get obstacle at index i
                        Mesh obs = O[i];

                        //Get the Object Type of collider C



                        //If C is a Mesh
                        Mesh meshC = (Mesh)C;

                        //Get the Object Type of obstacle


                        //If obstacle is a Mesh
                        Mesh meshO = (Mesh)obs;
                        //Both C and O are meshes. Perform intersection.
                        bool success = SolveIntersection(meshC, meshO);
                        if (success)
                        {
                            //Set boolean as true
                            isColliding = true;
                            //Save the index i as the colliding index
                            collidingIndex = i;
                            //Break the inner for loop
                            tList.Add(tTime / _loopProgram.Value.Duration);
                            iList.Add(j);

                            break;
                        }

                        //If Collider A does not collide with any other geometry
                        else
                        {
                            //Set boolean as false
                            isColliding = false;
                            //Save -1 as the colliding index
                            collidingIndex = -1;
                        }
                    }
                }
                finaliList = iList;
                finaltList = tList;
                Done();
            }
            public override void SetData(IGH_DataAccess DA)
            {
                if (finaltList.Count > 0)
                {
                    //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Collision happened!!!");
                    DA.SetData(0, "Collision happened!!!");
                }
                else
                {
                    DA.SetData(0, "Collision check pass");
                }
                DA.SetDataList(1, finaltList);
                DA.SetDataList(2, finaliList);
            }
            public override WorkerInstance Duplicate() => new LoopWorker();
            private bool SolveIntersection(Mesh meshA, Mesh meshB)
            {
                bool intersecting = false;
                double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
                Polyline[] polylines = Rhino.Geometry.Intersect.Intersection.MeshMeshAccurate
                  (meshA, meshB, tol);
                //If there is an intersection
                if (polylines != null && polylines.Length > 0)
                {
                    //Set boolean as true
                    intersecting = true;
                }
                return intersecting;
            }
        }          
    }


    /*public sealed class CollisionCheck2 : GH_Component
    {
        private bool _shouldExpire = false;
        private string _message = "";
        GH_Program loopProgram = new GH_Program();
        List<Mesh> envMeshList = new List<Mesh>();
        List<double> tList = new List<double>();
        List<int> iList = new List<int>();
        public CollisionCheck2() : base("Collision Test2", "CT", "Detect possible collisions in program", "Robim", "Components")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override Guid ComponentGuid => new Guid("{21B51A35-7001-447E-B836-6CDDA50CA196}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.collision;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program to ckeck", GH_ParamAccess.item);
            //pManager.AddBooleanParameter("Start", "S", "Set to true when you want to check collisions", GH_ParamAccess.item, false);
            pManager.AddMeshParameter("Environment", "E", "Optional environment mesh you want to check", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Result of oollision checking", GH_ParamAccess.item);
            pManager.AddNumberParameter("Time", "T", "Time points wehen collisions happen", GH_ParamAccess.list);
            pManager.AddNumberParameter("Index", "I", "indices of targets which cause collision", GH_ParamAccess.list);
        }

        private bool SolveIntersection(Mesh meshA, Mesh meshB)
        {
            bool intersecting = false;
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Polyline[] polylines = Rhino.Geometry.Intersect.Intersection.MeshMeshAccurate
              (meshA, meshB, tol);
            //If there is an intersection
            if (polylines != null && polylines.Length > 0)
            {
                //Set boolean as true
                intersecting = true;
            }
            return intersecting;
        }

        delegate void Act();

        Act act = () =>
        {
            Instances.DocumentEditor.Refresh();

        };

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_shouldExpire)
            {
                // This is the second time SI was invoked
                if (tList.Count > 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Collision happened!!!");
                    DA.SetData(0, "Collision happened!!!");
                }
                else
                {
                    DA.SetData(0, "Collision check pass");
                }
                DA.SetDataList(1, tList);
                DA.SetDataList(2, iList);
                _shouldExpire = false;
                this.Message = "Done!";
                return;
            }

            //int iterations = 0;
            //if (!DA.GetData(0, ref iterations)) return;


            GH_Program _program = new GH_Program();
            List<Mesh> _envMeshList = new List<Mesh>();
            DA.GetData(0, ref _program);
            //if (!DA.GetData(1, ref _envMeshList)) { return; }
            DA.GetDataList(1, _envMeshList);
            loopProgram = _program;
            envMeshList = _envMeshList;

            this.Message = "Computing...";
            int iterations = loopProgram.Value.Targets.Count;
            AsyncForLoop(loopProgram);
        }
        private void AsyncForLoop(GH_Program program)
        {
            Task.Run(() =>
            {
                //int iterations = 0;
                List<CellTarget> targetList = program.Value.Targets;
                double tTime = 0;
                tList = null;
                iList = null;

                for (int j = 0; j < targetList.Count; j++)
                {
                    Message = Math.Round(100 * (double)j / (targetList.Count - 1), 2) + "%";
                    Instances.DocumentEditor.Invoke(act);
                    tTime += targetList[j].DeltaTime;
                    program.Value.Animate(tTime, false);
                    var curTarget = program.Value.CurrentSimulationTarget;
                    var coMeshes = GeometryUtil.PoseMeshes(program.Value.RobotSystem, curTarget.ProgramTargets.Select(p => p.Kinematics).ToList(), curTarget.ProgramTargets.Select(p => p.Target.Tool.Mesh_).ToList());
                    Mesh C = coMeshes[coMeshes.Count - 1];
                    //A = C;
                    coMeshes.RemoveAt(coMeshes.Count - 1);
                    coMeshes.RemoveAt(coMeshes.Count - 1);
                    for (int k = 0; k < envMeshList.Count; k++)
                    {
                        coMeshes.Add(envMeshList[k]);
                    }
                    List<Mesh> O = coMeshes;
                    //B = O;
                    //Define boolean to check if there is a collision
                    bool isColliding = false;
                    //Define integer to hold the index of the colliding geometry
                    int collidingIndex = -1;

                    //Define tolerance as the model space absolute tolerance
                    double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

                    //Go through the list of incoming obstacles
                    for (int i = 0; i < O.Count; i++)
                    {
                        //Get obstacle at index i
                        Mesh obs = O[i];

                        //Get the Object Type of collider C



                        //If C is a Mesh
                        Mesh meshC = (Mesh)C;

                        //Get the Object Type of obstacle


                        //If obstacle is a Mesh
                        Mesh meshO = (Mesh)obs;
                        //Both C and O are meshes. Perform intersection.
                        bool success = SolveIntersection(meshC, meshO);
                        if (success)
                        {
                            //Set boolean as true
                            isColliding = true;
                            //Save the index i as the colliding index
                            collidingIndex = i;
                            //Break the inner for loop
                            tList.Add(tTime / program.Value.Duration);
                            iList.Add(j);

                            break;
                        }

                        //If Collider A does not collide with any other geometry
                        else
                        {
                            //Set boolean as false
                            isColliding = false;
                            //Save -1 as the colliding index
                            collidingIndex = -1;
                        }
                    }
                    /*if (j == targetList.Count - 1)
                    {
                        Message = "Done";
                        Rhino.RhinoApp.InvokeOnUiThread((Action)delegate {
                            base.ExpireDownStreamObjects();
                            OnDisplayExpired(true);
                        });
                    }*/
              /*  }

                //_message = "Completed " + iterations + " iterations";

                _shouldExpire = true;

                RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });



            });
        }
        protected override void ExpireDownStreamObjects()
        {
            if (_shouldExpire)
            {
                base.ExpireDownStreamObjects();
            }
        }

    }*/



    public class PlaneRotateSuggest : GH_Component, IGH_VariableParameterComponent
    {
        public PlaneRotateSuggest() : base("PlaneRotateSuggest", "PlaneRotateSuggest", "PlaneRotateSuggest", "Robim", "Suggestion") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{16926666-4522-4814-9B2D-35B45B9A8C5E}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconKinematics;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
            pManager.AddParameter(new ToolParameter(), "Tool", "T", "Tool system", GH_ParamAccess.item);
            pManager.AddTextParameter("Previous joints", "J", "Optional previous joint values. If the pose is ambigous is will select one based on this previous position.\nRadians", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane", GH_ParamAccess.item);
            pManager.AddTextParameter("Axis", "A", "Axis\nEx:0,1,2\n0:x,1:y,2:z", GH_ParamAccess.item);
            pManager.AddTextParameter("Rotate Step", "R", "Rotate step\nDegrees", GH_ParamAccess.item, "45");
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform", "T", "Transform", GH_ParamAccess.item);
            pManager.AddTextParameter("Result", "R", "Result", GH_ParamAccess.item);
            pManager.AddTextParameter("Joints", "joints", "joints", GH_ParamAccess.tree);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasdomain1 = Params.Input.Any(x => x.Name == "Axis 1 Domain");
            bool hasdomain2 = Params.Input.Any(x => x.Name == "Axis 2 Domain");
            bool hasdomain3 = Params.Input.Any(x => x.Name == "Axis 3 Domain");
            bool hasdomain4 = Params.Input.Any(x => x.Name == "Axis 4 Domain");
            bool hasdomain5 = Params.Input.Any(x => x.Name == "Axis 5 Domain");
            bool hasdomain6 = Params.Input.Any(x => x.Name == "Axis 6 Domain");
            Interval domain1 = new Interval(-Math.PI, Math.PI);
            Interval domain2 = new Interval(-Math.PI, Math.PI);
            Interval domain3 = new Interval(-Math.PI, Math.PI);
            Interval domain4 = new Interval(-Math.PI, Math.PI);
            Interval domain5 = new Interval(-Math.PI, Math.PI);
            Interval domain6 = new Interval(-Math.PI, Math.PI);
            if (hasdomain1)
            {
                DA.GetData("Axis 1 Domain", ref domain1);
            }
            if (hasdomain2)
            {
                DA.GetData("Axis 2 Domain", ref domain2);
            }
            if (hasdomain3)
            {
                DA.GetData("Axis 3 Domain", ref domain3);
            }
            if (hasdomain4)
            {
                DA.GetData("Axis 4 Domain", ref domain4);
            }
            if (hasdomain5)
            {
                DA.GetData("Axis 5 Domain", ref domain5);
            }
            if (hasdomain6)
            {
                DA.GetData("Axis 6 Domain", ref domain6);
            }

            GH_RobotSystem robotSystem = null;
            GH_Tool gH_Tool = null;
            string prevJoins = "";
            GH_Plane gH_Plane = null;
            string axistext = "";
            string rotationstep = "";
            Tool tool = null;

            if (!DA.GetData(0, ref robotSystem)) { return; }
            DA.GetData(1, ref gH_Tool);
            if (gH_Tool != null)
            {
                tool = gH_Tool.Value;
            }
            if (!DA.GetData(2, ref prevJoins)) { return; }
            if (!DA.GetData(3, ref gH_Plane)) { return; }
            if (!DA.GetData(4, ref axistext)) { return; }
            if (!DA.GetData(5, ref rotationstep)) { return; }

            List<double[]> prevJoints = new List<double[]>();
            string[] jointsText = prevJoins.Split(',');
            var prevJoint = new double[jointsText.Length];
            for (int i = 0; i < jointsText.Length; i++)
                if (!GH_Convert.ToDouble_Secondary(jointsText[i], ref prevJoint[i])) throw new Exception(" Previous joints not formatted correctly.");
            prevJoints.Add(prevJoint);

            double degree = Convert.ToDouble(rotationstep);
            Plane plane = gH_Plane.Value;
            var planes = GetNewPlanes(axistext, plane, degree);

            List<KinematicSolution> kinematicSolutions = new List<KinematicSolution>();
            foreach (Plane plane1 in planes)
            {
                List<Robim.Target> target1 = new List<Robim.Target>();
                target1.Add(new CartesianTarget(plane1, null, Motions.Linear, tool));
                var kinematics = robotSystem.Value.Kinematics(target1, prevJoints);
                kinematicSolutions.Add(kinematics[0]);
            }


            var jointsresult = kinematicSolutions.Select(x => x.Joints);

            //var errors = kinematicSolutions.SelectMany(x => x.Errors);
            //if (kinematicSolutions[0].Errors != null)
            //{

            //}

            DataTree<string> tree = new DataTree<string>();

            double lessmove = double.MaxValue;
            int index = 0;
            for (int i = 0; i < jointsresult.Count(); i++)
            {
                double[] js = jointsresult.ElementAt(i);
                for (int j = 0; j < 6; j++)
                {
                    GH_Path gH_Path = new GH_Path(i);
                    tree.Add(js[j].ToString(), gH_Path);
                }
                #region Joints Domain
                if (hasdomain1)
                {
                    if (!domain1.IncludesParameter(js[0]))
                    {
                        continue;
                    }
                }
                if (hasdomain2)
                {
                    if (!domain2.IncludesParameter(js[1]))
                    {
                        continue;
                    }
                }
                if (hasdomain3)
                {
                    if (!domain3.IncludesParameter(js[2]))
                    {
                        continue;
                    }
                }
                if (hasdomain4)
                {
                    if (!domain4.IncludesParameter(js[3]))
                    {
                        continue;
                    }
                }
                if (hasdomain5)
                {
                    if (!domain5.IncludesParameter(js[4]))
                    {
                        continue;
                    }
                }
                if (hasdomain6)
                {
                    if (!domain6.IncludesParameter(js[5]))
                    {
                        continue;
                    }
                }
                #endregion
                var a = Math.Abs(js[3] - prevJoint[3]) + Math.Abs(js[4] - prevJoint[4]) + Math.Abs(js[5] - prevJoint[5]);//radian
                if (a < lessmove)
                {
                    lessmove = a;
                    index = i;
                }
            }
            Transform transform = Transform.PlaneToPlane(plane, planes[index]);

            string result = "";

            if (index == 0)
            {
                result = "Is best plane,hasn't to rotate";
            }
            DA.SetData(0, transform);
            DA.SetData(1, result);
            DA.SetDataTree(2, tree);
        }
        List<Plane> GetNewPlanes(string str, Plane plane, double degree)
        {
            Vector3d xaxis = plane.XAxis;
            Vector3d yaxis = plane.YAxis;
            Vector3d zaxis = plane.ZAxis;
            double step = 360 / degree;
            List<double> rotatedegrees = new List<double>();
            for (int i = 1; i < step + 1; i++)
            {
                rotatedegrees.Add(degree * i);
            }
            List<Vector3d> rotatevecs = new List<Vector3d>();
            string[] axises = str.Split(',');
            foreach (string a in axises)
            {
                if (a.Contains("0"))
                {
                    rotatevecs.Add(xaxis);
                }
                else if (a.Contains("1"))
                {
                    rotatevecs.Add(yaxis);
                }
                else if (a.Contains("2"))
                {
                    rotatevecs.Add(zaxis);
                }
            }
            List<Plane> planes = new List<Plane>();
            //第一个 没旋转的plane
            planes.Add(plane);
            foreach (double deg in rotatedegrees)
            {
                Plane plane1 = plane;
                plane1.Rotate(deg, rotatevecs[0]);
                if (rotatevecs.Count == 2)
                    plane1.Rotate(deg, rotatevecs[1]);
                else if (rotatevecs.Count == 3)
                {
                    plane1.Rotate(deg, rotatevecs[1]);
                    plane1.Rotate(deg, rotatevecs[2]);
                }
                planes.Add(plane1);
            }
            return planes;
        }

        #region Varible methods
        IGH_Param[] parameters = new IGH_Param[6]
        {
            new Param_Interval() { Name = "Axis 1 Domain", NickName = "1 Domain", Description = "Axis 1 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 2 Domain", NickName = "2 Domain", Description = "Axis 2 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 3 Domain", NickName = "3 Domain", Description = "Axis 3 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 4 Domain", NickName = "4 Domain", Description = "Axis 4 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 5 Domain", NickName = "5 Domain", Description = "Axis 5 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 6 Domain", NickName = "6 Domain", Description = "Axis 6 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item}
        };
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Axis 1 Domain", AddDomain1, true, Params.Input.Any(x => x.Name == "Axis 1 Domain"));
            Menu_AppendItem(menu, "Axis 2 Domain", AddDomain2, true, Params.Input.Any(x => x.Name == "Axis 2 Domain"));
            Menu_AppendItem(menu, "Axis 3 Domain", AddDomain3, true, Params.Input.Any(x => x.Name == "Axis 3 Domain"));
            Menu_AppendItem(menu, "Axis 4 Domain", AddDomain4, true, Params.Input.Any(x => x.Name == "Axis 4 Domain"));
            Menu_AppendItem(menu, "Axis 5 Domain", AddDomain5, true, Params.Input.Any(x => x.Name == "Axis 5 Domain"));
            Menu_AppendItem(menu, "Axis 6 Domain", AddDomain6, true, Params.Input.Any(x => x.Name == "Axis 6 Domain"));
        }
        private void AddParam(int index)
        {
            IGH_Param parameter = parameters[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        private void AddDomain1(object sender, EventArgs e) => AddParam(0);
        private void AddDomain2(object sender, EventArgs e) => AddParam(1);
        private void AddDomain3(object sender, EventArgs e) => AddParam(2);
        private void AddDomain4(object sender, EventArgs e) => AddParam(3);
        private void AddDomain5(object sender, EventArgs e) => AddParam(4);
        private void AddDomain6(object sender, EventArgs e) => AddParam(5);

        #endregion

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }

    public class StandardPositionSuggest : GH_Component, IGH_VariableParameterComponent
    {
        public StandardPositionSuggest() : base("StandardPositionSuggest", "StandardPositionSuggest", "StandardPositionSuggest", "Robim", "Suggestion") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{726C86F1-112B-4718-A320-A2879431F266}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconKinematics;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
            pManager.AddParameter(new ToolParameter(), "Tool", "T", "Tool system", GH_ParamAccess.item);
            pManager.AddTextParameter("Previous joints", "J", "Optional previous joint values. If the pose is ambigous is will select one based on this previous position.\nRadians", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane", GH_ParamAccess.item);
            pManager.AddTextParameter("Rotate Step", "R", "Rotate step\nDegrees", GH_ParamAccess.item, "45");
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform", "T", "Transform", GH_ParamAccess.item);
            pManager.AddTextParameter("Result", "R", "Result", GH_ParamAccess.item);
            pManager.AddTextParameter("Joints", "joints", "joints", GH_ParamAccess.tree);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasdomain1 = Params.Input.Any(x => x.Name == "Axis 1 Domain");
            bool hasdomain2 = Params.Input.Any(x => x.Name == "Axis 2 Domain");
            bool hasdomain3 = Params.Input.Any(x => x.Name == "Axis 3 Domain");
            bool hasdomain4 = Params.Input.Any(x => x.Name == "Axis 4 Domain");
            bool hasdomain5 = Params.Input.Any(x => x.Name == "Axis 5 Domain");
            bool hasdomain6 = Params.Input.Any(x => x.Name == "Axis 6 Domain");
            Interval domain1 = new Interval(-Math.PI, Math.PI);
            Interval domain2 = new Interval(-Math.PI, Math.PI);
            Interval domain3 = new Interval(-Math.PI, Math.PI);
            Interval domain4 = new Interval(-Math.PI, Math.PI);
            Interval domain5 = new Interval(-Math.PI, Math.PI);
            Interval domain6 = new Interval(-Math.PI, Math.PI);
            if (hasdomain1)
            {
                DA.GetData("Axis 1 Domain", ref domain1);
            }
            if (hasdomain2)
            {
                DA.GetData("Axis 2 Domain", ref domain2);
            }
            if (hasdomain3)
            {
                DA.GetData("Axis 3 Domain", ref domain3);
            }
            if (hasdomain4)
            {
                DA.GetData("Axis 4 Domain", ref domain4);
            }
            if (hasdomain5)
            {
                DA.GetData("Axis 5 Domain", ref domain5);
            }
            if (hasdomain6)
            {
                DA.GetData("Axis 6 Domain", ref domain6);
            }

            GH_RobotSystem robotSystem = null;
            GH_Tool gH_Tool = null;
            string prevJoins = "";
            GH_Plane gH_Plane = null;
            string rotationstep = "";
            Tool tool = null;

            if (!DA.GetData(0, ref robotSystem)) { return; }
            DA.GetData(1, ref gH_Tool);
            if (gH_Tool != null)
            {
                tool = gH_Tool.Value;
            }
            if (!DA.GetData(2, ref prevJoins)) { return; }
            if (!DA.GetData(3, ref gH_Plane)) { return; }
            if (!DA.GetData(4, ref rotationstep)) { return; }

            List<double[]> prevJoints = new List<double[]>();
            string[] jointsText = prevJoins.Split(',');
            var prevJoint = new double[jointsText.Length];
            for (int i = 0; i < jointsText.Length; i++)
                if (!GH_Convert.ToDouble_Secondary(jointsText[i], ref prevJoint[i])) throw new Exception(" Previous joints not formatted correctly.");
            prevJoints.Add(prevJoint);

            Plane prelastplane = Plane.Unset;
            foreach (var prej in prevJoints)
            {
                List<Robim.Target> target0 = new List<Robim.Target>();
                target0.Add(new JointTarget(prej, tool));
                var kinematics0 = robotSystem.Value.Kinematics(target0);
                var preplanes = kinematics0.Select(x => x.Planes);
                prelastplane = preplanes.First().Last();
            }

            double degree = Convert.ToDouble(rotationstep);


            Plane plane = gH_Plane.Value;
            plane.Origin = prelastplane.Origin;
            //Transform transform = Transform.PlaneToPlane(prelastplane, plane);
            //prelastplane.Transform(transform);

            Vector3d vec_planetarget = plane.Normal;
            Vector3d vec_prejoint = prelastplane.Normal;
            Vector3d vec_rotate = Vector3d.CrossProduct(vec_prejoint, vec_planetarget);
            double planeangles = Vector3d.VectorAngle(vec_prejoint, vec_planetarget);

            var planes = GetNewPlanes(vec_rotate, prelastplane, degree, planeangles);


            List<KinematicSolution> kinematicSolutions = new List<KinematicSolution>();
            foreach (Plane plane1 in planes)
            {
                List<Robim.Target> target1 = new List<Robim.Target>();
                target1.Add(new CartesianTarget(plane1, null, Motions.Linear, tool));
                var kinematics = robotSystem.Value.Kinematics(target1, prevJoints);
                kinematicSolutions.Add(kinematics[0]);
            }
            var jointsresult = kinematicSolutions.Select(x => x.Joints);

            //var errors = kinematicSolutions.SelectMany(x => x.Errors);
            //if (kinematicSolutions[0].Errors != null) { }

            List<Robim.Target> target = new List<Robim.Target>();
            target.Add(new CartesianTarget(plane, null, Motions.Linear, tool));
            var Tokinematics = robotSystem.Value.Kinematics(target, prevJoints);
            var ToJoint = Tokinematics.Select(x => x.Joints).ToArray().FirstOrDefault();

            DataTree<string> tree = new DataTree<string>();

            double lessmove = double.MaxValue;
            int index = 0;
            for (int i = 0; i < jointsresult.Count(); i++)
            {
                double[] js = jointsresult.ElementAt(i);
                for (int j = 0; j < 6; j++)
                {
                    GH_Path gH_Path = new GH_Path(i);
                    tree.Add(js[j].ToString(), gH_Path);
                }
                #region Joints Domain
                if (hasdomain1)
                {
                    if (!domain1.IncludesParameter(js[0]))
                    {
                        continue;
                    }
                }
                if (hasdomain2)
                {
                    if (!domain2.IncludesParameter(js[1]))
                    {
                        continue;
                    }
                }
                if (hasdomain3)
                {
                    if (!domain3.IncludesParameter(js[2]))
                    {
                        continue;
                    }
                }
                if (hasdomain4)
                {
                    if (!domain4.IncludesParameter(js[3]))
                    {
                        continue;
                    }
                }
                if (hasdomain5)
                {
                    if (!domain5.IncludesParameter(js[4]))
                    {
                        continue;
                    }
                }
                if (hasdomain6)
                {
                    if (!domain6.IncludesParameter(js[5]))
                    {
                        continue;
                    }
                }
                #endregion
                //var a = Math.Abs(js[3] - ToJoint[3]) + Math.Abs(js[4] - ToJoint[4]) + Math.Abs(js[5] - ToJoint[5]);//radian
                var a = Math.Abs(js[3] - ToJoint[3]) + Math.Abs(js[4] - ToJoint[4]) + Math.Abs(js[5] - ToJoint[5]);//radian
                if (a < lessmove)
                {
                    lessmove = a;
                    index = i;
                }
            }
            Transform transform = Transform.PlaneToPlane(prelastplane, planes[index]);

            string result = "";

            if (index == 0)
            {
                result = "Is best plane,hasn't to rotate";
            }
            DA.SetData(0, transform);
            DA.SetData(1, result);
            DA.SetDataTree(2, tree);
        }
        List<Plane> GetNewPlanes(Vector3d vec_torotate, Plane plane, double degree, double planeangles)
        {
            double step = planeangles / degree;
            List<double> rotatedegrees = new List<double>();
            for (int i = 1; i < step + 1; i++)
            {
                rotatedegrees.Add(degree * i);
            }
            List<Vector3d> rotatevecs = new List<Vector3d>();

            List<Plane> planes = new List<Plane>();
            //第一个 没旋转的plane
            planes.Add(plane);
            foreach (double deg in rotatedegrees)
            {
                Plane plane1 = plane;
                plane1.Rotate(deg, vec_torotate);
                planes.Add(plane1);
            }
            return planes;
        }

        #region Varible methods
        IGH_Param[] parameters = new IGH_Param[6]
        {
            new Param_Interval() { Name = "Axis 1 Domain", NickName = "1 Domain", Description = "Axis 1 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 2 Domain", NickName = "2 Domain", Description = "Axis 2 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 3 Domain", NickName = "3 Domain", Description = "Axis 3 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 4 Domain", NickName = "4 Domain", Description = "Axis 4 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 5 Domain", NickName = "5 Domain", Description = "Axis 5 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item},
            new Param_Interval() { Name = "Axis 6 Domain", NickName = "6 Domain", Description = "Axis 6 Domain\nRadian", Optional = false , Access = GH_ParamAccess.item}
        };
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Axis 1 Domain", AddDomain1, true, Params.Input.Any(x => x.Name == "Axis 1 Domain"));
            Menu_AppendItem(menu, "Axis 2 Domain", AddDomain2, true, Params.Input.Any(x => x.Name == "Axis 2 Domain"));
            Menu_AppendItem(menu, "Axis 3 Domain", AddDomain3, true, Params.Input.Any(x => x.Name == "Axis 3 Domain"));
            Menu_AppendItem(menu, "Axis 4 Domain", AddDomain4, true, Params.Input.Any(x => x.Name == "Axis 4 Domain"));
            Menu_AppendItem(menu, "Axis 5 Domain", AddDomain5, true, Params.Input.Any(x => x.Name == "Axis 5 Domain"));
            Menu_AppendItem(menu, "Axis 6 Domain", AddDomain6, true, Params.Input.Any(x => x.Name == "Axis 6 Domain"));
        }
        private void AddParam(int index)
        {
            IGH_Param parameter = parameters[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        private void AddDomain1(object sender, EventArgs e) => AddParam(0);
        private void AddDomain2(object sender, EventArgs e) => AddParam(1);
        private void AddDomain3(object sender, EventArgs e) => AddParam(2);
        private void AddDomain4(object sender, EventArgs e) => AddParam(3);
        private void AddDomain5(object sender, EventArgs e) => AddParam(4);
        private void AddDomain6(object sender, EventArgs e) => AddParam(5);

        #endregion
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
}