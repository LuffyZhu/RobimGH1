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
using Robim.Commands;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using System.Windows.Forms;
using Robim.Grasshopper;
using Grasshopper.GUI;
using Robim;
using GH_IO.Serialization;


namespace Robim.Grasshopper
{
    #region Old
    //    public class ExternalSetup : GH_Component , IGH_VariableParameterComponent
    //    {
    //        GH_ValueList valueList_external = null;
    //        GH_ValueList valueList_platform = null;
    //        GH_BooleanToggle booleantoggle_external = new GH_BooleanToggle();
    //        //GH_NumberSlider numberList_external = new GH_NumberSlider();
    //        IGH_Param parameter_external = null;
    //        IGH_Param parameter_external2 = null;
    //        IGH_Param parameter_external3 = null;
    //        //IGH_Param parameter_revolvingnumber = null;
    //        string all;
    //        public static string trackName_tmp = null;
    //        public static string revolverName_tmp = null;
    //        public static string trackeulerangle = null;
    //        public static string platformeulerangle = null;
    //        public static bool hastrackeuler = false;
    //        public static bool hasplantformeuler = false;
    //        //public ExternalSetup() : base("Load external axis system", "Load external axis", "Loads a external axis system either from the library or from a custom file", "Robim", "Components") { }
    //        public override GH_Exposure Exposure => GH_Exposure.primary;
    //        public override Guid ComponentGuid => new Guid("{2EFCF99D-4463-4A85-A428-A221119B3695}");
    //        protected override Bitmap Icon => Properties.Resources.iconExternalaxis;
    //        //输入
    //        protected override void RegisterInputParams(GH_InputParamManager pManager)
    //        {
    //            //pManager.AddTextParameter("Name", "N", "Name of the Robot system", GH_ParamAccess.item);
    //            //pManager.AddPlaneParameter("Base", "P", "Base plane", GH_ParamAccess.item, Plane.WorldXY);
    //            //parameter_robot = pManager[0];
    //        }

    //        //输出
    //        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    //        {
    //            pManager.AddParameter(new Param_String(), "External axis system", "E.A", "External axis system", GH_ParamAccess.item);
    //        }

    //        //增加输入
    //        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
    //        {
    //            Menu_AppendSeparator(menu);
    //            Menu_AppendItem(menu, "External Axis: Track", AddTrack, true, Params.Input.Any(x => x.Name == "External Track"));
    //            Menu_AppendItem(menu, "External Axis: Opposite Direction", AddDirection, true, Params.Input.Any(x => x.Name == "Opposite Direction"));
    //            Menu_AppendItem(menu, "External Axis: RevolvingPlatform", AddRevolving, true, Params.Input.Any(x => x.Name == "External RevolvingPlatform"));
    //            //Menu_AppendItem(menu, "External Axis: Revolving Type", AddRevolvingNumber, true, Params.Input.Any(x => x.Name == "External Revolving Type"));
    //            Menu_AppendItem(menu, "External Axis: Euler", AddExernalEuler, true, Params.Input.Any(x => x.Name == "External Axis Euler"));
    //            //Menu_AppendItem(menu, "External Axis: Couple Plane", AddBaseEuler, true, Params.Input.Any(x => x.Name == "External Axis Couple Plane"));
    //        }

    //        //增加轨道
    //        private void AddTrack(object sender, EventArgs e)
    //        {
    //            List<int> listint = new List<int>();
    //            AddParam(0);
    //            parameter_external = parameters[0];
    //            if (Params.Input.Any(x => x.Name == parameter_external.Name))
    //            {
    //                if (valueList_external == null)
    //                {
    //                    if (parameter_external.Sources.Count == 0)
    //                    {
    //                        valueList_external = new GH_ValueList();
    //                    }
    //                    else
    //                    {
    //                        foreach (var source in parameter_external.Sources)
    //                        {
    //                            if (source is GH_ValueList) valueList_external = source as GH_ValueList;
    //                            return;
    //                        }
    //                    }

    //                    valueList_external.CreateAttributes();
    //                    valueList_external.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 300, this.Attributes.Pivot.Y);
    //                    //param.Attributes.Pivot = new PointF(parameter.Attributes.InputGrip.X - 70, parameter.Attributes.InputGrip.Y);
    //                    valueList_external.ListItems.Clear();

    //                    var robotSystems = RobotSystem.ListTrackSystems(ref listint);

    //                    foreach (string trackSystemName in robotSystems)
    //                    {
    //                        valueList_external.ListItems.Add(new GH_ValueListItem(trackSystemName, $"\"{trackSystemName}\""));
    //                    }
    //                    Instances.ActiveCanvas.Document.AddObject(valueList_external, false);
    //                    parameter_external.AddSource(valueList_external);
    //                    parameter_external.CollectData();
    //                    ExpireSolution(true);
    //                }
    //                /*
    //                var param = new CreatePRparam();
    //                param.CreateAttributes();
    //                param.Attributes.Pivot = new PointF(parameter_external.Attributes.InputGrip.X - 70, parameter_external.Attributes.InputGrip.Y);
    //                param.Description = "this is the value for PR";
    //                Instances.ActiveCanvas.Document.AddObject(param, false);
    //                //parameter.AddSource(param.Params.Output);
    //                parameter_external.CollectData();
    //                ExpireSolution(true);*/
    //            }
    //        }
    //        //增加方向
    //        private void AddDirection(object sender, EventArgs e)
    //        {
    //            AddParam(1);
    //            parameter_external2 = parameters[1];
    //            if (Params.Input.Any(x => x.Name == parameter_external2.Name))
    //            {
    //                booleantoggle_external.CreateAttributes();
    //                booleantoggle_external.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 300, this.Attributes.Pivot.Y);
    //                Instances.ActiveCanvas.Document.AddObject(booleantoggle_external, false);
    //                parameter_external2.AddSource(booleantoggle_external);
    //                parameter_external2.CollectData();
    //                ExpireSolution(true);
    //                /*
    //                var param = new CreatePRparam();
    //                param.CreateAttributes();
    //                param.Attributes.Pivot = new PointF(parameter_external.Attributes.InputGrip.X - 70, parameter_external.Attributes.InputGrip.Y);
    //                param.Description = "this is the value for PR";
    //                Instances.ActiveCanvas.Document.AddObject(param, false);
    //                //parameter.AddSource(param.Params.Output);
    //                parameter_external.CollectData();
    //                ExpireSolution(true);*/
    //            }
    //        }
    //        //增加工作平台
    //        private void AddRevolving(object sender, EventArgs e)
    //        {
    //            List<int> listint = new List<int>();
    //            AddParam(2);
    //            parameter_external3 = parameters[2];
    //            //新增一个工作平台节点(后续开发)
    //            if (Params.Input.Any(x => x.Name == parameter_external3.Name))
    //            {
    //                if (valueList_platform == null)
    //                {
    //                    if (parameter_external3.Sources.Count == 0)
    //                    {
    //                        valueList_platform = new GH_ValueList();
    //                    }
    //                    else
    //                    {
    //                        foreach (var source in parameter_external3.Sources)
    //                        {
    //                            if (source is GH_ValueList) valueList_platform = source as GH_ValueList;
    //                            return;
    //                        }
    //                    }

    //                    valueList_platform.CreateAttributes();
    //                    valueList_platform.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 300, this.Attributes.Pivot.Y);
    //                    valueList_platform.ListItems.Clear();

    //                    var robotSystems = RobotSystem.ListPlatformSystems(ref listint);

    //                    foreach (string platformSystemName in robotSystems)
    //                    {
    //                        valueList_platform.ListItems.Add(new GH_ValueListItem(platformSystemName, $"\"{platformSystemName}\""));
    //                    }
    //                    Instances.ActiveCanvas.Document.AddObject(valueList_platform, false);
    //                    parameter_external3.AddSource(valueList_platform);
    //                    parameter_external3.CollectData();
    //                    ExpireSolution(true);
    //                }
    //            }
    //        }
    //        //自定旋转轴类别
    //        /*private void AddRevolvingNumber(object sender, EventArgs e)
    //        {
    //            AddParam(3);
    //            parameter_revolvingnumber = parameters[3];
    //            if (Params.Input.Any(x => x.Name == parameter_revolvingnumber.Name))
    //            {
    //                numberList_external.CreateAttributes();
    //                numberList_external.Attributes.Pivot = new PointF(this.Attributes.Pivot.X - 300, this.Attributes.Pivot.Y);
    //                numberList_external.Slider.Maximum = 3;
    //                numberList_external.Slider.Minimum = 1;
    //                numberList_external.Slider.DecimalPlaces = 0;
    //                Instances.ActiveCanvas.Document.AddObject(numberList_external, false);
    //                parameter_revolvingnumber.AddSource(numberList_external);
    //                parameter_revolvingnumber.CollectData();
    //                ExpireSolution(true);
    //            }
    //        }*/
    //        //自定四元数
    //        private void AddExernalEuler(object sender, EventArgs e) => AddParam(3);
    //        private void AddBaseEuler(object sender, EventArgs e) => AddParam(4);

    //        //增加输入之属性
    //        IGH_Param[] parameters = new IGH_Param[5]
    //        {
    //         new Param_String() { Name = "External Track", NickName = "External Track", Description = "External Track", Optional = true},//修改
    //         new Param_Boolean() { Name = "Opposite Direction", NickName = "Opposite Direction", Description = "Opposite Direction", Optional = true},//修改
    //         new Param_String() { Name = "External RevolvingPlatform", NickName = "External RevolvingPlatform", Description = "External RevolvingPlatform", Optional = true},//修改
    //         new Param_String() { Name = "External Axis Euler", NickName = "External Axis Euler", Description = "External Axis Euler\nExample:x,y,z,a,b,c", Optional = true},//修改
    //         new Param_Plane() { Name = "External Axis Couple Plane", NickName = "Couple Plane", Description = "External Axis Couple Plane\nExample:x,y,z,a,b,c", Optional = true}//修改
    //        };

    //        private void AddParam(int index)
    //        {
    //            IGH_Param parameter = parameters[index];

    //            if (Params.Input.Any(x => x.Name == parameter.Name))
    //                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
    //            else
    //            {
    //                int insertIndex = Params.Input.Count;
    //                for (int i = 0; i < Params.Input.Count; i++)
    //                {
    //                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Input[i].Name);
    //                    if (otherIndex > index)
    //                    {
    //                        insertIndex = i;
    //                        break;
    //                    }
    //                }
    //                Params.RegisterInputParam(parameter, insertIndex);
    //            }
    //            Params.OnParametersChanged();
    //            ExpireSolution(true);
    //        }

    //        protected override void BeforeSolveInstance()
    //        {

    //        }
    //        public static Plane coupleplane = Plane.Unset;
    //        //public static Plane digital_coupling_plane;

    //        protected override void SolveInstance(IGH_DataAccess DA)
    //        {
    //            bool hasTrack = Params.Input.Any(x => x.Name == "External Track");
    //            bool hasDerection = Params.Input.Any(x => x.Name == "Opposite Direction");
    //            bool hasRevolving = Params.Input.Any(x => x.Name == "External RevolvingPlatform");
    //            bool hasExternaleuler = Params.Input.Any(x => x.Name == "External Axis Euler");
    //            bool hasBaseeuler = Params.Input.Any(x => x.Name == "External Axis Couple Plane");
    //            GH_String track = null;
    //            GH_String revolver = null;
    //            GH_Boolean oppositederection = null;
    //            GH_String externalaxiseuler = null;
    //            GH_Plane baseeuler = null;
    //            //if (!DA.GetData(0, ref name)) { return; }
    //            //if (!DA.GetData(1, ref basePlane)) { return; }
    //            if (hasTrack)
    //            {
    //                if (!DA.GetData("External Track", ref track)) { return; }
    //            }
    //            if (hasDerection)
    //            {
    //                if (!DA.GetData("Opposite Direction", ref oppositederection)) { return; }
    //            }
    //            if (hasRevolving)
    //            {
    //                if (!DA.GetData("External RevolvingPlatform", ref revolver)) { return; }
    //            }
    //            if (hasExternaleuler)
    //            {
    //                if (!DA.GetData("External Axis Euler", ref externalaxiseuler)) { return; }
    //            }
    //            if (hasBaseeuler)
    //            {
    //                DA.GetData("External Axis Couple Plane", ref baseeuler);
    //                if (baseeuler != null)
    //                    coupleplane = baseeuler.Value;
    //                else
    //                    coupleplane = Plane.Unset;
    //            }
    //            //DA.SetData(trackName, valueList_external.Name);


    //            bool opposite = false;
    //            //string axiseuler = null;
    //            //bool hasaxiseuler;
    //            if (track != null)
    //                trackName_tmp = track.Value;
    //            if (revolver != null)
    //                revolverName_tmp = revolver.Value;
    //            if (oppositederection != null)
    //                opposite = oppositederection.Value;
    //            if (externalaxiseuler != null)
    //            {
    //                trackeulerangle = externalaxiseuler.Value;
    //            }
    //            all = string.Format("{0}|{1}|{2}|{3}|{4}", trackeulerangle, platformeulerangle, opposite, trackName_tmp, revolverName_tmp);
    //            DA.SetData(0, all);
    //            //var robotSystem = RobotSystem.Load(name, basePlane.Value); ;
    //            //DA.SetData(0, new GH_RobotSystem(robotSystem));
    //        }
    //        public override string ToString() => $"{all}";
    //        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
    //        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
    //        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
    //        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
    //        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

    //        public override void CreateAttributes()
    //        {
    //            m_attributes = new ComponentButton(this);
    //        }

    //        public static MainForm form;
    //        public void DisForm()
    //        {
    //            //form = new MainForm();
    //            form.Save.Click += Save_Click;
    //            GH_WindowsFormUtil.CenterFormOnCursor(form, true);
    //        }

    //        private void Save_Click(object sender, EventArgs e)
    //        {
    //            //Get external axis name
    //            ComboBox TrackcomboBox = form.TrackcomboBox;
    //            ComboBox PlatformcomboBox = form.PlatformcomboBox;
    //            if (TrackcomboBox.Text == "None")
    //                trackName_tmp = null;
    //            else
    //                trackName_tmp = TrackcomboBox.Text;
    //            if (PlatformcomboBox.Text == "None")
    //                revolverName_tmp = null;
    //            else
    //                revolverName_tmp = PlatformcomboBox.Text;
    //            //Get is external axis has custom euler
    //            hastrackeuler = form.EulercheckBox_track.Checked;
    //            hasplantformeuler = form.EulercheckBox_platform.Checked;
    //            //Get custom euler value
    //            if (hastrackeuler)
    //                trackeulerangle = form.XaxisBox1.Text + "," + form.YaxisBox1.Text + "," + form.ZaxisBox1.Text + "," + form.AangleBox1.Text + "," + form.BangleBox1.Text + "," + form.CangleBox1.Text;
    //            else
    //                trackeulerangle = null;
    //            if (hasplantformeuler)
    //                platformeulerangle = form.XaxisBox2.Text + "," + form.YaxisBox2.Text + "," + form.ZaxisBox2.Text + "," + form.AangleBox2.Text + "," + form.BangleBox2.Text + "," + form.CangleBox2.Text;
    //            else
    //                platformeulerangle = null;
    //            form.Close();
    //            ExpireSolution(true);
    //        }

    //        public override bool Write(GH_IWriter writer)
    //        {
    //            if (trackName_tmp != null && trackName_tmp.Length > 0)
    //            {
    //                writer.SetString("TrackName", trackName_tmp);
    //            }
    //            if (revolverName_tmp != null && revolverName_tmp.Length > 0)
    //            {
    //                writer.SetString("PlatformName", revolverName_tmp);
    //            }
    //            writer.SetBoolean("hastrackeuler", hastrackeuler);
    //            writer.SetBoolean("hasplantformeuler", hasplantformeuler);
    //            if (hastrackeuler)
    //            {
    //                writer.SetString("trackeulerangle", trackeulerangle);
    //            }
    //            if (hasplantformeuler)
    //            {
    //                writer.SetString("platformeulerangle", platformeulerangle);
    //            }
    //            return base.Write(writer);
    //        }
    //        public override bool Read(GH_IReader reader)
    //        {
    //            reader.TryGetString("TrackName", ref trackName_tmp);
    //            reader.TryGetString("PlatformName", ref revolverName_tmp);
    //            reader.TryGetBoolean("hastrackeuler", ref hastrackeuler);
    //            reader.TryGetBoolean("hasplantformeuler", ref hasplantformeuler);
    //            if (hastrackeuler)
    //            {
    //                reader.TryGetString("trackeulerangle",ref trackeulerangle);
    //            }
    //            if (hasplantformeuler)
    //            {
    //                reader.TryGetString("platformeulerangle", ref platformeulerangle);
    //            }
    //            return base.Read(reader);
    //        }
    //    }
    #endregion
}

public class ComponentButton : GH_ComponentAttributes
{
    private bool mouseOver;
    public string buttonName;
    public GH_Component GH_Componenta;
    public ComponentButton(GH_Component owner,string name) : base(owner)
    {
        mouseOver = false;
        buttonName = name;
        GH_Componenta = owner;
    }
    protected override void Layout()
    {
        base.Layout();

        Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
        rec0.Height += 22;
        Rectangle rec1 = rec0;
        rec1.X = rec0.Left + 2;
        rec1.Y = rec0.Bottom - 22;
        rec1.Width = (rec0.Width) - 4;
        rec1.Height = 22;
        rec1.Inflate(-2, -2);
        Bounds = rec0;
        ButtonBounds = rec1;
    }
    private Rectangle ButtonBounds { get; set; }
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
            GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.White, buttonName, 2, 0);
            button.Render(graphics, Selected, Owner.Locked, false);
            if (mouseOver)
            {
                button.RenderEngine.RenderBackground_Alternative(graphics, Color.FromArgb(200, Color.Gray), false);
                button.RenderEngine.RenderText(graphics, Color.White);
            }
            button.Dispose();
        }
    }
    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {

        if (e.Button == MouseButtons.Left && mouseOver)
        {
            switch (buttonName)
            {
                case "Custom Robot System":
                    (Owner as LoadRobotSystem)?.DisForm(sender);
                    break;
                case "Compute":
                    (Owner as CheckCollisions)?.DisForm(sender);
                    break;
                case "Create":
                    (Owner as CreateProgram)?.DisForm(sender);
                    break;
                case "Analysis":
                    (Owner as CreateProgram)?.DisForm(sender);
                    break;
            }

            //(Owner as LoadRobotSystem)?.DisForm(sender);
            #region Old
            //LoadRobotSystem.form.ShowDialog(sender.FindForm());
            //(Owner as ExternalSetup)?.DisForm();
            //ExternalSetup.form.ShowDialog(sender.FindForm());
            /*Task task = Task.Factory.StartNew(() =>
            {
                Application.EnableVisualStyles();
                form = new MainForm();
                GH_WindowsFormUtil.CenterFormOnCursor(form, true);
                Application.Run(form);
            });*/
            /*Form form = new Form();
            form.Width = 400;
            form.Height = 200;
            form.StartPosition = FormStartPosition.Manual;
            GH_WindowsFormUtil.CenterFormOnCursor(form, true);
            form.Text = "Warnings";

            ListBox listBox = new ListBox();
            listBox.Width = 370;
            listBox.Height = 100;
            listBox.Left = 5;
            listBox.Top = 25;
            listBox.Items.Clear();
            listBox.HorizontalScrollbar = true;
            if (CreateProgram.reportwarnings != null)
            {
                for (int i = 0; i < CreateProgram.reportwarnings.Count; i++)
                {
                    listBox.Items.Add(CreateProgram.reportwarnings[i]);
                }
            }
            listBox.Font = new Font(listBox.Font.FontFamily, 14);
            form.Controls.Add(listBox);*/
            //form.ShowDialog(sender.FindForm());
            #endregion

            return GH_ObjectResponse.Handled;
        }

        return base.RespondToMouseDown(sender, e);
    }
    public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        System.Drawing.Point pt = GH_Convert.ToPoint(e.CanvasLocation);
        if (e.Button != MouseButtons.None)
        {
            return base.RespondToMouseMove(sender, e);
        }
        if (ButtonBounds.Contains(pt))
        {
            if (mouseOver != true)
            {
                mouseOver = true;
                sender.Invalidate();
            }
            return GH_ObjectResponse.Capture;
        }
        if (mouseOver != false)
        {
            mouseOver = false;
            sender.Invalidate();
        }
        return GH_ObjectResponse.Release;
    }
}

public class ExternalForm : GH_LinkedParamAttributes
{
    public ExternalForm(IGH_Param param, IGH_Attributes parent) : base(param, parent) { }
    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                /*Application.EnableVisualStyles();
                Form form = new MainForm();
                form.StartPosition = FormStartPosition.Manual;
                GH_WindowsFormUtil.CenterFormOnCursor(form, true);
                Application.Run(form);
                form.ShowDialog(sender.FindForm());*/
            });
            /*Form form = new Form();
            form.Width = 400;
            form.Height = 200;
            form.StartPosition = FormStartPosition.Manual;
            GH_WindowsFormUtil.CenterFormOnCursor(form, true);
            form.Text = "Warnings";

            ListBox listBox = new ListBox();
            listBox.Width = 370;
            listBox.Height = 100;
            listBox.Left = 5;
            listBox.Top = 25;
            listBox.Items.Clear();
            listBox.HorizontalScrollbar = true;
            if (CreateProgram.reportwarnings != null)
            {
                for (int i = 0; i < CreateProgram.reportwarnings.Count; i++)
                {
                    listBox.Items.Add(CreateProgram.reportwarnings[i]);
                }
            }
            listBox.Font = new Font(listBox.Font.FontFamily, 14);
            form.Controls.Add(listBox);*/
            //form.ShowDialog(sender.FindForm());
            return GH_ObjectResponse.Handled;
        }
        return base.RespondToMouseDown(sender, e);
    }
}