using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Robim.Grasshopper
{
    public class RPlanningRequest : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RPlanningRequest()
          : base("RPlanningRequest", "Nickname",
            "Description",
            "Robim", "Planning")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program to simulate", GH_ParamAccess.item);
            //pManager[0].Optional = true;

            //pManager.AddParameter(new ToolpathParameter(), "Targets", "T", "List of targets or toolpaths for the robot.", GH_ParamAccess.list);
            pManager.AddParameter(new TargetParameter(), "Target", "T", "List of targets or toolpaths for the robot.", GH_ParamAccess.list);
            pManager[0].Optional = true;

            pManager.AddParameter(new RobotSystemParameter(), "RSystem", "RS", "Robot system", GH_ParamAccess.item);
            pManager[1].Optional = true;

            pManager.AddPlaneParameter("wpcPlane", "WP", "workpiece insertion plane", GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddMeshParameter("wpcMesh", "WPM", "workpiece collision mesh", GH_ParamAccess.item);
            pManager[3].Optional = true;

            pManager.AddNumberParameter("JConst", "JC", "Joint constraints as list", GH_ParamAccess.list);
            pManager[4].Optional = true;

            pManager.AddNumberParameter("JLowerB", "JL", "Joint lower bound", GH_ParamAccess.item);
            pManager[5].Optional = true;

            pManager.AddNumberParameter("JUpperB", "JU", "Joint upper bound", GH_ParamAccess.item);
            pManager[6].Optional = true;

            pManager.AddNumberParameter("JStep", "JS", "Joint step", GH_ParamAccess.item);
            pManager[7].Optional = true;

            pManager.AddTextParameter("CAxis", "CA", "Cartesian axis", GH_ParamAccess.item);
            pManager[8].Optional = true;

            pManager.AddNumberParameter("CLowerB", "CL", "Cartesian lower bound", GH_ParamAccess.item);
            pManager[9].Optional = true;

            pManager.AddNumberParameter("CUpperB", "CU", "Cartesian upper bound", GH_ParamAccess.item);
            pManager[10].Optional = true;

            pManager.AddNumberParameter("CStep", "CS", "Cartesian step", GH_ParamAccess.item);
            pManager[11].Optional = true;

            pManager.AddTextParameter("CType", "CT", "Cartesian constrain type: rotation, translation etc", GH_ParamAccess.item);
            pManager[12].Optional = true;

            pManager.AddNumberParameter("InterpSt", "IS", "Interpolation step", GH_ParamAccess.item);
            pManager[13].Optional = true;

            //pManager.AddTextParameter("folder", "SF", "folder where to save JSON file to", GH_ParamAccess.item);
            //pManager[4].Optional = true;

            //pManager.AddBooleanParameter("save", "S", "save JSON request file to folder", GH_ParamAccess.item);
            //pManager[5].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("JSON", "JSON", "JSON", GH_ParamAccess.item);
            //pManager.AddPlaneParameter("plane", "plane", "plane", GH_ParamAccess.item);
            //pManager.AddTextParameter("Qtest", "Qtest", "Qtest", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //GH_Program program = null;
            //DA.GetData(0, ref program);

            Plane workPlane = new Plane(0, 0, 0, 0);
            DA.GetData(2, ref workPlane);

            Mesh workMesh = null;
            DA.GetData(3, ref workMesh);

            var inTargets = new List<GH_Target>();
            DA.GetDataList(0, inTargets); //-----------------

            var targets = inTargets.Select(t => t.Value).ToList();

            var cts = targets.Select(t => t as CartesianTarget).ToList();
            var jts = targets.Select(t => t as JointTarget).ToList();


            GH_RobotSystem rSystem = new GH_RobotSystem();
            DA.GetData(1, ref rSystem);

            List<string> qTest = new List<string>();
            bool saved = false;

            List<double> jconstraints = new List<double>() { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            List<double> jointConstr = new List<double>();
            //double[] jointConstr = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            DA.GetDataList(4, jointConstr);

            if (!jointConstr.Any())
            {
                jointConstr = jconstraints;
            }

            double jLowerBound = 0.0;
            DA.GetData(5, ref jLowerBound);

            double jUpperBound = 0.0;
            DA.GetData(6, ref jUpperBound);

            double jStep = 0.0;
            DA.GetData(7, ref jStep);

            string cAxis = "y";
            DA.GetData(8, ref cAxis);

            double cLowerBound = -180.0;
            DA.GetData(9, ref cLowerBound);

            double cUpperBound = 180.0;
            DA.GetData(10, ref cUpperBound);

            double cStep = 1.0;
            DA.GetData(11, ref cStep);

            string cType = "rotation";
            DA.GetData(8, ref cType);

            double iStep = 50.0;
            DA.GetData(13, ref iStep);

            //var targets = program.Value.Targets;

            RRequest request = new RRequest();

            #region start
            request.Name = "name";
            request.Path = new JRPath();
            request.Path.StartState = new StartState();

            var stRadians = TargetToJoints(targets[0], rSystem);
            var stDegrees = stRadians.Select((x, i) => (rSystem.Value).RadianToDegree(x, i, 0));

            request.Path.StartState.Robot = stDegrees.ToArray();

            //request.Path.StartState.External = targets[0].ProgramTargets[0].Target.External;
            request.Path.StartState.External = targets[0].External;
            #endregion

            #region path
            int counter = 0;
            List<JRSegment> allSegments = new List<JRSegment>();

            for (int i = 0; i < targets.Count; i++)
            {
                JRSegment jr = new JRSegment();
                if (counter == 0)
                {
                    jr.Index = counter;
                    jr.OriginalIndex = counter;
                    jr.Velocity = targets[0].Speed.TranslationSpeed;
                    jr.Mode = "ptp";
                    jr.Target = new JRTarget();
                    jr.Target.PtpTarget = new PtpTarget();
                    jr.Target.PtpTarget.JointTarget = new StartState();

                    var fRadians = TargetToJoints(targets[0], rSystem); //= tar.Joints;
                    var fDegrees = fRadians.Select((x, i) => (rSystem.Value).RadianToDegree(x, i, 0));
                    jr.Target.PtpTarget.JointTarget.Robot = fDegrees.ToArray();

                    jr.Target.PtpTarget.JointTarget.External = targets[0].External;
                    //jr.Target.PtpTarget.JointTarget.External = new double[] { 0.0 };

                }
                else
                {

                    //if (tar.ProgramTargets[0].IsJointMotion == true)
                    if (cts[i] == null)
                    {
                        jr.Index = counter;
                        jr.OriginalIndex = counter;
                        //jr.Velocity = tar.ProgramTargets[0].Target.Speed.TranslationSpeed;
                        jr.Velocity = jts[i].Speed.TranslationSpeed;
                        jr.Mode = "ptp";
                        jr.Target = new JRTarget();
                        jr.Target.PtpTarget = new PtpTarget();
                        jr.Target.PtpTarget.JointTarget = new StartState();

                        //var fRadians = tar.Joints;
                        var fRadians = jts[i].Joints;
                        var fDegrees = fRadians.Select((x, i) => (rSystem.Value).RadianToDegree(x, i, 0));
                        jr.Target.PtpTarget.JointTarget.Robot = fDegrees.ToArray();

                        jr.Target.PtpTarget.JointTarget.External = jts[i].External; //tar.ProgramTargets[0].Target.External;
                        //jr.Target.PtpTarget.JointTarget.External = new double[] { 0.0 };
                    }
                    else
                    {
                        if (cts[i].Motion == Motions.Joint)
                        {
                            jr.Index = counter;
                            jr.OriginalIndex = counter;
                            //jr.Velocity = tar.ProgramTargets[0].Target.Speed.TranslationSpeed;
                            jr.Velocity = cts[i].Speed.TranslationSpeed;
                            jr.Mode = "ptp";
                            jr.Target = new JRTarget();
                            jr.Target.PtpTarget = new PtpTarget();
                            jr.Target.PtpTarget.JointTarget = new StartState();

                            //var fRadians = tar.Joints;
                            var fRadians = TargetToJoints(cts[i], rSystem);
                            var fDegrees = fRadians.Select((x, i) => (rSystem.Value).RadianToDegree(x, i, 0));
                            jr.Target.PtpTarget.JointTarget.Robot = fDegrees.ToArray();

                            jr.Target.PtpTarget.JointTarget.External = new double[] { 0.0 }; //jts[i].External;
                        }
                        if (cts[i].Motion == Motions.Linear)
                        {
                            jr.Index = counter;
                            jr.OriginalIndex = counter;
                            jr.Velocity = cts[i].Speed.TranslationSpeed;
                            jr.Mode = "line";
                            jr.Target = new JRTarget();
                            jr.Target.Pose = new JRPose();
                            jr.Target.Pose.Position = new JRPosition();
                            //var tPlane = tar.ProgramTargets[0].Target.Frame.Plane;                                       
                            var tPlane = cts[i].Plane;
                            jr.Target.Pose.Position.X = tPlane.OriginX;
                            jr.Target.Pose.Position.Y = tPlane.OriginY;
                            jr.Target.Pose.Position.Z = tPlane.OriginZ;
                            jr.Target.Pose.Orientation = new JROrientation();
                            jr.Target.Pose.Orientation.Quaternion = new JRPositionQ();
                            var q = PosTransform.Plane2Quaternion(tPlane);
                            jr.Target.Pose.Orientation.Quaternion.X = Convert.ToDouble(q.B.ToString("F8"));
                            jr.Target.Pose.Orientation.Quaternion.Y = Convert.ToDouble(q.C.ToString("F8"));
                            jr.Target.Pose.Orientation.Quaternion.Z = Convert.ToDouble(q.D.ToString("F8"));
                            jr.Target.Pose.Orientation.Quaternion.W = Convert.ToDouble(q.A.ToString("F8"));
                        }

                        //DA.SetData(1, tPlane);
                    }
                }
                counter++;
                //p.Vertex = new double[] { point.X, point.Y, point.Z };

                allSegments.Add(jr);
            }

            request.Path.Segments = new JRSegment[allSegments.Count];
            request.Path.Segments = allSegments.ToArray();
            #endregion

            #region constraints
            request.InterpolationStep = iStep;

            var JC = new JointConstraints();
            JC.Coefficient = new StartState();
            //JC.Coefficient.Robot = new double[] { 0, 0, 0, 0, 0, 0 };
            JC.Coefficient.Robot = jointConstr.ToArray();
            JC.Coefficient.External = new double[0];
            JC.Bound = new Bound();
            JC.Bound.LowerBound = jLowerBound;
            JC.Bound.UpperBound = jUpperBound;
            JC.Bound.Step = jStep;

            var CC = new CartConstraints();
            var cBound = new CBound();
            cBound.Axis = cAxis;
            var c1B = new BoundChild();
            c1B.LowerBound = cLowerBound;
            c1B.UpperBound = cUpperBound;
            c1B.Step = 1.0;
            cBound.Bound = c1B;
            CC.Bounds = new CBound[] { cBound };
            CC.Type = "rotation";
            var pt = targets[0];
            var toolName = pt.Tool.Name;
            //CC.Frame = toolName;
            CC.Frame = "tool";

            var constraint1 = new JRConstraint();
            constraint1.JointConstraints = JC;
            var constraint2 = new JRConstraint();
            constraint2.CartConstraints = CC;

            List<JRConstraint> constraints = new List<JRConstraint>();
            constraints.Add(constraint1);
            constraints.Add(constraint2);
            //request.Constraints = new JRConstraint[] { constraint };
            request.Constraints = constraints;

            #endregion

            #region tool
            request.Tool = new JRTool();
            request.Tool.Type = "tool";
            //request.Tool.Name = toolName;
            request.Tool.Name = "tool";
            request.Tool.Pose = new JRPose();

            request.Tool.Pose.Position = new JRPosition();
            var toolPlane = pt.Tool.Tcp;
            //var updatedToolPlane = new Plane(toolPlane.Origin, -toolPlane.XAxis, -toolPlane.YAxis);
            request.Tool.Pose.Position.X = toolPlane.OriginX;
            request.Tool.Pose.Position.Y = toolPlane.OriginY;
            request.Tool.Pose.Position.Z = toolPlane.OriginZ;

            request.Tool.Pose.Orientation = new JROrientation();
            request.Tool.Pose.Orientation.Quaternion = new JRPositionQ();
            var qtool = PosTransform.Plane2Quaternion(toolPlane);
            request.Tool.Pose.Orientation.Quaternion.X = Convert.ToDouble(qtool.B.ToString("F8"));
            request.Tool.Pose.Orientation.Quaternion.Y = Convert.ToDouble(qtool.C.ToString("F8"));
            request.Tool.Pose.Orientation.Quaternion.Z = Convert.ToDouble(qtool.D.ToString("F8"));
            request.Tool.Pose.Orientation.Quaternion.W = Convert.ToDouble(qtool.A.ToString("F8"));

            var toolMeshes = pt.Tool.Mesh_;
            var toolMesh = toolMeshes[0];
            request.Tool.CollisionMesh = new CollisionMesh();
            List<Triangle> trs = new List<Triangle>();
            List<Vertice> vert = new List<Vertice>();

            fromMesh(toolMesh, out trs, out vert);

            request.Tool.CollisionMesh.Triangles = new Triangle[trs.Count];
            request.Tool.CollisionMesh.Triangles = trs.ToArray();

            request.Tool.CollisionMesh.Vertices = new Vertice[vert.Count];
            request.Tool.CollisionMesh.Vertices = vert.ToArray();
            #endregion

            #region workpiece
            request.Workpiece = new JRTool();
            request.Workpiece.Type = "workpiece";
            request.Workpiece.Name = "workpiece";

            request.Workpiece.Pose = new JRPose();

            request.Workpiece.Pose.Position = new JRPosition();
            request.Workpiece.Pose.Position.X = toolPlane.OriginX;
            request.Workpiece.Pose.Position.Y = toolPlane.OriginY;
            request.Workpiece.Pose.Position.Z = toolPlane.OriginZ;

            request.Workpiece.Pose.Orientation = new JROrientation();
            request.Workpiece.Pose.Orientation.Quaternion = new JRPositionQ();
            var qwork = PosTransform.Plane2Quaternion(workPlane);
            request.Workpiece.Pose.Orientation.Quaternion.X = Convert.ToDouble(qwork.B.ToString("F8"));
            request.Workpiece.Pose.Orientation.Quaternion.Y = Convert.ToDouble(qwork.C.ToString("F8"));
            request.Workpiece.Pose.Orientation.Quaternion.Z = Convert.ToDouble(qwork.D.ToString("F8"));
            request.Workpiece.Pose.Orientation.Quaternion.W = Convert.ToDouble(qwork.A.ToString("F8"));

            request.Workpiece.CollisionMesh = new CollisionMesh();
            List<Triangle> trsW = new List<Triangle>();
            List<Vertice> vertW = new List<Vertice>();

            fromMesh(workMesh, out trsW, out vertW);

            request.Workpiece.CollisionMesh.Triangles = new Triangle[trsW.Count];
            request.Workpiece.CollisionMesh.Triangles = trsW.ToArray();

            request.Workpiece.CollisionMesh.Vertices = new Vertice[vertW.Count];
            request.Workpiece.CollisionMesh.Vertices = vertW.ToArray();
            #endregion

            //JsonSerializerSettings settings = new JsonSerializerSettings();
            //settings.FloatParseHandlng(double);
            string json = JsonConvert.SerializeObject(request, Formatting.Indented);
            /*
            if (save == true)
            {
                File.WriteAllText(@"C:\Users\maria\Desktop\file.json", json);
            }*/

            DA.SetData(0, json);
        }

        IGH_Param[] robotSystem = new IGH_Param[1]
        {
            new RobotSystemParameter() { Name = "RSystem", NickName = "RS", Description = "Reference robot system", Optional = false }
        };
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconCreateTarget;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0651ae41-a0a5-47a3-a41c-c4ef8c78dd45");

        public double[] TargetToJoints(Target t, GH_RobotSystem rSystem)
        {
            List<double[]> prevJoints = null;
            List<Target> ts = new List<Target>();
            ts.Add(t);
            var kinematics = rSystem.Value.Kinematics(ts, prevJoints);
            return kinematics[0].Joints;
        }

        public void fromMesh(Mesh inputMesh, out List<Triangle> triangles, out List<Vertice> verts)
        {
            var triMesh = MeshTriangulate(inputMesh);
            var faces = triMesh.Faces;
            var vertices = triMesh.Vertices;

            List<Triangle> trs = new List<Triangle>();

            foreach (MeshFace mf in faces)
            {
                Triangle tr = new Triangle();
                tr.VertexIndices = new int[] { mf.A, mf.B, mf.C };
                trs.Add(tr);
            }

            triangles = trs;


            List<Vertice> vrtcs = new List<Vertice>();

            foreach (Point3f v in vertices)
            {
                Vertice j = new Vertice();
                j.X = v.X;
                j.Y = v.Y;
                j.Z = v.Z;
                vrtcs.Add(j);
            }

            verts = vrtcs;
        }

        public Mesh MeshTriangulate(Mesh x)
        {
            int facecount = x.Faces.Count;
            for (int i = 0; i < facecount; i++)
            {
                var mf = x.Faces[i];
                if (mf.IsQuad)
                {
                    double dist1 = x.Vertices[mf.A].DistanceTo(x.Vertices[mf.C]);
                    double dist2 = x.Vertices[mf.B].DistanceTo(x.Vertices[mf.D]);
                    if (dist1 > dist2)
                    {
                        x.Faces.AddFace(mf.A, mf.B, mf.D);
                        x.Faces.AddFace(mf.B, mf.C, mf.D);
                    }
                    else
                    {
                        x.Faces.AddFace(mf.A, mf.B, mf.C);
                        x.Faces.AddFace(mf.A, mf.C, mf.D);
                    }
                }
            }

            var newfaces = new List<MeshFace>();
            foreach (var mf in x.Faces)
            {
                if (mf.IsTriangle) newfaces.Add(mf);
            }

            x.Faces.Clear();
            x.Faces.AddFaces(newfaces);
            return x;
        }
    }

    #region serializer_classes


    public partial class RRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public JRPath Path { get; set; }

        [JsonProperty("interpolation_step")]
        public double InterpolationStep { get; set; }

        [JsonProperty("constraints")]
        public List<JRConstraint> Constraints { get; set; }

        [JsonProperty("tool")]
        public JRTool Tool { get; set; }

        [JsonProperty("workpiece")]
        public JRTool Workpiece { get; set; }
    }

    public partial class JRConstraint
    {
        [JsonProperty("joint_constraints", NullValueHandling = NullValueHandling.Ignore)]
        public JointConstraints JointConstraints { get; set; }

        [JsonProperty("cart_constraints", NullValueHandling = NullValueHandling.Ignore)]
        public CartConstraints CartConstraints { get; set; }
    }

    public partial class CartConstraints
    {
        [JsonProperty("bounds")]
        public CBound[] Bounds { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("frame")]
        public string Frame { get; set; }
    }

    public partial class CBound
    {
        [JsonProperty("axis")]
        public string Axis { get; set; }

        [JsonProperty("bound")]
        public BoundChild Bound { get; set; }
    }

    public partial class BoundChild
    {
        [JsonProperty("lower_bound")]
        public double LowerBound { get; set; }

        [JsonProperty("upper_bound")]
        public double UpperBound { get; set; }

        [JsonProperty("step")]
        public double Step { get; set; }
    }

    public partial class JointConstraints
    {
        [JsonProperty("coefficient")]
        public StartState Coefficient { get; set; }

        [JsonProperty("bound")]
        public Bound Bound { get; set; }
    }

    public partial class Bound
    {
        [JsonProperty("lower_bound")]
        public double LowerBound { get; set; }

        [JsonProperty("upper_bound")]
        public double UpperBound { get; set; }

        [JsonProperty("step")]
        public double Step { get; set; }
    }

    public partial class StartState
    {
        [JsonProperty("robot")]
        public double[] Robot { get; set; }

        [JsonProperty("external")]
        public double[] External { get; set; }
    }

    public partial class JRPath
    {
        [JsonProperty("start_state")]
        public StartState StartState { get; set; }

        [JsonProperty("segments")]
        public JRSegment[] Segments { get; set; }
    }

    public partial class JRSegment
    {
        [JsonProperty("index")]
        public long Index { get; set; }

        [JsonProperty("velocity")]
        public double Velocity { get; set; }

        [JsonProperty("original_index")]
        public long OriginalIndex { get; set; }

        [JsonProperty("target")]
        public JRTarget Target { get; set; }

        //[JsonProperty("pipe_centers")]
        //public Pipe Pipe { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }
    }

    public partial class JRTarget
    {
        [JsonProperty("ptp_target", NullValueHandling = NullValueHandling.Ignore)]
        public PtpTarget PtpTarget { get; set; }

        [JsonProperty("pose", NullValueHandling = NullValueHandling.Ignore)]
        public JRPose Pose { get; set; }
    }

    public partial class JRPose
    {
        [JsonProperty("position")]
        public JRPosition Position { get; set; }

        [JsonProperty("orientation")]
        public JROrientation Orientation { get; set; }
    }

    public partial class JROrientation
    {
        [JsonProperty("quaternion")]
        public JRPositionQ Quaternion { get; set; }
    }

    public partial class JRPosition
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }

        //[JsonProperty("w", NullValueHandling = NullValueHandling.Ignore)]
        //public double? W { get; set; }
    }

    public partial class JRPositionQ
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }

        [JsonProperty("w")]
        public double W { get; set; }
    }

    public partial class PtpTarget
    {
        [JsonProperty("joint_target")]
        public StartState JointTarget { get; set; }
    }

    public partial class JRTool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pose")]
        public JRPose Pose { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("collision_mesh")]
        public CollisionMesh CollisionMesh { get; set; }
    }

    public partial class CollisionMesh
    {
        [JsonProperty("triangles")]
        public Triangle[] Triangles { get; set; }

        [JsonProperty("vertices")]
        public Vertice[] Vertices { get; set; }
    }

    public partial class Vertice
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }

    public partial class Triangle
    {
        [JsonProperty("vertex_indices")]
        public int[] VertexIndices { get; set; }
    }
    #endregion
}