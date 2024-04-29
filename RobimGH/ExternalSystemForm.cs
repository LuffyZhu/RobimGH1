using Grasshopper.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Robim
{
    public partial class MainForm : Form
    {
        RobimFormSystem RobimFormSystem = null;
        //string ver = "20200914";
        string ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public MainForm(RobimFormSystem robimFormSystem)
        {
            InitializeComponent();
            RobimFormSystem = robimFormSystem;
        }
        List<int> trackjoints;
        List<int> platformjoints;
        TextBox[] roboteulerangles;
        TextBox[] trackeulerangles;
        TextBox[] platformeulerangles;
        TextBox[] coupledplaneeulerangles;
        private void MainForm_Load(object sender, EventArgs e)
        {
            version.Text = "ver_"+ver;
            Panel_ExternalSetting.Height = panel5.Height;
            //Load Robot & External axis List
            RobotList();
            if(RobimFormSystem.Externalextrasetting != null && RobimFormSystem.Externalextrasetting.Length > 0)
            {
                if (RobimFormSystem.Externalextrasetting[0].Contains("Track"))//first is track => TrackList() first
                {
                    TrackList();
                    PlatformList();
                }
                else
                {
                    PlatformList();
                    TrackList();
                }
                for (int i = 0, j = 0; i < RobimFormSystem.Externalextrasetting.Length; i += 2, j++)
                {
                    //External Direction
                    Panel_ExternalSetting.Controls.OfType<Panel>().Skip(2).ElementAt(j).Controls.OfType<CheckBox>().First().Checked = Convert.ToBoolean(RobimFormSystem.Externalextrasetting[i + 1]);//1//3
                }
            }
            else
            {
                TrackList();
                PlatformList();
            }
            //eulerangles textbox
            roboteulerangles = new TextBox[6] { 
                XaxisBox3,YaxisBox3,ZaxisBox3,AangleBox3,BangleBox3,CangleBox3
            };
            trackeulerangles = new TextBox[6] {
                XaxisBox1,YaxisBox1,ZaxisBox1,AangleBox1,BangleBox1,CangleBox1
            };
            platformeulerangles = new TextBox[6] {
                XaxisBox2,YaxisBox2,ZaxisBox2,AangleBox2,BangleBox2,CangleBox2
            };
            //CoupledPlane eulerangles textbox
            coupledplaneeulerangles = new TextBox[6] {
                XaxisBox4,YaxisBox4,ZaxisBox4,AangleBox4,BangleBox4,CangleBox4
            };
            //is robot & external axis has custom euler
            if (RobimFormSystem.R_HasEulerangle)
            {
                EulercheckBox_robot.Checked = true;
            }
            if (RobimFormSystem.T_HasEulerangle)
            {
                EulercheckBox_track.Checked = true;
            }
            if (RobimFormSystem.P_HasEulerangle)
            {
                EulercheckBox_platform.Checked = true;
            }
            //Has Coupled plane
            if (RobimFormSystem.C_HasEulerangle)
            {
                EulercheckBox_coupledplane.Checked = true;
            }
            switch (RobimFormSystem.trackHangUpSideDown)
            {
                case TrackHangUpSideDown.No:
                    Track_Hang_Up.Checked = false;
                    break;
                case TrackHangUpSideDown.X_axis:
                    Track_Hang_Up.Checked = true;
                    Track_Hang_Up_Axis.SelectedIndex = 1;
                    break;
                case TrackHangUpSideDown.Y_axis:
                    Track_Hang_Up.Checked = true;
                    Track_Hang_Up_Axis.SelectedIndex = 0;
                    break;
            }
        }

        #region Load Robot & External axis List
        private void RobotList()
        {
            if(RobotcomboBox.Items.Count != 0)
            {
                RobotcomboBox.Items.Clear();
            }
            RobotcomboBox.Items.Add("None");
            var robotSystems = RobotSystem.ListRobotSystems();
            int maxwidth = 0;
            int textwidth = 0;
            foreach (string robotSystemName in robotSystems)
            {
                RobotcomboBox.Items.Add($"{robotSystemName}");
                if (RobimFormSystem.R_Name == robotSystemName)
                {
                    RobotcomboBox.Text = robotSystemName;
                }
                textwidth = TextRenderer.MeasureText(robotSystemName, RobotcomboBox.Font).Width;
                if (maxwidth < textwidth)
                    maxwidth = textwidth;
            }
            RobotcomboBox.DropDownWidth = maxwidth;
            if (RobotcomboBox.Text == "")
            {
                RobotcomboBox.Text = "None";
            }
        }
        private void TrackList()
        {
            if (TrackcomboBox.Items.Count != 0)
            {
                TrackcomboBox.Items.Clear();
            }
            TrackcomboBox.Items.Add("None");
            trackjoints = new List<int>();
            var robotSystems = RobotSystem.ListTrackSystems(ref trackjoints);
            int maxwidth = 0;
            int textwidth = 0;
            foreach (string trackSystemName in robotSystems)
            {
                TrackcomboBox.Items.Add($"{trackSystemName}");
                if (RobimFormSystem.T_Name == trackSystemName)
                {
                    TrackcomboBox.Text = trackSystemName;
                }
                textwidth = TextRenderer.MeasureText(trackSystemName, TrackcomboBox.Font).Width;
                if (maxwidth < textwidth)
                    maxwidth = textwidth;
            }
            TrackcomboBox.DropDownWidth = maxwidth;
            if (TrackcomboBox.Text == "")
            {
                TrackcomboBox.Text = "None";
            }
        }
        private void PlatformList()
        {
            if (PlatformcomboBox.Items.Count != 0)
            {
                PlatformcomboBox.Items.Clear();
            }
            PlatformcomboBox.Items.Add("None");
            platformjoints = new List<int>();
            var robotSystems = RobotSystem.ListPlatformSystems(ref platformjoints);
            int maxwidth = 0;
            int textwidth = 0;
            foreach (string platformSystemName in robotSystems)
            {
                PlatformcomboBox.Items.Add($"{platformSystemName}");
                if (RobimFormSystem.P_Name == platformSystemName)
                {
                    PlatformcomboBox.Text = platformSystemName;
                }
                textwidth = TextRenderer.MeasureText(platformSystemName, PlatformcomboBox.Font).Width;
                if (maxwidth < textwidth)
                    maxwidth = textwidth;
            }
            PlatformcomboBox.DropDownWidth = maxwidth;
            if (PlatformcomboBox.Text == "")
            {
                PlatformcomboBox.Text = "None";
            }
        }
        #endregion

        #region Want custom euler
        private void EulercheckBox_CheckedChange(object sender, EventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            TextBox[] eulerangles = null;
            string eulerangle_old = null;
            switch (checkBox.Name)
            {
                case "EulercheckBox_robot":
                    eulerangle_old = RobimFormSystem.R_Eulerangle;
                    eulerangles = roboteulerangles;
                    break;
                case "EulercheckBox_track":
                    eulerangle_old = RobimFormSystem.T_Eulerangle;
                    eulerangles = trackeulerangles;
                    break;
                case "EulercheckBox_platform":
                    eulerangle_old = RobimFormSystem.P_Eulerangle;
                    eulerangles = platformeulerangles;
                    break;
                case "EulercheckBox_coupledplane":
                    eulerangle_old = RobimFormSystem.C_Eulerangle;
                    eulerangles = coupledplaneeulerangles;
                    break;
            }
            if (checkBox.Checked)
            {
                if (eulerangle_old != null)
                {
                    string[] str = eulerangle_old.Split(',');
                    for (int i = 0; i < eulerangles.Length; i++)
                    {
                        eulerangles[i].Enabled = true;
                        eulerangles[i].Text = str[i];
                    }
                }
                else
                {
                    foreach (TextBox textBox in eulerangles)
                    {
                        textBox.Enabled = true;
                        textBox.Text = "0";
                    }
                }
            }
            else
            {
                foreach (TextBox textBox in eulerangles)
                {
                    textBox.Enabled = false;
                    textBox.Text = "";
                }
            }
        }
        #endregion

        #region Get Euler input boxs value is number or not
        private void EulerTextBoxValue_Changed(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (!double.TryParse(textBox.Text,out _))
            {
                textBox.Text = "0";
                MessageBox.Show("Input is not a number");
            }
        }
        #endregion

        #region Select robot & external axis and user can select custom euler or not
        private void SystemcomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            int selectindex = comboBox.SelectedIndex;
            Panel panel = comboBox.Parent as Panel;
            CheckBox[] checkBoxes = panel.Controls.OfType<CheckBox>().ToArray();//platform has coupledplane so it has two checkboxes
            if (comboBox.Text == "None")
            {
                for(int i = 0;i< checkBoxes.Length; i++)
                {
                    checkBoxes[i].Checked = false;
                    checkBoxes[i].Enabled = false;
                }
                if(panel.Name == "Panel_Track" && RobotcomboBox.Text != "None")
                {
                    EulercheckBox_robot.Enabled = true;
                }
                if (panel.Name != "Panel_Robot")
                    RemoveExternal(panel);
            }
            else
            {
                for (int i = 0; i < checkBoxes.Length; i++) checkBoxes[i].Enabled = true;
                switch (panel.Name)
                {
                    case "Panel_Robot":
                        break;
                    case "Panel_Track":
                        RemoveExternal(panel);
                        AddExternal(panel, trackjoints[selectindex - 1]);
                        //EulercheckBox_robot.Checked = false;
                        //EulercheckBox_robot.Enabled = false;
                        break;
                    case "Panel_Platform":
                        RemoveExternal(panel);
                        AddExternal(panel, platformjoints[selectindex - 1]);
                        break;
                }
            }
        }
        #endregion

        #region License registered
        private void License_Click(object sender, EventArgs e)
        {
            Form form = new LicenseForm();
            GH_WindowsFormUtil.CenterFormOnCursor(form, true);
            form.ShowDialog(this.FindForm());
        }
        #endregion

        #region false is poistive ,true is opposite
        public void checkbox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox.Checked)
            {
                checkBox.Text = "Opposite";
            }
            else
            {
                checkBox.Text = "Positive";
            }
        }
        #endregion

        #region extenal axis joints
        int externalcount = 1;
        //直接添加
        public void AddExternal(Panel panel,int externaljoint)//if externalindex = 2
        {
            //bool hasexternal = false;
            ComboBox comboBox = panel.Controls.OfType<ComboBox>().Last();
            string newstrtype = panel.Name.Remove(0, 6);//Plane_Track => Track
            for(int i = 1; i <= externaljoint; i++)
            {
                string externaltype = newstrtype + "_" + comboBox.Text + "_" + i;//Track_SMS_1
                #region Add panel
                Panel panel1 = new Panel();
                panel1.BackColor = Color.Gainsboro;
                panel1.BorderStyle = BorderStyle.FixedSingle;
                panel1.Location = new Point(0, panel5.Height * externalcount);
                panel1.Margin = new Padding(0);
                panel1.Name = "Panel_ExternalValue" + newstrtype + externalcount.ToString();
                //panel1.Size = new Size(620, 30);
                panel1.Size = Panel_ExternalValue.Size;

                Label label = new Label();
                label.Dock = DockStyle.Left;
                label.Font = label_ExternalNumber.Font;
                //"微软雅黑 Light", 6.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(134))
                //label.Location = new Point(0, 0);
                label.Location = label_ExternalNumber.Location;
                label.Margin = new Padding(0);
                label.Name = "label_ExternalNumber" + externalcount.ToString();
                //label.Size = new Size(171, 28);
                label.Size = label_ExternalNumber.Size;
                label.Text = "E" + externalcount.ToString();
                label.TextAlign = ContentAlignment.MiddleCenter;

                Label label1 = new Label();
                label1.Dock = DockStyle.Left;
                label1.Font = label_ExternalType.Font;
                //label1.Location = new Point(171, 0);
                label1.Location = label_ExternalType.Location;
                label1.Margin = new Padding(0);
                label1.Name = "label_ExternalType" + externalcount.ToString();
                //label1.Size = new Size(259, 28);
                label1.Size = label_ExternalType.Size;
                label1.Text = externaltype;
                label1.TextAlign = ContentAlignment.MiddleCenter;

                CheckBox checkBox = new CheckBox();
                checkBox.Appearance = Appearance.Button;
                checkBox.Dock = DockStyle.Left;
                checkBox.Font = checkBox_OppositeDirection.Font;
                //checkBox.Location = new Point(430, 0);
                checkBox.Location = checkBox_OppositeDirection.Location;
                checkBox.Name = "checkBox_OppositeDirection" + externalcount.ToString();
                //checkBox.Size = new Size(170, 28);
                checkBox.Size = checkBox_OppositeDirection.Size;
                checkBox.TextAlign = ContentAlignment.MiddleCenter;
                checkBox.UseVisualStyleBackColor = true;
                checkBox.Text = "Positive";
                checkBox.CheckedChanged += checkbox_CheckedChanged;

                panel1.Controls.Add(checkBox);
                panel1.Controls.Add(label1);
                panel1.Controls.Add(label);
                Panel_ExternalSetting.Controls.Add(panel1);
                externalcount += 1;
                Panel_ExternalSetting.Height += 30;
                #endregion
            }

            #region Old
            /*Panel[] externalvalues = Panel_ExternalSetting.Controls.OfType<Panel>().ToArray();
            for (int j = 1; j < externalvalues.Length; j++)//0:panel5(test) ,1:Panel_Track
            {
                if (externalvalues[j].Name.Contains(newstrtype))//比对每个存在的Panel,Name(Panel_ExternalValueTrack1) 是不是 Track or Platform
                {
                    externalvalues[j].Controls.OfType<Label>().First().Text = externaltype + ;
                    hasexternal = true;
                    break;
                }
            }*/
            /*if (!hasexternal)
            {
                //Add panel
            }*/
            #endregion

        }
        //删除并将后者前移
        public void RemoveExternal(Panel panel)
        {
            string newstrtype = panel.Name.Remove(0, 6);//Plane_Track => Track
            Panel[] externalvalues = Panel_ExternalSetting.Controls.OfType<Panel>().ToArray();
            int externaldeletecount = 0;
            int lastdeleteindex = 0;
            bool needtodelete = false;
            for (int j = 1; j < externalvalues.Length; j++)//0:panel5,1:Panel_ExternalValue,2:Track
            {
                if (externalvalues[j].Name.Contains(newstrtype))//if has Track
                {
                    Panel_ExternalSetting.Controls.Remove(externalvalues[j]);
                    externaldeletecount += 1;
                    lastdeleteindex = j;
                    externalcount -= 1;
                    Panel_ExternalSetting.Height -= 30;
                    if(lastdeleteindex != externalvalues.Length - 1)
                        needtodelete = true;
                }
            }
            if (needtodelete)//后面往前移when j = 2 (E1)
            {
                int i = lastdeleteindex;
                for (i += 1; i < externalvalues.Length; i++)//beginning from j = 3 (E2)
                {
                    externalvalues[i].Location = new Point(0, externalvalues[i].Location.Y - (30 * externaldeletecount));
                    Label label = externalvalues[i].Controls.OfType<Label>().Last();
                    string oldexternalnumber = label.Text;
                    int.TryParse(oldexternalnumber.TrimStart('E'), out int k);
                    label.Text = "E" + (k - 1).ToString();
                }
            }
        }
        #endregion

        private void CheckUpdate_Click(object sender, EventArgs e)
        {
            UpdateRobimUpdateForm form = new UpdateRobimUpdateForm(ver);
            form.Show();
            //string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries\\RobimUpdate.exe");
            //Process.Start(dir, ver);//A程序完整路径
        }

        private void Track_Hang_Up_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox.Checked)
            {
                Track_Hang_Up_Axis.Enabled = true;
                Track_Hang_Up_Axis.SelectedIndex = 0;
            }
            else
            {
                Track_Hang_Up_Axis.Enabled = false;
            }
        }
    }
}