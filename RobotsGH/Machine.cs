using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Documentation;
using Grasshopper.Kernel.Components;
using System.Threading.Tasks;
using Grasshopper.Kernel.Undo.Actions;

namespace Robots.Grasshopper
{
    public class LoadRobotSystem : GH_Component
    {
        GH_ValueList valueList_robot = null;
        IGH_Param parameter_robot = null;
        GH_ValueList valueList_external = null;
        IGH_Param parameter_external = null;
        bool hasTrack = false;
        bool hasRevolver = false;

        public LoadRobotSystem() : base("Load robot system", "Load robot", "Loads a robot system either from the library or from a custom file", "Robots", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{7722D7E3-98DE-49B5-9B1D-E0D1B938B4A7}");
        protected override Bitmap Icon => Properties.Resources.iconRobot;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of the Robot system", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Base", "P", "Base plane", GH_ParamAccess.item, Plane.WorldXY);
            parameter_robot = pManager[0];
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
        }




        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "External Axis: Track", AddTrack, true, Params.Input.Any(x => x.Name == "External Track"));

        }



        private void AddTrack(object sender, EventArgs e)
        {
            AddParam(0);
            hasTrack = true;
            parameter_external = parameters[0];
            if (Params.Input.Any(x => x.Name == parameter_external.Name))
            {
                if (valueList_external == null)
                {
                    if (parameter_external.Sources.Count == 0)
                    {
                        valueList_external = new GH_ValueList();
                    }
                    else
                    {
                        foreach (var source in parameter_external.Sources)
                        {
                            if (source is GH_ValueList) valueList_external = source as GH_ValueList;
                            return;
                        }
                    }

                    valueList_external.CreateAttributes();
                    valueList_external.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 300, this.Attributes.Pivot.Y);
                    //param.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 70, parameter.Attributes.InputGrip.Y);
                    valueList_external.ListItems.Clear();

                    var robotSystems = RobotSystem.ListTrackSystems();

                    foreach (string trackSystemName in robotSystems)
                    {
                        valueList_external.ListItems.Add(new GH_ValueListItem(trackSystemName, $"\"{trackSystemName}\""));
                    }

                    Instances.ActiveCanvas.Document.AddObject(valueList_external, false);
                    parameter_external.AddSource(valueList_external);
                    parameter_external.CollectData();
                    ExpireSolution(true);
                }
                /*
                var param = new CreatePRparam();
                param.CreateAttributes();
                param.Attributes.Pivot = new PointF(parameter_external.Attributes.InputGrip.X - 70, parameter_external.Attributes.InputGrip.Y);
                param.Description = "this is the value for PR";
                Instances.ActiveCanvas.Document.AddObject(param, false);
                //parameter.AddSource(param.Params.Output);
                parameter_external.CollectData();
                ExpireSolution(true);*/
            }
        }






        IGH_Param[] parameters = new IGH_Param[1]
{
         new Param_String() { Name = "External Track", NickName = "External Track", Description = "External Track", Optional = true }//修改
};

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
        }

        protected override void BeforeSolveInstance()
        {
            if (valueList_robot == null)
            { 
                if (parameter_robot.Sources.Count == 0)
                {
                    valueList_robot = new GH_ValueList();
                }
                else
                {
                    foreach (var source in parameter_robot.Sources)
                    {
                        if (source is GH_ValueList) valueList_robot = source as GH_ValueList;
                        return;
                    }
                }

                valueList_robot.CreateAttributes();
                valueList_robot.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 180, this.Attributes.Pivot.Y - 31);
                valueList_robot.ListItems.Clear();

                var robotSystems = RobotSystem.ListRobotSystems();

                foreach (string robotSystemName in robotSystems)
                {
                    valueList_robot.ListItems.Add(new GH_ValueListItem(robotSystemName, $"\"{robotSystemName}\""));
                }

                Instances.ActiveCanvas.Document.AddObject(valueList_robot, true);
                parameter_robot.AddSource(valueList_robot);
                parameter_robot.CollectData();
            }
            
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = null;
            GH_Plane basePlane = null;
            GH_String track = null;
            GH_String revolver = null;

            if (!DA.GetData(0, ref name)) { return; }
            if (!DA.GetData(1, ref basePlane)) { return; }
            if (hasTrack)
            {
                if (!DA.GetData("External Track", ref track)) { return; }
            }
                
            //DA.SetData(trackName, valueList_external.Name);

            // add license date time
            string StartTime = "2070/05/10 10:00:00";
            DateTime st = Convert.ToDateTime(StartTime);
            DateTime localtime = DateTime.Now.ToLocalTime();
            TimeSpan ts = st.Subtract(localtime);
            double seconds = ts.TotalSeconds;//24.
            string trackName_tmp = null;
            string revolverName_tmp = null;

            if (seconds <= 0)
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "License expired");
            else
            {
                if (track != null)
                    trackName_tmp = track.Value;
                if (revolver != null)
                    revolverName_tmp = revolver.Value;
                var robotSystem = RobotSystem.Load(name, basePlane.Value, trackName_tmp, revolverName_tmp); 
                DA.SetData(0, new GH_RobotSystem(robotSystem));
            }
            //var robotSystem = RobotSystem.Load(name, basePlane.Value); ;
            //DA.SetData(0, new GH_RobotSystem(robotSystem));

        }
    }

    public class LoadTool : GH_Component
    {
        GH_ValueList valueList = null;
        IGH_Param parameter = null;

        public LoadTool() : base("Load robot tool", "Load tool", "Loads a tool either from the library or from a custom file", "Robots", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{542aa5fd-4f02-4ee5-a2a0-02b0fac8777f}");
        protected override Bitmap Icon => Properties.Resources.iconLoadTool;


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of the Tool", GH_ParamAccess.item);
            parameter = pManager[0];
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ToolParameter(), "Tool", "T", "Tool", GH_ParamAccess.item);
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

                var tools = Tool.ListTools();

                foreach (string toolName in tools)
                {
                    valueList.ListItems.Add(new GH_ValueListItem(toolName, $"\"{toolName}\""));
                }

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = null;

            if (!DA.GetData(0, ref name)) { return; }

            var tool = Tool.Load(name); ;
            DA.SetData(0, new GH_Tool(tool));
        }

    }

    public class CreateTool : GH_Component, IGH_VariableParameterComponent
    {
        public CreateTool() : base("Create tool", "Tool", "Creates a tool or end effector.", "Robots", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{E59E634B-7AD5-4682-B2C1-F18B73AE05C6}");
        protected override Bitmap Icon => Properties.Resources.iconCreateTool;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Tool name", GH_ParamAccess.item, "DefaultGHTool");
            pManager.AddPlaneParameter("TCP", "P", "TCP plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddPlaneParameter("Calibration", "4", "Optional 4 point TCP calibration. Orient the tool in 4 different ways around the same point in space and input the 4 planes that correspond to the flange", GH_ParamAccess.list);
            pManager.AddNumberParameter("Weight", "W", "Tool weight in kg", GH_ParamAccess.item, 0.0);
            pManager.AddPointParameter("Centroid", "C", "Optional tool center of mass", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "M", "Tool geometry", GH_ParamAccess.item);
            
            //pManager.AddParameter("PRnum", "PR", "FANUC PR number", GH_ParamAccess.item, 1);
            pManager[2].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            //pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ToolParameter(), "Tool", "T", "Tool", GH_ParamAccess.item);
            pManager.AddPlaneParameter("TCP", "P", "TCP plane. It might be different from the original if the 4 point calibration is used", GH_ParamAccess.item);
            
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasFanucPR = Params.Input.Any(x => x.Name == "FanucPR");

            string name = null;
            GH_Plane tcp = null;
            double weight = 0;
            GH_Mesh mesh = null;
            GH_Point centroid = null;
            List<GH_Plane> planes = new List<GH_Plane>();
            ProcessRegister pr = null;
            GH_PR sourcePR = null;


            if (!DA.GetData(0, ref name)) { return; }
            if (!DA.GetData(1, ref tcp)) { return; }
            DA.GetDataList(2, planes);
            if (!DA.GetData(3, ref weight)) { return; }
            DA.GetData(4, ref centroid);
            DA.GetData(5, ref mesh);

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


            var tool = new Tool(tcp.Value, name, weight, centroid?.Value, mesh?.Value, pr);

            if (planes.Count > 0)
            {
                if (planes.Count != 4)
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, " Calibration input must be 4 planes");
                else
                    tool.FourPointCalibration(planes[0].Value, planes[1].Value, planes[2].Value, planes[3].Value);
            }

            DA.SetData(0, new GH_Tool(tool));
            DA.SetData(1, tool.Tcp);
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
                param.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 70, parameter.Attributes.InputGrip.Y );
                param.Description = "this is the value for PR";
                Instances.ActiveCanvas.Document.AddObject(param, false);
                //parameter.AddSource(param.Params.Output);
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

    public abstract class ToolAttribute
    {
        public virtual string Name { get; internal set; }
        public T CloneWithName<T>(string name) where T : ToolAttribute
        {
            var attribute = MemberwiseClone() as T;
            attribute.Name = name;
            return attribute;
        }
    }

    public class CreatePRparam : GH_Component
    {

        public CreatePRparam() : base("Processor Register", "PR", "Creates a FANUC Processor Register.", "Robots", "Parameters") {}
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{C436196B-A0EE-4C5B-8F11-6FF053D8EEB7}");
        protected override Bitmap Icon => Properties.Resources.iconProcessorRegister;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("index", "i", "index", GH_ParamAccess.item, 1);
            pManager.AddTextParameter("content", "c", "PR content", GH_ParamAccess.item, "");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PRparameter(), "Processor Register", "PR", "pr", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int index = 0;
            string content = "";
            //var panelindex = new GH_Panel();
            //var panelcontent = new GH_Panel();
            //panelindex.CreateAttributes();
            //panelcontent.CreateAttributes();
            //panelindex.Attributes.Pivot = new PointF(0,0);
            //panelcontent.Attributes.Pivot = new PointF(0,0);

            //Instances.ActiveCanvas.Document.AddObject(panelindex, false);
            //Instances.ActiveCanvas.Document.AddObject(panelcontent, false);

            //Params.Input[0].AddSource(panelindex);
            //Params.Input[1].AddSource(panelcontent);
            DA.GetData(0, ref index);
            DA.GetData(1, ref content);
            //Params.Input[0].CollectData();
            //Params.Input[1].CollectData();
            //ExpireSolution(true);

            var pr = new ProcessRegister(index, content);
            DA.SetData(0, new GH_PR(pr));
        }
    }

}


   