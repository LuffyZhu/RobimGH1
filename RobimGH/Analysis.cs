using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Rhino.Geometry;
using ReactiveUI;
using System.Text.RegularExpressions;

namespace Robim
{
    public partial class Analysis : Form
    {
        public Analysis(RobotSystem robot, Program program)
        {
            InitializeComponent();

            Robot = robot;
            Program = program;

            //Initial settings
            GetJointColors();
            GetJointRangeColors(10);

            LayoutSizeSetting();
            JointCheckBoxSetting();

            GetJointNum();

            this.Icon = Robim.Properties.Resources.RobimFormlogo;

            //Draw chart    
            DrawChart();



        }


        public RobotSystem Robot { get; set; }
        public Program Program { get; set; }
        public AnalysisViewModel ViewModel { get; set; }



        // joint colors
        private List<Color> jointRangeColors;

        private List<Color> jointColors;



        private bool checkedListBox1MouseDown;

        private int changedJointIndex = 1000;

        private CheckState changedJointState = CheckState.Unchecked;




        // chart1 style field
        private SeriesChartType chartType = SeriesChartType.Line;
        private MarkerStyle markerStyle = MarkerStyle.Square;
        private int markerSize = 5;



        //jointNum
        private List<int> jointNum = new List<int>();

        //tablelayout field
        private int tableLayoutMargin = 50;

        // working area field
        private int workingAreaWidth = 200;

        //chart1 area field 
        private int chartAreaMargin = 10;

        //joint dispay checkbox group field
        private int jointDisplayLength = 60;

        //error display checkbox group field
        private int errorDisplayLength = 40;

        //chart interval distance(pixel)
        private int targetDistance = 5;

        //JointError
        private List<Tuple<int, string>> targetErrors = new List<Tuple<int, string>>();

        //Error column
        private Color errorColumnColor = Color.FromArgb(30, Color.Red);

        #region chart1
        private void DrawChart()
        {

            chart1.Margin = new Padding(chartAreaMargin);
            chart1.Dock = DockStyle.Fill;

            //AxisBound & Area setting 

            RobotCell robotCell = Robot as RobotCell;
            List<Interval> JointLimitation = robotCell.MechanicalGroups.First().Joints.Select(x => x.Range).ToList();
            double jMin = JointLimitation.Select(x => x.T0).ToList().Min();
            double jMax = JointLimitation.Select(x => x.T1).ToList().Max();

            double tMin = 1;
            double tMax = Program.Targets.Count();

            chart1.ChartAreas.First().AxisX.Minimum = tMin;
            chart1.ChartAreas.First().AxisX.Maximum = tMax;
            chart1.ChartAreas.First().AxisY.Minimum = jMin;
            chart1.ChartAreas.First().AxisY.Maximum = jMax;

            //int interval = (int)Math.Round((double)chart1.Width / (double)targetDistance, 0);


            //set interval 

            chart1.ChartAreas.First().AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart1.ChartAreas.First().AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;

            //if (chart1.ChartAreas.First().AxisX.Interval < 1)
            //{
            //    chart1.ChartAreas.First().AxisX.Interval = 1;
            //}

            foreach (var x in chart1.ChartAreas.First().Axes)
            {
                x.MajorGrid.LineColor = Color.LightGray;
            }

            //draw joints

            List<CellTarget> targets = Program.Targets;

            chart1.Series.Clear();

            for (int i = 0; i < jointNum.Count; i++)
            {
                chart1.Series.Add($"Joint_{jointNum[i]}");
                chart1.Series[$"Joint_{jointNum[i]}"].ChartType = chartType;
                chart1.Series[$"Joint_{jointNum[i]}"].MarkerStyle = markerStyle;
                chart1.Series[$"Joint_{jointNum[i]}"].MarkerSize = markerSize;
                chart1.Series[$"Joint_{jointNum[i]}"].Color = jointColors[jointNum[i] - 1];

                for (int j = 0; j < targets.Count(); j++)
                {
                    chart1.Series[$"Joint_{jointNum[i]}"].Points.AddXY(j + 1, Robot.RadianToDegree(targets[j].Joints[jointNum[i] - 1], jointNum[i] - 1));
                }

            }

            //draw joint lnterval


            for (int i = 0; i < jointNum.Count; i++)
            {
                chart1.Series.Add($"Joint_{jointNum[i]}" + "_Upper");
                chart1.Series[$"Joint_{jointNum[i]}" + "_Upper"].ChartType = SeriesChartType.Range;
                chart1.Series[$"Joint_{jointNum[i]}" + "_Upper"].IsVisibleInLegend = false;


                chart1.Series.Add($"Joint_{jointNum[i]}" + "_Lower");
                chart1.Series[$"Joint_{jointNum[i]}" + "_Lower"].ChartType = SeriesChartType.Range;
                chart1.Series[$"Joint_{jointNum[i]}" + "_Lower"].IsVisibleInLegend = false;


                for (int j = 0; j < targets.Count; j++)
                {
                    //set points
                    chart1.Series[$"Joint_{jointNum[i]}" + "_Upper"].Points.AddXY(j + 1, (Robot as RobotCell).MechanicalGroups.First().Joints[jointNum[i] - 1].Range.Max, chart1.ChartAreas.First().AxisY.Maximum);
                    chart1.Series[$"Joint_{jointNum[i]}" + "_Lower"].Points.AddXY(j + 1, (Robot as RobotCell).MechanicalGroups.First().Joints[jointNum[i] - 1].Range.Min, chart1.ChartAreas.First().AxisY.Minimum);
                    //set color
                    Color c = jointRangeColors[jointNum[i] - 1];

                    chart1.Series[$"Joint_{jointNum[i]}" + "_Upper"].BorderColor = c;
                    chart1.Series[$"Joint_{jointNum[i]}" + "_Upper"].Color = c;

                    chart1.Series[$"Joint_{jointNum[i]}" + "_Lower"].BorderColor = c;
                    chart1.Series[$"Joint_{jointNum[i]}" + "_Lower"].Color = c;

                }
            }

            //draw problem area
            List<string> errors = Program.Errors;
            for (int i = 0; i < errors.Count; i++)
            {
                //string pattern = "[1-9]/d*";
                string pattern = @"Errors in target \d+ of robot 0:";
                //string pattern = "[1-9]/d*";

                List<List<string>> errStruct = new List<List<string>>();

                Match match = Regex.Match(errors[i], pattern);
                if (match.Success)
                {
                    pattern = @"\d+";
                    match = Regex.Match(match.Value, pattern);
                    if (match.Success)
                    {
                        int index = int.Parse(match.Value);
                        targetErrors.Add(Tuple.Create(index, errors[i]));


                        //column color
                        chart1.Series.Add($"Target_{index}Error_{i}");

                        chart1.Series[$"Target_{index}Error_{i}"].ChartType = SeriesChartType.Range;

                        chart1.Series[$"Target_{index}Error_{i}"].Color = errorColumnColor;

                        //column xy
                        chart1.Series[$"Target_{index}Error_{i}"].Points.AddXY(index, jMax, jMin);
                        chart1.Series[$"Target_{index}Error_{i}"].Points.AddXY(index + 1, jMax, jMin);
                        chart1.Series[$"Target_{index}Error_{i}"].IsVisibleInLegend = false;

                        //chart1.Series[$"Target_{index}Error_{i}"]["PixelPointWidth"] = "100";
                    }

                }
            }


            chart1.Update();
        }

        private void GetJointColors()
        {
            jointColors = new List<Color>();

            jointColors.Add(Color.Red);
            jointColors.Add(Color.Green);
            jointColors.Add(Color.Blue);
            jointColors.Add(Color.Yellow);
            jointColors.Add(Color.Lime);
            jointColors.Add(Color.Fuchsia);
        }


        private void GetJointRangeColors(int transparency)
        {
            jointRangeColors = new List<Color>();

            for (int i = 0; i < jointColors.Count; i++)
            {
                Color tmpColor = jointColors[i];
                jointRangeColors.Add(Color.FromArgb(transparency, tmpColor.R, tmpColor.G, tmpColor.B));
            }

        }
        #endregion

        private void GetJointNum()
        {

            jointNum.Clear();

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                //if (checkedListBox1.GetItemChecked(i) == true)
                if (checkedListBox1.GetItemChecked(i) == true)
                {
                    jointNum.Add(i + 1);

                }


            }
            if (jointNum.Contains(changedJointIndex + 1))
            {

                if (changedJointState == CheckState.Unchecked)
                {
                    jointNum.Remove(changedJointIndex + 1);
                }

            }
            else
            {
                if (changedJointState == CheckState.Checked)
                {
                    jointNum.Add(changedJointIndex + 1);
                }
            }

            jointNum.Sort();
        }

        private void JointCheckBoxSetting()
        {

            checkedListBox1.BackColor = this.BackColor;

            checkedListBox1.BorderStyle = BorderStyle.None;
            checkedListBox1.Items.Clear();

            for (int i = 0; i < 6; i++)
            {
                checkedListBox1.Items.Add($"Joint_{i + 1}");

                checkedListBox1.SetItemCheckState(i, CheckState.Checked);
            }

        }

        private void LayoutSizeSetting()
        {
            tableLayoutPanel1.Dock = DockStyle.Fill;

            //work area size 
            tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
            tableLayoutPanel1.ColumnStyles[1].Width = workingAreaWidth;

            //chart1 area size
            chart1.Margin = new Padding(chartAreaMargin);
            chart1.Dock = DockStyle.Fill;

            //joint display size
            tableLayoutPanel2.RowStyles[0].Height = jointDisplayLength;
            tableLayoutPanel2.RowStyles[1].Height = errorDisplayLength;

            groupBox1.Padding = new Padding(0);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Height = (int)tableLayoutPanel2.RowStyles[0].Height;


            checkedListBox1.Dock = DockStyle.Fill;

        }


        private void ChartResize(object sender, EventArgs e)
        {
            LayoutSizeSetting();
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void Analysis_Load(object sender, EventArgs e)
        {

        }


        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (checkedListBox1.Items == null || checkedListBox1.Items.Count < 6)
            {
                return;
            }
            if (checkedListBox1MouseDown && e.CurrentValue != e.NewValue)
            {
                changedJointIndex = e.Index;
                changedJointState = e.NewValue;
                checkedListBox1MouseDown = false;

                GetJointNum();
                DrawChart();
            }



        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_MouseDown(object sender, MouseEventArgs e)
        {
            checkedListBox1MouseDown = true;
        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
