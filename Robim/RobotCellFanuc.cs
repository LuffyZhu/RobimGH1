using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using Rhino.Geometry;
using static System.Math;
using static Robim.Util;
using System.CodeDom.Compiler;
using RobimRobots;

namespace Robim
{
    public class RobotCellFanuc : RobotCell
    {
        public static double ZAxisOffset { get; set; } // 在一个类中实现静态属性和静态方法，以此达到全局变量和函数的效果
        public static double YAxisOffset { get; set; }
        public static double XAxisOffset { get; set; }
        internal RobotCellFanuc(string name, List<MechanicalGroup> mechanicalGroups, IO io, Plane basePlane, Mesh environment, RobimFormSystem robimFormSystem) : base(name, Manufacturers.FANUC, mechanicalGroups, io, basePlane, environment, robimFormSystem)
        {
        }
        public static Plane EulerToPlane(double x, double y, double z, double aDeg, double bDeg, double cDeg)
        {
            double ai = aDeg.ToRadians();
            double aj = bDeg.ToRadians();
            double ak = cDeg.ToRadians();
            double si = Sin(ai);
            double sj = Sin(aj);
            double sk = Sin(ak);

            double ci = Cos(ai);
            double cj = Cos(aj);
            double ck = Cos(ak);

            double cc = ci * ck;
            double cs = ci * sk;
            double sc = si * ck;
            double ss = si * sk;

            var tt = new Transform(1);
            tt[0, 0] = cj * ck; tt[0, 1] = sj * sc - cs; tt[0, 2] = sj * cc + ss;
            tt[1, 0] = cj * sk; tt[1, 1] = sj * ss + cc; tt[1, 2] = sj * cs - sc;
            tt[2, 0] = -sj; tt[2, 1] = cj * si; tt[2, 2] = cj * ci;

            var plane = tt.ToPlane();
            plane.Origin = new Point3d(x, y, z);
            return plane;
        }

        public static double[] PlaneToEuler(Plane plane)
        {
            double UnitTol = 0.00000001;
            Transform matrix = Transform.PlaneToPlane(Plane.WorldXY, plane);
            double cy = Math.Sqrt(matrix.M00 * matrix.M00 + matrix.M10 * matrix.M10);
            double ax, ay, az;
            double zAxisOffset = RobotCellFanuc.ZAxisOffset;
            double yAxisOffset = RobotCellFanuc.YAxisOffset;
            double xAxisOffset = RobotCellFanuc.XAxisOffset;
            if (cy > UnitTol)
            {
                ax = Math.Atan2(matrix.M21, matrix.M22);
                ay = Math.Atan2(-matrix.M20, cy);
                az = Math.Atan2(matrix.M10, matrix.M00);
            }
            else
            {
                ax = Math.Atan2(-matrix.M12, matrix.M11);
                ay = Math.Atan2(-matrix.M20, cy);
                az = 0.0;
            }

            return new double[] { plane.OriginX + xAxisOffset, plane.OriginY + yAxisOffset, plane.OriginZ + zAxisOffset, ax.ToDegrees(), ay.ToDegrees(), az.ToDegrees() };
        }

        public override double[] PlaneToNumbers(Plane plane) => PlaneToEuler(plane);
        public override Plane NumbersToPlane(double[] numbers) => EulerToPlane(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]);

        internal override void SaveCode(Program program, string folder)
        {
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($" Folder \"{folder}\" not found");
            if (program.Code == null) throw new NullReferenceException(" Program code not generated");
            Directory.CreateDirectory($@"{folder}\{program.Name}");
            string file = $@"{folder}\{program.Name}\{program.Name}.ls";
            string jointCode = "";
            for (int j = 0; j < program.Code.Count; j++)
            {
                for (int i = 0; i < program.Code[j].Count; i++)
                {
                    var jc = string.Join("\r\n", program.Code[j][i]);
                    //jointCode = jointCode.Join("\r\n", jc.ToList());
                    jointCode = jointCode + jc + "\n";

                }

            }

            //for (int i = 0; i < program.Code[1].Count; i++)
            //{
            //    var jc = string.Join("\r\n", program.Code[1][i]);
            //    //jointCode = jointCode.Join("\r\n", jc.ToList());
            //    jointCode = jointCode + jc + "\n";

            //}
            File.WriteAllText(file, jointCode);


            //throw new NotImplementedException("Fanuc postprocessor not yet implemented.");


            /*
             * TODO: Implement...
             * 
             */

            /*
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($" Folder \"{folder}\" not found");
            Directory.CreateDirectory($@"{folder}\{program.Name}");
            bool multiProgram = program.MultiFileIndices.Count > 1;

            for (int i = 0; i < program.Code.Count; i++)
            {
                string group = MechanicalGroups[i].Name;
                {
                    // program
                    string file = $@"{folder}\{program.Name}\{program.Name}_{group}.pgf";
                    string mainModule = $@"{program.Name}_{group}.mod";
                    string code = $@"<?xml version=""1.0"" encoding=""ISO-8859-1"" ?>
    <Program>
        <Module>{mainModule}</Module>
    </Program>
    ";
                    File.WriteAllText(file, code);
                }

                {
                    string file = $@"{folder}\{program.Name}\{program.Name}_{group}.mod";
                    var code = program.Code[i][0].ToList();
                    if (!multiProgram) code.AddRange(program.Code[i][1]);
                    var joinedCode = string.Join("\r\n", code);
                    File.WriteAllText(file, joinedCode);
                }

                if (multiProgram)
                {
                    for (int j = 1; j < program.Code[i].Count; j++)
                    {
                        int index = j - 1;
                        string file = $@"{folder}\{program.Name}\{program.Name}_{group}_{index:000}.mod";
                        var joinedCode = string.Join("\r\n", program.Code[i][j]);
                        File.WriteAllText(file, joinedCode);
                    }
                }
            }
            */
        }

        internal override List<List<List<string>>> Code(Program program) => new FanucPostProcessor(this, program).Code;

        class FanucPostProcessor
        {
            RobotCellFanuc cell;
            Program program;
            internal List<List<List<string>>> Code { get; }
            int lineCount = 1;
            //int PR_num = 1;
            int tool_num = 0;
            int frame_num = 0;
            string indent = "  ";
            int prnum = 50;



            internal FanucPostProcessor(RobotCellFanuc robotCell, Program program)
            {
                this.cell = robotCell;

                this.program = program;
                string fanuc_robotName = cell.Name;  // 获得具体的机械臂型号参数
                string programName = program.Name;
                int Z_AxisOffset = ZAxisOffset(fanuc_robotName);  // 得到z轴方向上的偏差

                //RobotCellFanuc.ZAxisOffset = Z_AxisOffset;  // 对全局变量进行修改，便于调用
                double zWeightTrack = 1;
                double yWeightTrack = 1;
                double xWeightTrack = 1;
                if (robotCell.MechanicalGroups.First().Externals.Any())
                {
                    zWeightTrack = robotCell.MechanicalGroups.First().Externals.First().BasePlane.ZAxis * (Plane.WorldXY.ZAxis);
                    yWeightTrack = robotCell.MechanicalGroups.First().Externals.First().BasePlane.YAxis * (Plane.WorldXY.ZAxis);
                    xWeightTrack = robotCell.MechanicalGroups.First().Externals.First().BasePlane.XAxis * (Plane.WorldXY.ZAxis);
                }

                double zWeightRobot = program.RobotSystem.BasePlane.ZAxis * (Plane.WorldXY.ZAxis);
                double yWeightRobot = program.RobotSystem.BasePlane.YAxis * (Plane.WorldXY.ZAxis);
                double xWeightRobot = program.RobotSystem.BasePlane.XAxis * (Plane.WorldXY.ZAxis);

                RobotCellFanuc.ZAxisOffset = zWeightRobot * zWeightTrack * Z_AxisOffset;
                RobotCellFanuc.YAxisOffset = yWeightRobot * yWeightTrack * Z_AxisOffset;
                RobotCellFanuc.XAxisOffset = xWeightRobot * xWeightTrack * Z_AxisOffset;

                List<string> frame_code = Frame();  // 添加平面
                List<string> tool_code = Tool(); // 添加工具头
                //List<string> prInsert_code = prInsert_code(); // 插入可定制PR信息
                List<List<string>> control_code = control(); // 运动控制指令
                List<string> attr_code = attribution(programName, lineCount);  // 属性

                var groupCode = new List<List<string>> { attr_code, frame_code, tool_code };
                this.Code = new List<List<List<string>>> { groupCode, control_code };
                //this.Code.Add(control_code);

            }

            int ZAxisOffset(string robotName)  // 因为gh模型中fanuc的原点位置和实际在z轴方向上存在偏差，所以需要根据实际的机械臂型号进行偏差的补偿
            {
                int offset = 0;
                string fanuc_2000ic270f = "2000ic270f";
                string fanuc_710ic50 = "710ic50";
                string fanuc_710ic20l = "710ic20l";
                string fanuc_m20ia = "20ia";
                string fanuc_2000ic125l = "2000ic125l";
                string fanuc_2000ic210f = "2000ic210f";
                if (robotName.ToLower().Contains(fanuc_2000ic270f))
                    offset = -670;
                else if (robotName.ToLower().Contains(fanuc_710ic50))
                    offset = -565;
                else if (robotName.ToLower().Contains(fanuc_710ic20l))
                    offset = -565;
                else if (robotName.ToLower().Contains(fanuc_m20ia))
                    offset = -525;
                else if (robotName.ToLower().Contains(fanuc_2000ic125l))
                    offset = -670;
                else if (robotName.ToLower().Contains(fanuc_2000ic210f))
                    offset = -670;
                else
                    offset = 0;


                return offset;
            }

            List<string> attribution(string programName, int line_count)  // 程序属性部分
            {
                String local_datetime = DateTime.Now.ToString("yyyy-MM-dd");
                String local_time = DateTime.Now.ToLongTimeString().ToString();
                var code = new List<string>
                    {
                        $@"/PROG {programName}
/ATTR
OWNER  = EDITOR;
COMMENT  = ""RP"";
PROG_SIZE  = 0;
CREATE  = DATE {local_datetime}  TIME {local_time};
MODIFIED  = DATE {local_datetime}  TIME {local_time};
FILE_NAME  = ;
VERSION  = 0;
LINE_COUNT  = {line_count-1};
MEMORY_SIZE  = 0;
PROTECT  = READ_WRITE;
TCD:  STACK_SIZE  = 0,
      TASK_PRIORITY = 50,
      TIME_SLICE = 0,
      BUSY_LAMP_OFF = 0,
      ABORT_REQUEST = 0,
      PAUSE_REQUEST = 0;
DEFAULT_GROUP = 1,*,*,*,*;
CONTROL_CODE = 00000000 00000000;"
                    };
                return code;
            }

            List<string> Tool()  // 设置工具头
            {
                double zAxisOffset = RobotCellFanuc.ZAxisOffset;
                double yAxisOffset = RobotCellFanuc.YAxisOffset;
                double xAxisOffset = RobotCellFanuc.XAxisOffset;

                var code = new List<string>();
                int toolCount = 0;
                foreach (var tool in program.Attributes.OfType<Tool>())
                {
                    tool_num += 1;
                    Plane tcp = tool.Tcp;
                    //Plane originPlane = new Plane(Point3d.Origin, -Vector3d.YAxis, Vector3d.XAxis);
                    //tcp.Transform(Transform.PlaneToPlane(Plane.WorldXY, originPlane));
                    if (tool.PR != null)
                        prnum = tool.PR.PRnum;
                    else
                    {
                        tool.PR = new ProcessRegister();
                        tool.PR.PRnum = prnum;
                    }

                    //string [] tmp = tool.PR.content.Split(',');
                    //PR_num = prnum;
                    double[] axisAngle = PlaneToEuler(tcp);
                    axisAngle[0] = axisAngle[0] - xAxisOffset;
                    axisAngle[1] = axisAngle[1] - yAxisOffset;
                    axisAngle[2] = axisAngle[2] - zAxisOffset;
                    for (int i = 1; i <= 6; ++i)
                    {
                        //code.Add(indent + $"{lineCount}:  PR[{PR_num},{i}]={axisAngle[i - 1]};");
                        code.Add(indent + $"{lineCount}:  PR[{prnum},{i}]={axisAngle[i - 1].ToString("f3")};");
                        //if (tmp.Length == 6)
                        //    code.Add(indent + $"{lineCount}:  PR[{prnum},{i}]={tmp[i-1]};");
                        //else
                        //    code.Add("工具头寄存器需要输入6个值。");
                        lineCount += 1;
                    }
                    code.Add(indent + $"{lineCount}: UTOOL[{tool_num}]=PR[{prnum}];");
                    lineCount += 1;
                    code.Add(indent + $"{lineCount}: UTOOL_NUM={tool_num};");
                    lineCount += 1;
                }
                prnum = prnum + 1;
                toolCount = toolCount + 1;
                return code;
            }

            List<string> Frame()  // 设置用户平面
            {
                double zAxisOffset = RobotCellFanuc.ZAxisOffset;
                double yAxisOffset = RobotCellFanuc.YAxisOffset;
                double xAxisOffset = RobotCellFanuc.XAxisOffset;

                var code = new List<string>
                    {
                        "/MN"
                    };
                foreach (var frame in program.Attributes.OfType<Frame>())
                {
                    frame_num += 1;
                    Plane plane = frame.Plane;
                    Plane baseplane = Plane.Unset;
                    if (program.RobotSystem.RobimFormSystem.T_HasEulerangle)
                    {
                        baseplane = program.RobotSystem.RobimFormSystem.T_EulerPlane;
                    }
                    else
                    {
                        baseplane = cell.BasePlane;
                    }
                    plane.Transform(Transform.PlaneToPlane(baseplane, Plane.WorldXY));
                    //if (cell.MechanicalGroups[0].Externals.Any(x => x.MovesRobot == true))
                    //{
                    //    plane = cell.MechanicalGroups[0].Externals.Find(x => x.MovesRobot == true).BasePlane;
                    //}
                    //else
                    //{
                    //    plane = cell.BasePlane;
                    //}
                    //Plane originPlane = new Plane(Point3d.Origin, Vector3d.YAxis, -Vector3d.XAxis);
                    //plane.Transform(Transform.PlaneToPlane(cell.BasePlane, Plane.WorldXY));
                    //int prnum = frame.PRnum;

                    if (frame.pr != null)
                        prnum = frame.pr.PRnum;
                    else
                    {
                        frame.pr = new ProcessRegister();
                        frame.pr.PRnum = prnum;
                    }
                    double[] axisAngle = PlaneToEuler(plane);
                    axisAngle[0] = axisAngle[0] - xAxisOffset;
                    axisAngle[1] = axisAngle[1] - yAxisOffset;
                    axisAngle[2] = axisAngle[2] - zAxisOffset;

                    //string[] tmp = frame.pr.content.Split(',');
                    for (int i = 1; i <= 6; ++i)
                    {
                        //code.Add(indent + $"{lineCount}:  PR[{PR_num},{i}]={axisAngle[i-1]};");
                        code.Add(indent + $"{lineCount}:  PR[{prnum},{i}]={axisAngle[i - 1].ToString("f3")};");
                        //if (tmp.Length == 6)
                        //    code.Add(indent + $"{lineCount}:  PR[{prnum},{i}]={tmp[i]};");
                        //else
                        //    code.Add("frame寄存器需要输入6个值。");
                        lineCount += 1;
                    }
                    code.Add(indent + $"{lineCount}: UFRAME[{frame_num}]=PR[{prnum}];");
                    lineCount += 1;
                    code.Add(indent + $"{lineCount}: UFRAME_NUM={frame_num};");
                    lineCount += 1;
                    prnum = prnum + 1;
                }
                return code;
            }

            List<List<string>> control()
            {
                Speed currentSpeed = null;
                Zone currentZone = null;
                int poseCount = 1;
                string zone = "CNT0";
                string speedJoint = "20%";
                string speedLinear = "500mm/sec";
                var codeMove = new List<string>();
                var codePos = new List<string> {
                    "/POS"
                };

                string fwrist = "N";
                string felbow = "U";
                string fshoulder = "T";
                
                foreach (var mechgroupTargets in program.Targets)
                {
                    foreach (var t in mechgroupTargets.ProgramTargets)
                    {
                        double[] joints = t.Kinematics.Joints;
                        //if (joints[4] < 0)
                        if(this.cell.RadianToDegree(joints[4], 4) < 0) 
                        {
                            fwrist = "N";
                        }
                        else
                        {
                            fwrist = "F";
                        }

                        //if (joints[2] < PI / 2)
                        if((this.cell.RadianToDegree(joints[1], 1) + this.cell.RadianToDegree(joints[2], 2)) < 90)
                        {
                            felbow = "U";
                        }
                        else
                        {
                            felbow = "D";
                        }
                    }
                }
                string fanucConfig = $@"CONFIG:'{fwrist} {felbow} {fshoulder},0,0,0'";
                var currentConfiguration = program.Targets[0].ProgramTargets[0].Kinematics.Configuration;
                


                foreach (var command in program.Attributes.OfType<Command>())
                {
                    string declaration = command.Declaration(program);
                    if (declaration != null && declaration.Length > 0)
                    {
                        declaration = indent + declaration;
                        //  declaration = indent + declaration.Replace("\n", "\n" + indent);
                        codeMove.Add(declaration);
                    }
                }

                foreach (var cellTarget in program.Targets)
                {
                    var programTarget = cellTarget.ProgramTargets[0];
                    var target = programTarget.Target;
                    string moveText = null;
                    string posText = null;
                    // 交融半径
                    if (currentZone == null || target.Zone != currentZone)
                    {
                        double zonenum = target.Zone.Distance;
                        if (zonenum < 0)  // 交融半径范围 0-100
                            zonenum = 0;
                        else if (zonenum > 100)
                            zonenum = 100;
                        zone = $"CNT{zonenum}";
                        currentZone = target.Zone;
                    }

                    // external axes
                    string external = string.Empty;
                    for (int index = 0; index < cell.MechanicalGroups.Count; index++)
                    {
                        double[] values = cell.MechanicalGroups[index].RadiansToDegreesExternal(target);
                        for (int i = 0; i < cell.RobimFormSystem.External_Direction.Length; i++)
                        {
                            int num = i + 1;
                            if (!cell.RobimFormSystem.External_Direction[i])
                            {
                                external += $@",
{indent}E{num}= {values[i].ToString("F3")} mm";
                            }
                            else
                            {
                                external += $@",
{indent}E{num}= {(-values[i]).ToString("F3")} mm";
                            }
                        }
                    }
                    // 输入是角度
                    if (programTarget.IsJointTarget || (programTarget.IsJointMotion && programTarget.ForcedConfiguration))
                    {
                        //cell.MechanicalGroups[0].Joints[0].Range = new Interval(0, 100);
                        double[] joints = programTarget.IsJointTarget ? (programTarget.Target as JointTarget).Joints : programTarget.Kinematics.Joints;
                        List<double> joints_degree = new List<double>();
                        for (int i = 0; i < joints.Count(); i++)
                        {
                            joints_degree.Add(this.cell.RadianToDegree(joints[i], i));
                        }


                        //下面6行代码本来是在if结构体内，但由于speedjoint是必然需要赋值的，防止if判断没有进入结构体（主要防止第一个ptp和之后的lin公用一个速度，无法进入if）从而使用默认速度
                        double speednum = target.Speed.TranslationSpeed;
                        if (speednum > 100)  // 关节运动时速度最大 100%
                            speednum = 100;
                        else if (speednum < 0)
                            speednum = 1;
                        speedJoint = $"{speednum}%";

                        if (currentSpeed == null || target.Speed != currentSpeed)
                        {
                            currentSpeed = target.Speed;
                        }
                        moveText = $"{indent}{lineCount}:J P[{poseCount}] {speedJoint} {zone};";
                        lineCount += 1;
                        posText = $@"P[{poseCount}]{{
{indent}GP1:
{indent}UF: {frame_num}, UT: {tool_num},
{indent}J1= { joints_degree[0].ToString("f3")} deg, J2= {joints_degree[1].ToString("f3")} deg,J3= {joints_degree[2].ToString("f3")} deg,
{indent}J4= { joints_degree[3].ToString("f3")} deg, J5= { joints_degree[4].ToString("f3")} deg,J6= {joints_degree[5].ToString("f3")} deg{external}
}};";
                        poseCount += 1;
                    }
                    else
                    {
                        var cartesian = target as CartesianTarget;
                        var plane = cartesian.Plane;
                        //plane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                        //plane.Transform(Transform.PlaneToPlane(cell.BasePlane, Plane.WorldXY));
                        var axisAngle = cell.PlaneToNumbers(plane);
                        double[] joints = programTarget.IsJointTarget ? (programTarget.Target as JointTarget).Joints : programTarget.Kinematics.Joints;
                        switch (cartesian.Motion)
                        {
                            case Motions.Joint:
                                {
                                    List<double> joints_degree = new List<double>();
                                    for (int i = 0; i < joints.Count(); i++)
                                    {
                                        joints_degree.Add(this.cell.RadianToDegree(joints[i], i));
                                    }

                                    double speednum = target.Speed.TranslationSpeed;  // 最大 100%
                                    if (speednum > 100)
                                        speednum = 100;
                                    else if (speednum < 0)
                                        speednum = 1;
                                    speedJoint = $"{speednum}%";

                                    if (currentSpeed == null || target.Speed != currentSpeed)
                                    {

                                        currentSpeed = target.Speed;
                                    }
                                    moveText = $"{indent}{lineCount}:J P[{poseCount}] {speedJoint} {zone};";
                                    lineCount += 1;
                                    posText = $@"P[{poseCount}]{{
{indent}GP1:
{indent}UF: {frame_num}, UT: {tool_num},
{indent}J1= { joints_degree[0].ToString("f3")} deg, J2= { joints_degree[1].ToString("f3")} deg,J3= { joints_degree[2].ToString("f3")} deg,
{indent}J4= { joints_degree[3].ToString("f3")} deg, J5= { joints_degree[4].ToString("f3")} deg,J6= { joints_degree[5].ToString("f3")} deg{external}
}};";

                                    poseCount += 1;
                                    break;
                                }
                            case Motions.Linear:
                                {
                                    double speednum = target.Speed.TranslationSpeed;
                                    if (speednum > 2000)   // 最大 2000 mm/sec
                                        speednum = 2000;
                                    else if (speednum < 0)
                                        speednum = 1;
                                    //speedLinear = $"{speednum}mm/sec";
                                    speedLinear = $"{speednum * 6}cm/min";

                                    if (currentSpeed == null || target.Speed != currentSpeed)
                                    {
                                        currentSpeed = target.Speed;
                                    }

                                    moveText = $"{indent}{lineCount}:L P[{poseCount}] {speedLinear} {zone};";
                                    lineCount += 1;
                                    posText = $@"P[{poseCount}]{{
{indent}GP1:
{indent}UF: {frame_num}, UT: {tool_num}, {fanucConfig},
{indent}X = { axisAngle[0].ToString("f3")} mm, Y = { axisAngle[1].ToString("f3")} mm, Z= { axisAngle[2].ToString("f3")} mm,
{indent}W = { axisAngle[3].ToString("f3")} deg, P = { axisAngle[4].ToString("f3")} deg,R = { axisAngle[5].ToString("f3")} deg{external}
}};";
                                    poseCount += 1;
                                    break;
                                }
                            case Motions.Arc:  // only for Fanuc
                                {
                                    double speednum = target.Speed.TranslationSpeed;
                                    if (speednum > 2000)   // 最大 2000 mm/sec
                                        speednum = 2000;
                                    else if (speednum < 0)
                                        speednum = 1;
                                    //speedLinear = $"{speednum}mm/sec";
                                    speedLinear = $"{speednum * 6}cm/min";

                                    if (currentSpeed == null || target.Speed != currentSpeed)
                                    {
                                        currentSpeed = target.Speed;
                                    }
                                    moveText = $"{indent}{lineCount}:A P[{poseCount}] {speedLinear} {zone};";
                                    lineCount += 1;
                                    posText = $@"P[{poseCount}]{{
{indent}GP1:
{indent}UF: {frame_num}, UT: {tool_num}, {fanucConfig},
{indent}X = { axisAngle[0].ToString("f3")} mm, Y = { axisAngle[1].ToString("f3")} mm, Z= { axisAngle[2].ToString("f3")} mm,
{indent}W = { axisAngle[3].ToString("f3")} deg, P = { axisAngle[4].ToString("f3")} deg,R = { axisAngle[5].ToString("f3")} deg{external}
}};";
                                    poseCount += 1;
                                    break;
                                }
                        }
                    }

                    foreach (var command in programTarget.Commands.Where(c => c.RunBefore))
                    {
                        string commands = command.Code(program, target);
                        commands = indent + $"{lineCount}:" + commands;
                        lineCount += 1;
                        codeMove.Add(commands);
                    }

                    codeMove.Add(moveText);

                    foreach (var command in programTarget.Commands.Where(c => !c.RunBefore))
                    {
                        string commands = command.Code(program, target);
                        commands = indent + $"{lineCount}:" + commands;
                        lineCount += 1;
                        codeMove.Add(commands);
                    }
                    codePos.Add(posText);
                }
                codePos.Add($"/END");
                var code = new List<List<string>> { codeMove, codePos };

                return code;
            }

        }

    }
}