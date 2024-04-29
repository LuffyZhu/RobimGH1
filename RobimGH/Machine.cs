using ConnectToDB;
using FoxLearn.License;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Robim.Grasshopper
{
    public class LoadRobotSystem : GH_Component , IGH_VariableParameterComponent
    {
        /*GH_ValueList valueList_robot = null;
        IGH_Param parameter_robot = null;
        IGH_Param parameter_externalsystem = null;*/
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
        string[] externalextrasetting = null;
        //Track Hang Up side Down
        TrackHangUpSideDown trackHangUpSideDown = TrackHangUpSideDown.No;

        RobimFormSystem RFS = null;

        public RobotSystem RobotSystem = null;


        public LoadRobotSystem() : base("Load robot system", "Load robot", "Loads a robot system either from the library or from a custom file", "Robim", "Components") { }
        public override Guid ComponentGuid => new Guid("{7722D7E3-98DE-49B5-9B1D-E0D1B938B4A7}");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.iconRobot;
        //输入
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("Name", "N", "Name of the Robot system", GH_ParamAccess.item);
            //pManager.AddPlaneParameter("Base", "P", "Base plane", GH_ParamAccess.item, Plane.WorldXY);
            //parameter_robot = pManager[0];
        }
        //输出
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
        }

        #region 增加输入
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Save", AddSave, true, Params.Input.Any(x => x.Name == "Save"));
            Menu_AppendItem(menu, "BasePlaneEuler", AddBasePlane, true, Params.Input.Any(x => x.Name == "BasePlaneEuler"));
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Error", AddError, true, Params.Output.Any(x => x.Name == "Error"));
        }
        private void AddError(object sender, EventArgs e) => AddParam(0);
        private void AddSave(object sender, EventArgs e) => AddInputParam(1);
        private void AddBasePlane(object sender, EventArgs e) => AddInputParam(2);
        //增加输入之属性
        IGH_Param[] parameters = new IGH_Param[3]
        {
            new Param_String() { Name = "Error", NickName = "E", Description = "Error of the Robot system", Optional = true},
            new Param_Boolean() { Name = "Save", NickName = "S", Description = "Save the Robot system", Optional = true},
            new Param_String(){ Name = "BasePlaneEuler", NickName = "B", Description = "Robot baseplane euler:x,y,z,a,b,c", Optional = true }
        };
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
                parameter.Access = GH_ParamAccess.list;
                Params.RegisterOutputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        private void AddInputParam(int index)
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
        #endregion
        protected override void BeforeSolveInstance()
        {
            string[] names = new string[3]
            {
                robotName_tmp,trackName_tmp,revolverName_tmp
            };
            string[] eulerangles = new string[4]
            {
                roboteulerangle,trackeulerangle,platformeulerangle,coupledplaneeulerangle
            };
            bool[] haseulerangles = new bool[4]
            {
                hasroboteuler,hastrackeuler,hasplatformeuler,hascoupledplaneeuler
            };
            RFS = new RobimFormSystem(names, eulerangles, haseulerangles, externalextrasetting, trackHangUpSideDown);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool start = false;
            if (Params.Input.Any(x=>x.Name == "Save"))
            {
                DA.GetData("Save", ref start);
            }
            if(Params.Input.Any(x=>x.Name == "BasePlaneEuler"))
            {
                string input_euler = null;
                DA.GetData("BasePlaneEuler",ref input_euler);
                
                if (input_euler != null && input_euler.Split(',').Length == 6)
                {
                    if (trackName_tmp != null)
                    {
                        RFS.T_Eulerangle = input_euler;
                        RFS.T_HasEulerangle = true;
                    }
                    else
                    {
                        RFS.R_Eulerangle = input_euler;
                        RFS.R_HasEulerangle = true;
                    }
                }
                else
                {
                    RFS.T_Eulerangle = null;
                    RFS.R_Eulerangle = null;
                    RFS.T_HasEulerangle = false;
                    RFS.R_HasEulerangle = false;
                }
            }

            var robotSystem = RobotSystem.Load(this, robotName_tmp, RFS, trackName_tmp, revolverName_tmp);
            RobotSystem = robotSystem;
            if (robotSystem != null)
                DA.SetData(0, new GH_RobotSystem(robotSystem));
            bool haserror = Params.Output.Any(x => x.Name == "Error");
            if (haserror)
            {
                IList<string> errors = RuntimeMessages(GH_RuntimeMessageLevel.Error);
                DA.SetDataList("Error",errors);
            }
        }
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
        
        public override void CreateAttributes()
        {
            m_attributes = new ComponentButton(this,"Custom Robot System");
        }
        MainForm form;
        public void DisForm(GH_Canvas sender)
        {
            form = new MainForm(RFS);
            form.Save.Click += Save_Click;
            GH_WindowsFormUtil.CenterFormOnCursor(form, true);
            form.ShowDialog(sender.FindForm());
        }
        private void Save_Click(object sender, EventArgs e)
        {
            //Get external axis name
            ComboBox RobotcomboBox = form.RobotcomboBox;
            ComboBox TrackcomboBox = form.TrackcomboBox;
            ComboBox PlatformcomboBox = form.PlatformcomboBox;
            if (RobotcomboBox.Text == "None")
                robotName_tmp = null;
            else
                robotName_tmp = RobotcomboBox.Text;
            if (TrackcomboBox.Text == "None")
                trackName_tmp = null;
            else
                trackName_tmp = TrackcomboBox.Text;
            if (PlatformcomboBox.Text == "None")
                revolverName_tmp = null;
            else
                revolverName_tmp = PlatformcomboBox.Text;
            //Get is external axis has custom euler
            hasroboteuler = form.EulercheckBox_robot.Checked;
            hastrackeuler = form.EulercheckBox_track.Checked;
            hasplatformeuler = form.EulercheckBox_platform.Checked;
            hascoupledplaneeuler = form.EulercheckBox_coupledplane.Checked;
            //Get custom euler value
            if (hasroboteuler)
                roboteulerangle = form.XaxisBox3.Text + "," + form.YaxisBox3.Text + "," + form.ZaxisBox3.Text + "," + form.AangleBox3.Text + "," + form.BangleBox3.Text + "," + form.CangleBox3.Text;
            else
                roboteulerangle = null;
            if (hastrackeuler)
                trackeulerangle = form.XaxisBox1.Text + "," + form.YaxisBox1.Text + "," + form.ZaxisBox1.Text + "," + form.AangleBox1.Text + "," + form.BangleBox1.Text + "," + form.CangleBox1.Text;
            else
                trackeulerangle = null;
            if (hasplatformeuler)
                platformeulerangle = form.XaxisBox2.Text + "," + form.YaxisBox2.Text + "," + form.ZaxisBox2.Text + "," + form.AangleBox2.Text + "," + form.BangleBox2.Text + "," + form.CangleBox2.Text;
            else
                platformeulerangle = null;
            if (hascoupledplaneeuler)
                coupledplaneeulerangle = form.XaxisBox4.Text + "," + form.YaxisBox4.Text + "," + form.ZaxisBox4.Text + "," + form.AangleBox4.Text + "," + form.BangleBox4.Text + "," + form.CangleBox4.Text;
            else
                coupledplaneeulerangle = null;
            //external extra setting
            Panel panel = form.Panel_ExternalSetting;
            Panel[] panels = panel.Controls.OfType<Panel>().Skip(2).ToArray();
            Array.Resize(ref externalextrasetting, panels.Length * 2);
            if(panels.Length > 0)
            {
                for (int i = 0, j = 0; i < panels.Length * 2; i += 2, j++)
                {
                    externalextrasetting.SetValue(panels[j].Name, i);//Panel_ExternalValueTrack1//0
                    //externalextrasetting.Append(panels[i].Controls.OfType<Label>().Last().Text);//E1
                    externalextrasetting.SetValue(panels[j].Controls.OfType<CheckBox>().First().Checked.ToString(), i + 1);//direction//1
                }
            }
            if (form.Track_Hang_Up.Checked)
            {
                if (form.Track_Hang_Up_Axis.SelectedIndex == 1)
                    trackHangUpSideDown = TrackHangUpSideDown.X_axis;
                else
                    trackHangUpSideDown = TrackHangUpSideDown.Y_axis;
            }
            else
                trackHangUpSideDown = TrackHangUpSideDown.No;

            //Close
            form.Close();
            ExpireSolution(true);
        }
        public override bool Write(GH_IWriter writer)
        {
            if (robotName_tmp != null && robotName_tmp.Length > 0)
            {
                writer.SetString("RobotName", robotName_tmp);
            }
            if (trackName_tmp != null && trackName_tmp.Length > 0)
            {
                writer.SetString("TrackName", trackName_tmp);
            }
            if (revolverName_tmp != null && revolverName_tmp.Length > 0)
            {
                writer.SetString("PlatformName", revolverName_tmp);
            }
            writer.SetBoolean("hasroboteuler", hasroboteuler);
            writer.SetBoolean("hastrackeuler", hastrackeuler);
            writer.SetBoolean("hasplantformeuler", hasplatformeuler);
            writer.SetBoolean("hascoupledplaneeuler", hascoupledplaneeuler);
            if (hasroboteuler)
            {
                writer.SetString("roboteulerangle", roboteulerangle);
            }
            if (hastrackeuler)
            {
                writer.SetString("trackeulerangle", trackeulerangle);
            }
            if (hasplatformeuler)
            {
                writer.SetString("platformeulerangle", platformeulerangle);
            }
            if (hascoupledplaneeuler)
            {
                writer.SetString("coupledplaneeulerangle", coupledplaneeulerangle);
            }
            if (externalextrasetting != null && externalextrasetting.Length > 0)
            {
                writer.SetInt32("ExternalExtraSettingCount", externalextrasetting.Length);
                for(int i = 0;i< externalextrasetting.Length; i++)
                {
                    writer.SetString("ExternalExtraSetting", i, externalextrasetting[i]);
                }
            }
            writer.SetString("trackHangUpSideDown", trackHangUpSideDown.ToString());
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            reader.TryGetString("RobotName", ref robotName_tmp);
            reader.TryGetString("TrackName", ref trackName_tmp);
            reader.TryGetString("PlatformName", ref revolverName_tmp);
            reader.TryGetBoolean("hasroboteuler", ref hasroboteuler);
            reader.TryGetBoolean("hastrackeuler", ref hastrackeuler);
            reader.TryGetBoolean("hasplantformeuler", ref hasplatformeuler);
            reader.TryGetBoolean("hascoupledplaneeuler", ref hascoupledplaneeuler);
            if (hasroboteuler)
            {
                reader.TryGetString("roboteulerangle", ref roboteulerangle);
            }
            if (hastrackeuler)
            {
                reader.TryGetString("trackeulerangle", ref trackeulerangle);
            }
            if (hasplatformeuler)
            {
                reader.TryGetString("platformeulerangle", ref platformeulerangle);
            }
            if (hascoupledplaneeuler)
            {
                reader.TryGetString("coupledplaneeulerangle", ref coupledplaneeulerangle);
            }
            int i = 0;
            reader.TryGetInt32("ExternalExtraSettingCount",ref i);
            Array.Resize(ref externalextrasetting, i);
            for (int j = 0; j < i; j++)
            {
                reader.TryGetString("ExternalExtraSetting", j, ref externalextrasetting[j]);
            }
            string trackhang = "No";
            reader.TryGetString("trackHangUpSideDown", ref trackhang);
            trackHangUpSideDown = (TrackHangUpSideDown)Enum.Parse(typeof(TrackHangUpSideDown), trackhang);

            return base.Read(reader);
        }
        /*public void download()
        {
            WebClient wc = new WebClient();
            string path = "http://roboticplus.cn4.quickconnect.cn/temporary/余知翰/RobotModel/";
            wc.DownloadFile($"{path}", "R+_ABB.xml");
        }*/
    }
    /*
    public class LoadRobotSystemForWPF : GH_Component//, IGH_VariableParameterComponent
    {

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
        string[] externalextrasetting = null;
        //Track Hang Up side Down
        TrackHangUpSideDown trackHangUpSideDown = TrackHangUpSideDown.No;

        RobimFormSystem RFS = null;

        public RobotSystem RobotSystem = null;


        public LoadRobotSystemForWPF() : base("Load robot system", "Load robot", "Loads a robot system either from the library or from a custom file", "Robim", "Components") { }
        public override Guid ComponentGuid => new Guid("{b9b7d5ed-cded-4b1b-b52c-94a46fdb413f}");
        //public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.iconRobot;
        //输入
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("RobotName", "RN", "Name of the Robot", GH_ParamAccess.item);
            pManager.AddTextParameter("TrackName", "TN", "Name of the track", GH_ParamAccess.item);
            pManager.AddTextParameter("PlatformName", "PN", "Name of the platform", GH_ParamAccess.item);
            pManager.AddNumberParameter("EulerRobot", "ER", "Euler plane of robot base", GH_ParamAccess.list);
            pManager.AddNumberParameter("EulerTrack", "ET", "Euler plane of track base", GH_ParamAccess.list);
            pManager.AddNumberParameter("EulerPlatform", "EP", "Euler plane of platform base", GH_ParamAccess.list);

            //pManager.AddPlaneParameter("Base", "P", "Base plane", GH_ParamAccess.item, Plane.WorldXY);
            //parameter_robot = pManager[0];
        }
        //输出
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
        }

        

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool start = false;
            if (Params.Input.Any(x => x.Name == "Save"))
            {
                DA.GetData("Save", ref start);
            }
            if (Params.Input.Any(x => x.Name == "BasePlaneEuler"))
            {
                string input_euler = null;
                DA.GetData("BasePlaneEuler", ref input_euler);

                if (input_euler != null && input_euler.Split(',').Length == 6)
                {
                    if (trackName_tmp != null)
                    {
                        RFS.T_Eulerangle = input_euler;
                        RFS.T_HasEulerangle = true;
                    }
                    else
                    {
                        RFS.R_Eulerangle = input_euler;
                        RFS.R_HasEulerangle = true;
                    }
                }
                else
                {
                    RFS.T_Eulerangle = null;
                    RFS.R_Eulerangle = null;
                    RFS.T_HasEulerangle = false;
                    RFS.R_HasEulerangle = false;
                }
            }

            DA.GetData(0, ref robotName_tmp);
            DA.GetData(1, ref trackName_tmp);
            DA.GetData(2, ref revolverName_tmp);

            if (trackName_tmp != null || revolverName_tmp != null)
            {
                if (trackName_tmp != null)
                {
                    externalextrasetting.SetValue("Panel_ExternalValueTrack1", 0);
                    externalextrasetting.SetValue("False", 1);
                    if (revolverName_tmp != null)
                    {
                        externalextrasetting.SetValue("Panel_ExternalValuePlatform2", 2);
                        externalextrasetting.SetValue("False", 3);
                    }
                }
                else if (revolverName_tmp != null)
                {
                    externalextrasetting.SetValue("Panel_ExternalValuePlatform2", 0);
                    externalextrasetting.SetValue("False", 1);
                }
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

            RFS = new RobimFormSystem(names, eulerangles, haseulerangles, externalextrasetting, trackHangUpSideDown);


            var robotSystem = RobotSystem.Load(this, robotName_tmp, RFS, trackName_tmp, revolverName_tmp);
            RobotSystem = robotSystem;
            if (robotSystem != null)
                DA.SetData(0, new GH_RobotSystem(robotSystem));
            bool haserror = Params.Output.Any(x => x.Name == "Error");
            if (haserror)
            {
                IList<string> errors = RuntimeMessages(GH_RuntimeMessageLevel.Error);
                DA.SetDataList("Error", errors);
            }
        }
        //bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        //bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        //IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        //bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        //void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        public override void CreateAttributes()
        {
            m_attributes = new ComponentButton(this, "Custom Robot System");
        }

    }*/
    public class LoadTool : GH_Component
    {
        GH_ValueList valueList = null;
        IGH_Param parameter = null;

        public LoadTool() : base("Load robot tool", "Load tool", "Loads a tool either from the library or from a custom file", "Robim", "Components") { }
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
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.list);
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

            var tool = Tool.Load(name);
            List<Mesh> meshes = tool.Mesh_.ToList();

            DA.SetData(0, new GH_Tool(tool));
            DA.SetDataList(1, meshes);
        }

    }
    public class CreateTool : GH_Component, IGH_VariableParameterComponent
    {
        public CreateTool() : base("Create tool", "Tool", "Creates a tool or end effector.", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{E59E634B-7AD5-4682-B2C1-F18B73AE05C6}");
        protected override Bitmap Icon => Properties.Resources.iconCreateTool;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Tool name", GH_ParamAccess.item, "DefaultGHTool");
            pManager.AddParameter(new ToolParameter(), "Tool", "T", "Tool want to reset properties.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("TCP Plane", "P", "TCP plane", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Calibration", "4", "Optional 4 point TCP calibration. Orient the tool in 4 different ways around the same point in space and input the 4 planes that correspond to the flange", GH_ParamAccess.list);
            pManager.AddNumberParameter("Weight", "W", "Tool weight in kg", GH_ParamAccess.item, 0.0);
            pManager.AddPointParameter("Centroid", "C", "Optional tool center of mass", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "M", "Tool geometry", GH_ParamAccess.list);
            pManager.AddMeshParameter("ConvexHull Mesh", "M(CH)", "Tool convexhull mesh", GH_ParamAccess.list);

            //pManager.AddParameter("PRnum", "PR", "FANUC PR number", GH_ParamAccess.item, 1);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ToolParameter(), "Tool", "T", "Tool", GH_ParamAccess.item);
            pManager.AddPlaneParameter("TCP Plane", "P", "TCP plane. It might be different from the original if the 4 point calibration is used", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasFanucPR = Params.Input.Any(x => x.Name == "FanucPR");

            string name = null;
            GH_Plane tcp = null;
            double weight = 0;
            List<GH_Mesh> mesh = new List<GH_Mesh>();
            List<GH_Mesh> meshCH = new List<GH_Mesh>();
            GH_Point centroid = null;
            List<GH_Plane> planes = new List<GH_Plane>();
            ProcessRegister pr = null;
            GH_PR sourcePR = null;
            GH_Tool Oldtool = null;

            if (!DA.GetData(0, ref name)) { return; }
            DA.GetData(1, ref Oldtool);
            DA.GetData(2, ref tcp);
            DA.GetDataList(3, planes);
            if (!DA.GetData(4, ref weight)) { return; }
            DA.GetData(5, ref centroid);
            DA.GetDataList(6, mesh);
            DA.GetDataList(7, meshCH);

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

            Tool tool = null;

            if (Oldtool == null)
                tool = new Tool(tcp.Value, name, weight, centroid?.Value, mesh.Select(x => x?.Value).ToArray(), meshCH.Select(x => x?.Value).ToArray(), pr);
            else
            {
                tool = Oldtool.Value;
                if (tcp != null)
                    tool.Tcp = tcp.Value;
                if (centroid != null)
                    tool.Centroid = centroid.Value;
                if (mesh.Count != 0)
                    tool.Mesh_ = mesh.Select(x => x.Value).ToArray();
                if (meshCH.Count != 0)
                    tool.ConvexHullMesh = meshCH.Select(x => x.Value).ToArray();
                if (pr != null)
                    tool.PR = pr;
            }

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
                param.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 70, parameter.Attributes.InputGrip.Y);
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
        public CreatePRparam() : base("Processor Register", "PR", "Creates a FANUC Processor Register.", "Robim", "Parameters") { }
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