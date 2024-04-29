using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using static System.Math;

using Rhino.Geometry;
using static Robim.Util;
using static Rhino.RhinoMath;
using System.IO.Ports;
using Robim.Commands;
using RobimRobots;

namespace Robim
{
    public class RobotCellGoogol : RobotCell
    {
        internal RobotCellGoogol(string name, List<MechanicalGroup> mechanicalGroup, IO io, Plane basePlane, Mesh environment, RobimFormSystem robimFormSystem) : base(name, Manufacturers.Googol, mechanicalGroup, io, basePlane, environment, robimFormSystem) { }

        public static Plane EulerToPlane(double x, double y, double z, double aDeg, double bDeg, double cDeg)
        {
            double a = -aDeg.ToRadians();
            double b = -bDeg.ToRadians();
            double c = -cDeg.ToRadians();
            double ca = Cos(a);
            double sa = Sin(a);
            double cb = Cos(b);
            double sb = Sin(b);
            double cc = Cos(c);
            double sc = Sin(c);
            var tt = new Transform(1);
            tt[0, 0] = ca * cb; tt[0, 1] = sa * cc + ca * sb * sc; tt[0, 2] = sa * sc - ca * sb * cc;
            tt[1, 0] = -sa * cb; tt[1, 1] = ca * cc - sa * sb * sc; tt[1, 2] = ca * sc + sa * sb * cc;
            tt[2, 0] = sb; tt[2, 1] = -cb * sc; tt[2, 2] = cb * cc;

            var plane = tt.ToPlane();
            plane.Origin = new Point3d(x, y, z);
            return plane;
        }

        public static double[] PlaneToEuler(Plane plane)
        {
            Transform matrix = Transform.PlaneToPlane(Plane.WorldXY, plane);
            double a = Atan2(-matrix.M10, matrix.M00);
            double mult = 1.0 - matrix.M20 * matrix.M20;
            if (Abs(mult) < UnitTol) mult = 0.0;
            double b = Atan2(matrix.M20, Sqrt(mult));
            double c = Atan2(-matrix.M21, matrix.M22);

            if (matrix.M20 < (-1.0 + UnitTol))
            {
                a = Atan2(matrix.M01, matrix.M11);
                b = -PI / 2;
                c = 0;
            }
            else if (matrix.M20 > (1.0 - UnitTol))
            {
                a = Atan2(matrix.M01, matrix.M11);
                b = PI / 2;
                c = 0;
            }

            return new double[] { plane.OriginX, plane.OriginY, plane.OriginZ, -a.ToDegrees(), -b.ToDegrees(), -c.ToDegrees() };
        }

        public override double[] PlaneToNumbers(Plane plane) => PlaneToEuler(plane);
        public override Plane NumbersToPlane(double[] numbers) => EulerToPlane(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]);


        public override Plane CartesianLerp(Plane a, Plane b, double t, double min, double max)
        {
            // return base.CartesianLerp(a, b, t, min, max);

            t = (t - min) / (max - min);
            if (double.IsNaN(t)) t = 0;

            var matrixA = a.ToTransform();
            var matrixB = b.ToTransform();

            var result = Transform.Identity;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[i, j] = matrixA[i, j] * (1.0 - t) + matrixB[i, j] * t;
                }
            }

            return result.ToPlane();
        }

        internal override List<List<List<string>>> Code(Program program) => new KRLPostProcessor(this, program).Code;

        internal override void SaveCode(Program program, string folder)
        {
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($" Folder \"{folder}\" not found");
            if (program.Code == null) throw new NullReferenceException(" Program code not generated");
            Directory.CreateDirectory($@"{folder}\GoogolProgram");

            for (int i = 0; i < program.Code.Count; i++)
            {
                string group = MechanicalGroups[i].Name;

                for (int j = 0; j < program.Code[i].Count; j++)
                {
                    string file = $@"{folder}\GoogolProgram\{program.Name}.prg";
                    var joinedCode = string.Join("\r\n", program.Code[i][j]);
                    File.WriteAllText(file, joinedCode);
                }
            }
        }


        class KRLPostProcessor
        {
            RobotCellGoogol cell;
            Program program;
            internal List<List<List<string>>> Code { get; }
            //int tool_num = 0;

            internal KRLPostProcessor(RobotCellGoogol robotCell, Program program)
            {
                this.cell = robotCell;
                this.program = program;
                this.Code = new List<List<List<string>>>();


                for (int i = 0; i < cell.MechanicalGroups.Count; i++)
                {
                    var groupCode = new List<List<string>>();

                    for (int j = 0; j < program.MultiFileIndices.Count; j++)
                    {
                        groupCode.Add(mainErp(j, i));           //存放main.erp文件，存放movement。
                    }

                    Code.Add(groupCode);
                }
            }

            List<string> mainErd(int file, int group)
            {
                string groupName = cell.MechanicalGroups[group].Name;
                var code = new List<string> { $@"//main.erd" };
                int start = program.MultiFileIndices[file];
                int end = (file == program.MultiFileIndices.Count - 1) ? program.Targets.Count : program.MultiFileIndices[file + 1];


                foreach (var command in program.Attributes.OfType<Command>())
                {
                    string declaration = command.Declaration(program);
                    if (declaration != null) code.Add(declaration);
                }


                for (int j = start; j < end; j++)
                {
                    var cellTarget = program.Targets[j];
                    var programTarget = cellTarget.ProgramTargets[group];
                    var target = programTarget.Target;
                    string moveText = null;


                    // external axes

                    string external = string.Empty;
                    double[] valuesExternal = new double[10];
                    double[] values = cell.MechanicalGroups[group].RadiansToDegreesExternal(target);

                    for (int i = 0; i < target.External.Length; i++)
                    {
                        int num = i + 1;
                        external += $", E{num} {values[i].ToString("f4")}";
                        valuesExternal[i] = values[i];
                    }

                    if (programTarget.IsJointTarget)
                    {
                        //a7-a16为外部轴参数
                        var jointTarget = target as JointTarget;
                        double[] jointDegrees = jointTarget.Joints.Select((x, i) => cell.MechanicalGroups[group].Robot.RadianToDegree(x, i)).ToArray();

                        moveText = $"P{j}={{_type=\"APOS\", a1={jointDegrees[0].ToString("f4")}, a2={jointDegrees[1].ToString("f4")}, a3={jointDegrees[2].ToString("f4")}, " +
                            $"a4={jointDegrees[3].ToString("f4")}, a5={jointDegrees[4].ToString("f4")}, a6={jointDegrees[5].ToString("f4")}" +
                            $", a7={valuesExternal[0].ToString("f4")}, a8={valuesExternal[1].ToString("f4")},a9={valuesExternal[2].ToString("f4")}, " +
                            $" a10={valuesExternal[3].ToString("f4")}, a11={valuesExternal[4].ToString("f4")}, a12={valuesExternal[5].ToString("f4")}, " +
                            $" a13={valuesExternal[6].ToString("f4")}, a14={valuesExternal[7].ToString("f4")}, a15={valuesExternal[8].ToString("f4")}, a16={valuesExternal[9].ToString("f4")}" +
                            $"}}";
                    }
                    else
                    {
                        var cartesian = target as CartesianTarget;
                        var plane = cartesian.Plane;
                        var euler = PlaneToEuler(plane);

                        var axisAngle = cell.PlaneToNumbers(plane);
                        double[] joints = programTarget.IsJointTarget ? (programTarget.Target as JointTarget).Joints : programTarget.Kinematics.Joints;
                        double[] joints_cf = jointsCF(joints);

                        var configuration = programTarget.Kinematics.Configuration;
                        bool shoulder = configuration.HasFlag(RobotConfigurations.Shoulder);
                        bool elbow = configuration.HasFlag(RobotConfigurations.Elbow);
                        //elbow = !elbow;
                        bool wrist = configuration.HasFlag(RobotConfigurations.Wrist);

                        int configNum = 0;
                        if (shoulder) configNum += 1;
                        if (elbow) configNum += 2;
                        if (wrist) configNum += 4;

                        moveText = $"P{j}={{_type=\"CPOS\",confdata={{_type=\"POSCFG\",mode={configNum}, " +
                            $"cf1={joints_cf[0]}, cf2={joints_cf[1]},cf3={joints_cf[2]}," +
                            $"cf4={joints_cf[3]},cf5={joints_cf[4]},cf6={joints_cf[5]}}}," +
                            $"x={euler[0].ToString("f4")},y={euler[1].ToString("f4")},z={euler[2].ToString("f4")}," +
                            $"a={euler[3].ToString("f4")},b={euler[4].ToString("f4")},c={euler[5].ToString("f4")}" +
                            $", a7={valuesExternal[0].ToString("f4")}, a8={valuesExternal[1].ToString("f4")}, a9={valuesExternal[2].ToString("f4")}, " +
                            $"a10={valuesExternal[3].ToString("f4")}, a11={valuesExternal[4].ToString("f4")}, a12={valuesExternal[5].ToString("f4")}, " +
                            $"a13={valuesExternal[6].ToString("f4")}, a14={valuesExternal[7].ToString("f4")}, a15={valuesExternal[8].ToString("f4")}, " +
                            $"a16={valuesExternal[9].ToString("f4")}" + //a7以后的参数为六轴机器人的外部轴参数
                            $"}}";
                    }
                    code.Add(moveText);

                }
                return code;
            }

            List<string> mainErp(int file, int group)
            {
                int start = program.MultiFileIndices[file];
                int end = (file == program.MultiFileIndices.Count - 1) ? program.Targets.Count : program.MultiFileIndices[file + 1];

                var code = new List<string>();

                Tool currentTool = null;
                Frame currentFrame = null;
                Speed currentSpeed = null;
                double currentPercentSpeed = 0;
                Zone currentZone = null;



                code.Add("NOP");

                var blending = " VBL=100.00 @1,1,0,1,0,0,0,0,";
                var blendingJ = " VBL=100.00 @2,1,0,1,0,0,0,0,";
                //var j7j8ending = ",0,0$0,0,0,0,0,0,0,0,0,0,0,0$0,0,0,0,0,0,0,0";
                var j7j8endingNew = ",0,0$0,0,0,0,0,0,0,0$1,0,0,0,0,0,0,0";
                var circStr = ";1,1,0,1,0,0,0,0,";
                var secondStr = ",0,0$1,1,0,1,0,0,0,0,";
                var secondStrJ = ",0,0$2,1,0,1,0,0,0,0,";

                for (int j = start; j < end; j++)
                {
                    var cellTarget = program.Targets[j];
                    var programTarget = cellTarget.ProgramTargets[group];
                    var target = programTarget.Target;

                    string BLtext = " BL=" + target.Zone.Distance.ToString("f2");

                    double[] joints = programTarget.IsJointTarget ? (programTarget.Target as JointTarget).Joints : programTarget.Kinematics.Joints;
                    double[] joints_cf = jointsCF(joints);

                    var speedStr = $" V={ target.Speed.TranslationSpeed.ToString("f2")}";
                    var configuration = programTarget.Kinematics.Configuration;

                    bool shoulder = configuration.HasFlag(RobotConfigurations.Shoulder);
                    bool elbow = configuration.HasFlag(RobotConfigurations.Elbow);
                    bool wrist = configuration.HasFlag(RobotConfigurations.Wrist);

                    int configNum = 0;
                    if (shoulder) configNum += 1;
                    if (elbow) configNum += 2;
                    if (wrist) configNum += 4;

                    if (currentTool == null || target.Tool != currentTool)
                    {
                        //code.Add(SetTool(target.Tool));
                        currentTool = target.Tool;
                    }

                    if (currentFrame == null || target.Frame != currentFrame)
                    {
                        if (target.Frame.IsCoupled)
                        {
                            int mech = target.Frame.CoupledMechanism + 2;
                        }
                        else
                        {
                        }
                        currentFrame = target.Frame;
                    }

                    if (target.Zone.IsFlyBy && (currentZone == null || target.Zone != currentZone))
                    {
                        currentZone = target.Zone;
                    }


                    if (programTarget.Index > 0)
                    {
                        if (programTarget.LeadingJoint > 5)
                        {
                            //code.Add(ExternalSpeed(programTarget));
                        }
                        else
                        {
                            if (currentSpeed == null || target.Speed != currentSpeed)
                            {
                                if (!programTarget.IsJointMotion)
                                {
                                    double rotation = target.Speed.RotationSpeed.ToDegrees();
                                    //code.Add($"$VEL={{CP {target.Speed.Name}, ORI1 {rotation:0.000}, ORI2 {rotation:0.000}}}");
                                    //code.Add($"$VEL.CP = {target.Speed.Name}\r\n$VEL.ORI1 = {rotation:0.###}\r\n$VEL.ORI2 = {rotation:0.####}");
                                    currentSpeed = target.Speed;
                                }
                            }

                            if (programTarget.IsJointMotion)
                            {
                                double percentSpeed = cellTarget.MinTime / cellTarget.DeltaTime;

                                if (Abs(currentPercentSpeed - percentSpeed) > UnitTol)
                                {
                                    //code.Add("BAS(#VEL_PTP, 100)");
                                    if (cellTarget.DeltaTime > UnitTol)
                                    {
                                        //code.Add($"$VEL_AXIS[{programTarget.LeadingJoint + 1}] = {percentSpeed * 100:0.000}");
                                    }
                                    currentPercentSpeed = percentSpeed;
                                }
                            }
                        }
                    }

                    // external axes

                    string external = string.Empty;

                    double[] values = cell.MechanicalGroups[group].RadiansToDegreesExternal(target);

                    for (int i = 0; i < target.External.Length; i++)
                    {
                        int num = i + 1;
                        external += $", E{num} {values[i]:0.####}";
                    }

                    // motion command

                    string moveText = null;
                    if (programTarget.IsJointTarget)
                    {
                        var jointTarget = target as JointTarget;
                        double[] jointDegrees = jointTarget.Joints.Select((x, i) => cell.MechanicalGroups[group].Robot.RadianToDegree(x, i)).ToArray();
                        var varMotion = $"{ jointDegrees[0].ToString("f3")},{ jointDegrees[1].ToString("f3")},{ jointDegrees[2].ToString("f3")}," +
                                   $"{jointDegrees[3].ToString("f3")},{jointDegrees[4].ToString("f3")},{jointDegrees[5].ToString("f3")}";

                        moveText = "MOVJ" + speedStr + "%" + BLtext + blendingJ + varMotion + secondStrJ + varMotion + j7j8endingNew;
                    }
                    else
                    {
                        var cartesian = target as CartesianTarget;
                        var plane = cartesian.Plane;
                        var euler = PlaneToEuler(plane);

                        switch (cartesian.Motion)
                        {
                            case Motions.Joint:
                                {

                                    //var jointTarget = target as JointTarget;
                                    double[] jointDegrees = programTarget.Kinematics.Joints.Select((x, i) => cell.MechanicalGroups[group].Robot.RadianToDegree(x, i)).ToArray();
                                    var varMotion = $"{ jointDegrees[0].ToString("f3")},{ jointDegrees[1].ToString("f3")},{ jointDegrees[2].ToString("f3")}," +
                                               $"{jointDegrees[3].ToString("f3")},{jointDegrees[4].ToString("f3")},{jointDegrees[5].ToString("f3")}";

                                    moveText = "MOVJ" + speedStr + "%" + BLtext + blendingJ + varMotion + secondStrJ + varMotion + j7j8endingNew;
                                    break;
                                }

                            case Motions.Linear:
                                {
                                    var varMotion = $"{euler[0].ToString("f3")},{euler[1].ToString("f3")},{euler[2].ToString("f3")},{euler[3].ToString("f3")},{euler[4].ToString("f3")},{euler[5].ToString("f3")}";

                                    moveText = "MOVL" + speedStr + BLtext + blending + varMotion + secondStr + varMotion + j7j8endingNew;
                                    break;
                                }
                            case Motions.Circular:
                                {

                                    var cellTarget2 = program.Targets[j + 1];
                                    var programTarget2 = cellTarget2.ProgramTargets[group];
                                    var target2 = programTarget2.Target;

                                    var cartesian2 = target2 as CartesianTarget;
                                    var plane2 = cartesian.Plane;
                                    var euler2 = PlaneToEuler(plane);

                                    var varMotion = $"{euler[0].ToString("f3")},{euler[1].ToString("f3")},{euler[2].ToString("f3")},{euler[3].ToString("f3")},{euler[4].ToString("f3")},{euler[5].ToString("f3")}";
                                    var varMotion2 = $"{euler2[0].ToString("f3")},{euler2[1].ToString("f3")},{euler2[2].ToString("f3")},{euler2[3].ToString("f3")},{euler2[4].ToString("f3")},{euler2[5].ToString("f3")}";

                                    moveText = "MOVC" + speedStr + BLtext + blending + varMotion + secondStr + varMotion + j7j8endingNew +
                                                circStr + varMotion2 + secondStr + varMotion2 + j7j8endingNew;

                                    j += 1;
                                    break;
                                }
                        }

                    }



                    /*command 命令*/
                    /*目前只有DO可以使用，AO等其他功能需要加在commands.cs里面，Currently only DO can be used, and other functions such as AO need to be added to commands.cs. */

                    foreach (var command in programTarget.Commands.Where(c => c.RunBefore))
                    {
                        string GoogolCode = command.Code(program, target);
                        if (GoogolCode != null && GoogolCode.Length > 0)
                        {
                            code.Add(GoogolCode);
                        }
                    }

                    code.Add(moveText);

                    foreach (var command in programTarget.Commands.Where(c => !c.RunBefore))
                    {
                        string GoogolCode = command.Code(program, target);
                        if (GoogolCode != null && GoogolCode.Length > 0)
                        {
                            code.Add(GoogolCode);
                        }

                    }
                }
                code.Add("END");
                return code;
            }

            string Speed(Speed speed, int index)
            {
                string[] type = speed.GetType().ToString().Split('.');
                double per = (double)index / 1000 * 100;
                if (index <= 10) per *= 2;
                return $"V{index}={{_Type=\"{type[1].ToUpper()}\", per={Min(per, 100).ToString("f4")},tcp={index.ToString("f4")},ori={(speed.RotationSpeed.ToDegrees() * 2).ToString("f4")}," +
                        $"exj_l={speed.TranslationExternal.ToString("f4")},exj_r={(speed.RotationExternal.ToDegrees() / 6).ToString("f4")}}}";
            }

            double[] jointsCF(double[] jointsRadian)
            {
                double[] jointsCF = new double[jointsRadian.Length];
                double cf = 0;

                for (int i = 0; i < jointsRadian.Length; i++)
                {
                    double radian = Abs(jointsRadian[i]);
                    for (int j = 0; j < 3; j++)
                    {
                        if (radian > 2 * j * PI - PI && radian <= 2 * j * PI + PI)
                        {
                            cf = j;
                            break;
                        }
                    }
                    if (jointsRadian[i] < 0)
                        cf = -cf;
                    jointsCF[i] = (cf);
                }
                return jointsCF;
            }
        }
    }
}