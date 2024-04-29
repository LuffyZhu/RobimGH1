using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
using Rhino.Geometry;
using System;
using System.Linq;
using static Robots.PosTransform;


namespace Robots.Grasshopper
{
    public class PlaneToMatrix : GH_Component
    {
        public PlaneToMatrix() : base("Plane to matrix", "PToM", "Transform plane to matrix", "Robots", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{B9A4D7AC-981D-4D81-A05D-6EC93FF83988}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconPlaneToMatrix;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Matrix", "M", "Matrix", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inputplane = new Plane();

            if (!DA.GetData("Plane", ref inputplane)) return;

            Transform matrix = Plane2Matrix(inputplane);

            DA.SetData(0, matrix);
        }
    }

    public class MatrixToPlane : GH_Component
    {
        public MatrixToPlane() : base("Matrix to plane", "MToP", "Transform matrix to plane", "Robots", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{D8DBCC02-877A-4EC8-9C0B-BB922EF6D622}");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconPlaneToMatrix;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTransformParameter("Matrix", "M", "Matrix", GH_ParamAccess.item);
            
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Transform matrix = new Transform();
            

            if (!DA.GetData("Matrix", ref matrix)) return;

            var plane = Matrix2Plane(matrix);

            DA.SetData(0, plane);
        }
    }

    public class PlaneToQuaternion : GH_Component
    {
        public PlaneToQuaternion() : base("Plane to quaternion", "PToQ", "Transform plane to quaternion", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{D47395A0-A42C-4CDB-BA90-69F90BB45CB8}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconPlaneToQuaternion;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("qx", "qx", "quaternion qx", GH_ParamAccess.item);
            pManager.AddNumberParameter("qy", "qy", "quaternion qy", GH_ParamAccess.item);
            pManager.AddNumberParameter("qz", "qz", "quaternion qz", GH_ParamAccess.item);
            pManager.AddNumberParameter("qw", "qw", "quaternion qw", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inputplane = new Plane();

            if (!DA.GetData("Plane", ref inputplane)) return;
            var origin = new Point3d(inputplane.OriginX, inputplane.OriginY, inputplane.OriginZ);
            var q = Plane2Quaternion(inputplane);
            DA.SetData(0, origin);
            DA.SetData(1, q.B);
            DA.SetData(2, q.C);
            DA.SetData(3, q.D);
            DA.SetData(4, q.A);
        }
    }

    public class QuaternionToPlane : GH_Component
    {
        public QuaternionToPlane() : base("Quaternion to plane", "QToP", "Transform quaternion to plane", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{26517474-27AF-4872-AA19-0498F8885A82}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconQuaternionToPlane;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("qx", "qx", "quaternion qx", GH_ParamAccess.item);
            pManager.AddNumberParameter("qy", "qy", "quaternion qy", GH_ParamAccess.item);
            pManager.AddNumberParameter("qz", "qz", "quaternion qz", GH_ParamAccess.item);
            pManager.AddNumberParameter("qw", "qw", "quaternion qw", GH_ParamAccess.item);
            
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            Point3d origin = new Point3d();
            Double qx = 0.0;
            Double qy = 0.0;
            Double qz = 0.0;
            Double qw = 0.0;
            if (!DA.GetData("Point", ref origin)) return;
            if (!DA.GetData("qx", ref qx)) return;
            if (!DA.GetData("qy", ref qy)) return;
            if (!DA.GetData("qz", ref qz)) return;
            if (!DA.GetData("qw", ref qw)) return;
            var plane = Quaternion2Plane(origin.X, origin.Y, origin.Z, qw, qx, qy, qz);
            DA.SetData(0, plane);
        }
    }

    public class PlaneToEuler : GH_Component, IGH_VariableParameterComponent
    {
        public PlaneToEuler() : base("Plane to euler", "PToE", "Transform plane to euler", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{1E7ABFC6-C2B8-4CED-BAA3-5EAA954A8FD9}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconPlaneToEuler;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("ax", "ax", "euler ax", GH_ParamAccess.item);
            pManager.AddNumberParameter("ay", "ay", "euler ay", GH_ParamAccess.item);
            pManager.AddNumberParameter("az", "az", "euler az", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasRobotModel = Params.Input.Any(x => x.Name == "RobotModel");
            bool hasAxes = Params.Input.Any(x => x.Name == "Axes");

            var inputplane = new Plane();
            string robotModel = null;
            string axis = "rzyx";

            if (hasRobotModel)
            {
                string rModel = null;
                DA.GetData("RobotModel", ref rModel);
                robotModel = (rModel == null) ? rModel = "KUKA" : rModel;
            }

            


            if (!DA.GetData("Plane", ref inputplane)) return;
            var origin = new Point3d(inputplane.OriginX, inputplane.OriginY, inputplane.OriginZ);

            if (robotModel == "KUKA")
            {
                axis = "rzyx";
            }
            if (robotModel == "FANUC")
            {
                axis = "sxyz";
            }
            if (robotModel == "Aubo")
            {
                axis = "rzyx";
            }

            if (hasAxes)
            {
                string rAxis = null;
                DA.GetData("Axes", ref rAxis);
                axis = (rAxis == null) ? null : rAxis;
            }

            double[] euler_angle = Plane2Euler(inputplane, axis);
            DA.SetData(0, origin);
            DA.SetData(1, euler_angle[0]);
            DA.SetData(2, euler_angle[1]);
            DA.SetData(3, euler_angle[2]);

        }

        IGH_Param[] parameters = new IGH_Param[2]
        {
            new Param_String() { Name = "RobotModel", NickName = "RobotModel", Description = "Choose robot model", Optional = true },
            new Param_String() { Name = "Axes", NickName = "Axes", Description = "First Character are S (static) or R (rotating frame), " +
                "remaining characters are successive rotation axis. Example: rzyx stands for Rotating frame, first rotate across z axis, then y, and then x.", Optional = true },
        };




        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "RobotModel", ChooseModel, true, Params.Input.Any(x => x.Name == "RobotModel"));
            Menu_AppendItem(menu, "Axes", AddAxis, true, Params.Input.Any(x => x.Name == "Axes"));
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
        private void ChooseModel(object sender, EventArgs e)
        {
            AddParam(0);
            var parameter = parameters[0];


            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_ValueList();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 130, parameter.Attributes.InputGrip.Y - 11);
                valueList.ListItems.Clear();
                valueList.ListItems.Add(new GH_ValueListItem("KUKA", "\"KUKA\""));
                valueList.ListItems.Add(new GH_ValueListItem("FANUC", "\"FANUC\""));
                valueList.ListItems.Add(new GH_ValueListItem("Aubo", "\"Aubo\""));
                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();

                ExpireSolution(true);
            }
        }
        private void AddAxis(object sender, EventArgs e)
        {
            AddParam(1);
            var parameter = parameters[1];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_Panel();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 200, parameter.Attributes.InputGrip.Y - 11);

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
        }
        

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

    }

    public class EulerToPlane : GH_Component, IGH_VariableParameterComponent
    {
        public EulerToPlane() : base("Euler to plane", "EToP", "Transform euler to plane", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{D0579F8A-F26C-46FD-8996-B028EFDEFD50}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconEulerToPlane;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("ax", "ax", "euler ax", GH_ParamAccess.item);
            pManager.AddNumberParameter("ay", "ay", "euler ay", GH_ParamAccess.item);
            pManager.AddNumberParameter("az", "az", "euler az", GH_ParamAccess.item);
            
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasRobotModel = Params.Input.Any(x => x.Name == "RobotModel");
            bool hasAxes = Params.Input.Any(x => x.Name == "Axes");

            string robotModel = null;
            string axis = "rzyx";

            if (hasRobotModel)
            {
                string rModel = null;
                DA.GetData("RobotModel", ref rModel);
                robotModel = (rModel == null) ? rModel = "KUKA" : rModel;
            }

            if (robotModel == "KUKA")
            {
                axis = "rzyx";
            }
            if (robotModel == "FANUC")
            {
                axis = "sxyz";
            }
            if (robotModel == "Aubo")
            {
                axis = "rzyx";
            }

            if (hasAxes)
            {
                string rAxis = null;
                DA.GetData("Axes", ref rAxis);
                axis = (rAxis == null) ? null : rAxis;
            }
            Point3d origin = new Point3d();
            double ax = 0.0;
            double ay = 0.0;
            double az = 0.0;


            if (!DA.GetData("Point", ref origin)) return;
            if (!DA.GetData("ax", ref ax)) return;
            if (!DA.GetData("ay", ref ay)) return;
            if (!DA.GetData("az", ref az)) return;

            Plane plane = Euler2Plane(origin.X, origin.Y, origin.Z, ax, ay, az, axis);
            DA.SetData(0, plane);
        }

        IGH_Param[] parameters = new IGH_Param[2]
        {
            new Param_String() { Name = "RobotModel", NickName = "RobotModel", Description = "Choose robot model", Optional = true },
            new Param_String() { Name = "Axes", NickName = "Axes", Description = "First Character are S (static) or R (rotating frame), " +
                "remaining characters are successive rotation axis. Example: rzyx stands for Rotating frame, first rotate across z axis, then y, and then x.", Optional = true },
        };



        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "RobotModel", ChooseModel, true, Params.Input.Any(x => x.Name == "RobotModel"));
            Menu_AppendItem(menu, "Axes", AddAxis, true, Params.Input.Any(x => x.Name == "Axes"));
        }
        private void ChooseModel(object sender, EventArgs e)
        {
            AddParam(0);
            var parameter = parameters[0];


            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_ValueList();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 130, parameter.Attributes.InputGrip.Y - 11);
                valueList.ListItems.Clear();
                valueList.ListItems.Add(new GH_ValueListItem("KUKA", "\"KUKA\""));
                valueList.ListItems.Add(new GH_ValueListItem("FANUC", "\"FANUC\""));
                valueList.ListItems.Add(new GH_ValueListItem("Aubo", "\"Aubo\""));
                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
        }

        private void AddAxis(object sender, EventArgs e)
        {
            AddParam(1);
            var parameter = parameters[1];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_Panel();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 200, parameter.Attributes.InputGrip.Y - 11);

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
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

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }

    public class EulerToQuaternion : GH_Component,IGH_VariableParameterComponent
    {
        public EulerToQuaternion() : base("Euler to quaternion", "EToQ", "Transform euler to quaternion", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("{D334AAB6-4294-49C1-B5BC-B9D37F020E60}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconEulerToQuaternion;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("ax", "ax", "euler ax", GH_ParamAccess.item);
            pManager.AddNumberParameter("ay", "ay", "euler ay", GH_ParamAccess.item);
            pManager.AddNumberParameter("az", "az", "euler az", GH_ParamAccess.item);

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("qx", "qx", "quaternion qx", GH_ParamAccess.item);
            pManager.AddNumberParameter("qy", "qy", "quaternion qy", GH_ParamAccess.item);
            pManager.AddNumberParameter("qz", "qz", "quaternion qz", GH_ParamAccess.item);
            pManager.AddNumberParameter("qw", "qw", "quaternion qw", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasRobotModel = Params.Input.Any(x => x.Name == "RobotModel");
            bool hasAxes = Params.Input.Any(x => x.Name == "Axes");

            string robotModel = null;
            string axis = "rzyx";

            if (hasRobotModel)
            {
                string rModel = null;
                DA.GetData("RobotModel", ref rModel);
                robotModel = (rModel == null) ? rModel = "KUKA" : rModel;
            }

            if (robotModel == "KUKA")
            {
                axis = "rzyx";
            }
            if (robotModel == "FANUC")
            {
                axis = "sxyz";
            }
            if (robotModel == "Aubo")
            {
                axis = "rzyx";
            }

            if (hasAxes)
            {
                string rAxis = null;
                DA.GetData("Axes", ref rAxis);
                axis = (rAxis == null) ? null : rAxis;
            }
            Point3d origin = new Point3d();
            double ax = 0.0;
            double ay = 0.0;
            double az = 0.0;


            if (!DA.GetData("Point", ref origin)) return;
            if (!DA.GetData("ax", ref ax)) return;
            if (!DA.GetData("ay", ref ay)) return;
            if (!DA.GetData("az", ref az)) return;

            Quaternion q = Euler2Quaternion(origin.X, origin.Y, origin.Z, ax, ay, az, axis);
            DA.SetData(0, origin);
            DA.SetData(1, q.B);
            DA.SetData(2, q.C);
            DA.SetData(3, q.D);
            DA.SetData(4, q.A);
        }

        IGH_Param[] parameters = new IGH_Param[2]
        {
            new Param_String() { Name = "RobotModel", NickName = "RobotModel", Description = "Choose robot model", Optional = true },
            new Param_String() { Name = "Axes", NickName = "Axes", Description = "First Character are S (static) or R (rotating frame), " +
                "remaining characters are successive rotation axis. Example: rzyx stands for Rotating frame, first rotate across z axis, then y, and then x.", Optional = true },
        };



        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "RobotModel", ChooseModel, true, Params.Input.Any(x => x.Name == "RobotModel"));
            Menu_AppendItem(menu, "Axes", AddAxis, true, Params.Input.Any(x => x.Name == "Axes"));
        }
        private void ChooseModel(object sender, EventArgs e)
        {
            AddParam(0);
            var parameter = parameters[0];


            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_ValueList();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 130, parameter.Attributes.InputGrip.Y - 11);
                valueList.ListItems.Clear();
                valueList.ListItems.Add(new GH_ValueListItem("KUKA", "\"KUKA\""));
                valueList.ListItems.Add(new GH_ValueListItem("FANUC", "\"FANUC\""));
                valueList.ListItems.Add(new GH_ValueListItem("Aubo", "\"Aubo\""));
                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
        }

        private void AddAxis(object sender, EventArgs e)
        {
            AddParam(1);
            var parameter = parameters[1];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_Panel();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 200, parameter.Attributes.InputGrip.Y - 11);

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
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

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }

    public class QuaternionToEuler : GH_Component, IGH_VariableParameterComponent
    {
        public QuaternionToEuler() : base("Quaternion to euler", "QToE", "Transform quaternion to euler", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{0E167C45-A57C-42A8-9F33-1B976B3363FB}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconQuaternionToEuler;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("qx", "qx", "quaternion qx", GH_ParamAccess.item);
            pManager.AddNumberParameter("qy", "qy", "quaternion qy", GH_ParamAccess.item);
            pManager.AddNumberParameter("qz", "qz", "quaternion qz", GH_ParamAccess.item);
            pManager.AddNumberParameter("qw", "qw", "quaternion qw", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("ax", "ax", "euler ax", GH_ParamAccess.item);
            pManager.AddNumberParameter("ay", "ay", "euler ay", GH_ParamAccess.item);
            pManager.AddNumberParameter("az", "az", "euler az", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasRobotModel = Params.Input.Any(x => x.Name == "RobotModel");
            bool hasAxes = Params.Input.Any(x => x.Name == "Axes");

            string robotModel = null;
            string axis = "rzyx";

            if (hasRobotModel)
            {
                string rModel = null;
                DA.GetData("RobotModel", ref rModel);
                robotModel = (rModel == null) ? rModel = "KUKA" : rModel;
            }

            if (robotModel == "KUKA")
            {
                axis = "rzyx";
            }
            if (robotModel == "FANUC")
            {
                axis = "sxyz";
            }
            if (robotModel == "Aubo")
            {
                axis = "rzyx";
            }

            if (hasAxes)
            {
                string rAxis = null;
                DA.GetData("Axes", ref rAxis);
                axis = (rAxis == null) ? null : rAxis;
            }
            Point3d origin = new Point3d();
            Double qx = 0.0;
            Double qy = 0.0;
            Double qz = 0.0;
            Double qw = 0.0;
            if (!DA.GetData("Point", ref origin)) return;
            if (!DA.GetData("qx", ref qx)) return;
            if (!DA.GetData("qy", ref qy)) return;
            if (!DA.GetData("qz", ref qz)) return;
            if (!DA.GetData("qw", ref qw)) return;

            double[] euler_angle = Quaternion2Euler(origin.X, origin.Y, origin.Z, qw, qx, qy, qz, axis);

            //调用函数
            DA.SetData(0, origin);
            DA.SetData(1, euler_angle[0]);
            DA.SetData(2, euler_angle[1]);
            DA.SetData(3, euler_angle[2]);
        }

        IGH_Param[] parameters = new IGH_Param[2]
        {
            new Param_String() { Name = "RobotModel", NickName = "RobotModel", Description = "Choose robot model", Optional = true },
            new Param_String() { Name = "Axes", NickName = "Axes", Description = "First Character are S (static) or R (rotating frame), " +
                "remaining characters are successive rotation axis. Example: rzyx stands for Rotating frame, first rotate across z axis, then y, and then x.", Optional = true },
        };



        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "RobotModel", ChooseModel, true, Params.Input.Any(x => x.Name == "RobotModel"));
            Menu_AppendItem(menu, "Axes", AddAxis, true, Params.Input.Any(x => x.Name == "Axes"));
        }
        private void ChooseModel(object sender, EventArgs e)
        {
            AddParam(0);
            var parameter = parameters[0];


            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_ValueList();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 130, parameter.Attributes.InputGrip.Y - 11);
                valueList.ListItems.Clear();
                valueList.ListItems.Add(new GH_ValueListItem("KUKA", "\"KUKA\""));
                valueList.ListItems.Add(new GH_ValueListItem("FANUC", "\"FANUC\""));
                valueList.ListItems.Add(new GH_ValueListItem("Aubo", "\"Aubo\""));
                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
        }

        private void AddAxis(object sender, EventArgs e)
        {
            AddParam(1);
            var parameter = parameters[1];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_Panel();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(parameter.Attributes.InputGrip.X - 200, parameter.Attributes.InputGrip.Y - 11);

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
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

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

    }

    public class PlaneToAxisAngle : GH_Component
    {
        public PlaneToAxisAngle() : base("Plane to axis angle", "PToA", "Transform plane to axis angle", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{6BEA54C6-F3AF-45F0-9EF4-47036A186494}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconPlaneToAxisAngle;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("vx", "vx", "axis angle vector x", GH_ParamAccess.item);
            pManager.AddNumberParameter("vy", "vy", "axis angle vector y", GH_ParamAccess.item);
            pManager.AddNumberParameter("vz", "vz", "axis angle vector z", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inputplane = new Plane();

            if (!DA.GetData("Plane", ref inputplane)) return;
            var origin = new Point3d(inputplane.OriginX, inputplane.OriginY, inputplane.OriginZ);
            double[] axisangle = Plane2AxisAngle(inputplane);
            DA.SetData(0, origin);
            DA.SetData(1,axisangle[0]);
            DA.SetData(2, axisangle[1]);
            DA.SetData(3, axisangle[2]);
        }
    }

    public class AxisAngleToPlane : GH_Component
    {
        public AxisAngleToPlane() : base("Axis angle to plane", "AToP", "Transform axis angle to plane", "Robots", "Util") { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("{1300B0B7-0687-4B68-A538-4CFF286E9945}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconAxisAngleToPlane;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "p", "point", GH_ParamAccess.item);
            pManager.AddNumberParameter("vx", "vx", "axis angle vector x", GH_ParamAccess.item);
            pManager.AddNumberParameter("vy", "vy", "axis angle vector y", GH_ParamAccess.item);
            pManager.AddNumberParameter("vz", "vz", "axis angle vector z", GH_ParamAccess.item);
            
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "plane from rhino", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Point3d origin = new Point3d();
            Double vx = 0.0;
            Double vy = 0.0;
            Double vz = 0.0;
            if (!DA.GetData("Point", ref origin)) return;
            if (!DA.GetData("vx", ref vx)) return;
            if (!DA.GetData("vy", ref vy)) return;
            if (!DA.GetData("vz", ref vz)) return;

            Plane plane = AxisAngle2Plane(origin.X, origin.Y, origin.Z, vx, vy, vz);
            DA.SetData(0, plane);
        }
    }

}