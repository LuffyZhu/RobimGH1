using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Components;
using Grasshopper.GUI;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
//using static Robots.Util;
using RobimRobots;
using Grasshopper.Kernel.Special;
using System.Threading;

namespace Robim.Grasshopper
{
    public class DeconstructProgramTargets : GH_Component
    {
        public DeconstructProgramTargets() : base("Deconstruct program targets", "DecProgTarg", "Exposes the calculated simulation data for all targets.", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override Guid ComponentGuid => new Guid("{0D380DCE-D2F9-4ceb-B8CB-883998879B2A}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconDeconstructProgramTarget;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "Program target planes", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Joints", "J", "Program target joints", GH_ParamAccess.tree);
            pManager.AddTextParameter("Configuration", "C", "Program target configuration", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Delta time", "T", "Program target time it takes to perform the motion", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Program program = null;
            DA.GetData(0, ref program);

            var path = DA.ParameterTargetPath(0);
            var cellTargets = program.Value.Targets;
            var groupCount = cellTargets[0].ProgramTargets.Count;

            var planes = new GH_Structure<GH_Plane>();
            var joints = new GH_Structure<GH_Number>();
            var configuration = new GH_Structure<GH_String>();
            var deltaTime = new GH_Structure<GH_Number>();

            for (int i = 0; i < groupCount; i++)
            {
                var tempPath = path.AppendElement(i);
                for (int j = 0; j < cellTargets.Count; j++)
                {
                    planes.AppendRange(cellTargets[j].ProgramTargets[i].Kinematics.Planes.Select(x => new GH_Plane(x)), tempPath.AppendElement(j));
                    joints.AppendRange(cellTargets[j].ProgramTargets[i].Kinematics.Joints.Select(x => new GH_Number(x)), tempPath.AppendElement(j));
                    configuration.Append(new GH_String(cellTargets[j].ProgramTargets[i].Kinematics.Configuration.ToString()), tempPath);
                    deltaTime.Append(new GH_Number(cellTargets[j].DeltaTime), tempPath);
                }
            }

            DA.SetDataTree(0, planes);
            DA.SetDataTree(1, joints);
            DA.SetDataTree(2, configuration);
            DA.SetDataTree(3, deltaTime);
        }
    }

    public class DegreesToRadians : GH_Component
    {
        public DegreesToRadians() : base("Degrees to radians", "DegToRad", "Manufacturer dependent degrees to radians conversion.", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{C10B3A17-5C19-4805-ACCF-839B85C4D21C}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconAngles;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Degrees", "D", "Degrees", GH_ParamAccess.list);
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mechanical group", "G", "Mechanical group index", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Radians", "R", "Radians", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var degrees = new List<double>();
            GH_RobotSystem robotSystem = null;
            int group = 0;

            if (!DA.GetDataList(0, degrees)) { return; }
            if (!DA.GetData(1, ref robotSystem)) { return; }
            if (!DA.GetData(2, ref group)) { return; }

            var radians = degrees.Select((x, i) => (robotSystem.Value).DegreeToRadian(x, i, group));
            string radiansText = string.Join(",", radians.Select(x => $"{x:0.00000}"));

            DA.SetData(0, radiansText);
        }
    }

    public class RadiansToDegrees : GH_Component
    {
        public RadiansToDegrees() : base("Radians to degrees", "RadToDeg", "Manufacturer dependent radians to degrees conversion.", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{0ED4F938-4223-4CDC-852D-A939C485F848}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconRadiansToDegree;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Radians", "R", "Radians", GH_ParamAccess.list);
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "Robot system", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mechanical group", "G", "Mechanical group index", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Degrees", "D", "Degrees", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var radians = new List<double>();
            GH_RobotSystem robotSystem = null;
            int group = 0;

            if (!DA.GetDataList(0, radians)) { return; }
            if (!DA.GetData(1, ref robotSystem)) { return; }
            if (!DA.GetData(2, ref group)) { return; }



            var degrees = radians.Select((x, i) => (robotSystem.Value).RadianToDegree(x, i, group));
            string degreesText = string.Join(",", degrees.Select(x => $"{x:0.00000}"));

            DA.SetData(0, degreesText);
        }
    }

    public class GetPlane : GH_Component
    {
        public GetPlane() : base("Get plane", "GetPlane", "Get a plane from a point in space and a 3D rotation. The input has to be a list of 6 or 7 numbers. ", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{F271BD0B-7249-4647-B273-577D8EA6328F}");
        protected override Bitmap Icon => Properties.Resources.iconGetPlane;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Numbers", "N", "Input 6 or 7 numbers. The first 3 should correspond to the x, y and z coordinates of the origin. The last 3 or 4 should be a 3D rotation expressed in euler angles in degrees, axis angles in radians or quaternions.", GH_ParamAccess.list);
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "The robot system will select the orientation type (ABB = quaternions, KUKA = euler angles in degrees, UR = axis angles in radians). If this input is left unconnected, it will assume the 3D rotation is expressed in quaternions.", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Plane", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var numbers = new List<double>();
            Plane plane = new Plane();
            GH_RobotSystem robotSystem = null;

            if (!DA.GetDataList(0, numbers)) { return; }
            DA.GetData(1, ref robotSystem);
            if (robotSystem == null)
            {
                if (numbers.Count != 7) { this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The list should be made out of 7 numbers."); return; }
                plane = RobotCellAbb.QuaternionToPlane(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6]);
            }
            else
            {
                if (robotSystem.Value.Manufacturer == Manufacturers.ABB)
                {
                    if (numbers.Count != 7) { this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The list should be made out of 7 numbers."); return; }
                }
                else
                {
                    if (numbers.Count != 6) { this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, " The list should be made out of 6 numbers."); return; }
                }

                plane = robotSystem.Value.NumbersToPlane(numbers.ToArray());
            }
            DA.SetData(0, plane);
        }
    }

    public class FromPlane : GH_Component
    {
        public FromPlane() : base("From plane", "FromPlane", "Returns a list of numbers from a plane. The first 3 numbers are the x, y and z coordinates of the origin. The last 3 or 4 values correspond to euler angles in degrees or quaternion values respectively.", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{03353E74-E816-4E0A-AF9A-8AFB4C111D0B}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconToPlane;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Plane to convert to euler, quaternion or axis angle values.", GH_ParamAccess.item);
            pManager.AddParameter(new RobotSystemParameter(), "Robot system", "R", "The robot system will select the orientation type (ABB = quaternions, KUKA = euler angles in degrees, UR = axis angles in radians). If this input is left unconnected, the 3D rotation will be expressed in quaternions.", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Numbers", "N", "The first 3 numbers are the x, y and z coordinates of the origin. The last 3 or 4 numbers represent a 3D rotation.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double[] numbers = null;
            GH_Plane plane = null;
            GH_RobotSystem robotSystem = null;

            if (!DA.GetData(0, ref plane)) { return; }
            DA.GetData(1, ref robotSystem);

            if (robotSystem == null)
            {
                numbers = RobotCellAbb.PlaneToQuaternion(plane.Value);
            }
            else
            {
                numbers = robotSystem.Value.PlaneToNumbers(plane.Value);
            }

            DA.SetDataList(0, numbers);
        }
    }


    public class FanucPRModify : GH_Component
    {
        public FanucPRModify() : base("Fanuc PR modify", "PR Modify", "Select the number of Program Register and input different values", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{C844C720-DD57-40EA-BD05-FD3F4F4625EB}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconProcessorRegister;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Custom PR", "I", "Input custom PR in the format of PR[a,b]:X.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("PR Output", "O", "Custom PR output", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<string> prInfo = new List<string>();
            var panel = new List<string>();

            if (!DA.GetDataList(0, panel)) { return; }
            else
            {
                prInfo = panel;
            }
            DA.SetDataList(0, prInfo);

        }
    }

    public class CodeInsert : GH_Component
    {
        public CodeInsert() : base("Code insert", "Code Insert", "Select the line number and input text file", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{1EE7B076-ADB2-4C9C-A79E-60C07EDB781E}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconCodeInsert;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            //pManager.AddTextParameter("Original Code", "C", "The Number of lines to insert.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Insert Index", "I", "The Index to insert custom code.", GH_ParamAccess.item);
            pManager.AddTextParameter("Custom Code", "c", "Input custom code.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            pManager.AddTextParameter("Code", "C", "Code output", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> oriCode = new List<string>();
            List<string> oriCode1 = new List<string>();
            List<string> oriCode2 = new List<string>();
            int index = 0;
            var insertCode = new List<string>();
            //string indent = "  ";
            GH_Program program = null;
            if (!DA.GetData(0, ref program)) { return; }
            DA.GetData(1, ref index);
            DA.GetDataList(2, insertCode);

            List<List<List<string>>> programCode = program.Value.Code;

            string RobotManufacturer = program.Value.RobotSystem.Manufacturer.ToString();
            if (RobotManufacturer == "KUKA")
            {
                for (int i = 0; i < programCode.Count; ++i)
                {
                    for (int j = 0; j < programCode[i].Count; ++j)
                    {
                        if (j == 0)
                        {
                            for (int k = 0; k < programCode[i][j].Count; ++k)
                            {
                                oriCode.Add(programCode[i][j][k]);
                            }
                        }
                        else if (j == 1)
                        {
                            for (int k = 0; k < programCode[i][j].Count; ++k)
                            {
                                oriCode1.Add(programCode[i][j][k]);
                            }
                        }
                        else if (j == 2)
                        {
                            for (int k = 0; k < programCode[i][j].Count; ++k)
                            {
                                oriCode2.Add(programCode[i][j][k]);
                            }
                        }
                    }
                }
                if (index >= oriCode2.Count || index < 2) { return; }
                List<string> newCode = new List<string>();
                int insertCodeLen = insertCode.Count;
                for (int i = 0; i < index + 1; ++i)
                {
                    newCode.Add(oriCode2[i]);  // index前面的代码不进行修改直接复制
                }
                for (int j = 0; j < insertCodeLen; ++j)
                {
                    newCode.Add(insertCode[j]);
                }
                for (int k = index + 1; k < oriCode2.Count; ++k)
                {
                    newCode.Add(oriCode2[k]);
                }
                var code2 = new List<List<string>> { oriCode, oriCode1, newCode };
                var code = new List<List<List<string>>> { code2 };
                var newProgram = program.Value.CustomCode(code);

                DA.SetData(0, new GH_Program(newProgram));

                var path = DA.ParameterTargetPath(1);
                var structure = new GH_Structure<GH_String>();
                var tempPath = path.AppendElement(0);
                structure.AppendRange(oriCode.Select(x => new GH_String(x)), tempPath.AppendElement(0));
                structure.AppendRange(oriCode1.Select(x => new GH_String(x)), tempPath.AppendElement(1));
                structure.AppendRange(newCode.Select(x => new GH_String(x)), tempPath.AppendElement(2));

                DA.SetDataTree(1, structure);
            }
            else
            {
                for (int i = 0; i < programCode.Count; ++i)
                {
                    for (int j = 0; j < programCode[i].Count; ++j)
                    {
                        for (int k = 0; k < programCode[i][j].Count; ++k)
                        {
                            oriCode.Add(programCode[i][j][k]);
                        }
                    }
                }
                if (index >= oriCode.Count || index < 2) { return; }

                List<string> newCode = new List<string>();
                int insertCodeLen = insertCode.Count;
                for (int i = 0; i < index + 2; ++i)
                {
                    newCode.Add(oriCode[i]);  // index前面的代码不进行修改直接复制
                }
                for (int j = 0; j < insertCodeLen; ++j)
                {
                    //newCode.Add(indent + $"{index+j+1}:{insertCode[j]};");
                    newCode.Add(insertCode[j]);
                }
                for (int k = index + 2; k < oriCode.Count; ++k)
                {
                    newCode.Add(oriCode[k]);
                    /*if (oriCode[k][0].ToString() != "/")
                    {
                        if (oriCode[k].IndexOf(":") < 8)
                            newCode.Add(oriCode[k].Replace(oriCode[k].Substring(1, oriCode[k].IndexOf(":") - 1), $"{ k + insertCodeLen - 1}")); //更改插入文本后行数
                        else
                            newCode.Add(oriCode[k]);
                    }
                    else
                        newCode.Add(oriCode[k]);*/
                }
                //AdjustLineCount(newCode);

                //var code = new List<List<List<string>>>();
                var code2 = new List<List<string>> { newCode };
                ////code.Add(code2);
                var code = new List<List<List<string>>> { code2 };
                //var code = program.Value.Code;
                var newProgram = program.Value.CustomCode(code);
                DA.SetData(0, new GH_Program(newProgram));
                DA.SetDataList(1, newCode);
            }

            //static void AdjustLineCount(List<string> oriScript)
            //{
            //    int totalLen = oriScript.IndexOf("/POS");
            //    int charStart = oriScript[0].IndexOf("LINE_COUNT") + "LINE_COUNT".Length + 4;
            //    int charStart2 = oriScript[0].IndexOf("LINE_COUNT");
            //    int searchEnd = oriScript[0].IndexOf("MEMORY_SIZE");
            //    int charEnd = oriScript[0].LastIndexOf(";", searchEnd);//semicolonIndex
            //    int len = charEnd - charStart;
            //    int len2 = charEnd - charStart2;
            //    string num = oriScript[0].Substring(charStart, len);
            //    string line = oriScript[0].Substring(charStart2, len2);//不能直接搜索数字，有相同数字出现可能引起冲突
            //    string newLine = line.Replace(num, totalLen.ToString());
            //    oriScript[0] = oriScript[0].Replace(line, newLine);
            //}
        }
    }


    public class KUKAUpload : GH_Component
    {
        public KUKAUpload() : base("KUKA upload", "KUKAUpload", "Input the local file path and upload. Remember ping address and get access to  //***.***.***.***/roboticplus", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{B8690640-6C9C-40D5-AC35-BD619F798476}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconKUKAUpload;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Button", "B", "Click the buttom and upload", GH_ParamAccess.item);
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            //pManager.AddTextParameter("Path", "P", "Folder path to save the code", GH_ParamAccess.item);
            pManager.AddTextParameter("IP", "I", "IP address of KUKA", GH_ParamAccess.item);

        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "M", "Message output", GH_ParamAccess.item);
        }

        protected static string connectState(string uristring)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "cmd.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            startInfo.Arguments += @"/c net use * /del /y |  net use " + uristring + " " + " " + "/User:guest";
            try
            {
                Process proc = Process.Start(startInfo);
                proc.Close();
                proc.Dispose();
                return "True";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Boolean toggle = true;
            DA.GetData(0, ref toggle);
            string path = " ";
            string ip = " ";
            GH_Program program = null;

            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "roboticplusTemp");
            Directory.CreateDirectory(path);

            if (Directory.Exists(path))
            {
                if (toggle)
                {
                    DA.GetData(1, ref program);
                    DA.GetData(2, ref ip);

                    Ping pinger = new Ping();
                    PingReply reply = pinger.Send(ip.Trim('\\'), 1000);//ping测试，1000ms超时
                    if (reply.Status == IPStatus.Success)
                    {

                        // 尝试远程登陆测试
                        string netuseFlag = connectState(Path.Combine(ip, "roboticplus"));
                        if (netuseFlag == "True")
                        {
                            program.Value.Save(path);

                            string[] subDir = Directory.GetDirectories(path);
                            string[] filePath = Directory.GetFiles(subDir[0]);
                            string[] fileNames = new string[filePath.Length];


                            for (int i = 0; i < filePath.Length; i++)
                            {
                                fileNames[i] = Path.GetFileName(filePath[i]);
                            }

                            for (int i = 0; i < fileNames.Length; i++)
                            {
                                if (fileNames[i].Split('.').Last() == "SRC" | fileNames[i].Split('.').Last() == "DAT")
                                {
                                    string dstDir = Path.Combine(Path.Combine(ip, "roboticplus"), fileNames[i]);
                                    string srcDir = filePath[i];
                                    WebClient myWebClient = new WebClient();
                                    //Ping pinger = null;
                                    try
                                    {
                                        Process prc = new Process();//dos 命令
                                        prc.StartInfo.FileName = @"cmd.exe";
                                        prc.StartInfo.UseShellExecute = false;
                                        prc.StartInfo.RedirectStandardInput = true;
                                        prc.StartInfo.RedirectStandardOutput = true;
                                        prc.StartInfo.RedirectStandardError = true;
                                        prc.StartInfo.CreateNoWindow = false;

                                        prc.Start();
                                        //string cmd = @"Net Use \\192.168.1.1\roboticplus/user:guest";
                                        string cmd = @"Net Use " + ip + @"\roboticplus/user:guest";
                                        prc.StandardInput.WriteLine(cmd);
                                        prc.StandardInput.Close();

                                        //File.Copy(srcDir, dstDir, true);
                                        byte[] responseArray = myWebClient.UploadFile(dstDir, srcDir);//上传
                                        DA.SetData(0, "upload successflly");
                                    }
                                    catch (Exception ex)
                                    {
                                        DA.SetData(0, ex.ToString());
                                    }
                                }
                                else
                                {
                                    DA.SetData(0, "can not find files");
                                }
                            }
                        }
                        else
                        {
                            DA.SetData(0, $"net use fail:{netuseFlag}");
                        }
                    }
                    else
                    {
                        DA.SetData(0, "ping failure!");
                    }
                }
                Directory.Delete(path, true);
            }
        }
    }

    public class FANUCUpload : GH_Component
    {
        public FANUCUpload() : base("FANUC upload", "FANUCUpload", "", "Robim", "Util")
        {

        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("FilePath", "F", "File path of upload file", GH_ParamAccess.item);
            pManager.AddTextParameter("FTPPath", "P", "FTP path of upload file, like 192.168.1.1/", GH_ParamAccess.item);
            pManager.AddTextParameter("Username", "U", "Username of FTP, default is \"anonymous\"", GH_ParamAccess.item, "anonymous");
            pManager.AddTextParameter("Password", "P", "Password of FTP, default is \"anonymous\"", GH_ParamAccess.item, "anonymous");
            pManager.AddBooleanParameter("Button", "B", "Click the buttom and upload", GH_ParamAccess.item);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Upload Message", "M", "Upload message output", GH_ParamAccess.list);
            pManager.AddTextParameter("Folder List Info", "F", "Folder List Info output, used for FTPPath setting", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string FilePath = null;
            string path = null;
            string Username = null;
            string Password = null;
            string[] folderInfo = null;
            bool toggle = false;

            if (!DA.GetData(0, ref FilePath)) { return; }
            if (!DA.GetData(1, ref path)) { return; }
            if (!DA.GetData(2, ref Username)) { return; }
            if (!DA.GetData(3, ref Password)) { return; }
            if (!DA.GetData(4, ref toggle)) { return; }

            if (toggle)
            {
                Thread t = new Thread(UploadAndSetValue);
                t.Start();

            }
            void UploadAndSetValue()
            {
                FtpHelper ftp = new FtpHelper();
                List<string> msg = new List<string>();

                ftp.FTPPath = @"ftp://" + path + "/";
                ftp.FilePath = FilePath;
                ftp.Username = Username;
                ftp.Password = Password;
                ftp.Msg = msg;
                ftp.Path = path;

                folderInfo = ftp.GetFolderAndFileList();
                DA.SetDataList(1, folderInfo);

                ftp.FileUpLoad();
                DA.SetDataList(0, ftp.Msg);
            }

        }


        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("13A31B24-E26D-4122-AAE1-6E87F826F210"); }
        }
    }

    public class ABBUpload : GH_Component
    {
        public ABBUpload() : base("ABB upload", "ABBUpload", "", "Robim", "Util")
        {

        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{D0F3C374-26AA-43C6-BA81-587403D05527}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconKUKAUpload;
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ProgramParameter(), "Program", "P", "Program", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Button", "B", "Click the buttom and upload", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "M", "Message output", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Program program = new GH_Program();
            bool button = false;
            DA.GetData(0, ref program);
            DA.GetData(1, ref button);
            if (button && program != null)
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "roboticplusTemp");
                Directory.CreateDirectory(path);
                program.Value.Save(path);
            }

        }
    }

    public class PlatformAngle : GH_Component
    {
        public PlatformAngle() : base("Platform angle", "Platform angle", "Calculate the rotation angle while using revolving platform", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{B71C8E4F-9519-4F06-AE3E-F3B4886806C8}");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconPlatformAngle;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Target plane", "P", "Target plane", GH_ParamAccess.list);
            pManager.AddPointParameter("Direction Point", "D.P", "Direction Point", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Angle plane", "P", "Angle plane for 2 vectors, default is WorlXY", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Opposite", "O", "Opposite values of angles, default is False", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Angle", "A", "Angle output", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Plane plane = Plane.WorldXY;
            var planeList = new List<Plane>();
            Point3d point = new Point3d();
            var pointList = new List<Point3d>();
            var angleList = new List<double>();

            Vector3d vectorA = new Vector3d();
            var vectorB = new List<Vector3d>();
            Boolean rev = false;

            //if (!DA.GetData(0, ref vectorA)) { return; }
            //DA.GetDataList(1,  vectorB);
            //DA.GetData(2, ref plane);
            if (!DA.GetDataList(0, planeList)) { return; }
            DA.GetData(1, ref point);
            DA.GetData(2, ref plane);
            DA.GetData(3, ref rev);

            Point3d centerPt = DigitalCoupledPlane.DCP.CustomPlane.PointAt(0, 0);
            vectorA = point - centerPt;

            for (int i = 0; i < planeList.Count; i++)
            {
                pointList.Add(planeList[i].PointAt(0.5, 0.5));
                vectorB.Add(pointList[i] - centerPt);
            }


            if ((vectorB != null) && (plane != null))
            {

                angleList = Util.CalculatePlatformAngle(vectorA, vectorB, plane, rev);


            }

            DA.SetDataList(0, angleList);
        }


    }

    public class RobimGroup : GH_Component
    {
        public RobimGroup() : base("Group/UnGroup(Robim)", "G/UnG", "Can group/ungroup all type of items in Grasshopper", "Robim", "Util") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{A69B87CB-A9E0-45DF-872E-63D68A84E506}");
        protected override Bitmap Icon
        {
            get
            {
                GH_GroupGeometryComponent gH_GroupGeometryComponent = new GH_GroupGeometryComponent();
                return gH_GroupGeometryComponent.Icon_24x24;
            }
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Group", "G", "Create a group", GH_ParamAccess.list);
            pManager.AddGenericParameter("UnGroup", "UnG", "Ungroup", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "R", "After group/ungroup", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> list = new List<object>();
            RobimGeneric<object> group = new RobimGeneric<object>();
            if (DA.GetDataList(0, list))
            {
                group.Name = this.NickName + DA.Iteration.ToString("_000");
                group.AddRange(list);
                DA.SetData(0, group);
            }
            else if (DA.GetData(1, ref group))
            {
                DA.SetDataList(0, group.GenericList);
            }
        }
    }
}
