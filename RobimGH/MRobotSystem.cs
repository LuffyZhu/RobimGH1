using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Robim.Grasshopper
{
    public class MRobotSystem : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MRobotSystem class.
        /// </summary>
        public MRobotSystem()
          : base("Load robot system", "Load robot", "Loads a robot system either from the library or from a custom file", "Robim", "Components")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // 0 
            pManager.AddTextParameter("RobotName", "RN", "Name of the Robot", GH_ParamAccess.item);
            // 1 
            pManager.AddTextParameter("TrackName", "TN", "Name of the track", GH_ParamAccess.item);
            // 2 
            pManager.AddTextParameter("PlatformName", "PN", "Name of the platform", GH_ParamAccess.item);
            // 3 
            pManager.AddNumberParameter("EulerRobot", "ER", "Euler plane of robot base", GH_ParamAccess.list);
            // 4 
            pManager.AddNumberParameter("EulerTrack", "ET", "Euler plane of track base", GH_ParamAccess.list);
            // 5 
            pManager.AddNumberParameter("EulerPlatform", "EP", "Euler plane of platform base", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string[] externalextrasetting = { "Panel_ExternalValueTrack1", "False", "Panel_ExternalValuePlatform2", "False" };

            //Names
            string robotName_tmp = null;
            string trackName_tmp = null;
            string revolverName_tmp = null;
            //Euler angles
            string roboteulerangle = null;
            string trackeulerangle = null;
            string platformeulerangle = null;
            string coupledplaneeulerangle = null;
            //has Euler angles
            bool hasroboteuler = false;
            bool hastrackeuler = false;
            bool hasplatformeuler = false;
            bool hascoupledplaneeuler = false;
            //external extra setting
            List<string> externalextrasettinglist = new List<string>();
            //Track Hang Up side Down
            TrackHangUpSideDown trackHangUpSideDown = TrackHangUpSideDown.No;

            DA.GetData(0, ref robotName_tmp);
            DA.GetData(1, ref trackName_tmp);
            DA.GetData(2, ref revolverName_tmp);

            List<double> robotEulers = new List<double>();
            List<double> trackEulers = new List<double>();
            List<double> platformEulers = new List<double>();

            DA.GetDataList(3, robotEulers);
            DA.GetDataList(4, trackEulers);
            DA.GetDataList(5, platformEulers);


            if (trackName_tmp != null || revolverName_tmp != null)
            {
                if (trackName_tmp != null)
                {
                    externalextrasettinglist.Add("Panel_ExternalValueTrack1");
                    externalextrasettinglist.Add("False");
                    if (revolverName_tmp != null)
                    {
                        externalextrasettinglist.Add("Panel_ExternalValuePlatform2");
                        externalextrasettinglist.Add("False");
                    }
                }
                else if (revolverName_tmp != null)
                {
                    externalextrasettinglist.Add("Panel_ExternalValuePlatform2");
                    externalextrasettinglist.Add("False");
                }
            }

            if(robotEulers.Count == 6)
            {
                roboteulerangle = string.Join(",", robotEulers);
                hasroboteuler = true;
            }

            if (trackEulers.Count == 6)
            {
                trackeulerangle = string.Join(",", trackEulers);
                hastrackeuler = true;
            }

            if (platformEulers.Count == 6)
            {
                platformeulerangle = string.Join(",", platformEulers);
                hasplatformeuler = true;
            }

            string[] names = new string[3]
            {
                robotName_tmp,trackName_tmp,revolverName_tmp
            };

            string[] externals = new string[2]
            {
                trackName_tmp,revolverName_tmp
            };

            string[] eulerangles = new string[4]
            {
                roboteulerangle,trackeulerangle,platformeulerangle,coupledplaneeulerangle
            };
            bool[] haseulerangles = new bool[4]
            {
                hasroboteuler,hastrackeuler,hasplatformeuler,hascoupledplaneeuler
            };

            externalextrasetting = externalextrasettinglist.ToArray(); 

            var RFS = new RobimFormSystem(names, eulerangles, haseulerangles, externalextrasetting, trackHangUpSideDown);

            var robotSystem = RobotSystem.Load(this, robotName_tmp, RFS, trackName_tmp, revolverName_tmp);
            if (robotSystem != null)
                DA.SetData(0, new GH_RobotSystem(robotSystem));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
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
            get { return new Guid("B210988A-1BEE-405A-853E-235AD641ABB1"); }
        }
    }
}