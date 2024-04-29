using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.CodeDom;
using System.Drawing;
using System.Linq;
using static System.Math;

namespace Robim.Grasshopper
{
    public sealed class CreateTarget : GH_Component, IGH_VariableParameterComponent
    {
        public CreateTarget() : base("Create target", "Target", "Creates or modifies a target. Right click for additional inputs", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("{BC68DC2C-EED6-4717-9F49-80A2B21B75B6}");
        protected override Bitmap Icon => Properties.Resources.iconCreateTarget;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            Params.RegisterInputParam(parameters[3]);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "Target", "T", "Target", GH_ParamAccess.item);
            //pManager[0].DataMapping = GH_DataMapping.Flatten;
        }
        protected override void BeforeSolveInstance()
        {

        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasTarget = Params.Input.Any(x => x.Name == "Target");
            bool hasJoints = Params.Input.Any(x => x.Name == "Joints");
            bool hasPlane = Params.Input.Any(x => x.Name == "Plane");
            bool hasConfig = Params.Input.Any(x => x.Name == "RobConf");
            bool hasMotion = Params.Input.Any(x => x.Name == "Motion");
            bool hasTool = Params.Input.Any(x => x.Name == "Tool");
            bool hasSpeed = Params.Input.Any(x => x.Name == "Speed");
            bool hasZone = Params.Input.Any(x => x.Name == "Zone");
            bool hasZoneType = Params.Input.Any(x => x.Name == "ZoneType");
            bool hasCommand = Params.Input.Any(x => x.Name == "Command");
            bool hasFrame = Params.Input.Any(x => x.Name == "Frame");
            bool hasExternal1 = Params.Input.Any(x => x.Name == "External 1");
            bool hasGeometry = Params.Input.Any(x => x.Name == "Geometry");
            bool hasWorkpiece = Params.Input.Any(x => x.Name == "Workpiece");

            GH_Target sourceTarget = null;
            if (hasTarget) if (!DA.GetData("Target", ref sourceTarget)) return;

            double[] joints = null;
            //TargetCurve targetcurve = null;
            var plane = new Plane();
            RobotConfigurations? configuration = null;
            Motions motion = Motions.Joint;
            Frame frame = null;
            Mesh workpiece = null;
            Tool tool = null;
            Speed speed = null;
            Zone zone = null;
            Command command = null;
            double[] external = null;

            //double[] external = new double[1];
            if (hasJoints)
            {
                GH_String jointsGH = null;
                if (!DA.GetData("Joints", ref jointsGH)) return;

                string[] jointsText = jointsGH.Value.Split(',');
                if (jointsText.Length != 6) return;

                joints = new double[6];

                for (int i = 0; i < 6; i++)
                    if (!GH_Convert.ToDouble_Secondary(jointsText[i], ref joints[i])) return;
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is JointTarget) joints = (sourceTarget.Value as JointTarget).Joints;
            }

            if (hasFrame)
            {
                GH_Frame frameGH = null;
                DA.GetData("Frame", ref frameGH);
                frame = frameGH?.Value;
                if (frame.IsCoupled)
                {
                    if (DigitalCoupledPlane.DCP.CustomPlane == Plane.WorldXY)//是否有自行输入耦合面
                    {
                        //RobotSystem.coupledplane = Positioner.coupledplane;
                        DigitalCoupledPlane.DCP.CustomPlane = DigitalCoupledPlane.DCP.P_CoupledPlane;
                    }
                    Plane plane1 = DigitalCoupledPlane.DCP.CustomPlane;
                    plane1.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.P_CoupledPlane, Plane.WorldXY));
                    frame.Plane = plane1;
                }
            }
            else if (sourceTarget != null)
            {
                frame = sourceTarget.Value.Frame;
            }

            if (hasPlane)
            {
                GH_Plane planeGH = null;
                if (!DA.GetData("Plane", ref planeGH)) return;
                plane = planeGH.Value;
                if (frame != null && frame.IsCoupled)
                {
                    plane.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, Plane.WorldXY));
                }
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is CartesianTarget) plane = (sourceTarget.Value as CartesianTarget).Plane;
            }

            if (hasGeometry)
            {
                GeometryBase geometryBase = null;
                DA.GetData("Geometry", ref geometryBase);
                if (geometryBase != null)
                {
                    geometryBase.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, Plane.WorldXY));
                    TargetGeometry.Set(geometryBase);
                }
            }

            if (hasConfig)
            {
                GH_Integer configGH = null;
                if (hasConfig) DA.GetData("RobConf", ref configGH);
                configuration = (configGH == null) ? null : (RobotConfigurations?)configGH.Value;
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is CartesianTarget) configuration = (sourceTarget.Value as CartesianTarget).Configuration;
            }

            if (hasMotion)
            {
                GH_String motionGH = null;
                DA.GetData("Motion", ref motionGH);
                motion = (motionGH == null) ? Motions.Joint : (Motions)Enum.Parse(typeof(Motions), motionGH.Value);
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is CartesianTarget) motion = (sourceTarget.Value as CartesianTarget).Motion;
            }

            if (hasTool)
            {
                GH_Tool toolGH = null;
                DA.GetData("Tool", ref toolGH);
                tool = toolGH?.Value;
            }
            else if (sourceTarget != null)
            {
                tool = sourceTarget.Value.Tool;
            }

            if (hasSpeed)
            {
                GH_Speed speedGH = null;
                DA.GetData("Speed", ref speedGH);
                speed = speedGH?.Value;
            }
            else if (sourceTarget != null)
            {
                speed = sourceTarget.Value.Speed;
            }

            if (hasZone)
            {
                GH_Zone zoneGH = null;
                DA.GetData("Zone", ref zoneGH);
                zone = zoneGH?.Value;
            }
            else if (sourceTarget != null)
            {
                zone = sourceTarget.Value.Zone;
            }

            if (hasZoneType)
            {
                GH_String zonetypeGH = null;
                DA.GetData("ZoneType", ref zonetypeGH);
                zone.Type = zonetypeGH?.Value;
            }

            if (hasCommand)
            {
                GH_Command commandGH = null;
                DA.GetData("Command", ref commandGH);
                command = commandGH?.Value;
            }
            else if (sourceTarget != null)
            {
                command = sourceTarget.Value.Command;
            }

            if (hasExternal1)
            {
                int firstexternal = Params.IndexOfInputParam("External 1");//if index is 2 => number is 3
                int lastexternal = Params.Input.Count;//if number is 5 (E3) => index is 4
                int externalnumber = lastexternal - firstexternal;//number 5 - index 2 = 3(has 3 External)
                GH_String[] externalGH = new GH_String[externalnumber];
                external = new double[externalnumber];
                for (int i = 0; i < externalnumber; i++)
                {
                    DA.GetData($"External {i + 1}", ref externalGH[i]);
                    if (externalGH[i] == null)
                        externalGH[i] = new GH_String("0");
                    if (!GH_Convert.ToDouble_Secondary(externalGH[i], ref external[i]))
                    {
                        return;
                    }
                }
            }
            else if (sourceTarget != null)
            {
                external = sourceTarget.Value.External;
            }
            if (hasWorkpiece)
            {
                GH_Mesh meshGH = null;
                DA.GetData("Workpiece", ref meshGH);
                workpiece = meshGH?.Value;
            }
            else if (sourceTarget != null)
            {
                workpiece = sourceTarget.Value.Workpiece;
            }

            Target target;

            bool localCartesian = isCartesian;

            if (hasTarget && !hasPlane && !hasJoints)
                localCartesian = sourceTarget.Value is CartesianTarget;

            if (localCartesian)
                target = new CartesianTarget(plane, configuration, motion, tool, speed, zone, command, frame, workpiece, external);
            else
                target = new JointTarget(joints, tool, speed, zone, command, frame, workpiece, external);

            if (sourceTarget != null)
                target.ExternalCustom = sourceTarget.Value.ExternalCustom;

            DA.SetData(0, target);
        }

        // Variable inputs
        IGH_Param[] parameters = new IGH_Param[14]
        {
         new TargetParameter() { Name = "Target", NickName = "T", Description = "Reference target", Optional = false },
         new Param_String() { Name = "Joints", NickName = "J", Description = "Joint rotations in radians", Optional = false },
         new TargetCurveParameter() { Name = "TargetCurve", NickName = "T(C)", Description = "Target curve(KUKA)", Optional = false },
         new Param_Plane() { Name = "Plane", NickName = "P", Description = "Target plane", Optional = false },
         new Param_Integer() { Name = "RobConf", NickName = "Cf", Description = "Robot configuration", Optional = true },
         new Param_String() { Name = "Motion", NickName = "M", Description = "Type of motion", Optional = true },
         new ToolParameter() { Name = "Tool", NickName = "T", Description = "Tool or end effector", Optional = true },
         new SpeedParameter() { Name = "Speed", NickName = "S", Description = "Speed of robot in mm/s", Optional = true },
         new ZoneParameter() { Name = "Zone", NickName = "Z", Description = "Aproximation zone in mm \n Preset:DIS & movel", Optional = true },
         new Param_String() { Name = "ZoneType", NickName = "Z.T", Description = "Type of Zone\n KUKA : DIS & VEL\n UR : movel & movep", Optional = true },
         new CommandParameter() { Name = "Command", NickName = "C", Description = "Robot command", Optional = true },
         new FrameParameter() { Name = "Frame", NickName = "F", Description = "Base frame", Optional = true },
         new Param_Mesh(){ Name = "Workpiece",NickName="W", Description = "Workpiece mesh", Optional = true },
         new Param_String() { Name = "External 1", NickName = "E1", Description = "External axis 1", Optional = true }
        };

        bool isCartesian = true;
        int addExternalInputCount = 0;

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("IsCartesian", isCartesian);
            writer.SetInt32("ExternalInputCount", addExternalInputCount);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            isCartesian = reader.GetBoolean("IsCartesian");
            reader.TryGetInt32("ExternalInputCount", ref addExternalInputCount);
            return base.Read(reader);
        }

        // Menu items
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Target input", AddTarget, true, Params.Input.Any(x => x.Name == "Target"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Joint target", SwitchCartesianEvent, true, !isCartesian);
            Menu_AppendItem(menu, "Joint input", AddJoints, !isCartesian, Params.Input.Any(x => x.Name == "Joints"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Cartesian target", SwitchCartesianEvent, true, isCartesian);
            Menu_AppendItem(menu, "Plane input", AddPlane, isCartesian, Params.Input.Any(x => x.Name == "Plane"));
            Menu_AppendItem(menu, "Configuration input", AddConfig, isCartesian, Params.Input.Any(x => x.Name == "RobConf"));
            Menu_AppendItem(menu, "Motion input", AddMotion, isCartesian, Params.Input.Any(x => x.Name == "Motion"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Tool input", AddTool, true, Params.Input.Any(x => x.Name == "Tool"));
            Menu_AppendItem(menu, "Speed input", AddSpeed, true, Params.Input.Any(x => x.Name == "Speed"));
            Menu_AppendItem(menu, "Zone input", AddZone, true, Params.Input.Any(x => x.Name == "Zone"));
            //Menu_AppendItem(menu, "Zone type input", AddZoneType, true, Params.Input.Any(x => x.Name == "ZoneType"));
            Menu_AppendItem(menu, "Command input", AddCommand, true, Params.Input.Any(x => x.Name == "Command"));
            Menu_AppendItem(menu, "Frame input", AddFrame, true, Params.Input.Any(x => x.Name == "Frame"));
            Menu_AppendItem(menu, "Workpiece input", AddWorkpiece, true, Params.Input.Any(x => x.Name == "Workpiece"));
            Menu_AppendItem(menu, "External input", AddExternal1, true, Params.Input.Any(x => x.Name.Contains("External")));
        }

        #region Varible methods
        private void SwitchCartesian()
        {
            if (isCartesian)
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Plane"), true);
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "RobConf"), true);
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Motion"), true);
                AddParam(1);
                isCartesian = false;
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Joints"), true);
                AddParam(2);
                isCartesian = true;
            }

            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        private void SwitchCartesianEvent(object sender, EventArgs e) => SwitchCartesian();
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
        private void AddTarget(object sender, EventArgs e) => AddParam(0);
        private void AddJoints(object sender, EventArgs e) => AddParam(1);
        private void AddPlane(object sender, EventArgs e) => AddParam(3);
        private void AddConfig(object sender, EventArgs e)
        {
            AddParam(4);
            var parameter = parameters[4];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var configParm = new ConfigParam();
                configParm.CreateAttributes();
                configParm.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 110, parameter.Attributes.InputGrip.Y - 33);
                configParm.ListItems.Clear();
                configParm.ListMode = GH_ValueListMode.CheckList;
                configParm.ListItems.Add(new GH_ValueListItem("Shoulder", "1"));
                configParm.ListItems.Add(new GH_ValueListItem("Elbow", "2"));
                configParm.ListItems.Add(new GH_ValueListItem("Wrist", "4"));
                Instances.ActiveCanvas.Document.AddObject(configParm, false);
                parameter.AddSource(configParm);
                parameter.CollectData();

                ExpireSolution(true);
            }
        }
        private void AddMotion(object sender, EventArgs e)
        {
            AddParam(5);
            var parameter = parameters[5];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_ValueList();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 130, parameter.Attributes.InputGrip.Y - 11);
                valueList.ListItems.Clear();
                valueList.ListItems.Add(new GH_ValueListItem("Joint", "\"Joint\""));
                valueList.ListItems.Add(new GH_ValueListItem("Linear", "\"Linear\""));
                valueList.ListItems.Add(new GH_ValueListItem("Arc", "\"Arc\""));
                //valueList.ListItems.Add(new GH_ValueListItem("Plane", "\"Plane\""));
                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
        }
        private void AddTool(object sender, EventArgs e) => AddParam(6);
        private void AddSpeed(object sender, EventArgs e) => AddParam(7);
        private void AddZone(object sender, EventArgs e) => AddParam(8);
        private void AddZoneType(object sender, EventArgs e) => AddParam(9);
        private void AddCommand(object sender, EventArgs e) => AddParam(10);
        private void AddFrame(object sender, EventArgs e) => AddParam(11);
        private void AddWorkpiece(object sender, EventArgs e) => AddParam(12);
        private void AddExternal1(object sender, EventArgs e)
        {
            int index = 13;
            var parameter = parameters[index];
            if (Params.Input.Any(x => x.Name.Contains("External")))
            {
                for (int i = 0; i < addExternalInputCount; i++)
                {
                    Params.UnregisterInputParameter(Params.Input.First(x => x.Name.Contains("External")), true);
                }
                addExternalInputCount = 0;
            }
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
                addExternalInputCount++;
                parameter.CollectData();
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        #endregion

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index)
        {
            int externalindex = Params.IndexOfInputParam("External 1");
            if (side == GH_ParameterSide.Input && addExternalInputCount > 0 && index == externalindex + addExternalInputCount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            int externalindex = Params.IndexOfInputParam("External 1");
            if (side == GH_ParameterSide.Input && addExternalInputCount > 1 && index == externalindex + addExternalInputCount - 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
        {
            addExternalInputCount++;
            Param_String param = new Param_String() { Name = $"External {addExternalInputCount}", NickName = $"E{addExternalInputCount}", Description = $"External axis {addExternalInputCount}", Optional = true };
            param.MutableNickName = false;
            return param;
        }
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            addExternalInputCount--;
            return true;
        }
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
    public sealed class CreateTargetList : GH_Component, IGH_VariableParameterComponent
    {
        public CreateTargetList() : base("Create target (For TargetCurve)", "Target(C)", "Creates or modifies a target(For TargetCurve). Right click for additional inputs", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("{95516025-EB5B-4C31-B8C3-D08AD6AF4876}");
        protected override Bitmap Icon => Properties.Resources.iconCurveTarget;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            Params.RegisterInputParam(parameters[2]);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "TargetList", "T", "Target as list for targetcurve", GH_ParamAccess.list);
            //pManager[0].DataMapping = GH_DataMapping.Flatten;
        }
        protected override void BeforeSolveInstance()
        {

        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasTarget = Params.Input.Any(x => x.Name == "Target");
            bool hasJoints = Params.Input.Any(x => x.Name == "Joints");
            bool hasTargetCurve = Params.Input.Any(x => x.Name == "TargetCurve");
            bool hasPlane = Params.Input.Any(x => x.Name == "Plane");
            bool hasConfig = Params.Input.Any(x => x.Name == "RobConf");
            bool hasMotion = Params.Input.Any(x => x.Name == "Motion");
            bool hasTool = Params.Input.Any(x => x.Name == "Tool");
            bool hasSpeed = Params.Input.Any(x => x.Name == "Speed");
            bool hasZone = Params.Input.Any(x => x.Name == "Zone");
            bool hasZoneType = Params.Input.Any(x => x.Name == "ZoneType");
            bool hasCommand = Params.Input.Any(x => x.Name == "Command");
            bool hasFrame = Params.Input.Any(x => x.Name == "Frame");
            bool hasExternal1 = Params.Input.Any(x => x.Name == "External 1");
            bool hasGeometry = Params.Input.Any(x => x.Name == "Geometry");
            bool hasWorkpiece = Params.Input.Any(x => x.Name == "Workpiece");

            GH_Target sourceTarget = null;
            if (hasTarget) if (!DA.GetData("Target", ref sourceTarget)) return;

            double[] joints = null;
            TargetCurve targetcurve = null;
            var plane = new Plane();
            RobotConfigurations? configuration = null;
            Motions motion = Motions.Joint;
            Frame frame = null;
            Mesh workpiece = null;
            Tool tool = null;
            Speed speed = null;
            Zone zone = null;
            Command command = null;
            double[] external = null;

            //double[] external = new double[1];
            if (hasJoints)
            {
                GH_String jointsGH = null;
                if (!DA.GetData("Joints", ref jointsGH)) return;

                string[] jointsText = jointsGH.Value.Split(',');
                if (jointsText.Length != 6) return;

                joints = new double[6];

                for (int i = 0; i < 6; i++)
                    if (!GH_Convert.ToDouble_Secondary(jointsText[i], ref joints[i])) return;
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is JointTarget) joints = (sourceTarget.Value as JointTarget).Joints;
            }

            if (hasFrame)
            {
                GH_Frame frameGH = null;
                DA.GetData("Frame", ref frameGH);
                frame = frameGH?.Value;
                if (frame.IsCoupled)
                {
                    if (DigitalCoupledPlane.DCP.CustomPlane == Plane.WorldXY)//是否有自行输入耦合面
                    {
                        //RobotSystem.coupledplane = Positioner.coupledplane;
                        DigitalCoupledPlane.DCP.CustomPlane = DigitalCoupledPlane.DCP.P_CoupledPlane;
                    }
                    Plane plane1 = DigitalCoupledPlane.DCP.CustomPlane;
                    plane1.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.P_CoupledPlane, Plane.WorldXY));
                    frame.Plane = plane1;
                }
            }
            else if (sourceTarget != null)
            {
                frame = sourceTarget.Value.Frame;
            }

            if (hasPlane)
            {
                GH_Plane planeGH = null;
                if (!DA.GetData("Plane", ref planeGH)) return;
                plane = planeGH.Value;
                if (frame != null && frame.IsCoupled)
                {
                    plane.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, Plane.WorldXY));
                }
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is CartesianTarget) plane = (sourceTarget.Value as CartesianTarget).Plane;
            }

            if (hasGeometry)
            {
                GeometryBase geometryBase = null;
                DA.GetData("Geometry", ref geometryBase);
                if (geometryBase != null)
                {
                    geometryBase.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, Plane.WorldXY));
                    TargetGeometry.Set(geometryBase);
                }
            }

            if (hasConfig)
            {
                GH_Integer configGH = null;
                if (hasConfig) DA.GetData("RobConf", ref configGH);
                configuration = (configGH == null) ? null : (RobotConfigurations?)configGH.Value;
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is CartesianTarget) configuration = (sourceTarget.Value as CartesianTarget).Configuration;
            }

            if (hasMotion)
            {
                GH_String motionGH = null;
                DA.GetData("Motion", ref motionGH);
                motion = (motionGH == null) ? Motions.Joint : (Motions)Enum.Parse(typeof(Motions), motionGH.Value);
            }
            else if (sourceTarget != null)
            {
                if (sourceTarget.Value is CartesianTarget) motion = (sourceTarget.Value as CartesianTarget).Motion;
            }

            if (hasTool)
            {
                GH_Tool toolGH = null;
                DA.GetData("Tool", ref toolGH);
                tool = toolGH?.Value;
            }
            else if (sourceTarget != null)
            {
                tool = sourceTarget.Value.Tool;
            }

            if (hasSpeed)
            {
                GH_Speed speedGH = null;
                DA.GetData("Speed", ref speedGH);
                speed = speedGH?.Value;
            }
            else if (sourceTarget != null)
            {
                speed = sourceTarget.Value.Speed;
            }

            if (hasZone)
            {
                GH_Zone zoneGH = null;
                DA.GetData("Zone", ref zoneGH);
                zone = zoneGH?.Value;
            }
            else if (sourceTarget != null)
            {
                zone = sourceTarget.Value.Zone;
            }

            if (hasZoneType)
            {
                GH_String zonetypeGH = null;
                DA.GetData("ZoneType", ref zonetypeGH);
                zone.Type = zonetypeGH?.Value;
            }

            if (hasCommand)
            {
                GH_Command commandGH = null;
                DA.GetData("Command", ref commandGH);
                command = commandGH?.Value;
            }
            else if (sourceTarget != null)
            {
                command = sourceTarget.Value.Command;
            }

            if (hasExternal1)
            {
                int firstexternal = Params.IndexOfInputParam("External 1");//if index is 2 => number is 3
                int lastexternal = Params.Input.Count;//if number is 5 (E3) => index is 4
                int externalnumber = lastexternal - firstexternal;//number 5 - index 2 = 3(has 3 External)
                GH_String[] externalGH = new GH_String[externalnumber];
                external = new double[externalnumber];
                for (int i = 0; i < externalnumber; i++)
                {
                    DA.GetData($"External {i + 1}", ref externalGH[i]);
                    if (externalGH[i] == null)
                        externalGH[i] = new GH_String("0");
                    if (!GH_Convert.ToDouble_Secondary(externalGH[i], ref external[i]))
                    {
                        return;
                    }
                }
            }
            else if (sourceTarget != null)
            {
                external = sourceTarget.Value.External;
            }
            if (hasWorkpiece)
            {
                GH_Mesh meshGH = null;
                DA.GetData("Workpiece", ref meshGH);
                workpiece = meshGH?.Value;
            }
            else if (sourceTarget != null)
            {
                workpiece = sourceTarget.Value.Workpiece;
            }

            Target[] Targetsarr = null;
            if (hasTargetCurve)
            {
                GH_TargetCurve targetcurveGH = null;
                if (!DA.GetData("TargetCurve", ref targetcurveGH)) return;
                targetcurve = targetcurveGH.Value;
                Command command_addend = null;
                motion = Motions.Joint;
                Target targetfirst = new CartesianTarget(targetcurve.PlaneofStartPoint, configuration, motion, tool, speed, zone, command_addend, frame, workpiece, external);

                Array.Resize(ref Targetsarr, targetcurve.Pattern.Length + 1);
                Targetsarr[0] = targetfirst;

                for (int i = 0, j = 0, k = 0; i < targetcurve.Pattern.Length; i++)
                {
                    Plane plane1 = new Plane();
                    if (i == targetcurve.Pattern.Length - 1)//command add at the position after end point of linear curve
                    {
                        command_addend = command;
                    }
                    switch (targetcurve.Pattern[i])
                    {
                        case 0:
                            motion = Motions.Linear;
                            plane1 = targetcurve.PlanesofLines[j];
                            j += 1;
                            break;
                        case 1:
                            motion = Motions.Arc;
                            plane1 = targetcurve.PlanesofArcs[k];
                            k += 1;
                            break;
                    }
                    Target target1 = new CartesianTarget(plane1, configuration, motion, tool, speed, zone, command_addend, frame, workpiece, external);
                    Targetsarr[i + 1] = target1;
                }
                DA.SetDataList(0, Targetsarr);
            }
        }

        // Variable inputs
        IGH_Param[] parameters = new IGH_Param[14]
        {
         new TargetParameter() { Name = "Target", NickName = "T", Description = "Reference target", Optional = false },
         new Param_String() { Name = "Joints", NickName = "J", Description = "Joint rotations in radians", Optional = false },
         new TargetCurveParameter() { Name = "TargetCurve", NickName = "T(C)", Description = "Target curve(KUKA)", Optional = false },
         new Param_Plane() { Name = "Plane", NickName = "P", Description = "Target plane", Optional = false },
         new Param_Integer() { Name = "RobConf", NickName = "Cf", Description = "Robot configuration", Optional = true },
         new Param_String() { Name = "Motion", NickName = "M", Description = "Type of motion", Optional = true },
         new ToolParameter() { Name = "Tool", NickName = "T", Description = "Tool or end effector", Optional = true },
         new SpeedParameter() { Name = "Speed", NickName = "S", Description = "Speed of robot in mm/s", Optional = true },
         new ZoneParameter() { Name = "Zone", NickName = "Z", Description = "Aproximation zone in mm \n Preset:DIS & movel", Optional = true },
         new Param_String() { Name = "ZoneType", NickName = "Z.T", Description = "Type of Zone\n KUKA : DIS & VEL\n UR : movel & movep", Optional = true },
         new CommandParameter() { Name = "Command", NickName = "C", Description = "Robot command", Optional = true },
         new FrameParameter() { Name = "Frame", NickName = "F", Description = "Base frame", Optional = true },
         new Param_Mesh(){ Name = "Workpiece",NickName="W", Description = "Workpiece mesh", Optional = true },
         new Param_String() { Name = "External 1", NickName = "E1", Description = "External axis 1", Optional = true }
        };

        bool isCartesian = true;
        int addExternalInputCount = 0;

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("IsCartesian", isCartesian);
            writer.SetInt32("ExternalInputCount", addExternalInputCount);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            isCartesian = reader.GetBoolean("IsCartesian");
            reader.TryGetInt32("ExternalInputCount", ref addExternalInputCount);
            return base.Read(reader);
        }

        // Menu items
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Target input", AddTarget, true, Params.Input.Any(x => x.Name == "Target"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "TargetCurve input", AddTargetCurve, true, Params.Input.Any(x => x.Name == "TargetCurve"));
            Menu_AppendItem(menu, "Configuration input", AddConfig, true, Params.Input.Any(x => x.Name == "RobConf"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Tool input", AddTool, true, Params.Input.Any(x => x.Name == "Tool"));
            Menu_AppendItem(menu, "Speed input", AddSpeed, true, Params.Input.Any(x => x.Name == "Speed"));
            Menu_AppendItem(menu, "Zone input", AddZone, true, Params.Input.Any(x => x.Name == "Zone"));
            //Menu_AppendItem(menu, "Zone type input", AddZoneType, true, Params.Input.Any(x => x.Name == "ZoneType"));
            Menu_AppendItem(menu, "Command input", AddCommand, true, Params.Input.Any(x => x.Name == "Command"));
            Menu_AppendItem(menu, "Frame input", AddFrame, true, Params.Input.Any(x => x.Name == "Frame"));
            Menu_AppendItem(menu, "Workpiece input", AddWorkpiece, true, Params.Input.Any(x => x.Name == "Workpiece"));
            Menu_AppendItem(menu, "External input", AddExternal1, true, Params.Input.Any(x => x.Name.Contains("External")));
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
        private void AddTarget(object sender, EventArgs e) => AddParam(0);
        private void AddTargetCurve(object sender, EventArgs e) => AddParam(2);
        private void AddConfig(object sender, EventArgs e)
        {
            AddParam(4);
            var parameter = parameters[4];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var configParm = new ConfigParam();
                configParm.CreateAttributes();
                configParm.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 110, parameter.Attributes.InputGrip.Y - 33);
                configParm.ListItems.Clear();
                configParm.ListMode = GH_ValueListMode.CheckList;
                configParm.ListItems.Add(new GH_ValueListItem("Shoulder", "1"));
                configParm.ListItems.Add(new GH_ValueListItem("Elbow", "2"));
                configParm.ListItems.Add(new GH_ValueListItem("Wrist", "4"));
                Instances.ActiveCanvas.Document.AddObject(configParm, false);
                parameter.AddSource(configParm);
                parameter.CollectData();

                ExpireSolution(true);
            }
        }
        private void AddTool(object sender, EventArgs e) => AddParam(6);
        private void AddSpeed(object sender, EventArgs e) => AddParam(7);
        private void AddZone(object sender, EventArgs e) => AddParam(8);
        private void AddZoneType(object sender, EventArgs e)
        {
            AddParam(9);
            var parameter = parameters[9];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var valueList = new GH_ValueList();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 130, parameter.Attributes.InputGrip.Y - 11);
                valueList.ListItems.Clear();
                valueList.ListItems.Add(new GH_ValueListItem("DIS", "\"DIS\""));
                valueList.ListItems.Add(new GH_ValueListItem("VEL", "\"VEL\""));
                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
                ExpireSolution(true);
            }
        }
        private void AddCommand(object sender, EventArgs e) => AddParam(10);
        private void AddFrame(object sender, EventArgs e) => AddParam(11);
        private void AddWorkpiece(object sender, EventArgs e) => AddParam(12);
        private void AddExternal1(object sender, EventArgs e)
        {
            int index = 13;
            var parameter = parameters[index];
            if (Params.Input.Any(x => x.Name.Contains("External")))
            {
                for (int i = 0; i < addExternalInputCount; i++)
                {
                    Params.UnregisterInputParameter(Params.Input.First(x => x.Name.Contains("External")), true);
                }
                addExternalInputCount = 0;
            }
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
                addExternalInputCount++;
                parameter.CollectData();
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        #endregion

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index)
        {
            int externalindex = Params.IndexOfInputParam("External 1");
            if (side == GH_ParameterSide.Input && addExternalInputCount > 0 && index == externalindex + addExternalInputCount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            int externalindex = Params.IndexOfInputParam("External 1");
            if (side == GH_ParameterSide.Input && addExternalInputCount > 1 && index == externalindex + addExternalInputCount - 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
        {
            addExternalInputCount++;
            Param_String param = new Param_String() { Name = $"External {addExternalInputCount}", NickName = $"E{addExternalInputCount}", Description = $"External axis {addExternalInputCount}", Optional = true };
            param.MutableNickName = false;
            return param;
        }
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            addExternalInputCount--;
            return true;
        }
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
    public sealed class DeconstructTarget : GH_Component, IGH_VariableParameterComponent
    {
        public DeconstructTarget() : base("Deconstruct target", "DeTarget", "Deconstructs a target. Right click for additional outputs", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("{3252D880-59F9-4C9A-8A92-A6CD4C0BA591}");
        protected override Bitmap Icon => Properties.Resources.iconDeconstructTarget;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "Target", "T", "Target", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            Params.RegisterOutputParam(parameters[0]);
            Params.RegisterOutputParam(parameters[1]);
        }
        int addExternalOutputCount = 0;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Target target = null;
            if (!DA.GetData("Target", ref target)) return;
            bool isCartesian = target.Value is CartesianTarget;
            /*
            bool hasPlane = false;
            bool hasConfig = false;
            bool hasMotion = false;
            bool hasTool = false;

            
            if (isCartesian)
            {
                CartesianTarget ct = target.Value as CartesianTarget;
                if (ct.Plane != null)
                {
                    hasPlane = true;
                }
                if (ct.Configuration != null)
                {
                    hasConfig = true;
                }
                if (ct.Motion != null)
                {
                    hasMotion = true;
                }
            }
            if (target.Value.Tool != null)
            {
                hasTool = true;
            }*/

            // if (isTargetCartesian != isCartesian) SwitchCartesian();

            bool hasJoints = Params.Output.Any(x => x.Name == "Joints");
            bool hasPlane = Params.Output.Any(x => x.Name == "Plane");
            bool hasConfig = Params.Output.Any(x => x.Name == "RobConf");
            bool hasMotion = Params.Output.Any(x => x.Name == "Motion");
            bool hasTool = Params.Output.Any(x => x.Name == "Tool");
            bool hasSpeed = Params.Output.Any(x => x.Name == "Speed");
            bool hasZone = Params.Output.Any(x => x.Name == "Zone");
            bool hasCommand = Params.Output.Any(x => x.Name == "Command");
            bool hasFrame = Params.Output.Any(x => x.Name == "Frame");
            bool hasExternal = Params.Output.Any(x => x.Name == "External 1");

            string[] externalvalues = target.Value.External.Select(x => $"{x:0.000}").ToArray();
            //if (hasExternal) DA.SetData("External1", new GH_String(string.Join(",", target.Value.External.Select(x => $"{x:0.000}"))));
            addExternalOutputCount = externalvalues.Length;
            if (hasExternal)
            {
                DA.SetData("External 1", new GH_String(externalvalues[0]));
                for (int i = 2; i <= addExternalOutputCount; i++)
                {
                    if (!Params.Output.Any(x => x.Name == $"External {i}"))
                    {
                        Param_String param = new Param_String() { Name = $"External {i}", NickName = $"E{i}", Description = $"External axis {i}", Optional = true };
                        //param.MutableNickName = false;
                        int insertIndex = parameters.Length - 1;
                        Params.RegisterOutputParam(param, insertIndex);
                        Params.OnParametersChanged();
                        ExpireSolution(true);
                        return;
                    }
                    DA.SetData($"External {i}", new GH_String(externalvalues[i - 1]));
                }
            }

            if (hasJoints) DA.SetData("Joints", isCartesian ? null : new GH_String(string.Join(",", (target.Value as JointTarget).Joints.Select(x => $"{x:0.000}"))));
            if (hasPlane) DA.SetData("Plane", isCartesian ? new GH_Plane((target.Value as CartesianTarget).Plane) : null);
            if (hasConfig) DA.SetData("RobConf", isCartesian ? (target.Value as CartesianTarget).Configuration == null ? null : new GH_Integer((int)(target.Value as CartesianTarget).Configuration) : null);
            if (hasMotion) DA.SetData("Motion", isCartesian ? new GH_String((target.Value as CartesianTarget).Motion.ToString()) : null);
            if (hasTool && (target.Value.Tool != null)) DA.SetData("Tool", new GH_Tool(target.Value.Tool));
            if (hasSpeed && (target.Value.Speed != null)) DA.SetData("Speed", new GH_Speed(target.Value.Speed));
            if (hasZone && (target.Value.Zone != null)) DA.SetData("Zone", new GH_Zone(target.Value.Zone));
            if (hasCommand) DA.SetData("Command", new GH_Command(target.Value.Command));
            if (hasFrame) DA.SetData("Frame", new GH_Frame(target.Value.Frame));
            //string[] externalvalues = target.Value.External.Select(x => $"{x:0.000}").ToArray();
            ////if (hasExternal) DA.SetData("External1", new GH_String(string.Join(",", target.Value.External.Select(x => $"{x:0.000}"))));
            //addExternalOutputCount = externalvalues.Length;
            //if (hasExternal)
            //{
            //    DA.SetData("External 1", new GH_String(externalvalues[0]));
            //    for (int i = 2; i <= addExternalOutputCount; i++)
            //    {
            //        if(!Params.Output.Any(x => x.Name == $"External {i}")){
            //            Param_String param = new Param_String() { Name = $"External {i}", NickName = $"E{i}", Description = $"External axis {i}", Optional = true };
            //            //param.MutableNickName = false;
            //            int insertIndex = parameters.Length - 1;
            //            Params.RegisterOutputParam(param, insertIndex);
            //            Params.OnParametersChanged();
            //        }
            //        DA.SetData($"External {i}", new GH_String(externalvalues[i - 1]));
            //    }
            //}
        }

        // Variable outputs

        //bool isCartesian = false;

        IGH_Param[] parameters = new IGH_Param[10]
{
         new Param_String() { Name = "Joints", NickName = "J", Description = "Joint rotations in radians", Optional = false },
         new Param_Plane() { Name = "Plane", NickName = "P", Description = "Target plane", Optional = false },
         new Param_Integer() { Name = "RobConf", NickName = "Cf", Description = "Robot configuration", Optional = true },
         new Param_String() { Name = "Motion", NickName = "M", Description = "Type of motion", Optional = true },
         new ToolParameter() { Name = "Tool", NickName = "T", Description = "Tool or end effector", Optional = true },
         new SpeedParameter() { Name = "Speed", NickName = "S", Description = "Speed of robot in mm/s", Optional = true },
         new ZoneParameter() { Name = "Zone", NickName = "Z", Description = "Approximation zone in mm", Optional = true },
         new CommandParameter() { Name = "Command", NickName = "C", Description = "Robot command", Optional = true },
         new FrameParameter() { Name = "Frame", NickName = "F", Description = "Base frame", Optional = true },
         new Param_String() { Name = "External 1", NickName = "E1", Description = "External axes 1", Optional = true }
};

        // Menu items

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Joints output", AddJoints, true, Params.Output.Any(x => x.Name == "Joints"));
            Menu_AppendItem(menu, "Plane output", AddPlane, true, Params.Output.Any(x => x.Name == "Plane"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Config output", AddConfig, true, Params.Output.Any(x => x.Name == "RobConf"));
            Menu_AppendItem(menu, "Motion output", AddMotion, true, Params.Output.Any(x => x.Name == "Motion"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Tool output", AddTool, true, Params.Output.Any(x => x.Name == "Tool"));
            Menu_AppendItem(menu, "Speed output", AddSpeed, true, Params.Output.Any(x => x.Name == "Speed"));
            Menu_AppendItem(menu, "Zone output", AddZone, true, Params.Output.Any(x => x.Name == "Zone"));
            Menu_AppendItem(menu, "Command output", AddCommand, true, Params.Output.Any(x => x.Name == "Command"));
            Menu_AppendItem(menu, "Frame output", AddFrame, true, Params.Output.Any(x => x.Name == "Frame"));
            Menu_AppendItem(menu, "External output", AddExternal, true, Params.Output.Any(x => x.Name == "External 1"));
        }

        private void AddParam(int index)
        {
            IGH_Param parameter = parameters[index];

            if (Params.Output.Any(x => x.Name == parameter.Name))
                Params.UnregisterOutputParameter(Params.Output.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Output.Count;
                for (int i = 0; i < Params.Output.Count; i++)
                {
                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Output[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                Params.RegisterOutputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        private void AddJoints(object sender, EventArgs e) => AddParam(0);
        private void AddPlane(object sender, EventArgs e) => AddParam(1);
        private void AddConfig(object sender, EventArgs e) => AddParam(2);
        private void AddMotion(object sender, EventArgs e) => AddParam(3);
        private void AddTool(object sender, EventArgs e) => AddParam(4);
        private void AddSpeed(object sender, EventArgs e) => AddParam(5);
        private void AddZone(object sender, EventArgs e) => AddParam(6);
        private void AddCommand(object sender, EventArgs e) => AddParam(7);
        private void AddFrame(object sender, EventArgs e) => AddParam(8);
        private void AddExternal(object sender, EventArgs e)
        {
            IGH_Param parameter = parameters[9];
            if (Params.Output.Any(x => x.Name.Contains("External")))
            {
                for (int i = 0; i < addExternalOutputCount; i++)
                {
                    Params.UnregisterOutputParameter(Params.Output.First(x => x.Name.Contains("External")), true);
                }
                addExternalOutputCount = 0;
            }
            else
            {
                int insertIndex = Params.Output.Count;
                for (int i = 0; i < Params.Output.Count; i++)
                {
                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Output[i].Name);
                    if (otherIndex > 9)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                Params.RegisterOutputParam(parameter, insertIndex);
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

    public class ConfigParam : GH_ValueList
    {
        public override string Name => "Flag fields";
        public override string Description => "Modified value list parameter for flag fields";
        public override Guid ComponentGuid => new Guid("{0381B555-BF9C-4D68-8E5C-10B2FCB16F30}");
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override void OnVolatileDataCollected()
        {
            int config = 0;
            if (VolatileDataCount > 0)
            {
                var values = VolatileData.get_Branch(0);

                foreach (var value in values)
                    if (value is GH_Integer)
                        config += (value as GH_Integer).Value;
            }

            VolatileData.Clear();
            AddVolatileData(new GH_Path(0), 0, new GH_Integer(config));
        }
    }

    public class CreateFrame : GH_Component, IGH_VariableParameterComponent
    {
        public CreateFrame() : base("Create frame", "Frame", "Creates a frame or work plane.", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("{467237C8-08F5-4104-A553-8814AACAFE51}");
        protected override Bitmap Icon => Properties.Resources.iconFrame;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Frame plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddIntegerParameter("Coupled mechanical group", "G", "Index of the mechanical group where the coupled mechanism or robot belongs, or -1 for no coupling.", GH_ParamAccess.item, -1);
            pManager.AddIntegerParameter("Coupled mechanism", "M", "Index of kinematically coupled mechanism or -1 for coupling of a robot in a multi robot cell. If input G is -1 this has no effect.\nWhen external has platform and track,use platform coupled plane is 1,and use track coupled plane is 0", GH_ParamAccess.item, -1);
            pManager.AddTextParameter("Name", "N", "Optional name for the frame.", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("PRnum", "PR", "FANUC PR number", GH_ParamAccess.item, 1);
            pManager[3].Optional = true;
            //pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new FrameParameter(), "Frame", "F", "Frame", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasFanucPR = Params.Input.Any(x => x.Name == "FanucPR");

            GH_Plane plane = null;
            int coupledGroup = -1;
            int coupledMechanism = -1;
            string name = null;
            ProcessRegister pr = null;
            GH_PR sourcePR = null;

            if (!DA.GetData(0, ref plane)) { return; }
            if (!DA.GetData(1, ref coupledGroup)) { return; }
            if (!DA.GetData(2, ref coupledMechanism)) { return; }
            DA.GetData(3, ref name);

            if (hasFanucPR)
            {
                GH_PR myPR = null;
                if (hasFanucPR) DA.GetData("FanucPR", ref myPR);
                pr = myPR?.Value;
            }
            else if (sourcePR != null)
            {
                pr = sourcePR.Value;
            }

            var frame = new Frame(plane.Value, coupledMechanism, coupledGroup, name, pr);
            DA.SetData(0, new GH_Frame(frame));
        }

        IGH_Param[] parameters = new IGH_Param[1]
        {
         new PRparameter() { Name = "FanucPR", NickName = "PR", Description = " Processor Register", Optional = true },
        };

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Fanuc Processor Register", AddFnuacPR, true, Params.Input.Any(x => x.Name == "FanucPR"));
        }
        private void AddFnuacPR(object sender, EventArgs e)
        {
            AddParam(0);
            var parameter = parameters[0];
            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                var param = new CreatePRparam();
                param.CreateAttributes();
                param.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 70, parameter.Attributes.InputGrip.Y);
                param.Description = "this is the value for PR";
                Instances.ActiveCanvas.Document.AddObject(param, false);
                //parameter.AddSource((IGH_Param)param.Params.Output);
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

    public class CreateSpeed : GH_Component
    {
        public CreateSpeed() : base("Create speed", "Speed", "Creates a target speed.", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("{BD11418C-74E1-4B13-BE1A-AF105906E1BC}");
        protected override Bitmap Icon => Properties.Resources.iconSpeed;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Translation", "T", "TCP translation speed", GH_ParamAccess.item, 100.0);
            pManager.AddNumberParameter("Rotation", "R", "TCP rotation and swivel speed", GH_ParamAccess.item, PI);
            pManager.AddNumberParameter("External translation", "Et", "External axes translation speed", GH_ParamAccess.item, 1000.0);
            pManager.AddNumberParameter("External rotation", "Er", "External axes rotation speed", GH_ParamAccess.item, PI * 6);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new SpeedParameter(), "Speed", "S", "Speed instance", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double translationSpeed = 0, rotationSpeed = 0, translationExternal = 0, rotationExternal = 0;

            if (!DA.GetData(0, ref translationSpeed)) { return; }
            if (!DA.GetData(1, ref rotationSpeed)) { return; }
            if (!DA.GetData(2, ref translationExternal)) { return; }
            if (!DA.GetData(3, ref rotationExternal)) { return; }

            var speed = new Speed(translationSpeed, rotationSpeed, translationExternal, rotationExternal);
            DA.SetData(0, new GH_Speed(speed));
        }
    }
    public class CreateZone : GH_Component
    {
        public CreateZone() : base("Create zone", "Zone", "Creates a target zone.", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("{622D113C-1399-4B02-B50D-E4B84E572E89}");
        protected override Bitmap Icon => Properties.Resources.iconCreateZone;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of zone", GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "T", "Zone type:VEL's value is percentage,Maxmum is 100", GH_ParamAccess.item);
            pManager.AddNumberParameter("Value", "V", "Zone Value\nVEL maxmum value is 100", GH_ParamAccess.item, 100);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ZoneParameter(), "Zone", "Z", "Zone instance", GH_ParamAccess.item);
        }
        protected override void BeforeSolveInstance()
        {
            if(Params.Input[1].Sources.Count == 0)
            {
                var valueList = new GH_ValueList();
                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 100, this.Attributes.Pivot.Y - 11);
                valueList.ListItems.Clear();
                valueList.ListItems.Add(new GH_ValueListItem("DIS", "\"DIS\""));
                valueList.ListItems.Add(new GH_ValueListItem("VEL", "\"VEL\""));
                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                Params.Input[1].AddSource(valueList);
                Params.Input[1].CollectData();
                ExpireSolution(true);
            }
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = null;
            string type = "";
            double value = 0;

            DA.GetData(0, ref name);
            DA.GetData(1, ref type);
            DA.GetData(2, ref value);

            if(value < 0)
            {
                value = 0;
            }
            else if(type == "VEL" && value > 100)
            {
                value = 100;
            }
            var zone = new Zone(type, value, name: name);
            DA.SetData(0, new GH_Zone(zone));
        }
    }
}