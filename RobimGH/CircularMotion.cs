using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Robim.Grasshopper;
using Curve = Rhino.Geometry.Curve;
using System.Windows.Forms;


namespace Robim
{
    public class CircularMotion : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the CircularMotion class.
        /// </summary>
        public CircularMotion()
          : base("Curve Custom", "Curve Custom",
              "Split curve to lines and arcs.\nIf one segment is a polyline it will transfer to line.",
              "Robim", "Util")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "The curve you want to use circular motion", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Variable", "V", "Use this curve's tangent vector to create variable planes", GH_ParamAccess.item, false);
            pManager.AddPlaneParameter("StandardPlane", "P", "The standard plane to transfer points into planes", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddTextParameter("Tolerance", "T", "The tolerance. This is the maximum deviation from arc midpoints to the curve.", GH_ParamAccess.item, "1");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Curves of lines and arcs", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Curve First Plane", "FirstP", "Curve first point,and transfer to plane by standard plane", GH_ParamAccess.item);
            pManager.AddLineParameter("Lines", "L", "Lines in this curve", GH_ParamAccess.list);
            pManager.AddArcParameter("Arcs", "A", "Arcs in this curve", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Lines Planes", "P(L)", "End points in these lines,and transfer to planes by standard plane", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Arcs Planes", "P(A)", "Mid points and End poins in these arcs,and transfer to planes by standard plane", GH_ParamAccess.list);
            pManager.AddTextParameter("PatternA", "PatA", "Pattern of lines and arcs ,line is 0 ,acr is 1 1", GH_ParamAccess.list);
            pManager.AddTextParameter("PatternB", "PatB", "Pattern of lines and arcs ,line is 0 ,acr is 1", GH_ParamAccess.list);
            pManager.AddParameter(new TargetCurveParameter(), "Target Curve", "T", "This is the target curve by input.", GH_ParamAccess.item);
        }

        IGH_Param[] parameters = new IGH_Param[6]
        {
            new Param_String() { Name = "Rotate by XAxis", NickName = "RotateX", Description = "Input angle to rotate plane by X-axis(degree)", Optional = true },
            new Param_String() { Name = "Rotate by YAxis", NickName = "RotateY", Description = "Input angle to rotate plane by Y-axis(degree)", Optional = true },
            new Param_String() { Name = "Rotate by ZAxis", NickName = "RotateZ", Description = "Input angle to rotate plane by Z-axis(degree)", Optional = true },
            new Param_String() { Name = "AngleTolerance", NickName = "T(A)", Description = "The angle tolerance in radians. This is the maximum deviation of the arc end directions from the curve direction.", Optional = false },
            new Param_String() { Name = "MinLength", NickName = "MinL", Description = "The minimum segment length.", Optional = true },
            new Param_String() { Name = "MaxLength", NickName = "MaxL", Description = "The maximum segment length.", Optional = true }
        };
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "XAxis Rotate", AddX, true, Params.Input.Any(x => x.Name == "Rotate by XAxis"));
            Menu_AppendItem(menu, "YAxis Rotate", AddY, true, Params.Input.Any(x => x.Name == "Rotate by YAxis"));
            Menu_AppendItem(menu, "ZAxis Rotate", AddZ, true, Params.Input.Any(x => x.Name == "Rotate by ZAxis"));
            Menu_AppendItem(menu, "AngleTolerance", AddTA, true, Params.Input.Any(x => x.Name == "AngleTolerance"));
            Menu_AppendItem(menu, "MinLength", AddMinL, true, Params.Input.Any(x => x.Name == "MinLength"));
            Menu_AppendItem(menu, "MaxLength", AddMaxL, true, Params.Input.Any(x => x.Name == "MaxLength"));
        }

        #region Varible methods
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
        private void AddX(object sender, EventArgs e) => AddParam(0);
        private void AddY(object sender, EventArgs e) => AddParam(1);
        private void AddZ(object sender, EventArgs e) => AddParam(2);
        private void AddTA(object sender, EventArgs e) => AddParam(3);
        private void AddMinL(object sender, EventArgs e) => AddParam(4);
        private void AddMaxL(object sender, EventArgs e) => AddParam(5);
        #endregion

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasX = Params.Input.Any(x => x.Name == "Rotate by XAxis");
            bool hasY = Params.Input.Any(x => x.Name == "Rotate by YAxis");
            bool hasZ = Params.Input.Any(x => x.Name == "Rotate by ZAxis");
            bool hasTA = Params.Input.Any(x => x.Name == "AngleTolerance");
            bool hasMinL = Params.Input.Any(x => x.Name == "MinLength");
            bool hasMaxL = Params.Input.Any(x => x.Name == "MaxLength");
            Curve curve = null;
            bool variable = false;
            Plane plane = new Plane();
            string torlerance = null;
            string RotateX = null;
            string RotateY = null;
            string RotateZ = null;
            string angleTorlerance = null;
            string minlength = null;
            string maxlength = null;

            if (!DA.GetData(0,ref curve)) return;
            DA.GetData(1, ref variable);
            DA.GetData(2, ref plane);
            DA.GetData(3, ref torlerance);

            if (hasX)
                DA.GetData("Rotate by XAxis", ref RotateX);
            if (hasY)
                DA.GetData("Rotate by YAxis", ref RotateY);
            if (hasZ)
                DA.GetData("Rotate by ZAxis", ref RotateZ);
            if (hasTA)
                DA.GetData("AngleTolerance", ref angleTorlerance);
            if (hasMinL)
                DA.GetData("MinLength", ref minlength);
            if (hasMaxL)
                DA.GetData("MaxLength", ref maxlength);

            double Xangle = Convert.ToDouble(RotateX);
            double Yangle = Convert.ToDouble(RotateY);
            double Zangle = Convert.ToDouble(RotateZ);

            double tor = Convert.ToDouble(torlerance);
            double ator = Convert.ToDouble(angleTorlerance);//default:0
            double min = Convert.ToDouble(minlength);//default:0
            double max = Convert.ToDouble(maxlength);//default:0

            CurveSetting curveSetting = new CurveSetting(plane, variable, Xangle, Yangle, Zangle);
            //TargetCurve.SplitCurve(curve, curveSetting, plane, tor, ator, min, max, out TargetCurve circularTransfer);
            //TargetCurve[] circularTransfers;
            TargetCurve targetCurve1;
            if (variable)
                TargetCurve.SplitCurve_V(curve, curveSetting, tor, ator, min, max, out targetCurve1);
            else
                TargetCurve.SplitCurve_S(curve, curveSetting, tor, ator, min, max, out targetCurve1);

            
            
            DA.SetDataList(0, targetCurve1.Curves);
            DA.SetData(1, targetCurve1.PlaneofStartPoint);
            DA.SetDataList(2, targetCurve1.Lines);
            DA.SetDataList(3, targetCurve1.Arcs);
            DA.SetDataList(4, targetCurve1.PlanesofLines);
            DA.SetDataList(5, targetCurve1.PlanesofArcs);
            DA.SetDataList(6, targetCurve1.Pattern);
            DA.SetDataList(7, targetCurve1.PatternB);
            DA.SetData(8, targetCurve1);

            #region list
            //DataTree<Curve> curveslist = new DataTree<Curve>();
            //DataTree<Line> curveslineslist = new DataTree<Line>();
            //DataTree<Arc> curvesarclist = new DataTree<Arc>();
            //DataTree<Plane> lineplanelist = new DataTree<Plane>();
            //DataTree<Plane> arcplanelist = new DataTree<Plane>();
            //DataTree<int> patternlist = new DataTree<int>();
            //DataTree<int> patternlistB = new DataTree<int>();
            //for (int i = 0; i < circularTransfers.Length; i++)
            //{
            //    TargetCurve targetCurve = circularTransfers[i];
            //    for(int a = 0; a < targetCurve.Curves.Length; a++)
            //    {
            //        GH_Path gH_Path = new GH_Path(i,a);
            //        curveslist.Add(targetCurve.Curves[a],gH_Path);
            //    }
            //    for(int b = 0; b < targetCurve.Lines.Length; b++)
            //    {
            //        GH_Path gH_Path = new GH_Path(i, b);
            //        curveslineslist.Add(targetCurve.Lines[b],gH_Path);
            //    }
            //    for (int c = 0; c < targetCurve.Arcs.Length; c++)
            //    {
            //        GH_Path gH_Path = new GH_Path(i, c);
            //        curvesarclist.Add(targetCurve.Arcs[c], gH_Path);
            //    }
            //    for (int d = 0; d < targetCurve.PlanesofLines.Length; d++)
            //    {
            //        GH_Path gH_Path = new GH_Path(i, d);
            //        lineplanelist.Add(targetCurve.PlanesofLines[d], gH_Path);
            //    }
            //    for (int e = 0; e < targetCurve.PlanesofArcs.Length; e++)
            //    {
            //        GH_Path gH_Path = new GH_Path(i, e);
            //        arcplanelist.Add(targetCurve.PlanesofArcs[e], gH_Path);
            //    }
            //    for (int f = 0; f < targetCurve.Pattern.Length; f++)
            //    {
            //        GH_Path gH_Path = new GH_Path(i, f);
            //        patternlist.Add(targetCurve.Pattern[f], gH_Path);
            //    }
            //    for (int f = 0; f < targetCurve.PatternB.Length; f++)
            //    {
            //        GH_Path gH_Path = new GH_Path(i, f);
            //        patternlistB.Add(targetCurve.PatternB[f], gH_Path);
            //    }
            //}

            //DA.SetDataTree(0, curveslist);
            //DA.SetData(1, circularTransfers[0].PlaneofStartPoint);
            //DA.SetDataTree(2, curveslineslist);
            //DA.SetDataTree(3, curvesarclist);
            //DA.SetDataTree(4, lineplanelist);
            //DA.SetDataTree(5, arcplanelist);
            //DA.SetDataTree(6, patternlist);
            //DA.SetDataTree(7, patternlistB);
            //DA.SetDataList(8, circularTransfers);
            #endregion
        }
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.iconCurveCustom;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ceb56d8a-6f3c-442c-86ac-628073a95471"); }
        }
    }
    public class PTPSort : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CircularMotion class.
        /// </summary>
        public PTPSort()
          : base("PTP Sort", "PTP Sort",
              "PTP target sort out.",
              "Robim", "Util")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "Target", "T", "Target List Input", GH_ParamAccess.list);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "Target", "T", "Target", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_Target> gH_Targets = new List<GH_Target>();
            List<GH_Target> gH_TargetsOut = new List<GH_Target>();
            DA.GetDataList(0, gH_Targets);
            gH_TargetsOut.Add(gH_Targets[0]);
            for (int i = 1; i < gH_Targets.Count; i++)
            {
                CartesianTarget target = gH_Targets[i].Value as CartesianTarget;//这个
                double[] targetA = RobotCellKuka.PlaneToEuler(target.Plane);
                if (target.Motion == Motions.Linear || target.Motion == Motions.Joint)
                {
                    CartesianTarget target2 = gH_Targets[i - 1].Value as CartesianTarget;//上个
                    double[] targetB = RobotCellKuka.PlaneToEuler(target2.Plane);
                    int same = 0;
                    //只判断XYZ
                    for (int j = 0; j < 3; j++)
                    {
                        if (targetA[j].ToString("F2") == targetB[j].ToString("F2"))//点重复，不加进输出
                        {
                            same += 1;
                        }
                    }
                    if (same != 3)//点不重复，改成LIN后，加进输出
                    {
                        target.Motion = Motions.Linear;
                        gH_TargetsOut.Add(new GH_Target(target));
                    }
                }
                else
                {
                    gH_TargetsOut.Add(gH_Targets[i]);
                }
            }
            DA.SetDataList(0, gH_TargetsOut);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.iconPTPSort;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{919658F3-57F6-4E15-9ED9-625A534C55FC}"); }
        }
    }
}