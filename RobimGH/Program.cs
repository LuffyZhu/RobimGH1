using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Robim;
using Robim.Grasshopper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static System.Math;

namespace Robim.Grasshopper
{
    public enum ComputeStatus { Stop, Computing, Done }
    public sealed class CreateProgram : GH_Component, IGH_VariableParameterComponent
    {
        new string Name = null;
        RobotSystem RobotSystem = null;
        List<IToolpath> Toolpaths = new List<IToolpath>();
        Robim.Commands.Group InitCommands = null;
        List<int> MultiFileIndices = new List<int>();
        double StepSize = 0;
        //Mesh Workpiece = null;
        Program Program = null;

        public CreateProgram() : base("Create program", "Program", "Creates a program, checks for possible issues and fixes common mistakes", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("{5186EFD5-C042-4CA9-A7D2-E143F4848DEF}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconCreateProgram;
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Program name", GH_ParamAccess.item, "DefaultProgram");
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system used in program", GH_ParamAccess.item);
            pManager.AddParameter(new ToolpathParameter(), "Targets 1", "T1", "List of targets or toolpaths for the first or only robot.", GH_ParamAccess.list);
            pManager.AddParameter(new ToolpathParameter(), "Targets 2", "T2", "List of targets or toolpaths for a second coordinated robot.", GH_ParamAccess.list);
            pManager.AddParameter(new CommandParameter(), "Init commands", "C", "Optional list of commands that will run at the start of the program", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Multifile indices", "I", "Optional list of indices to split the program into multiple files. The indices correspond to the first target of the aditional files", GH_ParamAccess.list);
            pManager.AddNumberParameter("Step Size", "S", "Distance in mm to step through linear motions, used for error checking and program simulation. Smaller is more accurate but slower.Default =10", GH_ParamAccess.item, 10);//原本是1
            //pManager.AddTextParameter("Custom PR", "P", "Custom Promgram Register ", GH_ParamAccess.list);
            pManager.AddBooleanParameter("UseIkfast", "UseIK", "Use ikfast.dll", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("OneFileCode", "1FC", "Save code as one file (currently only for Kuka robots)", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Compute", "C", "Compute", GH_ParamAccess.item, false);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            // pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            pManager.AddTextParameter("Code", "C", "Code", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Duration", "D", "Program duration in seconds", GH_ParamAccess.item);
            pManager.AddTextParameter("Warnings", "W", "Warnings in program", GH_ParamAccess.list);
            pManager.AddTextParameter("Errors", "E", "Errors in program", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Progress", "Num", "Inverse kinematic compute progress", GH_ParamAccess.item);
            if (Attributes == null) CreateAttributes();
            Params.Output[3].Attributes = new ReportWarnings(Params.Output[3], this.Attributes);
            Params.Output[4].Attributes = new ReportErrors(Params.Output[4], this.Attributes);
        }
        
        public static List<string> reportwarnings;
        public static List<string> reporterrors;
        LicenseManage licenseManage = null;
        protected override void BeforeSolveInstance()
        {
            if(licenseManage == null || !licenseManage.LicensePass)
            {
                licenseManage = new LicenseManage(this);
            }

        }
        BackgroundWorker Backgroundworker;
        ComputeStatus computeStatus = ComputeStatus.Stop;
        bool recompute = false;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = null;
            GH_RobotSystem robotSystem = null;
            var initCommandsGH = new List<GH_Command>();
            var toolpathsA = new List<GH_Toolpath>();
            var toolpathsB = new List<GH_Toolpath>();
            var multiFileIndices = new List<int>();
            double stepSize = 1;
            //var prInfo = new List<string>();
            //Mesh workpiece = null;

            if (!DA.GetData(0, ref name)) { return; }
            if (!DA.GetData(1, ref robotSystem)) { return; }
            if (!DA.GetDataList(2, toolpathsA)) { return; }
            DA.GetDataList(3, toolpathsB);
            DA.GetDataList(4, initCommandsGH);
            DA.GetDataList(5, multiFileIndices);
            if (!DA.GetData(6, ref stepSize)) { return; }
            //DA.GetDataList(7, prInfo);
            bool useik = false;
            DA.GetData(7, ref useik);
            KinematicSolution.UseIKFast = useik;
            bool ifOneCode = false;
            DA.GetData(8, ref ifOneCode);
            RobotCellKuka.oneFcode = ifOneCode;

            var initCommands = initCommandsGH.Count > 0 ? new Robim.Commands.Group(initCommandsGH.Select(x => x.Value)) : null;

            var toolpaths = new List<IToolpath>();
            var toolpathA = new SimpleToolpath(toolpathsA.Select(t => t.Value));
            toolpaths.Add(toolpathA);

            if (toolpathsB.Count > 0)
            {
                var toolpathB = new SimpleToolpath(toolpathsB.Select(t => t.Value));
                toolpaths.Add(toolpathB);
            }

            this.Name = name;
            this.RobotSystem = robotSystem.Value;
            this.Toolpaths = toolpaths;
            this.InitCommands = initCommands;
            this.MultiFileIndices = multiFileIndices;
            this.StepSize = stepSize;
            //this.Workpiece = workpiece;

            bool start = false;
            DA.GetData(9, ref start);
            //var program = new Program(name, robotSystem.Value, toolpaths, initCommands, multiFileIndices, stepSize);

            if (licenseManage.IsLicensePass())
            if(true)
            {
                if (start)
                {
                    if (toolpathsA.Count + toolpathsB.Count > 10000 || computeStatus != ComputeStatus.Stop)
                    {
                        switch (computeStatus)
                        {
                            case ComputeStatus.Stop:
                                this.Message = "0%";
                                using (Backgroundworker = new BackgroundWorker())
                                {
                                    Backgroundworker.ProgressChanged += BackgroundWorker_ProgressChanged;
                                    Backgroundworker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                                    Backgroundworker.DoWork += BackgroundWorker_DoWork;
                                    Backgroundworker.WorkerReportsProgress = true;
                                    Backgroundworker.WorkerSupportsCancellation = true;
                                    Backgroundworker.RunWorkerAsync();
                                };
                                DA.SetData(5, 0);
                                computeStatus = ComputeStatus.Computing;
                                break;
                            case ComputeStatus.Computing:
                                recompute = true;
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Computing");
                                IList<string> warning = RuntimeMessages(GH_RuntimeMessageLevel.Warning);
                                DA.SetDataList(3, warning);
                                break;
                            case ComputeStatus.Done:
                                var program = this.Program;

                                DA.SetData(0, new GH_Program(program));

                                if (program.Code != null)
                                {
                                    var path = DA.ParameterTargetPath(2);
                                    var structure = new GH_Structure<GH_String>();

                                    for (int i = 0; i < program.Code.Count; i++)
                                    {
                                        var tempPath = path.AppendElement(i);
                                        for (int j = 0; j < program.Code[i].Count; j++)
                                        {
                                            structure.AppendRange(program.Code[i][j].Select(x => new GH_String(x)), tempPath.AppendElement(j));
                                        }
                                    }
                                    DA.SetDataTree(1, structure);
                                }
                                DA.SetData(2, program.Duration);

                                reportwarnings = new List<string>();
                                if (program.Warnings.Count > 0)
                                {
                                    DA.SetDataList(3, program.Warnings);
                                    reportwarnings.AddRange(program.Warnings);
                                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Warnings in program");
                                }
                                reporterrors = new List<string>();
                                if (program.Errors.Count > 0)
                                {
                                    DA.SetDataList(4, program.Errors);
                                    reporterrors.AddRange(program.Errors);
                                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Errors in program");
                                }
                                DA.SetData(5, 100);
                                computeStatus = ComputeStatus.Stop;
                                if (recompute)
                                {
                                    recompute = false;
                                    ExpireSolution(true);
                                }
                                break;
                        }
                    }
                    else
                    {
                        this.Program = new Program(Name, RobotSystem, Toolpaths, InitCommands, MultiFileIndices, StepSize);
                        this.Message = "0%";
                        this.Program.Compute();

                        var program = this.Program;

                        DA.SetData(0, new GH_Program(program));

                        if (program.Code != null)
                        {
                            var path = DA.ParameterTargetPath(2);
                            var structure = new GH_Structure<GH_String>();

                            for (int i = 0; i < program.Code.Count; i++)
                            {
                                var tempPath = path.AppendElement(i);
                                for (int j = 0; j < program.Code[i].Count; j++)
                                {
                                    structure.AppendRange(program.Code[i][j].Select(x => new GH_String(x)), tempPath.AppendElement(j));
                                }
                            }
                            DA.SetDataTree(1, structure);
                        }
                        DA.SetData(2, program.Duration);

                        reportwarnings = new List<string>();
                        if (program.Warnings.Count > 0)
                        {
                            DA.SetDataList(3, program.Warnings);
                            reportwarnings.AddRange(program.Warnings);
                            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Warnings in program");
                        }
                        reporterrors = new List<string>();
                        if (program.Errors.Count > 0)
                        {
                            DA.SetDataList(4, program.Errors);
                            reporterrors.AddRange(program.Errors);
                            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Errors in program");
                        }
                        DA.SetData(5, 100);
                        this.Message = "Done";
                    }
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Compute");
                    IList<string> errors = RuntimeMessages(GH_RuntimeMessageLevel.Error);
                    DA.SetDataList(4, errors);
                }
                #region
                //if (computetime == 0 && start)
                //{
                //    inverseKinematicsCompute = new InverseKinematicsCompute(Name, RobotSystem, Toolpaths, InitCommands, MultiFileIndices, StepSize);
                //    inverseKinematicsCompute.BackgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
                //    inverseKinematicsCompute.BackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                //    inverseKinematicsCompute.BackgroundWorker.RunWorkerAsync();
                //    #region Open Form
                //    //GH_Canvas gH_Canvas = Instances.ActiveCanvas;
                //    //DisForm(gH_Canvas);
                //    #endregion
                //    computetime = 1;
                //}
                //else if (computetime == 1 && start)
                //{
                //    computetime = 0;
                //}
                //if (!this.ComputeStop && this.status)
                //{
                //    var program = this.Program;

                //    DA.SetData(0, new GH_Program(program));

                //    if (program.Code != null)
                //    {
                //        var path = DA.ParameterTargetPath(2);
                //        var structure = new GH_Structure<GH_String>();

                //        for (int i = 0; i < program.Code.Count; i++)
                //        {
                //            var tempPath = path.AppendElement(i);
                //            for (int j = 0; j < program.Code[i].Count; j++)
                //            {
                //                structure.AppendRange(program.Code[i][j].Select(x => new GH_String(x)), tempPath.AppendElement(j));
                //            }
                //        }
                //        DA.SetDataTree(1, structure);
                //    }
                //    DA.SetData(2, program.Duration);

                //    reportwarnings = new List<string>();
                //    if (program.Warnings.Count > 0)
                //    {
                //        DA.SetDataList(3, program.Warnings);
                //        reportwarnings.AddRange(program.Warnings);
                //        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Warnings in program");
                //    }
                //    reporterrors = new List<string>();
                //    if (program.Errors.Count > 0)
                //    {
                //        DA.SetDataList(4, program.Errors);
                //        reporterrors.AddRange(program.Errors);
                //        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Errors in program");
                //    }
                //}
                //else
                //{
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Stop Create");
                //    IList<string> errors = RuntimeMessages(GH_RuntimeMessageLevel.Error);
                //    DA.SetDataList(4, errors);
                //}
                #endregion
            }
            else
            {
                IList<string> errors = RuntimeMessages(GH_RuntimeMessageLevel.Error);
                DA.SetDataList(4, errors);
            }

            #region Other(IKFAST)
            //if (KinematicSolution.IKJointss.Count > 0)
            //{
            //    DataTree<double> dataTree = new DataTree<double>();
            //    for(int i = 0; i < KinematicSolution.IKJointss.Count; i++)
            //    {
            //        double[][] asd = KinematicSolution.IKJointss[i];
            //        for (int j = 0; j < 8; j++)
            //        {
            //            for(int k = 0; k < 6; k++)
            //            {
            //                dataTree.Add(asd[j][k], new GH_Path(i, j, k));
            //            }
            //        }
            //    }
            //    DA.SetDataTree(5, dataTree);
            //    KinematicSolution.IKJointss = new List<double[][]>();
            //}
            //if (KinematicSolution.Jointss.Count > 0)
            //{
            //    DataTree<double> dataTree = new DataTree<double>();
            //    for (int i = 0; i < KinematicSolution.Jointss.Count; i++)
            //    {
            //        double[] asd = KinematicSolution.Jointss[i];
            //        for (int j = 0; j < 6; j++)
            //        {
            //            dataTree.Add(asd[j], new GH_Path(i, j));
            //        }
            //    }
            //    DA.SetDataTree(6, dataTree);
            //    KinematicSolution.Jointss = new List<double[]>();
            //}
            #endregion
        }

        #region Backgroundworker
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Message = $"{e.ProgressPercentage}%";
            Instances.ActiveCanvas.Refresh();
            Params.Output[5].AddVolatileData(new GH_Path(0), 0, e.ProgressPercentage);
            if(this.Params.Output[5].Recipients.Count != 0) 
            {
                var id = this.Params.Output[5].Recipients[0].Attributes.GetTopLevel.InstanceGuid;
                OnPingDocument().FindObject(id, true).ExpireSolution(true);
            }
        }
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Rhino.RhinoApp.WriteLine("IK Computing : Done");
            computeStatus = ComputeStatus.Done;
            this.Message = "Done";

            //ManualResetEvent timerDisposed = new ManualResetEvent(false);
            //if (!timer.Dispose(timerDisposed))
            //{
            //    timerDisposed.WaitOne();//WaitHandle.WaitOne()方法会等待收到一个信号，否则一直被阻塞
            //    timerDisposed.Dispose();
            //    timer.Change(Timeout.Infinite, Timeout.Infinite);
            //}
            Backgroundworker.Dispose();
            ExpireSolution(true);
        }
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //TimerCallback callback = new TimerCallback(loadcount);
            ////1.function 2.開關  3.等多久再開始  4.隔多久反覆執行
            //timer = new System.Threading.Timer(callback, null, 0, 1000);
            this.Program = new Program(Name, RobotSystem, Toolpaths, InitCommands, MultiFileIndices, StepSize);
            this.Program.checkProgram.StepChange += CheckProgram_StepChange;
            this.Program.Compute();
        }

        private void CheckProgram_StepChange(int answer)
        {
            if (computeStatus == ComputeStatus.Computing)
            {
                Backgroundworker.ReportProgress(answer);
            }
        }
        //private void loadcount(object state)
        //{
        //    int stepcount = CheckProgram.Stepcount;
        //    if (stepcount != i && computeStatus == ComputeStatus.Computing)
        //    {
        //        double a = CheckProgram.CellTargetCount / 100.00;
        //        Backgroundworker.ReportProgress((int)(stepcount / a));
        //        i = stepcount;
        //    }
        //}
        #endregion

        //public override void CreateAttributes()
        //{
        //    m_attributes = new ComponentButton(this, "Create");
        //}
        InverseKinematicsComputeForm form;


        #region old
        //public void DisForm(GH_Canvas sender)
        //{
        //    form = new InverseKinematicsComputeForm(Name, RobotSystem, Toolpaths, InitCommands, MultiFileIndices, StepSize);
        //    form.FormClosing += Form_FormClosing;
        //    GH_WindowsFormUtil.CenterFormOnCursor(form, true);
        //    form.ShowDialog(sender.FindForm());
        //}
        //private void Form_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    this.Program = form.Program;
        //    //this.ComputeStop = form.ComputeStop;
        //    //this.status = form.status;
        //    form.Dispose();
        //    ExpireSolution(true);
        //}

        #endregion

        public void DisForm(GH_Canvas sender)
        {
            Analysis analysisForm = new Analysis(RobotSystem, Program);
            //analysisForm.ViewModel = new AnalysisViewModel(RobotSystem, Program);
            analysisForm.ShowDialog(sender.FindForm());
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        //Analysis
        public override void CreateAttributes()
        {
            m_attributes = new ComponentButton(this, "Analysis");
        }
    }
    public class SaveProgram : GH_Component
    {
        public SaveProgram() : base("Save program", "SaveProg", "Saves a program to a text file", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.quinary;
        public override Guid ComponentGuid => new Guid("{1DE69EAA-AA4C-44F2-8748-F19B041F8F58}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconSave;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            pManager.AddTextParameter("Folder", "F", "Folder", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Save", "S", "save program", GH_ParamAccess.item, false);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }
        IGH_Param[] parameters = new IGH_Param[1]
        {
            new Param_String() { Name = "Ratio", NickName = "Ratio", Description = "Ratio", Optional = true }
        };

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Ratio", AddRatio, true, Params.Input.Any(x => x.Name == "Ratio"));
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
        private void AddRatio(object sender, EventArgs e) => AddParam(0);
        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool hasRatio = Params.Input.Any(x => x.Name == "Ratio");
            GH_Program program = null;
            var code = new List<string>();
            string folder = null;
            bool save = false;
            string ratio = "0";
            

            if (!DA.GetData(0, ref program)) { return; }
            if (!DA.GetData(1, ref folder)) { return; }
            DA.GetData(2, ref save);
            if (hasRatio)
            {
                if (!DA.GetData("Ratio", ref ratio)) ratio = "0";
            }
            Console.WriteLine(program.Value);
            if (save)
            {
                program.Value.Save(folder);
                //RobotSystem is UR & has external
                if (program.Value.RobotSystem.ToString().Contains("RobotCellUR") && program.Value.CurrentSimulationTarget.Joints.Length > 6)
                {
                    string debugPath = System.Environment.CurrentDirectory;           //此c#项目的debug文件夹路径
                    //string pyexePath = @"C:\Users\Kevin_RoboticPlus\Desktop\URTransform.exe";
                    //python文件所在路径，一般不使用绝对路径，此处仅作为例子，建议转移到debug文件夹下
                    string filename = $"{program.Value.Name}.script";
                    string filename2 = $"{program.Value.Name}_new.script";
                    string filepath = System.IO.Path.Combine(folder, filename);
                    string filepath2 = System.IO.Path.Combine(folder, filename2);
                    /*Process p = new Process();
                    p.StartInfo.FileName = pyexePath;//需要执行的文件路径
                    p.StartInfo.UseShellExecute = false; //必需
                    p.StartInfo.RedirectStandardOutput = true;//输出参数设定
                    p.StartInfo.RedirectStandardInput = true;//传入参数设定
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.Arguments = $"{filepath} {ratio}";//参数以空格分隔，如果某个参数为空，可以传入””
                    p.Start();*/
                    //string output = p.StandardOutput.ReadToEnd();
                    //p.WaitForExit();//关键，等待外部程序退出后才能往下执行}
                    //AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, output);
                    //p.Close();

                    byte[] myResBytes = Properties.Resources.URTransform;
                    string tempExeName = Path.Combine(folder, "URTransform.exe");
                    using (FileStream fsDst = new FileStream(tempExeName, FileMode.Create, FileAccess.Write))
                    {
                        byte[] bytes = myResBytes;
                        fsDst.Write(bytes, 0, bytes.Length);
                        fsDst.Close();
                        fsDst.Dispose();
                    }
                    Process p = Process.Start(tempExeName, $"{filepath} {ratio}");
                    if (!p.HasExited)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        //Console.WriteLine(output);
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, output);
                        string err = p.StandardError.ReadToEnd();
                        //Console.WriteLine(err);
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err);
                        p.WaitForExit();//关键，等待外部程序退出后才能往下执行}
                        p.Close();
                    }
                    Process[] ps = Process.GetProcessesByName("URTransform");
                    foreach(Process pr in ps)
                        pr.Kill();

                    System.IO.File.Delete(filepath);
                    System.IO.File.Delete(tempExeName);
                    System.IO.File.Move(filepath2, filepath);
                }
            }
        }
    }
    public class CustomCode : GH_Component
    {
        public CustomCode() : base("Custom code", "Custom", "Creates a program using manufacturer specific custom code. This program cannot be simulated", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("{FF997511-4A84-4426-AB62-AF94D19FF58F}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconCustomCode;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            pManager.AddTextParameter("Code", "C", "Custom code", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Program program = null;
            GH_Structure<GH_String> codeTree;

            if (!DA.GetData(0, ref program)) { return; }
            if (!DA.GetDataTree(1, out codeTree)) { return; }

            var code = new List<List<List<string>>>();
            code.Add(new List<List<string>>());

            foreach (var branch in codeTree.Branches)
            {
                code[0].Add(branch.Select(s => s.Value).ToList());
            }

            var programCode = program.Value.Code;
            if (programCode != null && programCode.Count > 0)
            {
                //var copyCode = programCode.ToList();

                //for (int i = 0; i < copyCode.Count; i++)
                //{
                //    copyCode[i] = copyCode[i].ToList();

                //    for (int j = 0; j < copyCode[i].Count; j++)
                //        copyCode[i][j] = copyCode[i][j].ToList();
                //}

                //copyCode[0][0] = code;

                var newProgram = program.Value.CustomCode(code);
                DA.SetData(0, new GH_Program(newProgram));
            }
        }
    }
    public sealed class CheckCollisions : GH_Component, IGH_VariableParameterComponent
    {
        public CheckCollisions() : base("Check collisions", "Collisions", "Checks for possible collisions. Will test if any object from group A collide with any objects from group B.", "Robim", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public override Guid ComponentGuid => new Guid("{2848F557-8DF4-415A-800B-261E782E92F8}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconCheckCollisions;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            pManager.AddIntegerParameter("First set", "A", "First set of objects. Input a list of index values that correspond to the first collision group. The order is the same as the meshes output of the kinematics component. The environment would be an additional last mesh.\n0-6 is robot joints, 7 is tool, 8 is workpiece, 9 is environment\nIf has external,external joint will add above others", GH_ParamAccess.list, new int[] { 7 });
            pManager.AddIntegerParameter("Second set", "B", "Second set of objects. Input a list of index values that correspond to the second collision group. The order is the same as the meshes output of the kinematics component. The environment would be an additional last mesh.", GH_ParamAccess.list, new int[] { 0,1,2,3,4 });
            pManager.AddMeshParameter("Environment", "E", "Single mesh object representing the environment", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Environment plane", "P", "If attached to the robot, plane index where the environment is attached to", GH_ParamAccess.item, -1);
            pManager.AddNumberParameter("Linear step size", "Ls", "Linear step size in mm to check for collisions", GH_ParamAccess.item, 100);
            pManager.AddNumberParameter("Angular step size", "As", "Angular step size in rad to check for collisions", GH_ParamAccess.item, PI / 4);
            pManager.AddBooleanParameter("Compute", "C", "Compute", GH_ParamAccess.item, false);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Collision found", "C", "True if a collision was found", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Target index", "I", "Index of the first target where a collision was found (targets are not necessarily calculated in order)", GH_ParamAccess.item);
            pManager.AddMeshParameter("Collided meshes", "M", "Meshes involved in the collision", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Progress", "Num", "Collision compute progress", GH_ParamAccess.item);
        }
        Collision collision = null;
        GH_Program program = null;
        List<GH_Integer> first = new List<GH_Integer>();
        List<GH_Integer> second = new List<GH_Integer>();
        GH_Mesh environment = null;
        int environmentPlane = -1;
        double linearStep = 100;
        double angularStep = PI / 4;

        BackgroundWorker Backgroundworker;
        ComputeStatus computeStatus = ComputeStatus.Stop;
        bool recompute = false;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            first = new List<GH_Integer>();
            second = new List<GH_Integer>();
            if (!DA.GetData(0, ref program)) { return; }
            if (!DA.GetDataList(1, first)) { return; }
            if (!DA.GetDataList(2, second)) { return; }
            DA.GetData(3, ref environment);
            if (!DA.GetData(4, ref environmentPlane)) { return; }
            if (!DA.GetData(5, ref linearStep)) { return; }
            if (!DA.GetData(6, ref angularStep)) { return; }

            bool start = false;
            DA.GetData(7, ref start);

            if (start)
            {
                //if (program.Value.Targets.Count > 1000 || computeStatus != ComputeStatus.Stop)
                if (false)
                {
                    switch (computeStatus)
                    {
                        case ComputeStatus.Stop:
                            using (Backgroundworker = new BackgroundWorker())
                            {
                                Backgroundworker.DoWork += BackgroundWorker_DoWork;
                                Backgroundworker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                                Backgroundworker.ProgressChanged += BackgroundWorker_ProgressChanged;
                                Backgroundworker.WorkerReportsProgress = true;
                                Backgroundworker.WorkerSupportsCancellation = true;
                                Backgroundworker.RunWorkerAsync();
                            };
                            DA.SetData(3, 0);
                            computeStatus = ComputeStatus.Computing;
                            break;
                        case ComputeStatus.Computing:
                            recompute = true;
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Computing");
                            break;
                        case ComputeStatus.Done:
                            DA.SetData(0, collision.HasCollision);
                            if (collision.HasCollision)
                            {
                                DA.SetData(1, collision.CollisionTarget.Index);
                                //DA.SetDataList(2, collision.Meshes);
                            }
                            DA.SetDataList(2, collision.Meshes);
                            DA.SetData(3, 100);
                            computeStatus = ComputeStatus.Stop;
                            if (recompute)
                            {
                                recompute = false;
                                ExpireSolution(true);
                            }
                            break;
                    }
                }
                else
                {
                    collision = program.Value.CheckCollisions(first.Select(x => x.Value), second.Select(x => x.Value), environment?.Value, environmentPlane, linearStep, angularStep);
                    collision.RunAllCompute();

                    DA.SetData(0, collision.HasCollision);
                    if (collision.HasCollision)
                    {
                        DA.SetData(1, collision.CollisionTarget.Index);
                        //DA.SetDataList(2, collision.Meshes);
                    }
                    DA.SetDataList(2, collision.Meshes);
                    DA.SetData(3, 100);
                }
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Compute");
            }
            
            #region
            //if (computetimes == 0 && start)
            //{
            //    GH_Canvas gH_Canvas = new GH_Canvas();
            //    DisForm(gH_Canvas);
            //    computetimes = 1;
            //}
            //else if(computetimes == 1 && start)
            //{
            //    computetimes = 0;
            //}
            //if (collision != null)
            //{
            //    if (Collision.EmergenceStop)
            //    {
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cancel Compute");
            //    }
            //    DA.SetData(0, collision.HasCollision);
            //    if (collision.HasCollision)
            //    {
            //        DA.SetData(1, collision.CollisionTarget.Index);
            //        //DA.SetDataList(2, collision.Meshes);
            //    }
            //    DA.SetDataList(2, collision.Meshes);
            //}
            #endregion
        }

        #region Backgroundworker
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Message = $"{e.ProgressPercentage}%";
            Instances.ActiveCanvas.Refresh();
            Params.Output[3].AddVolatileData(new GH_Path(0), 0, e.ProgressPercentage);
            if (this.Params.Output[3].Recipients.Count != 0)
            {
                var id = this.Params.Output[3].Recipients[0].Attributes.GetTopLevel.InstanceGuid;
                OnPingDocument().FindObject(id, true).ExpireSolution(true);
            }
        }
        System.Threading.Timer timer;
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            computeStatus = ComputeStatus.Done;
            this.Message = "Done";

            ManualResetEvent timerDisposed = new ManualResetEvent(false);
            if (!timer.Dispose(timerDisposed))
            {
                timerDisposed.WaitOne();//WaitHandle.WaitOne()方法会等待收到一个信号，否则一直被阻塞
                timerDisposed.Dispose();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            Backgroundworker.Dispose();
            ExpireSolution(true);
        }
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            collision = program.Value.CheckCollisions(first.Select(x => x.Value), second.Select(x => x.Value), environment?.Value, environmentPlane, linearStep, angularStep);
            TimerCallback callback = new TimerCallback(loadcount);
            //1.function 2.開關  3.等多久再開始  4.隔多久反覆執行
            timer = new System.Threading.Timer(callback, null, 0, 1000);
            collision.RunAllCompute();
            //collision = new Collision(program.Value, first.Select(x => x.Value), second.Select(x => x.Value), environment.Value, environmentPlane, linearStep, angularStep);
        }
        int i = 0;
        private void loadcount(object state)
        {
            int c = collision.Count;
            if (c != i && computeStatus == ComputeStatus.Computing)
            {
                double a = program.Value.Targets.Count / 100.00;
                Backgroundworker.ReportProgress((int)(c / a));
                i = c;
            }
        }
        
        #endregion

        CollisionComputeForm form;
        public void DisForm(GH_Canvas sender)
        {
            form = new CollisionComputeForm(program,first,second,environment,environmentPlane,linearStep,angularStep);
            form.FormClosing += Form_FormClosing;
            GH_WindowsFormUtil.CenterFormOnCursor(form, true);
            form.ShowDialog(sender.FindForm());
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            collision = form.collision;
            form.Dispose();
            ExpireSolution(true);
        }
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
    public class ProgramUI : GH_ComponentAttributes
    {
        public ProgramUI(GH_Component owner) : base(owner)
        {

        }
        protected override void Layout()
        {
            base.Layout();
            // Compute the width of the NickName of the owner (plus some extra padding),
            // then make sure we have at least 80 pixels.
            //int width = GH_FontServer.StringWidth(Owner.NickName, GH_FontServer.Standard);
            //width = Math.Max(width + 10, 100);

            // The height of our object is always 60 pixels
            //int height = 100;

            // Assign the width and height to the Bounds property.
            // Also, make sure the Bounds are anchored to the Pivot
            //Bounds = new RectangleF(Pivot, new SizeF(width, height));
            Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
            //rec0.Height += 42;
            Rectangle rec1 = rec0;
            rec1.X = rec0.Left;
            rec1.Y = rec0.Bottom + 20;
            rec1.Width = rec0.Width;
            rec1.Height = 200;
            //rec1.Inflate(-2, -2);
            Rectangle rec2 = rec0;
            rec2.X = rec1.Right;
            rec2.Y = rec0.Bottom - 42;
            rec2.Width = (rec0.Width) / 3;
            rec2.Height = 22;
            rec2.Inflate(-2, -2);
            Rectangle rec3 = rec0;
            rec3.X = rec2.Right;
            rec3.Y = rec0.Bottom - 42;
            rec3.Width = (rec0.Width) / 3;
            rec3.Height = 22;
            rec3.Inflate(-2, -2);
            Rectangle rec4 = rec0;
            rec4.X = rec0.Left + 2;
            rec4.Y = rec0.Bottom - 22;
            rec4.Width = (rec0.Width) / 3;
            rec4.Height = 22;
            rec4.Inflate(-2, -2);
            Rectangle rec5 = rec0;
            rec5.X = rec4.Right;
            rec5.Y = rec0.Bottom - 22;
            rec5.Width = (rec0.Width) / 3;
            rec5.Height = 22;
            rec5.Inflate(-2, -2);
            Rectangle rec6 = rec0;
            rec6.X = rec5.Right;
            rec6.Y = rec0.Bottom - 22;
            rec6.Width = (rec0.Width) / 3;
            rec6.Height = 22;
            rec6.Inflate(-2, -2);
            Bounds = rec0;
            ButtonBounds = rec1;
            ButtonBounds2 = rec2;
            ButtonBounds3 = rec3;
            ButtonBounds4 = rec4;
            ButtonBounds5 = rec5;
            ButtonBounds6 = rec6;
        }
        private Rectangle ButtonBounds { get; set; }
        private Rectangle ButtonBounds2 { get; set; }
        private Rectangle ButtonBounds3 { get; set; }
        private Rectangle ButtonBounds4 { get; set; }
        private Rectangle ButtonBounds5 { get; set; }
        private Rectangle ButtonBounds6 { get; set; }
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
                /*GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Blue, "X", 2, 0);
                button.Render(graphics, Selected, Owner.Locked, false);
                button.Dispose();
                GH_Capsule button2 = GH_Capsule.CreateTextCapsule(ButtonBounds2, ButtonBounds2, GH_Palette.Black, "Y", 2, 0);
                button2.Render(graphics, Selected, Owner.Locked, false);
                button2.Dispose();
                GH_Capsule button3 = GH_Capsule.CreateTextCapsule(ButtonBounds3, ButtonBounds3, GH_Palette.Pink, "Z", 2, 0);
                button3.Render(graphics, Selected, Owner.Locked, false);
                button3.Dispose();
                GH_Capsule button4 = GH_Capsule.CreateTextCapsule(ButtonBounds4, ButtonBounds4, GH_Palette.White, "u", 2, 0);
                button4.Render(graphics, Selected, Owner.Locked, false);
                button4.Dispose();
                GH_Capsule button5 = GH_Capsule.CreateTextCapsule(ButtonBounds5, ButtonBounds5, GH_Palette.Grey, "v", 2, 0);
                button5.Render(graphics, Selected, Owner.Locked, false);
                button5.Dispose();
                GH_Capsule button6 = GH_Capsule.CreateTextCapsule(ButtonBounds6, ButtonBounds6, GH_Palette.Error, "w", 2, 0);
                button6.Render(graphics, Selected, Owner.Locked, false);
                button6.Dispose();*/
                // Define the default palette.
                GH_Palette palette = GH_Palette.Normal;
                // Adjust palette based on the Owner's worst case messaging level.
                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        palette = GH_Palette.Warning;
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        palette = GH_Palette.Error;
                        break;
                }

                // Create a new Capsule without text or icon.
                GH_Capsule capsule = GH_Capsule.CreateCapsule(ButtonBounds, palette);

                // Render the capsule using the current Selection, Locked and Hidden states.
                // Integer parameters are always hidden since they cannot be drawn in the viewport.
                capsule.Render(graphics, Selected, Owner.Locked, true);

                // Always dispose of a GH_Capsule when you're done with it.
                capsule.Dispose();
                capsule = null;

                // Now it's time to draw the text on top of the capsule.
                // First we'll draw the Owner NickName using a standard font and a black brush.
                // We'll also align the NickName in the center of the Bounds.
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                //format.Trimming = StringTrimming.EllipsisCharacter;

                // Our entire capsule is 60 pixels high, and we'll draw
                // three lines of text, each 20 pixels high.
                RectangleF textRectangle = ButtonBounds;
                textRectangle.Height = 10;

                // Draw the NickName in a Standard Grasshopper font.
                graphics.DrawString("warning&error", GH_FontServer.Standard, Brushes.Black, textRectangle, format);


                // Now we need to draw the median and mean information.
                // Adjust the formatting and the layout rectangle.
                format.Alignment = StringAlignment.Near;
                //textRectangle.Inflate(-5, 0);
                textRectangle.Height = 40;
                for (int i = 0; i < CreateProgram.reportwarnings.Count; i++)
                {
                    textRectangle.Y += 20;
                    graphics.DrawString(String.Format("{0} : {1}", i, CreateProgram.reportwarnings[i]),


                                    GH_FontServer.Standard, Brushes.Black,


                                    textRectangle, format);
                }
                
                /*textRectangle.Y += 20;
                graphics.DrawString(String.Format("Mean: {0:0.00}", "sadasd"),


                                    GH_FontServer.StandardItalic, Brushes.Red,


                                    textRectangle, format);*/

                // Always dispose of any GDI+ object that implement IDisposable.
                format.Dispose();
            }
        }
    }
}
public class ReportWarnings : GH_LinkedParamAttributes
{
    public ReportWarnings(IGH_Param param, IGH_Attributes parent) : base(param, parent) { }
    public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Form form = new Form();
            form.Width = 400;
            form.Height = 200;
            form.StartPosition = FormStartPosition.Manual;
            Grasshopper.GUI.GH_WindowsFormUtil.CenterFormOnCursor(form, true);
            form.Text = "Warnings";
            ListBox listBox = new ListBox();
            listBox.Width = 370;
            listBox.Height = 100;
            listBox.Left = 5;
            listBox.Top = 25;
            listBox.Items.Clear();
            listBox.HorizontalScrollbar = true;
            if(CreateProgram.reportwarnings != null)
            {
                for (int i = 0; i < CreateProgram.reportwarnings.Count; i++)
                {
                    listBox.Items.Add(CreateProgram.reportwarnings[i]);
                }
            }
            listBox.Font = new Font(listBox.Font.FontFamily, 14);
            form.Controls.Add(listBox);
            form.ShowDialog(sender.FindForm());
            return GH_ObjectResponse.Handled;
        }
        return base.RespondToMouseDoubleClick(sender, e);
    }
}
public class ReportErrors : GH_LinkedParamAttributes
{
    public ReportErrors(IGH_Param param, IGH_Attributes parent) : base(param, parent) { }
    public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Form form = new Form();
            form.Width = 400;
            form.Height = 200;
            form.StartPosition = FormStartPosition.Manual;
            Grasshopper.GUI.GH_WindowsFormUtil.CenterFormOnCursor(form, true);
            form.Text = "Errors";

            ListBox listBox = new ListBox();
            listBox.Width = 370;
            listBox.Height = 100;
            listBox.Left = 5;
            listBox.Top = 25;
            listBox.Items.Clear();
            listBox.HorizontalScrollbar = true;
            if(CreateProgram.reporterrors != null)
            {
                for (int i = 0; i < CreateProgram.reporterrors.Count; i++)
                {
                    listBox.Items.Add(CreateProgram.reporterrors[i]);
                }
            }
            listBox.Font = new Font(listBox.Font.FontFamily, 14);
            form.Controls.Add(listBox);
            form.ShowDialog(sender.FindForm());
            return GH_ObjectResponse.Handled;
        }
        return base.RespondToMouseDoubleClick(sender, e);
    }
}