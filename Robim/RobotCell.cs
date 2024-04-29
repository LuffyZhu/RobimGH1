using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RobimRobots;

namespace Robim
{
    public abstract class RobotCell : RobotSystem
    {
        public List<MechanicalGroup> MechanicalGroups { get; }

        internal RobotCell(string name, Manufacturers manufacturer, List<MechanicalGroup> mechanicalGroups, IO io, Plane basePlane, Mesh environment,RobimFormSystem robimFormSystem) : base(name, manufacturer, io, basePlane, environment, robimFormSystem)
        {
            this.MechanicalGroups = mechanicalGroups;
            this.DisplayMesh = new Mesh();

            foreach (var group in mechanicalGroups)
            {
                var movesRobot = group.Externals.FirstOrDefault(m => m.MovesRobot);
                var robotDisplay = group.Robot.DisplayMesh;
                if (movesRobot != null)
                {
                    var movableBase = movesRobot.Joints.Last().Plane;
                    movableBase.Transform(movesRobot.BasePlane.ToTransform());
                    robotDisplay.Transform(movableBase.ToTransform());
                }

                DisplayMesh.Append(robotDisplay);
                foreach (var external in group.Externals) DisplayMesh.Append(external.DisplayMesh);
            }
            this.DisplayMesh.Transform(this.BasePlane.ToTransform());
        }

        internal override double Payload(int group)
        {
            return this.MechanicalGroups[group].Robot.Payload;
        }

        internal override Joint[] GetJoints(int group)
        {
            return MechanicalGroups[group].Joints.ToArray();
        }

        internal int GetPlaneIndex(Frame frame)
        {
            if (frame.CoupledMechanism != -1)
            {
                int index = -1;

                for (int i = 0; i < frame.CoupledMechanicalGroup; i++)
                {
                    index += this.MechanicalGroups[i].Joints.Count + 2;
                }

                for (int i = 0; i <= frame.CoupledMechanism; i++)
                {
                    index += this.MechanicalGroups[frame.CoupledMechanicalGroup].Externals[i].Joints.Length + 1;
                }

                return index;
            }
            else
            {
                int index = -1;

                for (int i = 0; i <= frame.CoupledMechanicalGroup; i++)
                {
                    index += this.MechanicalGroups[i].Joints.Count + 2;
                }

                return index;
            }
        }

        public override List<KinematicSolution> Kinematics(IEnumerable<Target> targets, IEnumerable<double[]> prevJoints = null) => new RobotCellKinematics(this, targets, prevJoints).Solutions;
        public override double DegreeToRadian(double degree, int i, int group = 0) => this.MechanicalGroups[group].DegreeToRadian(degree, i);
        public override double RadianToDegree(double radian, int i, int group = 0) => this.MechanicalGroups[group].RadianToDegree(radian, i);

        //public Plane CoupledPlaneTransform;
        public class RobotCellKinematics
        {
            internal List<KinematicSolution> Solutions;

            //public static Plane CoupledPlaneTransform;
            internal RobotCellKinematics(RobotCell cell, IEnumerable<Target> targets, IEnumerable<double[]> prevJoints)
            {
                this.Solutions = new List<KinematicSolution>(new KinematicSolution[cell.MechanicalGroups.Count]);

                if (targets.Count() != cell.MechanicalGroups.Count) throw new Exception(" Incorrect number of targets.");
                if (prevJoints != null && prevJoints.Count() != cell.MechanicalGroups.Count) throw new Exception(" Incorrect number of previous joint values.");

                var groups = cell.MechanicalGroups.ToList();

                foreach (var target in targets)
                {
                    var index = target.Frame.CoupledMechanicalGroup;
                    if (index == -1) continue;
                    var group = cell.MechanicalGroups[index];
                    groups.RemoveAt(index);
                    groups.Insert(0, group);
                }

                var targetsArray = targets.ToArray();
                var prevJointsArray = prevJoints?.ToArray();

                foreach (var group in groups)
                {
                    int i = group.Index;
                    var target = targetsArray[i];
                    var prevJoint = prevJointsArray?[i];
                    Plane? coupledPlane = null;

                    int coupledGroup = target.Frame.CoupledMechanicalGroup;

                    if (coupledGroup != -1 && target.Frame.CoupledMechanism == -1)
                    {
                        if (coupledGroup == i) throw new Exception(" Can't couple a robot with itself.");
                        coupledPlane = Solutions[coupledGroup].Planes[Solutions[coupledGroup].Planes.Length - 2] as Plane?;
                        //CoupledPlaneTransform = (Plane)coupledPlane;
                    }
                    else
                    {
                        coupledPlane = null;
                    }

                    var kinematics = group.Kinematics(target, prevJoint, coupledPlane, cell.BasePlane);
                    Solutions[i] = kinematics;
                }
            }
        }
    }

    public class MechanicalGroup
    {
        internal int Index { get; }
        internal string Name { get; }
        public RobotArm Robot { get; }
        public List<Mechanism> Externals { get; }
        public List<Joint> Joints { get; }
        public List<Plane> DefaultPlanes { get; }
        public List<Mesh> DefaultMeshes { get; }

        public ModelProperties[] ModelProperties { get; }
        public string[] External_Type { get; set; }
        internal MechanicalGroup(int index, List<Mechanism> mechanisms,ModelProperties[] modelProperties,RobimFormSystem robimFormSystem)
        {
            Index = index;
            Name = $"T_ROB{index + 1}";
            Joints = mechanisms.SelectMany(x => x.Joints.OrderBy(y => y.Number)).ToList();
            Robot = mechanisms.OfType<RobotArm>().FirstOrDefault();
            mechanisms.Remove(Robot);
            Externals = mechanisms;

            DefaultPlanes = mechanisms
                .Select(m => m.Joints.Select(j => j.Plane).Prepend(Plane.WorldXY)).SelectMany(p => p)
                .Append(Plane.WorldXY).Concat(Robot.Joints.Select(j => j.Plane).Append(Plane.WorldXY))
                .ToList();

            DefaultMeshes = mechanisms
                 .Select(m => m.Joints.Select(j => j.Mesh).Prepend(m.BaseMesh)).SelectMany(p => p)
                 .Append(Robot.BaseMesh).Concat(Robot.Joints.Select(j => j.Mesh))
                 .ToList();

            ModelProperties = modelProperties;
            External_Type = robimFormSystem.External_Type;
        }

        internal static MechanicalGroup Create(ModelProperties[] modelProperties , RobimFormSystem rfs)
        {
            int index = 0;
            var groupAttribute = modelProperties[0] as RobotProperties;
            if (groupAttribute != null) index = groupAttribute.Group;

            var mechanisms = new List<Mechanism>();
            foreach (var mechanismElement in modelProperties)
            {
                if(mechanismElement != null)
                    mechanisms.Add(Mechanism.Create(mechanismElement,rfs));
            }

            return new MechanicalGroup(index, mechanisms, modelProperties, rfs);
        }

        public KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? coupledPlane = null, Plane? basePlane = null) => new MechanicalGroupKinematics(this, target, prevJoints, coupledPlane, basePlane, External_Type);

        public double DegreeToRadian(double degree, int i)
        {
            if (i < 6)
                return Robot.DegreeToRadian(degree, i);
            else
                return Externals.First(x => x.Joints.Contains(Joints.First(y => y.Number == i))).DegreeToRadian(degree, i);
        }

        public double RadianToDegree(double radian, int i)
        {
            if (i < 6)
                return Robot.RadianToDegree(radian, i);
            else
                return Externals.First(x => x.Joints.Contains(Joints.First(y => y.Number == i))).RadianToDegree(radian, i);
        }

        public double[] RadiansToDegreesExternal(Target target)
        {
            double[] values = new double[target.External.Length];
            bool isrevolve = false;
            if(ModelProperties[2]!= null)
            {
                var plat = ModelProperties[2] as PlatformProperties;
                isrevolve = plat.IsRevolve;
            }
            for (int i = 0; i < External_Type.Length; i++)
            {
                if (External_Type[i].Contains("Track") || !isrevolve)
                {
                    values[i] = target.External[i];
                }
                else
                {
                    values[i] = target.External[i] * (180 / Math.PI);
                }
            }
            //foreach (var mechanism in Externals)//when target.External.Length = 2 ; External[0] = Track,External[1] = Positioner
            {
                //为了配合External有两个值
                /*foreach (var joint in mechanism.Joints)
                {
                    values[joint.Number - 6] = mechanism.RadianToDegree(target.External[joint.Number - 6], joint.Index);
                }*/
                
                /*if (mechanism.ToString().Contains("Positioner"))
                {
                    int i = target.External.Length;
                    values[i - 1] = target.External[i - 1] * (180 / Math.PI);
                }
                else if (mechanism.ToString().Contains("Track"))
                {
                    values[0] = mechanism.RadianToDegree(target.External[0], mechanism.Joints[0].Index);
                }*/
                /*for (int i = 0; i< target.External.Length; i++)
                {
                    if (i == 0)
                    {
                        values[i] = mechanism.RadianToDegree(target.External[i], 1);//无法 RadianToDegree
                    }
                    else if (i == 1)
                    {
                        values[i] = target.External[i] * (180 / Math.PI);
                        //无法 RadianToDegree
                    }
                    //values[i] = mechanism.RadianToDegree(target.External[i], mechanism.Joints[0].Index);//无法 RadianToDegree
                    //values[i] = target.External[i] * (180 / Math.PI);
                }*/
            }

            return values;
        }
        public class MechanicalGroupKinematics : KinematicSolution
        {
            //public static Plane CoupledPlaneTransform;
            internal MechanicalGroupKinematics(MechanicalGroup group, Target target, double[] prevJoints, Plane? coupledPlane, Plane? basePlane,string[] externaltype)
            {
                var jointCount = group.Joints.Count;
                Joints = new double[jointCount];
                var planes = new List<Plane>();
                var errors = new List<string>();

                Plane? robotBase = basePlane;

                target = target.ShallowClone();
                Mechanism coupledMech = null;

                if (target.Frame.CoupledMechanism != -1 && target.Frame.CoupledMechanicalGroup == group.Index)
                {
                    coupledMech = group.Externals[target.Frame.CoupledMechanism];
                }
                
                // Externals
                //判断有几个External System
                foreach (var external in group.Externals)
                {
                    int[] resize = null;
                    int externalnumber = 0;
                    int externaljointcount = 0;
                    //var externalPrevJoints = prevJoints?.Subset(external.Joints.Select(x => x.Number).ToArray());
                    //var externalKinematics = external.Kinematics(target, externalPrevJoints, basePlane);
                    string str = null;
                    if (external.ToString().Contains("Track"))
                        str = "Track";
                    else
                        str = "Platform";
                    for (int i = 0; i < externaltype.Length; i++)
                    {
                        if (externaltype[i].Contains(str))
                        {
                            externalnumber = i;
                            externaljointcount += 1;
                            Array.Resize(ref resize, externaljointcount);
                            resize.SetValue(externalnumber + external.Joints[0].Number, externaljointcount - 1);//joint number = 6 , index = 5
                        }
                    }

                    #region Old
                    /*if (external.ToString().Contains("Track"))
                    {
                        for(int i = 0; i < RobotSystem.externaltype.Length; i++)
                        {
                            if (RobotSystem.externaltype[i].Contains("Track"))
                            {
                                externalnumber = i;
                                externaljointcount += 1;
                                Array.Resize(ref j, externaljointcount);
                                j.SetValue(externalnumber + external.Joints[0].Number, externaljointcount - 1);//joint number = 6 , index = 5
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < RobotSystem.externaltype.Length; i++)
                        {
                            if (RobotSystem.externaltype[i].Contains("Platform"))
                            {
                                externalnumber = i;
                                externaljointcount += 1;
                            }
                        }
                    }*/
                    #endregion

                    //int[] j = new int[1] { externalnumber + 6 };
                    //j = new int[1] { externalnumber + external.Joints[0].Number };
                    var externalPrevJoints = prevJoints?.Subset(resize.ToArray());
                    var externalKinematics = external.Kinematics(target, externalPrevJoints, basePlane);
                    for (int k = 0; k < external.Joints.Length; k++)
                        Joints[resize[k]] = externalKinematics.Joints[k];

                    planes.AddRange(externalKinematics.Planes);
                    errors.AddRange(externalKinematics.Errors);

                    if (external == coupledMech)
                    {
                        coupledPlane = externalKinematics.Planes[externalKinematics.Planes.Length - 1];
                    }

                    if (external.MovesRobot)
                    {
                        //Plane externalPlane = externalKinematics.Planes[externalKinematics.Planes.Length - 1];
                        Plane externalPlane = externalKinematics.Planes[externalKinematics.Planes.Length - external.Joints.Length];//配合木加工平台，robot放置在第一个joint
                        // With the wood processing platform, the robot is placed in the first joint
                        robotBase = externalPlane;
                    }
                }

                // Coupling
                if (coupledPlane != null)
                {
                    var coupledFrame = target.Frame.ShallowClone();
                    var plane = coupledFrame.Plane;//WorldXY
                    plane.Transform(Transform.PlaneToPlane(Plane.WorldXY, (Plane)coupledPlane));//this coupledPlane isn't initial coupling surface,is Coupling surface after rotation
                    coupledFrame.Plane = plane;
                    target.Frame = coupledFrame;
                }

                // Robot
                var robot = group.Robot;

                if (robot != null)
                {
                    var robotPrevJoints = prevJoints?.Subset(robot.Joints.Select(x => x.Number).ToArray());
                    //var robotKinematics = robot.Kinematics(target, robotPrevJoints, robotBase);
                    KinematicSolution robotKinematics;
                    if (UseIKFast)
                    {
                        robotKinematics = robot.Kinematics_Analytic(target, robotPrevJoints, robotBase);
                    }
                    else
                    {
                        robotKinematics = robot.Kinematics(target, robotPrevJoints, robotBase);
                    }

                    for (int j = 0; j < robot.Joints.Length; j++)
                        Joints[robot.Joints[j].Number] = robotKinematics.Joints[j];

                    planes.AddRange(robotKinematics.Planes);
                    Configuration = robotKinematics.Configuration;

                    if (robotKinematics.Errors.Count > 0)
                    {
                        errors.AddRange(robotKinematics.Errors);
                    }
                }

                // Tool
                Plane toolPlane = target.Tool.Tcp;
                toolPlane.Transform(planes[planes.Count - 1].ToTransform());
                planes.Add(toolPlane);

                Planes = planes.ToArray();

                if (errors.Count > 0)
                {
                    Errors.AddRange(errors);
                }
            }
        }
    }
}
