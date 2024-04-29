using Alea.IL.Reflection;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Undo.Actions;
using Rhino;
using Rhino.Geometry;
using Rhino.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Windows.Documents;
using Rhino.Geometry.Intersect;
using Rhino.Geometry.Collections;
using System.Net;
using System.Xml;
using System.Threading.Tasks;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Parameters;
using GH_IO.Serialization;
using RobimRobots;
//using Alea;
//using Alea.CSharp;
//using Alea.Parallel;

namespace Robim.Grasshopper
{
    #region
    
    public class Test_IK : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Test_IK() : base("IK Fast", "IK", "IKfast result", "Robim", "Test") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("TargetPlane", "P", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("index", "i", "index of solution (0-7)", GH_ParamAccess.item, 2);
            pManager.AddBooleanParameter("Allsol", "ALL", "All", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("IK solution", "IK", "IKfast solution", GH_ParamAccess.item);
            pManager.AddTextParameter("time", "T", "Time", GH_ParamAccess.item);
            pManager.AddTextParameter("Sol", "S", "Solution", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Plane plane1 = Plane.Unset;
            DA.GetData(0, ref plane1);
            int sol = 2;
            DA.GetData(1, ref sol);
            if (sol < 0)
            {
                sol = 0;
            }
            if (sol > 7)
            {
                sol = 7;
            }
            bool allsol = true;
            DA.GetData(2, ref allsol);
            List<string> errors;
            var transform = Transform.PlaneToPlane(Plane.WorldXY, plane1);
            DataTree<double> dataTree = new DataTree<double>();
            GH_Path gH_Path = null;
            Stopwatch sw = new Stopwatch();
            if (allsol)
            {
                dataTree = new DataTree<double>();
                sw.Start();
                double[][] done = InverseKinematics(transform, out errors);
                sw.Stop();
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        gH_Path = new GH_Path(i);
                        dataTree.Add(done[i][j], gH_Path);
                    }
                }
            }
            else
            {
                dataTree = new DataTree<double>();
                sw.Start();
                double[] done = InverseKinematics_GetOneSol(transform, sol, out errors);
                sw.Stop();
                for (int i = 0; i < 6; i++)
                {
                    gH_Path = new GH_Path(0);
                    dataTree.Add(done[i], gH_Path);
                }
            }
            DA.SetData(0, "done");
            DA.SetData(1, sw.Elapsed.TotalMilliseconds.ToString());
            DA.SetDataTree(2, dataTree);
        }
        public static double[][] InverseKinematics(Transform transform, out List<string> errors)
        {
            errors = new List<string>();
            double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
            double[] rota = new double[9]//ZYX
            {
                transform[0, 0], transform[0, 1], transform[0, 2],
                transform[1, 0], transform[1, 1], transform[1, 2],
                transform[2, 0], transform[2, 1], transform[2, 2]
            };
            double[] solutions = new double[48];

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            bool a = ComputeIk_KRTwenty_REighteenTen(xyz, rota, 0, solutions);
            //sw.Stop();
            ////需要打开VS输出窗口查看
            //Debug.WriteLine("时间:" + sw.Elapsed);
            //sw.Reset();
            double[][] eightsolutions = new double[8][];
            if (!a)
            {
                errors.Add("Failed to get 8 ik solutions");
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    double[] onesolutions = new double[6];
                    for (int j = 0; j < 6; j++)
                    {
                        double onevalue = solutions[i * 6 + j];
                        onesolutions[j] = onevalue;
                    }
                    eightsolutions[i] = onesolutions;
                }
            }
            return eightsolutions;
        }

        public static double[] InverseKinematics_GetOneSol(Transform transform, int solindex, out List<string> errors)
        {
            errors = new List<string>();
            double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
            double[] rota = new double[9]//ZYX
            {
                transform[0, 0], transform[0, 1], transform[0, 2],
                transform[1, 0], transform[1, 1], transform[1, 2],
                transform[2, 0], transform[2, 1], transform[2, 2]
            };
            double[] solutions = new double[6];
            bool a = ComputeIkOne_KRTwenty_REighteenTen(xyz, rota, 0, solutions, solindex);
            if (!a)
            {
                errors.Add($"Failed to get No.{solindex} ik solution");
            }
            return solutions;
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("181f2808-0915-47e8-b30d-90590987ba81"); }
        }

        [DllImport("IKFast_KUKA.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ComputeIk_KRTwenty_REighteenTen(double[] eetrans, double[] eerot, double pfree, double[] solutions);//eetrans = 机械臂末端位置，eerot = 机械臂末端旋转矩阵，pfree = 第7自由度，solutions = 回传值

        [DllImport("IKFast_KUKA.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ComputeIkOne_KRTwenty_REighteenTen(double[] eetrans, double[] eerot, double pfree, double[] solutions, int solindex);
    }
    public class Test_GJK : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Test_GJK() : base("GJK", "GJK", "Test Some Developing Script", "Robim", "Test") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("FirstPoints", "FPs", "First Points", GH_ParamAccess.list);
            pManager.AddPointParameter("SecondPoints", "SPs", "Second Poins", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Result", "R", "Result of Collision", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> points1 = new List<Point3d>();
            List<Point3d> points2 = new List<Point3d>();
            DA.GetDataList(0, points1);
            DA.GetDataList(1, points2);
            ConvexHull convex1 = new ConvexHull(points1.ToArray());
            ConvexHull convex2 = new ConvexHull(points2.ToArray());
            bool hascollison = GJKAlgorithm.Intersects(convex1, convex2);
            //Intersection;

            DA.SetData(0, hascollison);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("32D1A0AB-07DC-439B-805A-98B4FC08F6DD"); }
        }
    }
    public class Timing : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Timing() : base("Timing", "T", "Get component run time", "Robim", "Test") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Data", "D", "Input component data", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Clear", "C", "Input component data", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Time", "T", "Component run time", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// 

        List<string> time = new List<string>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool clear = false;
            DA.GetData(1, ref clear);
            var id = this.Params.Input[0].Sources[0].Attributes.GetTopLevel.InstanceGuid;
            GH_Component comp = OnPingDocument().FindComponent(id) as GH_Component;
            double ms = comp.ProcessorTime.TotalMilliseconds;
            time.Add(ms.ToString("f5") + "  ms");
            if (clear)
            {
                time.Clear();
            }
            DA.SetDataList(0, time);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{D269E66A-B201-4022-9E89-ED9206E03A07}"); }
        }
    }
    public class Test_SaveMesh : GH_Component
    {
        GH_ValueList valueList = null;
        GH_ValueList valueList2 = null;
        IGH_Param parameter = null;
        IGH_Param parameter2 = null;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Test_SaveMesh() : base("GetResourcesMesh", "GRM", "Get mesh form binary", "Robim", "Test") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Folder", "F", "Folder want to save model data", GH_ParamAccess.item);
            pManager.AddTextParameter("Manufacturers", "Ma", "Model manufacturers", GH_ParamAccess.item);
            pManager.AddTextParameter("ModelType", "Mt", "Model type", GH_ParamAccess.item);
            pManager.AddTextParameter("ModelName", "N", "Model name", GH_ParamAccess.item);
            pManager.AddIntegerParameter("JointCount", "Int", "Joint count", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint0", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint1", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint2", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint3", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint4", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint5", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint6", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint_CH_0", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint_CH_1", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint_CH_2", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint_CH_3", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint_CH_4", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint_CH_5", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Joint_CH_6", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Save", "S", "Save mesh", GH_ParamAccess.item, false);
            parameter = pManager[1];
            parameter2 = pManager[2];
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
            pManager[11].Optional = true;
            pManager[12].Optional = true;
            pManager[13].Optional = true;
            pManager[14].Optional = true;
            pManager[15].Optional = true;
            pManager[16].Optional = true;
            pManager[17].Optional = true;
            pManager[18].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "S", "text", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            if (valueList == null)
            {
                if (parameter.Sources.Count == 0)
                {
                    valueList = new GH_ValueList();
                }
                else
                {
                    foreach (var source in parameter.Sources)
                    {
                        if (source is GH_ValueList) valueList = source as GH_ValueList;
                        return;
                    }
                }

                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 180, this.Attributes.Pivot.Y - 31);
                valueList.ListItems.Clear();

                var manufacturers = Enum.GetNames(typeof(Manufacturers));

                foreach (string manufacturer in manufacturers)
                {
                    valueList.ListItems.Add(new GH_ValueListItem(manufacturer, $"\"{manufacturer}\""));
                }

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
            }
            if (valueList2 == null)
            {
                if (parameter2.Sources.Count == 0)
                {
                    valueList2 = new GH_ValueList();
                }
                else
                {
                    foreach (var source in parameter2.Sources)
                    {
                        if (source is GH_ValueList) valueList2 = source as GH_ValueList;
                        return;
                    }
                }

                valueList2.CreateAttributes();
                valueList2.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 180, this.Attributes.Pivot.Y - 31);
                valueList2.ListItems.Clear();

                var manufacturers = Enum.GetNames(typeof(ModelType));

                foreach (string manufacturer in manufacturers)
                {
                    valueList2.ListItems.Add(new GH_ValueListItem(manufacturer, $"\"{manufacturer}\""));
                }

                Instances.ActiveCanvas.Document.AddObject(valueList2, false);
                parameter2.AddSource(valueList2);
                parameter2.CollectData();
            }
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            string manufacturer = "";
            string modeltype = "";
            string modelname = "";
            int jointcount = 0;
            if (!DA.GetData(0, ref path)) { return; }
            if (!DA.GetData(1, ref manufacturer)) { return; }
            if (!DA.GetData(2, ref modeltype)) { return; }
            if (!DA.GetData(3, ref modelname)) { return; }
            if (!DA.GetData(4, ref jointcount)) { return; }

            List<Mesh> Joints = new List<Mesh>();
            List<Mesh> Joints_CH = new List<Mesh>();
            for (int i = 0; i < jointcount + 1; i++)
            {
                Mesh mesh = null;
                Mesh mesh_CH = null;
                DA.GetData($"Joint{i}", ref mesh);
                if (mesh == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Joint{i} has no mesh.");
                    return;
                }
                DA.GetData($"Joint_CH_{i}", ref mesh_CH);
                if (mesh_CH == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Joint_CH_{i} has no mesh.");
                    return;
                }
                Joints.Add(mesh);
                Joints_CH.Add(mesh_CH);
            }

            bool save = false;
            DA.GetData("Save", ref save);
            string done = "Error";
            if (save)
            {
                string model = "";
                switch ((ModelType)Enum.Parse(typeof(ModelType), modeltype))
                {
                    case ModelType.Robot:
                        model = $"{manufacturer}_{modelname}";
                        break;
                    case ModelType.Track:
                        model = $"Track_{modelname}";
                        break;
                    case ModelType.Platform:
                        model = $"Platform_{modelname}";
                        break;
                    case ModelType.Tool:
                        model = $"Tool_{modelname}";
                        break;
                    case ModelType.Custom:
                        model = $"Custom_{modelname}";
                        break;
                }
                string loacal = Path.Combine(path, model);
                Directory.CreateDirectory(loacal + "\\");
                for (int i = 0; i < jointcount + 1; i++)
                {
                    string meshbyte = "";
                    meshbyte = Convert.ToBase64String(GH_Convert.CommonObjectToByteArray(Joints[i]));
                    string txt = Path.Combine(loacal, $"{model}_{i}.txt");
                    File.WriteAllText(txt, meshbyte);

                    string meshbyte_CH = "";
                    meshbyte_CH = Convert.ToBase64String(GH_Convert.CommonObjectToByteArray(Joints_CH[i]));
                    string txt_CH = Path.Combine(loacal, $"{model}_CH_{i}.txt");
                    File.WriteAllText(txt_CH, meshbyte_CH);

                    if (File.Exists(txt) && File.Exists(txt_CH))
                    {
                        done = "Done";
                    }
                }
            }

            //string a = Properties.Resources.CSaw;
            //mesh = GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(a));

            DA.SetData(0, done);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{07717649-5AB5-43A7-A81E-89026D213F3C}"); }
        }
    }
    public class CreateConvexHull : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CreateConvexHull() : base("ConvexHull", "CHull", "Outputs convex hull points to a given set of points", "Robim", "Test") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddVectorParameter("Input points", "Pt", "Point cloud to calculate convex hull from", GH_ParamAccess.list);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("CHull points", "C_H", "Points that make up generated convex hull", GH_ParamAccess.list);
            pManager.AddMeshFaceParameter("Mesh faces", "F", "Generated mesh faces", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Generated mesh", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Face planes", "Pl", "", GH_ParamAccess.list);
            pManager.AddBoxParameter("Minimal Bounding Box", "BB", "", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //List<Rhino.Geometry.Point3d> inputP = new List<Rhino.Geometry.Point3d>();
            var calc = new ConvexHullCalculator();

            var verts = new List<Vector3d>();
            var tris = new List<int>();
            var normals = new List<Vector3d>();
            var points = new List<Vector3d>();
            var faces = new List<MeshFace>();
            var cpoints = new List<Point3d>();
            var planes = new List<Plane>();
            var minbox = new Rhino.Geometry.Box();

            var volumes = new List<double>();

            var al_boxes = new List<Rhino.Geometry.Box>();

            var mesh = new Rhino.Geometry.Mesh();

            //IEnumerable<Point3d> enumPts = new List<Rhino.Geometry.Point3d>();

            if (!DA.GetDataList(0, points)) { return; }
            if (points == null) { return; }
            var y = points.Distinct().ToList();
            calc.GenerateHull(y, true, ref verts, ref tris, ref normals);

            int trisLen = tris.Count;

            foreach (Vector3d vec in verts)
            {
                mesh.Vertices.Add(vec.X, vec.Y, vec.Z);
                cpoints.Add((Point3d)vec);
            }

            IEnumerable<Point3d> enumPts = cpoints as IEnumerable<Point3d>;


            for (int i = 0; i < trisLen / 3; i++)
            {
                faces.Add(new Rhino.Geometry.MeshFace(tris[i * 3 + 0], tris[i * 3 + 1], tris[i * 3 + 2]));
                mesh.Faces.AddFace(tris[i * 3 + 0], tris[i * 3 + 1], tris[i * 3 + 2]);
                var tempPlane = new Rhino.Geometry.Plane((Point3d)verts[tris[i * 3 + 0]], (Point3d)verts[tris[i * 3 + 1]], (Point3d)verts[tris[i * 3 + 2]]);
                planes.Add(tempPlane);
                var tempBox = new Rhino.Geometry.Box(tempPlane, enumPts);
                al_boxes.Add(tempBox);
                volumes.Add(tempBox.Volume);
            }
            var minIndex = new int();
            minIndex = volumes.IndexOf(volumes.Min());

            minbox = al_boxes[minIndex];
            var z = verts.Distinct().ToList();
            DA.SetDataList(0, z);
            DA.SetDataList(1, faces);
            DA.SetData(2, mesh);
            DA.SetDataList(3, planes);
            DA.SetData(4, minbox);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{9E6B3023-2125-450D-A00F-5DF3F0D20F08}"); }
        }
    }
    public class InverseCSG : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public InverseCSG() : base("InverseCSG", "InverseCSG", "InverseCSG", "Robim", "Test") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Origin Geometry", "G", "Input Geometry", GH_ParamAccess.item);
            pManager.AddBrepParameter("Target Geometry", "G", "Input Geometry", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("BoundingBox Brep", "BBB", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Lines", "Ls", "", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Brep gH_Brep = null;
            GH_Brep gH_Brep1 = null;
            DA.GetData(0, ref gH_Brep);
            DA.GetData(1, ref gH_Brep1);
            //Mesh.CreateBooleanUnion();
            Brep[] breps = Brep.CreateBooleanDifference(gH_Brep.Value, gH_Brep1.Value, 1);
            List<Mesh> meshes = new List<Mesh>();
            List<Brep> breps1 = new List<Brep>();
            foreach (Brep brep in breps)
            {
                var a = Mesh.CreateFromBrep(brep, MeshingParameters.Minimal);
                Mesh mesh0 = a[0].DuplicateMesh();
                for (int i = 1; i < a.Length; i++)
                {
                    mesh0.Append(a[i].DuplicateMesh());
                }
                meshes.Add(mesh0.DuplicateMesh());
                breps1.Add(mesh0.GetBoundingBox(true).ToBrep());
            }

            //List<Point3d> point3Ds = GetPoints(breps1[0], meshes[0]);
            List<Curve> curves = GetLines(meshes[1]);
            curves.AddRange(GetLines(meshes[0]));
            DA.SetDataList(0, meshes);
            DA.SetDataList(1, breps1);
            DA.SetDataList(2, curves);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{8EF5AB2F-E6EE-440B-A643-8D2CEC57C52F}"); }
        }
        List<Curve> GetLines(Mesh mesh)
        {
            BoundingBox boundingBox = mesh.GetBoundingBox(true);
            var brep = boundingBox.ToBrep();
            brep.Transform(Transform.Scale(boundingBox.Center, 0.999));
            var a = Mesh.CreateFromBrep(brep, MeshingParameters.Default);
            Mesh mesh0 = a[0].DuplicateMesh();
            for (int i = 1; i < a.Length; i++)
            {
                mesh0.Append(a[i].DuplicateMesh());
            }
            Polyline[] polylines = Intersection.MeshMeshAccurate(mesh, mesh0, 0);
            List<Curve> curves = new List<Curve>();
            foreach (Polyline point3Ds in polylines)
            {
                curves.Add(point3Ds.ToNurbsCurve());
            }
            return curves;
        }

        List<Point3d> GetPoints(Brep brep, Mesh mesh)
        {
            BrepEdgeList a2 = brep.Edges;

            List<Point3d> point3Ds = new List<Point3d>();

            for (int index = 0; index < a2.Count; index++)
            {
                Point3d rightpoint = a2[index].PointAtStart;
                Point3d leftpoint = a2[index].PointAtEnd;
                Line line = new Line(rightpoint, leftpoint);
                bool rightdone = false;
                bool leftdone = false;
                int j = 1000;
                for (int i = 0; i < j; i++)
                {
                    double l = line.Length / (j * 2) * i;
                    if (!rightdone)
                    {
                        bool right = mesh.IsPointInside(line.PointAtLength(l), 1, false);//右到左
                        if (!right)
                        {
                            rightpoint = line.PointAtLength(l);
                            rightdone = true;
                        }
                    }
                    if (!leftdone)
                    {
                        double reverse_l = line.Length - l;
                        bool left = mesh.IsPointInside(line.PointAtLength(reverse_l), 1, false);//左到右
                        if (!left)
                        {
                            leftpoint = line.PointAtLength(reverse_l);
                            leftdone = true;
                        }
                    }
                    if (rightdone && leftdone)
                    {
                        break;
                    }
                }
                if (rightpoint != a2[index].PointAtStart)
                    point3Ds.Add(rightpoint);
                if (leftpoint != a2[index].PointAtEnd)
                    point3Ds.Add(leftpoint);
            }
            return point3Ds;
        }
    }
    public class GetSurface : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public GetSurface() : base("GetSurface", "GetSurface", "GetSurface", "Robim", "Test") { }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "", "", GH_ParamAccess.list);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Meshes", "M", "", GH_ParamAccess.list);
            pManager.AddPointParameter("BoundingBox Brep", "BBB", "", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            DA.GetDataList(0, curves);

            List<Point3d> Allpoint3Ds = new List<Point3d>();
            List<Plane> planes = new List<Plane>();

            foreach (Curve curve1 in curves)
            {
                Curve[] curve = curve1.DuplicateSegments();
                List<Point3d> point3Ds = curve.Select(x => x.PointAtStart).ToList();
                Allpoint3Ds.AddRange(point3Ds);
                Plane.FitPlaneToPoints(point3Ds, out Plane plane);
                plane.Origin = curve1.PointAtStart;
                planes.Add(plane);
            }

            List<Plane> sortplane = new List<Plane>();
            sortplane.Add(planes[0]);
            for (int i = 1; i < planes.Count; i++)
            {
                bool issameplane = false;
                for (int j = 0; j < sortplane.Count; j++)
                {
                    Vector3d vector3D = sortplane[j].Normal;
                    if (planes[i].Origin == sortplane[j].Origin)
                    {
                        double a = Vector3d.VectorAngle(planes[i].Normal, vector3D);
                        if (a == 0 || a == Math.PI)
                        {
                            issameplane = true;
                            break;
                        }
                    }
                    else
                    {
                        Vector3d cross = Vector3d.CrossProduct(planes[i].Origin - sortplane[j].Origin, sortplane[j].XAxis);
                        double a = Vector3d.VectorAngle(cross, vector3D);//radian
                        if (cross == Vector3d.Zero || a == 0 || a == Math.PI)
                        {
                            issameplane = true;
                            break;
                        }
                    }
                }
                if (!issameplane)
                {
                    sortplane.Add(planes[i]);
                }
            }

            List<Point3d> facepoints = new List<Point3d>();
            for (int p = 0; p < sortplane.Count; p++)
            {
                Plane plane = sortplane[p];
                for (int i = 0; i < Allpoint3Ds.Count; i++)
                {
                    Point3d point3D = plane.ClosestPoint(Allpoint3Ds[i]);
                    if (i == 0)
                    {
                        facepoints.Add(point3D);
                    }
                    else
                    {
                        bool samepoint = false;
                        for (int j = 0; j < facepoints.Count; j++)
                        {
                            if (point3D == facepoints[j])
                            {
                                samepoint = true;
                                break;
                            }
                        }
                        if (!samepoint)
                            facepoints.Add(point3D);
                    }
                }
            }
            DA.SetDataList(0, sortplane);
            DA.SetDataList(1, facepoints);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{C1161D6F-9D9A-4223-8B75-A248AFFE46CB}"); }
        }
    }
    public class RPCTest : GH_Component
    {
        public RPCTest() : base("RPCTest", "RPCTest", "RPCTest", "Robim", "Test") { }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "", "", GH_ParamAccess.list);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Meshes", "M", "", GH_ParamAccess.list);
            pManager.AddPointParameter("BoundingBox Brep", "BBB", "", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            DA.GetDataList(0, curves);

            List<Point3d> Allpoint3Ds = new List<Point3d>();
            List<Plane> planes = new List<Plane>();

            foreach (Curve curve1 in curves)
            {
                Curve[] curve = curve1.DuplicateSegments();
                List<Point3d> point3Ds = curve.Select(x => x.PointAtStart).ToList();
                Allpoint3Ds.AddRange(point3Ds);
                Plane.FitPlaneToPoints(point3Ds, out Plane plane);
                plane.Origin = curve1.PointAtStart;
                planes.Add(plane);
            }

            List<Plane> sortplane = new List<Plane>();
            sortplane.Add(planes[0]);
            for (int i = 1; i < planes.Count; i++)
            {
                bool issameplane = false;
                for (int j = 0; j < sortplane.Count; j++)
                {
                    Vector3d vector3D = sortplane[j].Normal;
                    if (planes[i].Origin == sortplane[j].Origin)
                    {
                        double a = Vector3d.VectorAngle(planes[i].Normal, vector3D);
                        if (a == 0 || a == Math.PI)
                        {
                            issameplane = true;
                            break;
                        }
                    }
                    else
                    {
                        Vector3d cross = Vector3d.CrossProduct(planes[i].Origin - sortplane[j].Origin, sortplane[j].XAxis);
                        double a = Vector3d.VectorAngle(cross, vector3D);//radian
                        if (cross == Vector3d.Zero || a == 0 || a == Math.PI)
                        {
                            issameplane = true;
                            break;
                        }
                    }
                }
                if (!issameplane)
                {
                    sortplane.Add(planes[i]);
                }
            }

            List<Point3d> facepoints = new List<Point3d>();
            for (int p = 0; p < sortplane.Count; p++)
            {
                Plane plane = sortplane[p];
                for (int i = 0; i < Allpoint3Ds.Count; i++)
                {
                    Point3d point3D = plane.ClosestPoint(Allpoint3Ds[i]);
                    if (i == 0)
                    {
                        facepoints.Add(point3D);
                    }
                    else
                    {
                        bool samepoint = false;
                        for (int j = 0; j < facepoints.Count; j++)
                        {
                            if (point3D == facepoints[j])
                            {
                                samepoint = true;
                                break;
                            }
                        }
                        if (!samepoint)
                            facepoints.Add(point3D);
                    }
                }
            }
            DA.SetDataList(0, sortplane);
            DA.SetDataList(1, facepoints);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{2B75235D-39F5-410A-AC54-F42F870A3363}"); }
        }
    }
    public class SimpleIntegerParameterAttributes : GH_ComponentAttributes
    {
        public SimpleIntegerParameterAttributes(GH_Component owner) : base(owner)
        {

        }
        protected override void Layout()
        {
            base.Layout();
            // Compute the width of the NickName of the owner (plus some extra padding),
            // then make sure we have at least 80 pixels.
            //int width = GH_FontServer.StringWidth(Owner.NickName, GH_FontServer.Standard);
            //width = Math.Max(width + 10, 100);

            // The height of our object is always 60 pixels
            //int height = 100;

            // Assign the width and height to the Bounds property.
            // Also, make sure the Bounds are anchored to the Pivot
            //Bounds = new RectangleF(Pivot, new SizeF(width, height));
            Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
            rec0.Height += 42;
            Rectangle rec1 = rec0;
            rec1.X = rec0.Left + 2;
            rec1.Y = rec0.Bottom - 42;
            rec1.Width = (rec0.Width) / 3;
            rec1.Height = 22;
            rec1.Inflate(-2, -2);
            Rectangle rec2 = rec0;
            rec2.X = rec1.Right;
            rec2.Y = rec0.Bottom - 42;
            rec2.Width = (rec0.Width) / 3;
            rec2.Height = 22;
            rec2.Inflate(-2, -2);
            Rectangle rec3 = rec0;
            rec3.X = rec2.Right;
            rec3.Y = rec0.Bottom - 42;
            rec3.Width = (rec0.Width) / 3;
            rec3.Height = 22;
            rec3.Inflate(-2, -2);
            Rectangle rec4 = rec0;
            rec4.X = rec0.Left + 2;
            rec4.Y = rec0.Bottom - 22;
            rec4.Width = (rec0.Width) / 3;
            rec4.Height = 22;
            rec4.Inflate(-2, -2);
            Rectangle rec5 = rec0;
            rec5.X = rec4.Right;
            rec5.Y = rec0.Bottom - 22;
            rec5.Width = (rec0.Width) / 3;
            rec5.Height = 22;
            rec5.Inflate(-2, -2);
            Rectangle rec6 = rec0;
            rec6.X = rec5.Right;
            rec6.Y = rec0.Bottom - 22;
            rec6.Width = (rec0.Width) / 3;
            rec6.Height = 22;
            rec6.Inflate(-2, -2);
            Bounds = rec0;
            ButtonBounds = rec1;
            ButtonBounds2 = rec2;
            ButtonBounds3 = rec3;
            ButtonBounds4 = rec4;
            ButtonBounds5 = rec5;
            ButtonBounds6 = rec6;
        }
        private Rectangle ButtonBounds { get; set; }
        private Rectangle ButtonBounds2 { get; set; }
        private Rectangle ButtonBounds3 { get; set; }
        private Rectangle ButtonBounds4 { get; set; }
        private Rectangle ButtonBounds5 { get; set; }
        private Rectangle ButtonBounds6 { get; set; }
        public override void ExpireLayout()
        {
            base.ExpireLayout();

            // Destroy any data you have that becomes
            // invalid when the layout expires.
        }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            // Render the parameter capsule and any additional text on top of it.
            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Blue, "X", 2, 0);
                button.Render(graphics, Selected, Owner.Locked, false);
                button.Dispose();
                GH_Capsule button2 = GH_Capsule.CreateTextCapsule(ButtonBounds2, ButtonBounds2, GH_Palette.Black, "Y", 2, 0);
                button2.Render(graphics, Selected, Owner.Locked, false);
                button2.Dispose();
                GH_Capsule button3 = GH_Capsule.CreateTextCapsule(ButtonBounds3, ButtonBounds3, GH_Palette.Pink, "Z", 2, 0);
                button3.Render(graphics, Selected, Owner.Locked, false);
                button3.Dispose();
                GH_Capsule button4 = GH_Capsule.CreateTextCapsule(ButtonBounds4, ButtonBounds4, GH_Palette.White, "u", 2, 0);
                button4.Render(graphics, Selected, Owner.Locked, false);
                button4.Dispose();
                GH_Capsule button5 = GH_Capsule.CreateTextCapsule(ButtonBounds5, ButtonBounds5, GH_Palette.Grey, "v", 2, 0);
                button5.Render(graphics, Selected, Owner.Locked, false);
                button5.Dispose();
                GH_Capsule button6 = GH_Capsule.CreateTextCapsule(ButtonBounds6, ButtonBounds6, GH_Palette.Error, "w", 2, 0);
                button6.Render(graphics, Selected, Owner.Locked, false);
                button6.Dispose();
                // Define the default palette.
                GH_Palette palette = GH_Palette.White;
                // Adjust palette based on the Owner's worst case messaging level.
                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        palette = GH_Palette.Warning;
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        palette = GH_Palette.Error;
                        break;
                }

                // Create a new Capsule without text or icon.
                GH_Capsule capsule = GH_Capsule.CreateCapsule(Bounds, palette);

                // Render the capsule using the current Selection, Locked and Hidden states.
                // Integer parameters are always hidden since they cannot be drawn in the viewport.
                capsule.Render(graphics, Selected, Owner.Locked, true);

                // Always dispose of a GH_Capsule when you're done with it.
                capsule.Dispose();
                capsule = null;

                // Now it's time to draw the text on top of the capsule.
                // First we'll draw the Owner NickName using a standard font and a black brush.
                // We'll also align the NickName in the center of the Bounds.
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;

                // Our entire capsule is 60 pixels high, and we'll draw
                // three lines of text, each 20 pixels high.
                RectangleF textRectangle = Bounds;
                textRectangle.Height = 20;

                // Draw the NickName in a Standard Grasshopper font.
                graphics.DrawString(Owner.NickName, GH_FontServer.Standard, Brushes.Black, textRectangle, format);


                // Now we need to draw the median and mean information.
                // Adjust the formatting and the layout rectangle.
                format.Alignment = StringAlignment.Near;
                textRectangle.Inflate(-5, 0);

                textRectangle.Y += 20;
                graphics.DrawString(String.Format("Median: {0}", "asd"),


                                    GH_FontServer.StandardItalic, Brushes.Yellow,


                                    textRectangle, format);

                textRectangle.Y += 20;
                graphics.DrawString(String.Format("Mean: {0:0.00}", "sadasd"),


                                    GH_FontServer.StandardItalic, Brushes.Red,


                                    textRectangle, format);

                // Always dispose of any GDI+ object that implement IDisposable.
                format.Dispose();
            }
        }
    }
    
    #endregion
}