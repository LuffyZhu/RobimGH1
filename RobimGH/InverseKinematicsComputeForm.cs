using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Robim.Grasshopper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Math;
using Grasshopper;

namespace Robim
{
    public partial class InverseKinematicsComputeForm : Form
    {
        public Program Program { get; internal set; }
        public bool ComputeStop { get; internal set; }

        new string Name = null;
        RobotSystem RobotSystem = null;
        List<IToolpath> Toolpaths = new List<IToolpath>();
        Commands.Group InitCommands = null;
        List<int> MultiFileIndices = new List<int>();
        double StepSize = 0;
        //Mesh Workpiece = null;
        int i = 0;
        public BackgroundWorker BackgroundWorker { get; } = new BackgroundWorker();
        public bool status { get; internal set; } = false;
        System.Threading.Timer timer;
        public InverseKinematicsComputeForm(string name,RobotSystem robotSystem,List<IToolpath> toolpaths,Commands.Group initCommands, List<int> multiFileIndices,double stepSize)
        {
            InitializeComponent();
            this.Name = name;
            this.RobotSystem = robotSystem;
            this.Toolpaths = toolpaths;
            this.InitCommands = initCommands;
            this.MultiFileIndices = multiFileIndices;
            this.StepSize = stepSize;
            //this.Workpiece = workpiece;
            ComputeStop = false;
            InitializeBackgroundWorker();
        }
        public void Compute()
        {
            ComputeStop = false;
            BackgroundWorker.RunWorkerAsync();
        }
        // Set up the BackgroundWorker object by 
        // attaching event handlers. 
        private void InitializeBackgroundWorker()
        {
            BackgroundWorker.DoWork += BackgroundWorker_DoWork;
            BackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            BackgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            BackgroundWorker.WorkerReportsProgress = true;
            BackgroundWorker.WorkerSupportsCancellation = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Rhino.RhinoApp.WriteLine($"IK Computing : {e.ProgressPercentage}%");
            label1.Text = e.ProgressPercentage.ToString() + "%";
            progressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Rhino.RhinoApp.WriteLine("IK Computing : Done");
            status = true;
            label1.Text = "Done";
            ManualResetEvent timerDisposed = new ManualResetEvent(false);
            if (!timer.Dispose(timerDisposed))
            {
                timerDisposed.WaitOne();//WaitHandle.WaitOne()方法会等待收到一个信号，否则一直被阻塞
                timerDisposed.Dispose();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            this.Dispose();
            this.Close();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TimerCallback callback = new TimerCallback(loadcount);
            //1.function 2.開關  3.等多久再開始  4.隔多久反覆執行
            timer = new System.Threading.Timer(callback, null, 0, 1000);
            this.Program = new Program(Name, RobotSystem, Toolpaths, InitCommands, MultiFileIndices, StepSize);
        }

        private void CollisionComputeForm_Load(object sender, EventArgs e)
        {
            label1.Text = "Start Compute";
            //progressBar1.Step = 100 / program.Value.Targets.Count;
            BackgroundWorker.RunWorkerAsync();
            
        }
        private void loadcount(object state)
        {
            //int stepcount = CheckProgram.Stepcount;
            int stepcount = 0;
            if (stepcount != i && !status)
            {
                double a = 1 / 100.00;
                BackgroundWorker.ReportProgress((int)(stepcount / a));
                i = stepcount;
            }
        }

        private void Stopbtn_Click(object sender, EventArgs e)
        {
            ComputeStop = true;
            CheckProgram.tokenSource.Cancel();
            CheckProgram.tokenSource.Dispose();
        }
    }
}
