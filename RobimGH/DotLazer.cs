using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Runtime.InteropServices;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Robim.Grasshopper
{
    public class DotLazer : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public DotLazer()
          : base("Lazer", "ASpi",
              "Construct an Archimedean, or arithmetic, spiral given its radii and number of turns.",
              "Robim", "Util")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want 
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddIntegerParameter("LazerCount", "Count", "lazer measure result count.must >= 5", GH_ParamAccess.item, 5);
            pManager.AddTextParameter("LazerInfoFile", "File", "File folder to save lazer infomation", GH_ParamAccess.item, "");
            // If you want to change properties of certain parameters, 
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddGenericParameter("XYZABC", "Plane", "calculate result (euler)", GH_ParamAccess.list);

            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> result = new List<double>();
            string file = "";
            int count = 10;
            DA.GetData(0, ref count);
            DA.GetData(1, ref file);

            if (count <= 4)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Measure count must be bigger than  or equal to five");
                return;
            }

            if (!System.IO.Directory.Exists(file))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "file is not existed");
                return;
            }

            double[] N = new double[6];
            caliPointToPoint1(count, file, N);
            for(int i = 0;i<6;i++)
            {
                result.Add(N[i]);
            }

            DA.SetDataList(0, result);
        }
        

        [DllImport("Tools.dll")]
        public static extern bool caliPointToPoint1(int count, string file, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] double[] N);
        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconLaserCalibration;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c2c3d28b-c6e1-4344-8581-42a25ee1bbe8"); }
        }
    }
}
