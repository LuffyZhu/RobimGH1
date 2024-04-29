using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;

using Robim.Mech;
using Robim.Cloud;
using System.Collections;

using PInvokeCSharp;
using System.Linq;
using Rhino.Collections;
using System.Windows.Documents;

namespace Robim
{
    public class CVision3d : GH_Component
    {
        bool mem = false;

        /// <summary>
        /// Initializes a new instance of the CVision3d class.
        /// </summary>
        public CVision3d()
          : base("RobimCV3d", "CV",
              "Connect to Mech 3D camera and obtain scan mesh",
              "Robim", "Vision")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("IP Adress", "IP", "Camera IP", GH_ParamAccess.item);
            pManager[0].Optional = true;

            pManager.AddBooleanParameter("Capture", "C", "Toggle to capture", GH_ParamAccess.item);
            pManager[1].Optional = true;

            //pManager.AddIntegerParameter("Points", "NP", "Amount of points to show", GH_ParamAccess.item);
            //pManager[2].Optional = true;

            // pManager.AddIntervalParameter("Dx", "Dx", "Cutoff domain in X direction", GH_ParamAccess.item);
            // pManager[3].Optional = true;

            // pManager.AddIntervalParameter("Dy", "Dy", "Cutoff domain in Y direction", GH_ParamAccess.item);
            // pManager[4].Optional = true;
            pManager.AddBooleanParameter("Mesh", "M", "Meshify captured pointcloud", GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddBooleanParameter("PlaneCl", "PC", "Spli the pointcloud according to planes fitting (RANSAC)", GH_ParamAccess.item);
            pManager[3].Optional = true;

            //pManager.AddPointParameter("TestCloud", "TC", "TCloud", GH_ParamAccess.list);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            /* 0 */pManager.AddTextParameter("Info", "I", "Camera info", GH_ParamAccess.item);

            //pManager.AddPointParameter("Points", "P", "Point cloud", GH_ParamAccess.colors);

            //pManager.AddNumberParameter("X", "X", "X", GH_ParamAccess.colors);

            //pManager.AddNumberParameter("Y", "Y", "Y", GH_ParamAccess.colors);

            //pManager.AddNumberParameter("Z", "Z", "Z", GH_ParamAccess.colors);

            /* 1 */pManager.AddParameter((IGH_Param)new Param_Cloud(), "PC", "PC", "Rhino PointCloud preview", GH_ParamAccess.item);
            /* 2 */pManager.AddMeshParameter("Mesh", "M", "Meshes built from pointcloud", GH_ParamAccess.list);

            pManager.AddParameter((IGH_Param)new Param_Cloud(), "PlaneClouds", "PC", "", GH_ParamAccess.list);
            // 3

            pManager.AddPointParameter("Points", "P", "Point cloud", GH_ParamAccess.list);
            // 4
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Interval xreg = new Interval();
            //Interval yreg = new Interval();
            //DA.GetData(3, ref xreg);
            //DA.GetData(4, ref yreg);

            //=======================================================================================================
            bool meshify = false;
            DA.GetData(2, ref meshify);

            bool ransacify = false;
            DA.GetData(3, ref ransacify);
            //=======================================================================================================
            // TEST CLOUD METHODS

            List<Mesh> outmeshes = new List<Mesh>();

            List<Point3d> incloud0 = new List<Point3d>();

            //DA.GetDataList(4, incloud0);

            List<Vector3d> cvecs = new List<Vector3d>();
            List<Color> ccolors = new List<Color>();

            var inc = CloudCreate(incloud0, cvecs, ccolors, DA);

            if (ransacify)
            {

            }
            if (meshify)
            {
                //outmeshes.Add(CloudMesh(incloud));
            }
            

            //=======================================================================================================

            bool cap = false;
            string info = null;
            var pts = new List<Point3d>();
            var xs = new List<double>();
            var ys = new List<double>();
            var zs = new List<double>();

            var pcl = new PointCloud();

            int npt = 2000;
            string ip = "";

            //DA.GetData(2, ref npt);
            DA.GetData(0, ref ip);
            DA.GetData(1, ref cap);


            if (ip!="" && cap!=false)
            {
                CameraClient camera = new CameraClient();
                        //camera ip should be modified to actual ip address
                        //always set ip before doing anything else
                camera.connect(ip);
                info = "Camera ID: " + camera.getCameraId() + " Version: " + camera.getCameraVersion();
                DA.SetData(0, info);

                    //double[,] rel = camera.captureRGBCloud(); //point cloud mesh in xyzrgb3
                var ptsies = camera.captureRGBCloud(npt);//, xreg, yreg);
                //xs = ptsies.xs;
                //ys = ptsies.ys;
                //zs = ptsies.zs;
                DA.SetDataList(4, ptsies.pts);
                pcl = ptsies.pcl;
            }

            List<Vector3d> vecs = new List<Vector3d>();
            List<Color> colors = new List<Color>(); 

            //GH_Cloud outcloud = CloudCreate(pts, vecs, colors, DA);

            //outcloud = incloud; // TEMP!!!!!!!!!!!!!

            
            //DA.SetDataList(2, xs);
            //DA.SetDataList(3, ys);
            //DA.SetDataList(4, zs);
            //DA.SetData(1, incloud);
            //DA.SetDataList(2, outmeshes);
        }


        //====================================================================================================================
        // CLOUD CREATE  ===========================================

                                                                               // normals
        private PointCloud CloudCreate(List<Point3d> point3dList, List<Vector3d> vector3dList, List<Color> list, IGH_DataAccess DA)
        {
            PointCloud outcloud = new PointCloud();
            try
            {
                PointCloud c = new PointCloud();
                if (point3dList.Count == vector3dList.Count && point3dList.Count == list.Count)
                    c.AddRange((IEnumerable<Point3d>)point3dList, (IEnumerable<Vector3d>)vector3dList, (IEnumerable<Color>)list);
                else if (point3dList.Count == vector3dList.Count)
                    c.AddRange((IEnumerable<Point3d>)point3dList, (IEnumerable<Vector3d>)vector3dList);
                else if (point3dList.Count == list.Count)
                {
                    c.AddRange((IEnumerable<Point3d>)point3dList, (IEnumerable<Color>)list);
                }
                else
                {
                    if (point3dList.Count <= 0)
                        return c;
                    c.AddRange((IEnumerable<Point3d>)point3dList);
                }
                GH_Cloud data = new GH_Cloud(c);
                DA.SetData(1, (object)data);

                outcloud = c;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.ToString());
            }

            return outcloud;
        }


        // CLOUD NORMALS  ===========================================

        private GH_Cloud CloudNormals(List<Point3d> point3dList, List<Vector3d> vector3dList, List<Color> list)
        {
            GH_Cloud data = new GH_Cloud();
            try
            {
                PointCloud c = new PointCloud();
                if (point3dList.Count == vector3dList.Count && point3dList.Count == list.Count)
                    c.AddRange((IEnumerable<Point3d>)point3dList, (IEnumerable<Vector3d>)vector3dList, (IEnumerable<Color>)list);
                else if (point3dList.Count == vector3dList.Count)
                    c.AddRange((IEnumerable<Point3d>)point3dList, (IEnumerable<Vector3d>)vector3dList);
                else if (point3dList.Count == list.Count)
                {
                    c.AddRange((IEnumerable<Point3d>)point3dList, (IEnumerable<Color>)list);
                }
                else
                {
                    if (point3dList.Count <= 0)
                        return null;
                    c.AddRange((IEnumerable<Point3d>)point3dList);
                }
                data = new GH_Cloud(c);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.ToString());
            }

            return data;
        }

        // CLOUD DOWNSAMPLE  ===========================================

        private GH_Cloud CloudDownsample(GH_Cloud cloud, int numpts)
        {
            int numberOfPoints = Math.Min(cloud.Value.Count, Math.Max(0, numpts));
            var data = new GH_Cloud(DownsampleUniform(cloud.Value, numberOfPoints));

            return data;
        }

        private PointCloud DownsampleUniform(PointCloud cloud, int numberOfPoints)
        {
            int nth = Math.Max(1, (int)((double)cloud.Count * (1.0 / (double)numberOfPoints * 1.0)));
            PointCloud pointCloud = new PointCloud();
            int length = 0;
            for (int index = 0; index < cloud.Count; index += nth)
                ++length;
            Point3d[] points = new Point3d[length];
            Vector3d[] normals = new Vector3d[length];
            Color[] colors = new Color[length];
            Parallel.For(0, cloud.Count / nth, (Action<int>)(k =>
            {
                PointCloudItem pointCloudItem = cloud[nth * k];
                points[k] = pointCloudItem.Location;
                normals[k] = pointCloudItem.Normal;
                colors[k] = pointCloudItem.Color;
            }));
            pointCloud.AddRange((IEnumerable<Point3d>)points, (IEnumerable<Vector3d>)normals, (IEnumerable<Color>)colors);
            return pointCloud;
        }

        // CLOUD RANSAC  ===========================================

        private List<GH_Cloud> CloudRansac(GH_Cloud cloud, double radius, double neighbours, double iterations)
        {
            List <GH_Cloud> outclouds = new List < GH_Cloud >();
            try
            {
                //double radius = 0.1;
                //double neighbours = 10.0;
                //double iterations = 10.0;
                bool inliers = true;

                PointCloud[] pointCloudArray = TestOpen3D.RANSACPlane(cloud.Value, radius, neighbours, iterations, inliers);
                GH_Cloud[] data = new GH_Cloud[pointCloudArray.Length];
                for (int index = 0; index < pointCloudArray.Length; ++index)
                    data[index] = new GH_Cloud(pointCloudArray[index]);
                outclouds = data.ToList();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.ToString());
            }

            return outclouds;
        }

        // CLOUD MESH  ===========================================


        private Mesh CloudMesh(GH_Cloud incloud)
        {
            Mesh mesh = new Mesh();
            try
            {
                int max = 8;
                int min = 0;
                double scale = 1.1000000238418579;
                bool linear = false;
                bool colors = false;
                bool o3d = true;

                if (o3d)
                {
                    Tuple<Mesh, double[]> tuple = TestOpen3D.Poisson(incloud.Value, (ulong)max, (ulong)min, (float)scale, linear);
                    mesh = tuple.Item1;
                    if (!colors)
                        mesh.VertexColors.Clear();

                }
                else { }
                    //DA.SetData(0, (object)TestCGAL.CreatePoissonSurfaceReconstruction(incloud.Value));
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.ToString());
            }


            return mesh;
        }


        // CLOUD SECTION  ===========================================

        public PointCloud SectionCloud(PointCloud cloud, List<Plane> planes, double tol = 0.001, bool project = false)
        {
            bool[] flags = new bool[cloud.Count];
            int[] cID = new int[cloud.Count];
            int count = 1;
            foreach (Plane plane in planes)
            {
                double[] eq = plane.GetPlaneEquation();
                double denom = 1.0 / Math.Sqrt(eq[0] * eq[0] + eq[1] * eq[1] + eq[2] * eq[2]);
                Parallel.For(0, cloud.Count, (Action<int>)(i =>
                {
                    if (flags[i] || Math.Abs(this.FastPlaneToPt(denom, eq[0], eq[1], eq[2], eq[3], cloud[i].Location)) > tol)
                        return;
                    flags[i] = true;
                    cID[i] = count;
                }));
                count++;
            }
            int length = 0;
            List<int> intList = new List<int>();
            for (int index = 0; index < cloud.Count; ++index)
            {
                if (flags[index])
                {
                    intList.Add(index);
                    ++length;
                }
            }
            int[] idArray = intList.ToArray();
            Point3d[] points = new Point3d[length];
            Vector3d[] normals = new Vector3d[length];
            Color[] colors = new Color[length];
            Parallel.For(0, idArray.Length, (Action<int>)(i =>
            {
                PointCloudItem pointCloudItem = cloud[idArray[i]];
                points[i] = pointCloudItem.Location;
                normals[i] = pointCloudItem.Normal;
                colors[i] = pointCloudItem.Color;
            }));
            PointCloud pointCloud = new PointCloud();
            pointCloud.AddRange((IEnumerable<Point3d>)points, (IEnumerable<Vector3d>)normals, (IEnumerable<Color>)colors);
            if (project)
                pointCloud.Transform(Transform.PlanarProjection(planes[0]));
            return pointCloud;
        }


        // ===========================================
        public double FastPlaneToPt(
          double Denom,
          double a,
          double b,
          double c,
          double d,
          Point3d Pt)
        {
            return (a * Pt.X + b * Pt.Y + c * Pt.Z + d) * Denom;
        }


        //====================================================================================================================


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources._3d;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("099E01C1-5892-4EE1-9ED8-7B9396B3E85F"); }
        }
    }
}