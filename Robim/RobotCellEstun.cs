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
    public class RobotCellEstun : RobotCell
    {
        internal RobotCellEstun(string name, List<MechanicalGroup> mechanicalGroup, IO io, Plane basePlane, Mesh environment, RobimFormSystem robimFormSystem) : base(name, Manufacturers.Estun, mechanicalGroup, io, basePlane, environment , robimFormSystem) { }

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
            Directory.CreateDirectory($@"{folder}\Project");
            Directory.CreateDirectory($@"{folder}\Project\{program.Name}.er");

            for (int i = 0; i < program.Code.Count; i++)
            {
                string group = MechanicalGroups[i].Name;
                {
                    string file = $@"{folder}\Project\_global.erd";
                    var joinedCode = string.Join("\r\n", program.Code[i][0]);
                    File.WriteAllText(file, joinedCode);
                }
                {
                    string file = $@"{folder}\Project\_system.erd";
                    var joinedCode = string.Join("\r\n", program.Code[i][1]);
                    File.WriteAllText(file, joinedCode);
                }
                
                for (int j = 2; j < program.Code[i].Count; j+=3)
                {
                    string file = $@"{folder}\Project\{program.Name}.er\_prjglobal.erd";
                    var joinedCode = string.Join("\r\n", program.Code[i][j]);
                    File.WriteAllText(file, joinedCode);

                    string fileerd = $@"{folder}\Project\{program.Name}.er\{program.Name}.erd";
                    var joinedCodeerd = string.Join("\r\n", program.Code[i][j+1]);
                    File.WriteAllText(fileerd, joinedCodeerd);

                    string fileerp = $@"{folder}\Project\{program.Name}.er\{program.Name}.erp";
                    var joinedCodeerp = string.Join("\r\n", program.Code[i][j+2]);
                    File.WriteAllText(fileerp, joinedCodeerp);
                }
            }
        }


        class KRLPostProcessor
        {
            RobotCellEstun cell;
            Program program;
            internal List<List<List<string>>> Code { get; }
            int tool_num = 0;

            internal KRLPostProcessor(RobotCellEstun robotCell, Program program)
            {
                this.cell = robotCell;
                this.program = program;
                this.Code = new List<List<List<string>>>();
                

                for (int i = 0; i < cell.MechanicalGroups.Count; i++)
                {
                    var groupCode = new List<List<string>>
                    {
                        globalErd(i),     //生成global.erd文件，存放target预设位置。目前这份文件为空。
                        systemErd(i)      //生成system.erd文件，存放Tool, World coor, Zone, Speed的预设值。此文件不可更改。
                    };

                    for (int j = 0; j < program.MultiFileIndices.Count; j++)
                    {
                        groupCode.Add(PrjGlobalErd(j, i));       //生成pjGlobal.erd文件，目前为空。
                        groupCode.Add(mainErd(j, i));           //生成main.erd文件，存放点位。
                        groupCode.Add(mainErp(j, i));           //存放main.erp文件，存放movement。
                        
                    }

                    Code.Add(groupCode);
                    
                }
            }



            List<string> globalErd(int group)
            {
                /****************************************/
                /*此文件中应当存放target初始值。可以为空。*/
                /*        工具头应该可以写在这里。       */
                /***************************************/
                //设置工具头

                var code = new List<string> { $@"//global.erd" };
                Tool();


                /*
                code.Add("P1 ={ _type = \"CPOS\",confdata ={ _type = \"POSCFG\",mode = 0,cf1 = 0,cf2 = 0,cf3 = 0,cf4 = 0,cf5 = 0,cf6 = 0},x = 0.0000000,y = 0.0000000,z = 0.0000000,a = 0.0000000,b = 0.0000000,c = 0.0000000,a7 = 0.0000000,a8 = 0.0000000,a9 = 0.0000000,a10 = 0.0000000,a11 = 0.0000000,a12 = 0.0000000,a13 = 0.0000000,a14 = 0.0000000,a15 = 0.0000000,a16 = 0.0000000}");
                code.Add("P0 ={ _type = \"APOS\",a1 = 0.0000000,a2 = 0.0000000,a3 = 0.0000000,a4 = 0.0000000,a5 = 0.0000000,a6 = 0.0000000,a7 = 0.0000000,a8 = 0.0000000,a9 = 0.0000000,a10 = 0.0000000,a11 = 0.0000000,a12 = 0.0000000,a13 = 0.0000000,a14 = 0.0000000,a15 = 0.0000000,a16 = 0.0000000}");
                code.Add("DCPOS0 ={ _type = \"DCPOS\",dx = 0.0000000,dy = 0.0000000,dz = 0.0000000,da = 0.0000000,db = 0.0000000,dc = 0.0000000,da7 = 0.0000000,da8 = 0.0000000,da9 = 0.0000000,da10 = 0.0000000,da11 = 0.0000000,da12 = 0.0000000,da13 = 0.0000000,da14 = 0.0000000,da15 = 0.0000000,da16 = 0.0000000}");
                code.Add("DAPOS0 ={ _type = \"DAPOS\",da1 = 0.0000000,da2 = 0.0000000,da3 = 0.0000000,da4 = 0.0000000,da5 = 0.0000000,da6 = 0.0000000,da7 = 0.0000000,da8 = 0.0000000,da9 = 0.0000000,da10 = 0.0000000,da11 = 0.0000000,da12 = 0.0000000,da13 = 0.0000000,da14 = 0.0000000,da15 = 0.0000000,da16 = 0.0000000}");
                 */
                return code;
                
            }

            List<string> systemErd(int group)
            {
                /*****************************/
                /*        此文件不可更改      */
                /*****************************/
                var code = new List<string> { $@"//system.erd" };

                code.Add($"nullTool={nullTool()}");         //初始Tool设定
                code.Add($"World={worldCoor()}");           //初始世界坐标系设定

                var zone = Robim.Zone.Default;
                var speed = Robim.Speed.Default;

                List<int> ZoneType = new List<int>          //预设Zone数值
                {
                    0, 5, 10, 20, 30, 50, 60, 80, 100, 150, 200
                };
                List<int> SpeedType = new List<int>         //预设Speed数值
                {
                    5, 10, 30, 50, 60, 80, 100, 200, 300, 500, 800, 1000, 1500, 2000, 3000, 4000
                };

                for (int i = 0; i < ZoneType.Count(); i++)
                { 
                    code.Add(Zone(zone, ZoneType[i]));
                }
                for (int i = 0; i < SpeedType.Count(); i++)
                {
                    code.Add(Speed(speed, SpeedType[i]));
                }
                return code;
            }

            List<string> PrjGlobalErd(int file, int group)
            {
                var code = new List<string> { $@"//prgGlobal.erd" };

                int start = program.MultiFileIndices[file];
                int end = (file == program.MultiFileIndices.Count - 1) ? program.Targets.Count : program.MultiFileIndices[file + 1];
                for (int j = start; j < end; j++)
                {
                    var cellTarget = program.Targets[j];
                    var programTarget = cellTarget.ProgramTargets[group];
                    var target = programTarget.Target;
                    var speed = Robim.Speed.Default;
                    code.Add(Speed(speed, (int)target.Speed.TranslationSpeed));
                }

                    return code;
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
                            $", a7={valuesExternal[0].ToString("f4")}, a8={valuesExternal[1].ToString("f4")},a9={valuesExternal[2].ToString("f4")}, "+
                            $" a10={valuesExternal[3].ToString("f4")}, a11={valuesExternal[4].ToString("f4")}, a12={valuesExternal[5].ToString("f4")}, "+
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

                var code = new List<string>
                {
                    $@"//main.erp"
                };
                Tool currentTool = null;
                Frame currentFrame = null;
                Speed currentSpeed = null;
                double currentPercentSpeed = 0;
                Zone currentZone = null;

                code.Add("Start:");

                for (int j = start; j < end; j++)
                {
                    var cellTarget = program.Targets[j];
                    var programTarget = cellTarget.ProgramTargets[group];
                    var target = programTarget.Target;

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
                            //code.Add($"$BASE = EK(MACHINE_DEF[{mech}].ROOT, MACHINE_DEF[{mech}].MECH_TYPE, {target.Frame.Name})");
                            //code.Add($"$ACT_EX_AX = 2");
                        }
                        else
                        {
                            //code.Add($"$BASE={target.Frame.Name}");
                        }
                        currentFrame = target.Frame;
                    }

                    if (target.Zone.IsFlyBy && (currentZone == null || target.Zone != currentZone))
                    {
                        //code.Add($"$APO.CDIS={target.Zone.Name}");
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
                        moveText = $"MovJ{{P=t_l.P{j},V=t_s.V{target.Speed.TranslationSpeed.ToString("f4")},B=\"RELATIVE\",C=t_s.C{target.Zone.Distance.ToString("f4")},DO=\"{null}\"}}";
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
                                    moveText = $"MovJ{{P=t_l.P{j},V=t_s.V{target.Speed.TranslationSpeed.ToString("f4")},B=\"RELATIVE\",C=t_s.C{target.Zone.Distance.ToString("f4")},DO=\"{null}\"}}";
                                    break;
                                }

                            case Motions.Linear:
                                {
                                    moveText = $"MovL{{P=t_l.P{j},V=t_s.V{target.Speed.TranslationSpeed.ToString("f4")},B=\"RELATIVE\",C=t_s.C{target.Zone.Distance.ToString("f4")},DO=\"{null}\"}}";
                                    break;
                                }
                            case Motions.Circular:
                                {
                                    moveText = $"MovC{{A=t_l.P{j}, P=t_l.P{j+1},V=t_s.V{target.Speed.TranslationSpeed.ToString("f4")},B=\"RELATIVE\",C=t_s.C{target.Zone.Distance.ToString("f4")},DO=\"{null}\"}}";
                                    j += 1;
                                    break;
                                }
                        }
                        
                    }



                    /*command 命令*/
                    /*目前只有DO可以使用，AO等其他功能需要加在commands.cs里面，*/

                    foreach (var command in programTarget.Commands.Where(c => c.RunBefore))
                    {
                        string ESTUNcode = command.Code(program, target);
                        if (ESTUNcode != null && ESTUNcode.Length > 0)
                        {
                            code.Add(ESTUNcode);
                        }
                    }
                    
                    code.Add(moveText);
                    
                    foreach (var command in programTarget.Commands.Where(c => !c.RunBefore))
                    {
                        string ESTUNcode = command.Code(program, target);
                        if (ESTUNcode != null && ESTUNcode.Length > 0)
                        {
                            code.Add(ESTUNcode);
                        }

                    }
                }
                code.Add("END;");
                return code;
            }

            string nullTool()
            {
                //预定义工具头
                string nTool = " {_type = \"TOOL\",id = 0,x = 0.000000,y = 0.000000,z = 0.000000,a = 0.000000,b = 0.000000,c = 0.000000}";
                return nTool;
            }

            string worldCoor()
            {
                //预定义world coordinate
                string wCoor = "{_type=\"USERCOOR\",id=0,x=0.000000,y=0.000000,z=0.000000,a=0.000000,b=0.000000,c=0.000000}";
                return wCoor;
            }


            string Zone(Zone zone, int index)
            {
                string[] type = zone.GetType().ToString().Split('.');
                double per = (double)index / 100 * 100;
                return $"C{index}={{_Type=\"{type[1].ToUpper()}\",per={Min(per, 100).ToString("f4")},dis={index.ToString("f4")},vConst={zone.VConst:0}}}";
            }

            string Speed(Speed speed, int index)
            {
                string[] type = speed.GetType().ToString().Split('.');
                double per = (double)index / 1000 * 100;
                if (index <= 10) per *= 2;
                return $"V{index}={{_Type=\"{type[1].ToUpper()}\", per={Min(per, 100).ToString("f4")},tcp={index.ToString("f4")},ori={(speed.RotationSpeed.ToDegrees() * 2).ToString("f4")}," +
                        $"exj_l={speed.TranslationExternal.ToString("f4")},exj_r={(speed.RotationExternal.ToDegrees() / 6).ToString("f4")}}}";
            }

            List<string> Tool()  // 设置工具头
            {
                var code = new List<string>();
                foreach (var tool in program.Attributes.OfType<Tool>())
                {
                    tool_num += 1;
                    Plane tcp = tool.Tcp;
                    //Plane originPlane = new Plane(Point3d.Origin, -Vector3d.YAxis, Vector3d.XAxis);
                    //tcp.Transform(Transform.PlaneToPlane(Plane.WorldXY, originPlane));
                    double[] axisAngle = PlaneToEuler(tcp);
                    code.Add($"_type =\"TOOL\",id=tool_num,x ={axisAngle[0]},y={axisAngle[1]},z={axisAngle[2]}," +
                        $"a={axisAngle[3]},b={axisAngle[4]},c={axisAngle[5]}");
                }
                return code;
            }


            string ExternalSpeed(ProgramTarget target)
            {
                string externalSpeedCode = "";
                var joints = cell.GetJoints(target.Group);
                //   int externalJointsCount = target.External.Length;

                // for (int i = 0; i < externalJointsCount; i++)
                {
                    var joint = joints[target.LeadingJoint];
                    double percentSpeed = 0;
                    if (joint is PrismaticJoint) percentSpeed = target.Target.Speed.TranslationExternal / joint.MaxSpeed;
                    if (joint is RevoluteJoint) percentSpeed = target.Target.Speed.RotationExternal / joint.MaxSpeed;
                    percentSpeed = Clamp(percentSpeed, 0.0, 1.0);
                    externalSpeedCode += $"BAS(#VEL_PTP, 100)" + "\r\n";
                    externalSpeedCode += $"$VEL_EXTAX[{target.LeadingJoint + 1 - 6}] = {percentSpeed * 100:0.###}";
                    //     if (i < externalJointsCount - 1) externalSpeedCode += "\r\n";
                }

                return externalSpeedCode;
            }

            string SetTool(Tool tool)
            {
                string toolTxt = $"$TOOL={tool.Name}";
                string load = $"$LOAD.M={tool.Weight}";
                Point3d centroid = tool.Centroid;
                string centroidTxt = $"$LOAD.CM={{X {centroid.X:0.###},Y {centroid.Y:0.###},Z {centroid.Z:0.###},A 0,B 0,C 0}}";
                return $"{toolTxt}\r\n{load}\r\n{centroidTxt}";
            }

            string Tool(Tool tool)
            {
                double[] euler = PlaneToEuler(tool.Tcp);
                return $"DECL GLOBAL FRAME {tool.Name}={{FRAME: X {euler[0]:0.###},Y {euler[1]:0.###},Z {euler[2]:0.###},A {euler[3]:0.####},B {euler[4]:0.####},C {euler[5]:0.####}}}";
            }

            string Frame(Frame frame)
            {
                Plane plane = frame.Plane;
                plane.Transform(Transform.PlaneToPlane(cell.BasePlane, Plane.WorldXY));

                double[] euler = PlaneToEuler(plane);
                return $"DECL GLOBAL FRAME {frame.Name}={{FRAME: X {euler[0]:0.###},Y {euler[1]:0.###},Z {euler[2]:0.###},A {euler[3]:0.####},B {euler[4]:0.####},C {euler[5]:0.####}}}";
            }

            


            double[] jointsCF(double[] jointsRadian)
            {
                double [] jointsCF = new double[jointsRadian.Length];
                double cf = 0;

                for (int i = 0; i < jointsRadian.Length; i++)
                {
                    double radian = Abs(jointsRadian[i]);
                    for(int j=0; j<3; j++)
                    {
                        if(radian>2*j*PI-PI && radian<=2*j*PI+PI)
                        {
                            cf = j;
                            break;
                        }
                    }
                    if (jointsRadian[i] < 0)
                        cf = -cf;
                    jointsCF[i]=(cf);
                }
                return jointsCF;
            }
        }
    }
}