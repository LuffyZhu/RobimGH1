using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SimpleTcp;

namespace Robim.Grasshopper
{
    public class RPlanningTargets : GH_Component
    {

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RPlanningTargets()
          : base("RPlanningTargets", "Nickname",
            "Description",
            "Robim", "Planning")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "Target", "T", "List of targets or toolpaths for the robot.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddParameter(new RobotSystemParameter(), "RSystem", "RS", "Robot system", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddTextParameter("JSONr", "JSONr", "JSON response", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

            pManager.AddParameter(new TargetParameter(), "Target", "T", "Target", GH_ParamAccess.item);
            //pManager.AddParameter(new TargetParameter(), "LTarget", "LT", "LTarget", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inTargets = new List<GH_Target>();
            DA.GetDataList(0, inTargets);

            var targets = inTargets.Select(t => t.Value).ToList();

            var tool = targets[0].Tool; //pt[0].Target.Tool;
            var speed = targets[0].Speed; //pt[0].Target.Speed;
            var zone = targets[0].Zone; //pt[0].Target.Zone;
            var external = targets[0].External; //pt[0].Target.External;
            var command = targets[0].Command; //pt[0].Target.Command;
            Mesh workpiece = null;
            Frame frame = null;

            GH_RobotSystem rSystem = new GH_RobotSystem();
            DA.GetData(1, ref rSystem);

            string response = null;
            DA.GetData(2, ref response);

            List<Target> finalTargets = new List<Target>();


            if (response != null && response.Length > 50)
            {
                RPlanning path = JsonConvert.DeserializeObject<RPlanning>(response);
                var segments = path.Path.Segments;

                List<string> modes = new List<string>();
                List<bool> ifPTP = new List<bool>();
                //List<string> robot = new List<string>();
                //List<Point3d> lPos = new List<Point3d>();
                List<int> indices = new List<int>();

                DataTree<double> quaternions = new DataTree<double>();

                int counter = 0;
                List<JointTarget> jTargets = new List<JointTarget>();
                List<CartesianTarget> cTargets = new List<CartesianTarget>();

                foreach (Segment s in segments)
                {
                    indices.Add(Convert.ToInt32(s.OriginalIndex));
                    var currMode = s.Mode.ToString();
                    modes.Add(currMode);
                    if (currMode == "Ptp")
                    {
                        var joints = s.Target.PtpTarget.JointTarget.Robot;

                        var stDegrees = joints;
                        var stRadians = (stDegrees.Select((x, i) => (rSystem.Value).DegreeToRadian(x, i, 0))).ToArray();
                        /*
                        if (rSystem.Value.Manufacturer == RobimRobots.Manufacturers.FANUC)
                        {
                            //stRadians = fixRadianFanuc(stRadians);
                            stRadians = DegreeToRadianFormula(stDegrees);
                        }*/

                        var target = new JointTarget(stRadians, tool, speed, zone, command, frame, workpiece, external);
                        jTargets.Add(target);

                        var strJoints = stRadians[0].ToString() + ", " + stRadians[1].ToString() + ", " + stRadians[2].ToString() + ", " + stRadians[3].ToString() + ", " + stRadians[4].ToString() + ", " + stRadians[5].ToString();

                        ifPTP.Add(true);
                    }
                    else
                    {
                        ifPTP.Add(false);

                        var x = s.Target.Pose.Position.X;
                        var y = s.Target.Pose.Position.Y;
                        var z = s.Target.Pose.Position.Z;
                        var point = new Point3d(x, y, z);

                        var qw = s.Target.Pose.Orientation.Quaternion.W;
                        var qx = s.Target.Pose.Orientation.Quaternion.X;
                        var qy = s.Target.Pose.Orientation.Quaternion.Y;
                        var qz = s.Target.Pose.Orientation.Quaternion.Z;

                        counter++;

                        var plane = PosTransform.Quaternion2Plane(x, y, z, qw, qx, qy, qz);

                        var lTarget = new CartesianTarget(plane, null, Motions.Linear, tool, speed, zone, command, frame, workpiece, external);
                        cTargets.Add(lTarget);
                    }

                }

                var wovenTargets = weaveTargets(ifPTP, jTargets, cTargets);
                finalTargets = wovenTargets;

            }


            DA.SetDataList(0, finalTargets);

        }

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
        public override Guid ComponentGuid => new Guid("c1996ef3-21d3-4a38-a5fb-552ea1e60497");

        IGH_Param[] target = new IGH_Param[1]
        {
            new TargetParameter() { Name = "Target", NickName = "T", Description = "Reference target", Optional = false }
        };

        double[] fixRadianFanuc(double[] jointsRadian)
        {
            List<double> jointsRadianF = new List<double> { };
            double j1_temp = 0;

            for (int i = 0; i < 6; ++i)
            {
                double radian = jointsRadian[i];
                if (i == 1)
                {
                    radian = 0.5 * Math.PI - radian;
                    j1_temp = radian;
                }
                if (i == 2)
                {
                    radian = radian - j1_temp;
                }
                if (i == 3)
                {
                    radian = -1 * radian;
                }
                if (i == 5)
                {
                    radian = Math.PI - radian;
                }
                jointsRadianF.Add(radian);
            }
            return jointsRadianF.ToArray();
        }

        public List<Target> weaveTargets(List<bool> pattern, List<JointTarget> jTargets, List<CartesianTarget> cTargets)
        {
            List<Target> allT = new List<Target>();
            for (int i = 0; i < pattern.Count; i++)
            {
                if (pattern[i] == true)
                {
                    allT.Add(jTargets[0]);
                    jTargets.RemoveAt(0);
                }
                else
                {
                    allT.Add(cTargets[0]);
                    cTargets.RemoveAt(0);
                }
            }

            return allT;
        }


        public double[] DegreeToRadianFormula(double[] degrees)
        {
            double[] radians = new double[degrees.Length];

            for (int i = 0; i < degrees.Length; i++)
            {
                var radian = degrees[i] * Math.PI / 180;
                radians[i] = radian;
            }
            return radians;
        }

    }
}