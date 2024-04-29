using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static Robim.Util;
using static System.Math;
using RobimRobots;

namespace Robim
{
    public class RobotCellAubo: RobotCell
    {
        //public RobotAubo Robot { get; }

        //internal RobotCellAubo(string name, RobotArm robot, IO io, Plane basePlane, Mesh environment) : base(name, Manufacturers.Aubo, io, basePlane, environment)
        //{
        //    this.Robot = robot as RobotAubo;
        //    this.DisplayMesh = new Mesh();
        //    DisplayMesh.Append(robot.DisplayMesh);
        //    this.DisplayMesh.Transform(this.BasePlane.ToTransform());
        //}
        internal RobotCellAubo(string name, List<MechanicalGroup> mechanicalGroup, IO io, Plane basePlane, Mesh environment,RobimFormSystem robimFormSystem) : base(name, Manufacturers.Aubo, mechanicalGroup, io, basePlane, environment,robimFormSystem) { }
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
            const double UnitTol = 0.0000001;
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

        internal override List<List<List<string>>> Code(Program program) => new AuboPostProcessor(this, program).Code;

        internal override void SaveCode(Program program, string folder)
        {
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($" Folder \"{folder}\" not found");
            if (program.Code == null) throw new NullReferenceException(" Program code not generated");
            Directory.CreateDirectory($@"{folder}\{program.Name}");

            for (int i = 0; i < program.Code.Count; i++)
            {
                string group = MechanicalGroups[i].Name;
                {
                    string file = $@"{folder}\{program.Name}\{program.Name}_{group}.SRC";
                    var joinedCode = string.Join("\r\n", program.Code[i][0]);
                    File.WriteAllText(file, joinedCode);
                }
                {
                    string file = $@"{folder}\{program.Name}\{program.Name}_{group}.DAT";
                    var joinedCode = string.Join("\r\n", program.Code[i][1]);
                    File.WriteAllText(file, joinedCode);
                }
                for (int j = 2; j < program.Code[i].Count; j++)
                {
                    int index = j - 2;
                    string file = $@"{folder}\{program.Name}\{program.Name}_{group}_{index:000}.SRC";
                    var joinedCode = string.Join("\r\n", program.Code[i][j]);
                    File.WriteAllText(file, joinedCode);
                }
            }
        }



        class AuboPostProcessor
        {
            RobotCellAubo cell;
            Program program;
            internal List<List<List<string>>> Code { get; }

            internal AuboPostProcessor(RobotCellAubo robotCell, Program program)
            {
                this.cell = robotCell;
                this.program = program;
                var groupCode = new List<List<string>> { Program() };
                this.Code = new List<List<List<string>>> { groupCode };
            }

            List<string> Program()
            {
                string indent = "  ";
                var code = new List<string>
                {
                    "def AuboProgram():"
                };

                // Attribute declarations

                foreach (var tool in program.Attributes.OfType<Tool>())
                {
                    Plane tcp = tool.Tcp;
                    Plane originPlane = new Plane(Point3d.Origin, Vector3d.YAxis, -Vector3d.XAxis);
                    tcp.Transform(Transform.PlaneToPlane(Plane.WorldXY, originPlane));
                    Point3d tcpPoint = tcp.Origin / 1000;
                    tcp.Origin = tcpPoint;
                    double[] axisAngle = PlaneToEuler(tcp);

                    Point3d cog = tool.Centroid;
                    cog.Transform(Transform.PlaneToPlane(Plane.WorldXY, originPlane));
                    cog /= 1000;
                    //code.Add(indent + $"{tool.Name}Tcp = p[{axisAngle[0]:0.#####}, {axisAngle[1]:0.#####}, {axisAngle[2]:0.#####}, {axisAngle[3]:0.#####}, {axisAngle[4]:0.#####}, {axisAngle[5]:0.#####}]");
                    code.Add(indent + $"tool_tcp = [{axisAngle[0]:0.#####}, {axisAngle[1]:0.#####}, {axisAngle[2]:0.#####}, {axisAngle[3]*PI/180:0.#####}, {axisAngle[4] * PI / 180:0.#####}, {axisAngle[5] * PI / 180:0.#####}]");  // xyz ABC 
                    //code.Add(indent + $"set_tcp(tool_tcp)");
                }

                foreach (var speed in program.Attributes.OfType<Speed>()) // 限制速度
                {
                    double linearSpeed = speed.TranslationSpeed / 1000;
                    if (linearSpeed < 0) linearSpeed = 0.1;
                    if (linearSpeed > 1) linearSpeed = 1;  // aubo最大速度应该限制在 2000mm/s 下，加速度限制在 2000mm/s2 下
                    code.Add(indent + $"{speed.Name} = {linearSpeed:0.#####}");
                }

                /*foreach (var zone in program.Attributes.OfType<Zone>()) // 限制交融半径
                {
                    double zoneDistance = zone.Distance / 1000;
                    if (zoneDistance < 0.01) zoneDistance = 0.01;
                    if (zoneDistance > 0.05) zoneDistance = 0.05;
                    code.Add(indent + $"{zone.Name} = {zoneDistance:0.#####}");
                }*/
                double targetzone = 0;
                foreach (var zone in program.Attributes.OfType<Zone>()) // 限制交融半径
                {
                    double zoneDistance = zone.Distance / 1000;
                    if (zoneDistance < 0.001) zoneDistance = 0.001;
                    if (zoneDistance > 0.008) zoneDistance = 0.008;
                    code.Add(indent + $"{zone.Name} = {zoneDistance:0.#####}");
                    targetzone = zoneDistance;
                }

                foreach (var command in program.Attributes.OfType<Command>())
                {
                    string declaration = command.Declaration(program);
                    if (declaration != null && declaration.Length > 0)
                    {
                        declaration = indent + declaration;
                        //  declaration = indent + declaration.Replace("\n", "\n" + indent);
                        code.Add(declaration);
                    }
                }

                // Init commands

                foreach (var command in program.InitCommands)
                    code.Add(command.Code(program, Target.Default));

                Tool currentTool = null;

                // Targets
                int start = 0;
                string[] split = null;


                foreach (var cellTarget in program.Targets)
                {
                    var programTarget = cellTarget.ProgramTargets[0];
                    var target = programTarget.Target;

                    if (currentTool == null || target.Tool != currentTool)
                    {
                        code.Add(Tool(target.Tool));  // 设置工具头
                        currentTool = target.Tool;
                    }

                    string moveText = null;
                    string zoneDistance = $"{target.Zone.Name:0.#####}";
                    //double zoneDistance = target.Zone.Distance / 1000;

                    if (programTarget.IsJointTarget || (programTarget.IsJointMotion && programTarget.ForcedConfiguration))
                    {
                        double[] joints = programTarget.IsJointTarget ? (programTarget.Target as JointTarget).Joints : programTarget.Kinematics.Joints;
                        
                        double maxAxisSpeed = 90;  //aubo最大关节速度为 148.75度/秒 ，最大关节加速度 991.72 度每秒
                        double percentage = (cellTarget.DeltaTime > 0) ? cellTarget.MinTime / cellTarget.DeltaTime : 0.1;
                        double axisSpeed = percentage * maxAxisSpeed;
                        double axisAccel = target.Speed.AxisAccel / 2;

                        string speed = null;
                        if (target.Speed.Time == 0)
                            speed = $"v={axisSpeed: 0.###}";
                        else
                            speed = $"t={target.Speed.Time: 0.###}";

                        moveText = $"  movej([{joints[0]:0.####}, {joints[1]:0.####}, {joints[2]:0.####}, {joints[3]:0.####}, {joints[4]:0.####}, {joints[5]:0.####}], a={axisAccel:0.####}, {speed}, r={zoneDistance})";
                            
                    }
                    else
                    {
                        var cartesian = target as CartesianTarget;
                        var plane = cartesian.Plane;
                        plane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                        plane.Transform(Transform.PlaneToPlane(cell.BasePlane, Plane.WorldXY));
                        var axisAngle = cell.PlaneToNumbers(plane);

                        double[] joints = programTarget.IsJointTarget ? (programTarget.Target as JointTarget).Joints : programTarget.Kinematics.Joints;

                        switch (cartesian.Motion)
                        {
                            // 下面的移动 无论是 movej 还是 movel 传入的参数都是 x y z a b c  需要改成 joints
                            case Motions.Joint:
                                {
                                    double maxAxisSpeed = 90;
                                    double percentage = (cellTarget.DeltaTime > 0) ? cellTarget.MinTime / cellTarget.DeltaTime : 0.1;
                                    double axisSpeed = percentage * maxAxisSpeed;
                                    double axisAccel = target.Speed.AxisAccel / 2;

                                    string speed = null;
                                    if (target.Speed.Time == 0)
                                        speed = $"v={axisSpeed: 0.###}";
                                    else
                                        speed = $"t={target.Speed.Time: 0.###}";

                                    moveText = $"  movej([{joints[0]:0.####}, {joints[1]:0.####}, {joints[2]:0.####}, {joints[3]:0.####}, {joints[4]:0.####}, {joints[5]:0.####}], a={axisAccel:0.####}, {speed}, r={zoneDistance})";
                                    break;
                                }

                            case Motions.Linear:
                                {
                                    double linearSpeed = target.Speed.TranslationSpeed / 1000;
                                    double linearAccel = target.Speed.TranslationAccel / 1000;
                                    

                                    string speed = null;
                                    if (target.Speed.Time == 0)
                                        // speed = $"v={linearSpeed: 0.000}";
                                        speed = $"v={target.Speed.Name}";
                                    else
                                        speed = $"t={target.Speed.Time: 0.000}";

                                    //moveText = $"  movel([{joints[0]:0.####}, {joints[1]:0.####}, {joints[2]:0.####}, {joints[3]:0.####}, {joints[4]:0.####}, {joints[5]:0.####}], a={linearAccel:0.####}, {speed}, r={zoneDistance})";
                                    moveText = $"  add_point([{joints[0]:0.####}, {joints[1]:0.####}, {joints[2]:0.####}, {joints[3]:0.####}, {joints[4]:0.####}, {joints[5]:0.####}])";
                                    break;
                                }
                            case Motions.Plane:
                                {
                                    double linearSpeed = target.Speed.TranslationSpeed / 1000;
                                    double linearAccel = target.Speed.TranslationAccel / 1000;
                                    if (linearAccel > 3)
                                        linearAccel = 3;
                                    if (linearSpeed > 3)
                                        linearSpeed = 3;


                                    string speed = null;
                                    if (target.Speed.Time == 0)
                                        // speed = $"v={linearSpeed: 0.000}";
                                        speed = $"v={target.Speed.Name}";
                                    else
                                        speed = $"t={target.Speed.Time: 0.000}";

                                    moveText = $"  movep([{joints[0]:0.####}, {joints[1]:0.####}, {joints[2]:0.####}, {joints[3]:0.####}, {joints[4]:0.####}, {joints[5]:0.####}], a={linearAccel:0.####}, {speed}, r={zoneDistance})";
                                    break;
                                }
                        }
                    }

                    foreach (var command in programTarget.Commands.Where(c => c.RunBefore))
                    {
                        string commands = command.Code(program, target);
                        commands = indent + commands;
                        code.Add(commands);
                    }

                    //code.Add(moveText);
                    if(start == 0)
                    {
                        int a = moveText.IndexOf('[') + 1;
                        int b = moveText.IndexOf(']');
                        string c = moveText.Substring(a, b - a);
                        split = c.Split(',');
                        start += 1;
                        code.Add(moveText);
                    }
                    else if(start == 1)
                    {
                        double linearSpeed = target.Speed.TranslationSpeed / 1000;
                        double linearAccel = target.Speed.TranslationAccel / 1000;
                        code.Add($"  maxLineVel({linearSpeed:0.#####})");
                        code.Add($"  maxLineAcc({linearAccel:0.#####})");
                        code.Add("  track_start()");
                        code.Add($"  add_point([{split[0]:0.####},{split[1]:0.####},{split[2]:0.####},{split[3]:0.####},{split[4]:0.####},{split[5]:0.####}])");
                        start += 1;
                        code.Add(moveText);
                    }
                    else
                    {
                        code.Add(moveText);
                    }

                    foreach (var command in programTarget.Commands.Where(c => !c.RunBefore))
                    {
                        string commands = command.Code(program, target);
                        commands = indent + commands;
                        code.Add(commands);
                    }
                }
                code.Add($"  set_blend_radius({targetzone:0.#####})");
                code.Add("  move_track()");
                code.Add("  track_end()");
                code.Add("end");
                return code;
            }

            string Tool(Tool tool)
            {
                string pos = $"  set_tcp(tool_tcp)";
                //string mass = $"  set_payload({tool.Name}Weight, {tool.Name}Cog)";

                /*
                Plane tcp = tool.Tcp;
                Plane originPlane = new Plane(Point3d.Origin, Vector3d.YAxis, -Vector3d.XAxis);
                tcp.Transform(Transform.PlaneToPlane(Plane.WorldXY, originPlane));
                Point3d tcpPoint = tcp.Origin / 1000;
                double[] axisAngle = AxisAngle(tcp, Plane.WorldXY);

                Point3d cog = tool.Centroid;
                cog.Transform(Transform.PlaneToPlane(Plane.WorldXY, originPlane));
                cog /= 1000;

                string tcpString = $"p[{tcpPoint.X:0.00000}, {tcpPoint.Y:0.00000}, {tcpPoint.Z:0.00000}, {axisAngle[0]:0.0000}, {axisAngle[1]:0.0000}, {axisAngle[2]:0.0000}]";
                string cogString = $"[{cog.X:0.00000}, {cog.Y:0.00000}, {cog.Z:0.00000}]";
                string pos = $"  set_tcp({tcpString})";
                string mass = $"  set_payload({tool.Weight:0.000}, {cogString})";
                */
                return $"{pos}";
            }
        }

    }
}
