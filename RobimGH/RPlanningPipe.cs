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
    public class RPlanningPipe : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RPlanningPipe()
          : base("RPlanningPipe", "RPPipe",
            "Description",
            "Robim", "Planning")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program to simulate", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager.AddPlaneParameter("wpcPlane", "WP", "workpiece insertion plane", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddMeshParameter("wpcMesh", "WPM", "workpiece collision mesh", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddPointParameter("centerpts", "CPT", "centerpts", GH_ParamAccess.list);
            pManager[3].Optional = true;
            pManager.AddPlaneParameter("centerpl", "CPL", "center planes", GH_ParamAccess.list);
            pManager[4].Optional = true;
            pManager.AddPlaneParameter("passagepl", "PPL", "passage plane", GH_ParamAccess.item);
            pManager[5].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("JSON", "JSON", "JSON", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Program program = null;
            DA.GetData(0, ref program);

            Plane workPlane = new Plane(0, 0, 0, 0);
            DA.GetData(1, ref workPlane);

            Mesh workMesh = null;
            DA.GetData(2, ref workMesh);

            List<Point3d> cenPts = new List<Point3d>();
            DA.GetDataList(3, cenPts);

            List<Plane> cenPlanes = new List<Plane>();
            DA.GetDataList(4, cenPlanes);

            Plane pasPlane = new Plane(0, 0, 0, 0);
            DA.GetData(5, ref pasPlane);

            var targets = program.Value.Targets;

            RRequestPipe request = new RRequestPipe();

            #region start
            request.Name = "name";
            request.Path = new JRPathPipe();
            request.Path.StartState = new StartStatePipe();
            request.Path.StartState.Robot = targets[0].Joints;

            //request.Path.StartState.External = new double[] { 0.0 };
            request.Path.StartState.External = targets[0].ProgramTargets[0].Target.External;
            #endregion

            #region path
            int counter = 0;
            List<JRSegmentPipe> allSegments = new List<JRSegmentPipe>();

            JRSegmentPipe jrj = new JRSegmentPipe();

            jrj.Index = counter;
            jrj.OriginalIndex = counter;
            jrj.Velocity = 0.0;
            jrj.Mode = "ptp";

            jrj.Target = new JRTargetPipe();
            jrj.Target.PtpTarget = new PtpTargetPipe();
            jrj.Target.PtpTarget.JointTarget = new StartStatePipe();
            jrj.Target.PtpTarget.JointTarget.Robot = new double[] { 0, 0, 0, 0, 0, 0 };
            jrj.Target.PtpTarget.JointTarget.External = new double[] { 0.0 };

            allSegments.Add(jrj);
            counter++;

            foreach (Plane p in cenPlanes)
            {
                JRSegmentPipe jr = new JRSegmentPipe();
                //var p = cenPlanes[i];

                jr.Index = counter;
                jr.OriginalIndex = counter;
                jr.Velocity = 0.0;
                jr.Mode = "line";
                jr.Target = new JRTargetPipe();
                jr.Target.Pose = new JRPosePipe();
                jr.Target.Pose.Position = new JRPositionPipe();

                jr.Target.Pose.Position.X = p.OriginX;
                jr.Target.Pose.Position.Y = p.OriginY;
                jr.Target.Pose.Position.Z = p.OriginZ;
                jr.Target.Pose.Orientation = new JROrientationPipe();
                jr.Target.Pose.Orientation.Quaternion = new JRPositionQPipe();

                var q = PosTransform.Plane2Quaternion(p);

                jr.Target.Pose.Orientation.Quaternion.X = Convert.ToDouble(q.B.ToString("F8"));
                jr.Target.Pose.Orientation.Quaternion.Y = Convert.ToDouble(q.C.ToString("F8"));
                jr.Target.Pose.Orientation.Quaternion.Z = Convert.ToDouble(q.D.ToString("F8"));
                jr.Target.Pose.Orientation.Quaternion.W = Convert.ToDouble(q.A.ToString("F8"));

                counter++;
                //p.Vertex = new double[] { point.X, point.Y, point.Z };

                allSegments.Add(jr);
            }

            request.Path.Segments = new JRSegmentPipe[allSegments.Count];
            request.Path.Segments = allSegments.ToArray();
            #endregion

            #region constraints
            request.InterpolationStep = 50.0;

            request.PassagePath = true;
            request.Reverse = true;
            var PG = new JRPosePipe();
            PG.Position = new JRPositionPipe();
            PG.Orientation = new JROrientationPipe();
            PG.Orientation.Quaternion = new JRPositionQPipe();
            PG.Position.X = pasPlane.OriginX;
            PG.Position.Y = pasPlane.OriginY;
            PG.Position.Z = pasPlane.OriginZ;
            var pasQ = PosTransform.Plane2Quaternion(pasPlane);
            PG.Orientation.Quaternion.X = Convert.ToDouble(pasQ.B.ToString("F8"));
            PG.Orientation.Quaternion.Y = Convert.ToDouble(pasQ.C.ToString("F8"));
            PG.Orientation.Quaternion.Z = Convert.ToDouble(pasQ.D.ToString("F8"));
            PG.Orientation.Quaternion.W = Convert.ToDouble(pasQ.A.ToString("F8"));  //????? probably plane is flipped, look into this
            request.Passage = PG;

            var JC = new JointConstraintsPipe();
            JC.Coefficient = new StartStatePipe();
            JC.Coefficient.Robot = new double[] { 0, 0, 0, 0, 0, 0 };
            JC.Coefficient.External = new double[0];
            JC.Bound = new BoundPipe();
            JC.Bound.LowerBound = 0.0;
            JC.Bound.UpperBound = 0.0;
            JC.Bound.Step = 0.0;

            var CC = new CartConstraintsPipe();
            var cBound = new CBoundPipe();
            cBound.Axis = "y";
            var c1B = new BoundChildPipe();
            c1B.LowerBound = 0;// -180.0;
            c1B.UpperBound = 0;// 180.0;
            c1B.Step = 1.0;
            cBound.Bound = c1B;
            CC.Bounds = new CBoundPipe[] { cBound };
            CC.Type = "rotation";
            var pt = targets[0].ProgramTargets;
            var toolName = pt[0].Target.Tool.Name;
            //CC.Frame = toolName;
            CC.Frame = "tool";

            var constraint1 = new JRConstraintPipe();
            constraint1.JointConstraints = JC;
            var constraint2 = new JRConstraintPipe();
            constraint2.CartConstraints = CC;

            List<JRConstraintPipe> constraints = new List<JRConstraintPipe>();
            constraints.Add(constraint1);
            constraints.Add(constraint2);

            request.Constraints = constraints;

            //var constraint = new JRConstraintPipe();
            //constraint.JointConstraints = JC;
            //constraint.CartConstraints = CC;
            //List<JRConstraintPipe> cs = new List<JRConstraintPipe>() { constraint };
            //request.Constraints = cs;
            #endregion

            #region tool
            request.Tool = new JRToolPipe();
            request.Tool.Type = "tool";
            request.Tool.Name = toolName;
            request.Tool.Pose = new JRPosePipe();

            request.Tool.Pose.Position = new JRPositionPipe();
            var toolPlane = pt[0].Target.Tool.Tcp;
            request.Tool.Pose.Position.X = toolPlane.OriginX;
            request.Tool.Pose.Position.Y = toolPlane.OriginY;
            request.Tool.Pose.Position.Z = toolPlane.OriginZ;

            request.Tool.Pose.Orientation = new JROrientationPipe();
            request.Tool.Pose.Orientation.Quaternion = new JRPositionQPipe();
            var qtool = PosTransform.Plane2Quaternion(toolPlane);
            request.Tool.Pose.Orientation.Quaternion.X = Convert.ToDouble(qtool.B.ToString("F8"));
            request.Tool.Pose.Orientation.Quaternion.Y = Convert.ToDouble(qtool.C.ToString("F8"));
            request.Tool.Pose.Orientation.Quaternion.Z = Convert.ToDouble(qtool.D.ToString("F8"));
            request.Tool.Pose.Orientation.Quaternion.W = Convert.ToDouble(qtool.A.ToString("F8"));

            var toolMeshes = pt[0].Target.Tool.Mesh_;
            var toolMesh = toolMeshes[0];
            request.Tool.CollisionMesh = new CollisionMeshPipe();
            List<Triangle> trs = new List<Triangle>();
            List<Vertice> vert = new List<Vertice>();

            fromMesh(toolMesh, out trs, out vert);

            request.Tool.CollisionMesh.Triangles = new Triangle[trs.Count];
            request.Tool.CollisionMesh.Triangles = trs.ToArray();

            request.Tool.CollisionMesh.Vertices = new Vertice[vert.Count];
            request.Tool.CollisionMesh.Vertices = vert.ToArray();
            #endregion

            #region workpiece
            request.Workpiece = new JRToolPipe();
            request.Workpiece.Type = "workpiece";
            request.Workpiece.Name = "workpiece";

            request.Workpiece.Pose = new JRPosePipe();

            request.Workpiece.Pose.Position = new JRPositionPipe();
            request.Workpiece.Pose.Position.X = workPlane.OriginX;
            request.Workpiece.Pose.Position.Y = workPlane.OriginY;
            request.Workpiece.Pose.Position.Z = workPlane.OriginZ;

            request.Workpiece.Pose.Orientation = new JROrientationPipe();
            request.Workpiece.Pose.Orientation.Quaternion = new JRPositionQPipe();
            var qwork = PosTransform.Plane2Quaternion(workPlane);
            request.Workpiece.Pose.Orientation.Quaternion.X = Convert.ToDouble(qwork.B.ToString("F8"));
            request.Workpiece.Pose.Orientation.Quaternion.Y = Convert.ToDouble(qwork.C.ToString("F8"));
            request.Workpiece.Pose.Orientation.Quaternion.Z = Convert.ToDouble(qwork.D.ToString("F8"));
            request.Workpiece.Pose.Orientation.Quaternion.W = Convert.ToDouble(qwork.A.ToString("F8"));

            request.Workpiece.CollisionMesh = new CollisionMeshPipe();
            List<Triangle> trsW = new List<Triangle>();
            List<Vertice> vertW = new List<Vertice>();

            fromMesh(workMesh, out trsW, out vertW);

            request.Workpiece.CollisionMesh.Triangles = new Triangle[trsW.Count];
            request.Workpiece.CollisionMesh.Triangles = trsW.ToArray();

            request.Workpiece.CollisionMesh.Vertices = new Vertice[vertW.Count];
            request.Workpiece.CollisionMesh.Vertices = vertW.ToArray();
            #endregion

            string json = JsonConvert.SerializeObject(request, Formatting.Indented);
            DA.SetData(0, json);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconCreateTarget;

        public override Guid ComponentGuid => new Guid("b2e46338-e09c-4bfc-ba60-522a282cd6d1");

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

        #region serializer_classes


        public partial class RRequestPipe
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("path")]
            public JRPathPipe Path { get; set; }

            [JsonProperty("interpolation_step")]
            public double InterpolationStep { get; set; }

            [JsonProperty("passage_path")]
            public Boolean PassagePath { get; set; }

            [JsonProperty("reverse")]
            public Boolean Reverse { get; set; }

            [JsonProperty("passage")]
            public JRPosePipe Passage { get; set; }

            [JsonProperty("constraints")]
            public List<JRConstraintPipe> Constraints { get; set; }

            [JsonProperty("tool")]
            public JRToolPipe Tool { get; set; }

            [JsonProperty("workpiece")]
            public JRToolPipe Workpiece { get; set; }
        }

        public partial class JRConstraintPipe
        {
            [JsonProperty("joint_constraints", NullValueHandling = NullValueHandling.Ignore)]
            public JointConstraintsPipe JointConstraints { get; set; }

            [JsonProperty("cart_constraints", NullValueHandling = NullValueHandling.Ignore)]
            public CartConstraintsPipe CartConstraints { get; set; }
        }

        public partial class CartConstraintsPipe
        {
            [JsonProperty("bounds")]
            public CBoundPipe[] Bounds { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("frame")]
            public string Frame { get; set; }
        }

        public partial class CBoundPipe
        {
            [JsonProperty("axis")]
            public string Axis { get; set; }

            [JsonProperty("bound")]
            public BoundChildPipe Bound { get; set; }
        }

        public partial class BoundChildPipe
        {
            [JsonProperty("lower_bound")]
            public double LowerBound { get; set; }

            [JsonProperty("upper_bound")]
            public double UpperBound { get; set; }

            [JsonProperty("step")]
            public double Step { get; set; }
        }

        public partial class JointConstraintsPipe
        {
            [JsonProperty("coefficient")]
            public StartStatePipe Coefficient { get; set; }

            [JsonProperty("bound")]
            public BoundPipe Bound { get; set; }
        }

        public partial class BoundPipe
        {
            [JsonProperty("lower_bound")]
            public double LowerBound { get; set; }

            [JsonProperty("upper_bound")]
            public double UpperBound { get; set; }

            [JsonProperty("step")]
            public double Step { get; set; }
        }

        public partial class StartStatePipe
        {
            [JsonProperty("robot")]
            public double[] Robot { get; set; }

            [JsonProperty("external")]
            public double[] External { get; set; }
        }

        public partial class JRPathPipe
        {
            [JsonProperty("start_state")]
            public StartStatePipe StartState { get; set; }

            [JsonProperty("segments")]
            public JRSegmentPipe[] Segments { get; set; }
        }

        public partial class JRSegmentPipe
        {
            [JsonProperty("index")]
            public long Index { get; set; }

            [JsonProperty("velocity")]
            public double Velocity { get; set; }

            [JsonProperty("original_index")]
            public long OriginalIndex { get; set; }

            [JsonProperty("target")]
            public JRTargetPipe Target { get; set; }

            //[JsonProperty("pipe_centers")]
            //public Pipe Pipe { get; set; }

            [JsonProperty("mode")]
            public string Mode { get; set; }
        }

        public partial class JRTargetPipe
        {
            [JsonProperty("ptp_target", NullValueHandling = NullValueHandling.Ignore)]
            public PtpTargetPipe PtpTarget { get; set; }

            [JsonProperty("pose", NullValueHandling = NullValueHandling.Ignore)]
            public JRPosePipe Pose { get; set; }
        }

        public partial class JRPosePipe
        {
            [JsonProperty("position")]
            public JRPositionPipe Position { get; set; }

            [JsonProperty("orientation")]
            public JROrientationPipe Orientation { get; set; }
        }

        public partial class JROrientationPipe
        {
            [JsonProperty("quaternion")]
            public JRPositionQPipe Quaternion { get; set; }
        }

        public partial class JRPositionPipe
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

        public partial class JRPositionQPipe
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

        public partial class PtpTargetPipe
        {
            [JsonProperty("joint_target")]
            public StartStatePipe JointTarget { get; set; }
        }

        public partial class JRToolPipe
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("pose")]
            public JRPosePipe Pose { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("collision_mesh")]
            public CollisionMeshPipe CollisionMesh { get; set; }
        }

        public partial class CollisionMeshPipe
        {
            [JsonProperty("triangles")]
            public Triangle[] Triangles { get; set; }

            [JsonProperty("vertices")]
            public Vertice[] Vertices { get; set; }
        }

        #endregion
    }
}