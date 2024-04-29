using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using PInvokeCSharp;
using Rhino;
using Rhino.ApplicationSettings;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime;
using Robim.Mech;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Robim.Cloud
{
    public class CreateCl : GH_Component
    {

        /// <summary>
        /// Initializes a new instance of the CVision3d class.
        /// </summary>
        public CreateCl()
          : base("Create", "CC",
              "--",
              "Robim", "Vision")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Points", GH_ParamAccess.list);
            pManager.AddVectorParameter("Normals", "N", "Normals", GH_ParamAccess.list);
            pManager.AddColourParameter("Colors", "C", "Colors", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter((IGH_Param)new Param_Cloud(), "PointCloud", "C", "PointCloud", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            try
            {
                List<Point3d> point3dList = new List<Point3d>();
                List<Vector3d> vector3dList = new List<Vector3d>();
                List<Color> list = new List<Color>();
                DA.GetDataList<Point3d>(0, point3dList);
                DA.GetDataList<Vector3d>(1, vector3dList);
                DA.GetDataList<Color>(2, list);
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
                        return;
                    c.AddRange((IEnumerable<Point3d>)point3dList);
                }
                GH_Cloud data = new GH_Cloud(c);
                DA.SetData(0, (object)data);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.ToString());
            }
        }



        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources._3d;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6f5b34ed-3ceb-42f9-b116-46fbc91f3a4a"); }
        }
    }
}
