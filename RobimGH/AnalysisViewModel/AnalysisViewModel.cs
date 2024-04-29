using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using ReactiveUI;
using Rhino.Geometry;

namespace Robim
{
    public class AnalysisViewModel : ReactiveObject
    {
        public AnalysisViewModel(RobotSystem robot, Program program)
        {
            Robot = robot;
            Program = program;

            JointChart = new Chart();

            RenewChartCmd = ReactiveCommand.Create(()=>GetJointChart(),this.WhenAny(vm => vm.jointNum, jn => true));
        }
        public RobotSystem Robot { get; internal set; }
        public Program Program { get; internal set; }

        //Command
        public ICommand RenewChartCmd { get; set; }

        //jointNum
        private List<int> jointNum;
        public List<int> JointNum
        {
            get => jointNum;
            set => this.RaiseAndSetIfChanged(ref jointNum, value);
        }

        //joint colors field
        private List<Color> jointRangeColors = new List<Color>();
        private List<Color> jointColors = new List<Color>();

        // chart series field
        //private int chartXMin;
        //private int chartYMin;
        //private int chartXMax;
        //private int chartYMax;

        // chart style field
        private SeriesChartType chartType = SeriesChartType.Line;
        private MarkerStyle markerStyle = MarkerStyle.Square;
        private int markerSize = 5;


        ////chart area field 
        private int chartAreaMargin = 10;

        //Chart
        private Chart jointChart;
        public Chart JointChart
        {
            get => jointChart;
            set => this.RaiseAndSetIfChanged(ref jointChart, value);
        }


        //Groupbox
        private GroupBox jointGroupBox;
        public GroupBox JointGroupBox
        {
            get => jointGroupBox; 
            set=>this.RaiseAndSetIfChanged(ref jointGroupBox, value); 
        }

        private List<Color> GetJointColors()
        {
            List<Color> cs = new List<Color>();

            cs.Add(Color.Red);
            cs.Add(Color.Green);
            cs.Add(Color.Blue);
            cs.Add(Color.Yellow);
            cs.Add(Color.Lime);
            cs.Add(Color.Fuchsia);

            jointColors = cs;

            return cs;
        }
        private List<Color> GetJointRangeColors(int transparency)
        {
            List<Color> cs = new List<Color>();

            for (int i = 0; i < jointColors.Count; i++)
            {
                Color tmpColor = jointColors[i];
                cs.Add(Color.FromArgb(transparency, tmpColor.R, tmpColor.G, tmpColor.B));
            }

            jointRangeColors = cs;

            return cs;
        }

        public Chart GetJointChart()
        {
            Chart chart = new Chart();
            ChartArea chartArea = new ChartArea();
            chart.ChartAreas.Add(chartArea);

            GetJointColors();
            GetJointRangeColors(30);

            chart.Margin = new Padding(chartAreaMargin);
            chart.Dock = DockStyle.Fill;

            //AxisBound & Area setting 

            RobotCell robotCell = Robot as RobotCell;
            List<Interval> JointLimitation = robotCell.MechanicalGroups.First().Joints.Select(x => x.Range).ToList();
            double jMin = JointLimitation.Select(x => x.T0).ToList().Min();
            double jMax = JointLimitation.Select(x => x.T1).ToList().Max();

            double tMin = 0;
            double tMax = Program.Targets.Count();

            chart.ChartAreas.First().AxisX.Minimum = tMin;
            chart.ChartAreas.First().AxisX.Maximum = tMax;
            chart.ChartAreas.First().AxisY.Minimum = jMin;
            chart.ChartAreas.First().AxisY.Maximum = jMax;

            chart.ChartAreas.First().AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart.ChartAreas.First().AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;

            foreach (var x in chart.ChartAreas.First().Axes)
            {
                x.MajorGrid.LineColor = Color.LightGray;
            }

            //draw joints

            List<CellTarget> targets = Program.Targets;

            chart.Series.Clear();

            for (int i = 0; i < jointNum.Count; i++)
            {
                chart.Series.Add($"Joint_{jointNum[i]}");
                chart.Series[$"Joint_{jointNum[i]}"].ChartType = chartType;
                chart.Series[$"Joint_{jointNum[i]}"].MarkerStyle = markerStyle;
                chart.Series[$"Joint_{jointNum[i]}"].MarkerSize = markerSize;

                for (int j = 0; j < targets.Count(); j++)
                {
                    chart.Series[$"Joint_{jointNum[i]}"].Points.AddXY(j, Robot.RadianToDegree(targets[j].Joints[jointNum[i] - 1], jointNum[i] - 1));
                }
            }

            //draw joint lnterval


            for (int i = 0; i < jointNum.Count; i++)
            {
                chart.Series.Add($"Joint_{jointNum[i]}" + "_Upper");
                chart.Series[$"Joint_{jointNum[i]}" + "_Upper"].ChartType = SeriesChartType.Range;
                chart.Series[$"Joint_{jointNum[i]}" + "_Upper"].IsVisibleInLegend = false;


                chart.Series.Add($"Joint_{jointNum[i]}" + "_Lower");
                chart.Series[$"Joint_{jointNum[i]}" + "_Lower"].ChartType = SeriesChartType.Range;
                chart.Series[$"Joint_{jointNum[i]}" + "_Lower"].IsVisibleInLegend = false;


                for (int j = 0; j < targets.Count; j++)
                {
                    //set points
                    chart.Series[$"Joint_{jointNum[i]}" + "_Upper"].Points.AddXY(j, (Robot as RobotCell).MechanicalGroups.First().Joints[jointNum[i] - 1].Range.Max, chart.ChartAreas.First().AxisY.Maximum);
                    chart.Series[$"Joint_{jointNum[i]}" + "_Lower"].Points.AddXY(j, (Robot as RobotCell).MechanicalGroups.First().Joints[jointNum[i] - 1].Range.Min, chart.ChartAreas.First().AxisY.Minimum);
                    //set color
                    Color c = jointRangeColors[jointNum[i] - 1];

                    chart.Series[$"Joint_{jointNum[i]}" + "_Upper"].BorderColor = c;
                    chart.Series[$"Joint_{jointNum[i]}" + "_Upper"].Color = c;

                    chart.Series[$"Joint_{jointNum[i]}" + "_Lower"].BorderColor = c;
                    chart.Series[$"Joint_{jointNum[i]}" + "_Lower"].Color = c;

                }
            }


            jointChart = chart;
            return chart;
        }


        //private ChartAreaParam()
        //{
        //    //AxisBound setting 
        //    RobotCell robotCell = Robot as RobotCell;
        //    List<Interval> JointLimitation = robotCell.MechanicalGroups.First().Joints.Select(x => x.Range).ToList();
        //    double jMin = JointLimitation.Select(x => x.T0).ToList().Min();
        //    double jMax = JointLimitation.Select(x => x.T1).ToList().Max();

        //    double tMin = 0;
        //    double tMax = Program.Targets.Count();
        //}
    }
}
