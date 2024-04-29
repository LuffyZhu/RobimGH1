using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Robim.Grasshopper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Math;

namespace Robim
{
    public partial class CollisionComputeForm : Form
    {
        public Collision collision { get; set; }
        GH_Program program = null;
        List<GH_Integer> first = new List<GH_Integer>();
        List<GH_Integer> second = new List<GH_Integer>();
        GH_Mesh environment = null;
        int environmentPlane = -1;
        double linearStep = 100;
        double angularStep = PI / 4;
        int i = 0;
        BackgroundWorker BackgroundWorker = new BackgroundWorker();
        bool status = false;
        public CollisionComputeForm(GH_Program gH_Program, List<GH_Integer> first, List<GH_Integer> second, GH_Mesh environment, int environmentPlane, double linearStep, double angularStep)
        {
            InitializeComponent();
            InitializeBackgroundWorker();
            this.program = gH_Program;
            this.first = first;
            this.second = second;
            this.environment = environment;
            this.environmentPlane = environmentPlane;
            this.linearStep = linearStep;
            this.angularStep = angularStep;
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
            label1.Text = e.ProgressPercentage.ToString() + "%";
            progressBar1.Value = e.ProgressPercentage;
        }
        System.Threading.Timer timer;
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            status = true;
            label1.Text = "Done";
            ManualResetEvent timerDisposed = new ManualResetEvent(false);
            if (!timer.Dispose(timerDisposed))
            {
                timerDisposed.WaitOne();//WaitHandle.WaitOne()方法会等待收到一个信号，否则一直被阻塞
                timerDisposed.Dispose();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            this.Close();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TimerCallback callback = new TimerCallback(loadcount);
            //1.function 2.開關  3.等多久再開始  4.隔多久反覆執行
            timer = new System.Threading.Timer(callback, null, 0, 1000);
            collision = program.Value.CheckCollisions(first.Select(x => x.Value), second.Select(x => x.Value), environment?.Value, environmentPlane, linearStep, angularStep);
            //collision = new Collision(program.Value, first.Select(x => x.Value), second.Select(x => x.Value), environment.Value, environmentPlane, linearStep, angularStep);
        }

        private void CollisionComputeForm_Load(object sender, EventArgs e)
        {
            label1.Text = "Start Compute";
            //progressBar1.Step = 100 / program.Value.Targets.Count;
            BackgroundWorker.RunWorkerAsync();
            
        }
        private void loadcount(object state)
        {
            if(collision.Count != i && !status)
            {
                double a = program.Value.Targets.Count / 100.00;
                BackgroundWorker.ReportProgress((int)(collision.Count / a));
                i = collision.Count;
            }
        }

        private void Stopbtn_Click(object sender, EventArgs e)
        {
            Collision.EmergenceStop = true;
        }
    }
}
