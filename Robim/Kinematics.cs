using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;
using static Robim.Util;
using static System.Math;
using static Robim.RobotCell;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Robim
{
    public abstract class KinematicSolution
    {
        public double[] Joints { get; internal set; }
        public double[][] Joints8 { get; internal set; }
        public Plane[] Planes { get; internal set; }
        public List<string> Errors { get; internal set; } = new List<string>();
        public RobotConfigurations Configuration { get; internal set; }
        public Target Target { get; internal set; }
        //public int Index => cellTarget.Index;
        // public CellTarget cellTarget { get; internal set; }
        public static bool UseIKFast { get; set; } = false;

        public Mechanism Mechanism { get; internal set; }
    }

    public abstract partial class RobotArm
    {
        protected abstract class RobotKinematics : MechanismKinematics
        {

            protected RobotKinematics(RobotArm robot, Target target, double[] prevJoints = null, Plane? basePlane = null) : base(robot, target, prevJoints, basePlane) 
            { 
            }

            protected override void SetJoints(Target target, double[] prevJoints)
            {
                if (target is JointTarget jointTarget)
                {
                    Joints = jointTarget.Joints;
                }
                else if (target is CartesianTarget cartesianTarget)
                {
                    Plane tcp = target.Tool.Tcp;
                    tcp.Rotate(PI, Vector3d.ZAxis, Point3d.Origin);

                    Plane targetPlane = cartesianTarget.Plane;
                    //变换到Frame坐标下，如在旋转情况下坐标值不发生改动
                    if (target.Frame.IsCoupled)
                    {
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                    }
                    else 
                    {
                        //targetPlane.Transform(Transform.PlaneToPlane(target.Frame.Plane, Plane.WorldXY));
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                    }

                    //targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                    //targetPlane.Transform(target.Frame.Plane.ToTransform());//把坐标转换到Frame坐标系？

                    var transform = Transform.PlaneToPlane(Planes[0], Plane.WorldXY) * Transform.PlaneToPlane(tcp, targetPlane);

                    List<string> errors;
                    double[] joints;
                    if (cartesianTarget.Configuration != null || prevJoints == null)
                    {
                        Configuration = cartesianTarget.Configuration ?? RobotConfigurations.None;
                        joints = InverseKinematics(transform, Configuration, out errors);
                    }
                    else
                    {
                        joints = GetClosestSolution(transform, prevJoints, out var configuration, out errors, out var difference);
                        Configuration = configuration;
                    }
                    if (prevJoints != null)
                        Joints = JointTarget.GetAbsoluteJoints(joints, prevJoints);
                    else
                        Joints = joints;

                    //Joints8 = Get8Solution(transform, out errors);

                    Errors.AddRange(errors);
                }
            }

            protected override void SetPlanes(Target target)
            {
                var jointTransforms = ForwardKinematics(Joints);

                if (target is JointTarget)
                {
                    var closest = GetClosestSolution(jointTransforms[jointTransforms.Length - 1], Joints, out var configuration, out var errors, out var difference);
                    this.Configuration = difference < AngleTol ? configuration : RobotConfigurations.Undefined;
                }

                for (int i = 0; i < 6; i++)
                {
                    var plane = jointTransforms[i].ToPlane();
                    plane.Rotate(PI, plane.ZAxis);
                    Planes[i + 1] = plane;
                }

                /* 
                 {
                     Planes[7] = target.Tool.Tcp;
                     Planes[7].Transform(Planes[6].ToTransform());
                 }
                */
            }
            protected abstract double[] InverseKinematics(Transform transform, RobotConfigurations configuration, out List<string> errors);
            protected abstract Transform[] ForwardKinematics(double[] joints);

            double SquaredDifference(double a, double b)
            {
                double difference = Abs(a - b);
                if (difference > PI)
                    difference = PI * 2 - difference;
                return difference * difference;
            }
            double[] GetClosestSolution(Transform transform, double[] prevJoints, out RobotConfigurations configuration, out List<string> errors, out double difference)
            {
                var solutions = new double[8][];
                var solutionsErrors = new List<List<string>>(8);
                List<string> solutionErrors;

                //for (int i = 0; i < 8; i++)
                //{
                //    solutions[i] = JointTarget.GetAbsoluteJoints(solutions[i], prevJoints);
                //}
                #region for
                //算8个逆解组合
                //for (int i = 0; i < 8; i++)
                //{
                //    solutionErrors = new List<string>();
                //    if (UseIKFast)
                //    {
                //        #region IKfast.dll InverseKinematics
                //        solutions[i] = IKfast.InverseKinematics_GetOneSol(transform, i, out solutionErrors);
                //        #endregion
                //    }
                //    else
                //    {
                //        solutions[i] = InverseKinematics(transform, (RobotConfigurations)i, out solutionErrors);
                //    }
                //    solutionsErrors.Add(solutionErrors);
                //    solutions[i] = JointTarget.GetAbsoluteJoints(solutions[i], prevJoints);
                //}
                #endregion

                for (int i = 0; i < 8; i++)
                {
                    solutionErrors = new List<string>();
                    solutions[i] = InverseKinematics(transform, (RobotConfigurations)i, out solutionErrors);
                    solutions[i] = JointTarget.GetAbsoluteJoints(solutions[i], prevJoints);
                    solutionsErrors.Add(solutionErrors);
                }

                int closestSolutionIndex = 0;
                double closestDifference = double.MaxValue;
                for (int i = 0; i < 8; i++)
                {
                    double currentDifference = prevJoints.Zip(solutions[i], (x, y) => SquaredDifference(x, y)).Sum();

                    if (currentDifference < closestDifference)
                    {
                        closestSolutionIndex = i;
                        closestDifference = currentDifference;
                    }
                }

                difference = closestDifference;
                configuration = (RobotConfigurations)closestSolutionIndex;
                errors = solutionsErrors[closestSolutionIndex];
                return solutions[closestSolutionIndex];
            }
            protected override void IKSetJoints(Target target, double[] prevJoints)
            {
                if (target is JointTarget jointTarget)
                {
                    Joints = jointTarget.Joints;
                    this.Configuration = RobotConfigurations.None;
                }
                else if (target is CartesianTarget cartesianTarget)
                {
                    double[] joints = new double[6];
                    Plane tcp = target.Tool.Tcp;
                    tcp.Rotate(PI, Vector3d.ZAxis, Point3d.Origin);

                    Plane targetPlane = cartesianTarget.Plane;
                    //变换到Frame坐标下，如在旋转情况下坐标值不发生改动
                    if (target.Frame.IsCoupled)
                    {
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                    }
                    else
                    {
                        //targetPlane.Transform(Transform.PlaneToPlane(target.Frame.Plane, Plane.WorldXY));
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));

                    }
                    var transform = Transform.PlaneToPlane(Planes[0], Plane.WorldXY) * Transform.PlaneToPlane(tcp, targetPlane);

                    List<string> errors = new List<string>();

                    if (cartesianTarget.Motion == Motions.Joint)
                    {
                        if (prevJoints == null)
                        {
                            //Joints = solutions[2];
                            Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                        }
                        else
                        {
                            var solutions = new double[8][];
                            solutions = IKfast.InverseKinematics(transform, out errors);
                            int closestSolutionIndex = 0;
                            double closestDifference = double.MaxValue;
                            for (int i = 0; i < 8; i++)
                            {
                                solutions[i] = JointTarget.GetAbsoluteJoints(solutions[i], prevJoints);
                                double currentDifference = prevJoints.Zip(solutions[i], (x, y) => SquaredDifference(x, y)).Sum();

                                if (currentDifference < closestDifference)
                                {
                                    closestSolutionIndex = i;
                                    closestDifference = currentDifference;
                                }
                            }
                            Joints = solutions[closestSolutionIndex];
                        }
                    }
                    else//motion = linear & motion = arc
                    {
                        //Joints = solutions[2];
                        Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                    }

                    #region
                    //var solutions = new double[8][];
                    //solutions = IKfast.InverseKinematics(transform, out errors);
                    //if (prevJoints == null)
                    //{
                    //    //Joints = solutions[2];
                    //    Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                    //}
                    //else
                    //{
                    //    if (cartesianTarget.Motion == Motions.Linear)
                    //    {
                    //        //Joints = solutions[2];
                    //        Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                    //    }
                    //    else if(cartesianTarget.Motion == Motions.Joint)
                    //    {
                    //        var solutions = new double[8][];
                    //        solutions = IKfast.InverseKinematics(transform, out errors);
                    //        int closestSolutionIndex = 0;
                    //        double closestDifference = double.MaxValue;
                    //        for (int i = 0; i < 8; i++)
                    //        {
                    //            solutions[i] = JointTarget.GetAbsoluteJoints(solutions[i], prevJoints);
                    //            double currentDifference = prevJoints.Zip(solutions[i], (x, y) => SquaredDifference(x, y)).Sum();

                    //            if (currentDifference < closestDifference)
                    //            {
                    //                closestSolutionIndex = i;
                    //                closestDifference = currentDifference;
                    //            }
                    //        }
                    //        Joints = solutions[closestSolutionIndex];
                    //    }
                    //}
                    #endregion
                    Configuration = cartesianTarget.Configuration ?? RobotConfigurations.None;
                    Errors.AddRange(errors);
                }
            }
            protected override void IKSetPlanes()
            {
                var jointTransforms = ForwardKinematics(Joints);
                for (int i = 0; i < 6; i++)
                {
                    var plane = jointTransforms[i].ToPlane();
                    plane.Rotate(PI, plane.ZAxis);
                    Planes[i + 1] = plane;
                }
            }
        }
        protected abstract class RobotKinematics_Analytic : MechanismKinematics_Analytic
        {
            protected RobotKinematics_Analytic(RobotArm robot, Target target, double[] prevJoints = null, Plane? basePlane = null) : base(robot, target,prevJoints, basePlane) { }
            protected abstract double[][] InverseKinematics(Transform transform, out List<string> errors);
            protected abstract double[] InverseKinematics_GetOneSol(Transform transform, int solindex, out List<string> errors);
            protected abstract double[] InverseK(Transform transform, RobotConfigurations configuration, out List<string> errors);
            protected abstract Transform[] ForwardKinematics(double[] joints);
            double SquaredDifference(double a, double b)
            {
                double difference = Abs(a - b);
                if (difference > PI)
                    difference = PI * 2 - difference;
                return difference * difference;
            }
            protected override void IKSetJoints(Target target, double[] prevJoints)
            {
                if (target is JointTarget jointTarget)
                {
                    Joints = jointTarget.Joints;
                    this.Configuration = RobotConfigurations.None;
                }
                else if (target is CartesianTarget cartesianTarget)
                {
                    double[] joints = new double[6];
                    Plane tcp = target.Tool.Tcp;
                    tcp.Rotate(PI, Vector3d.ZAxis, Point3d.Origin);

                    Plane targetPlane = cartesianTarget.Plane;
                    //变换到Frame坐标下，如在旋转情况下坐标值不发生改动
                    if (target.Frame.IsCoupled)
                    {
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                    }
                    else
                    {
                        //targetPlane.Transform(Transform.PlaneToPlane(target.Frame.Plane, Plane.WorldXY));
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));

                    }
                    var transform = Transform.PlaneToPlane(Planes[0], Plane.WorldXY) * Transform.PlaneToPlane(tcp, targetPlane);

                    List<string> errors = new List<string>();

                    if (cartesianTarget.Motion == Motions.Joint)
                    {
                        if (prevJoints == null)
                        {
                            //Joints = solutions[2];
                            //Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                            Joints = InverseKinematics_GetOneSol(transform, 2, out errors);
                        }
                        else
                        {
                            var solutions = new double[8][];
                            //solutions = IKfast.InverseKinematics(transform, out errors);
                            solutions = InverseKinematics(transform, out errors);
                            int closestSolutionIndex = 0;
                            double closestDifference = double.MaxValue;
                            for (int i = 0; i < 8; i++)
                            {
                                solutions[i] = JointTarget.GetAbsoluteJoints(solutions[i], prevJoints);
                                double currentDifference = prevJoints.Zip(solutions[i], (x, y) => SquaredDifference(x, y)).Sum();

                                if (currentDifference < closestDifference)
                                {
                                    closestSolutionIndex = i;
                                    closestDifference = currentDifference;
                                }
                            }
                            Joints = solutions[closestSolutionIndex];
                        }
                    }
                    else//motion = linear & motion = arc
                    {
                        //Joints = solutions[2];
                        //Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                        Joints = InverseKinematics_GetOneSol(transform, 2, out errors);
                    }

                    #region
                    //var solutions = new double[8][];
                    //solutions = IKfast.InverseKinematics(transform, out errors);
                    //if (prevJoints == null)
                    //{
                    //    //Joints = solutions[2];
                    //    Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                    //}
                    //else
                    //{
                    //    if (cartesianTarget.Motion == Motions.Linear)
                    //    {
                    //        //Joints = solutions[2];
                    //        Joints = IKfast.InverseKinematics_GetOneSol(transform, 2, out errors);
                    //    }
                    //    else if(cartesianTarget.Motion == Motions.Joint)
                    //    {
                    //        var solutions = new double[8][];
                    //        solutions = IKfast.InverseKinematics(transform, out errors);
                    //        int closestSolutionIndex = 0;
                    //        double closestDifference = double.MaxValue;
                    //        for (int i = 0; i < 8; i++)
                    //        {
                    //            solutions[i] = JointTarget.GetAbsoluteJoints(solutions[i], prevJoints);
                    //            double currentDifference = prevJoints.Zip(solutions[i], (x, y) => SquaredDifference(x, y)).Sum();

                    //            if (currentDifference < closestDifference)
                    //            {
                    //                closestSolutionIndex = i;
                    //                closestDifference = currentDifference;
                    //            }
                    //        }
                    //        Joints = solutions[closestSolutionIndex];
                    //    }
                    //}
                    #endregion
                    Configuration = cartesianTarget.Configuration ?? RobotConfigurations.None;
                    Errors.AddRange(errors);
                }
            }
            protected override void IKSetPlanes()
            {
                var jointTransforms = ForwardKinematics(Joints);
                for (int i = 0; i < 6; i++)
                {
                    var plane = jointTransforms[i].ToPlane();
                    plane.Rotate(PI, plane.ZAxis);
                    Planes[i + 1] = plane;
                }
            }
            protected override void ComputeJoint8Solution(Target target)
            {
                if (target is JointTarget jointTarget)
                {
                    Joints = jointTarget.Joints;
                }
                else if (target is CartesianTarget cartesianTarget)
                {
                    Plane tcp = target.Tool.Tcp;
                    tcp.Rotate(PI, Vector3d.ZAxis, Point3d.Origin);

                    Plane targetPlane = cartesianTarget.Plane;
                    //变换到Frame坐标下，如在旋转情况下坐标值不发生改动
                    if (target.Frame.IsCoupled)
                    {
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                    }
                    else
                    {
                        //targetPlane.Transform(Transform.PlaneToPlane(target.Frame.Plane, Plane.WorldXY));
                        targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));

                    }

                    //targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                    //targetPlane.Transform(target.Frame.Plane.ToTransform());//把坐标转换到Frame坐标系？

                    var transform = Transform.PlaneToPlane(Planes[0], Plane.WorldXY) * Transform.PlaneToPlane(tcp, targetPlane);

                    List<string> errors;
                    //double[] joints;
                    //if (cartesianTarget.Configuration != null || prevJoints == null)
                    //{
                    //    Configuration = cartesianTarget.Configuration ?? RobotConfigurations.None;
                    //    joints = InverseKinematics(transform, Configuration, out errors);
                    //}
                    //else
                    //{
                    //    joints = GetClosestSolution(transform, prevJoints, out var configuration, out errors, out var difference);
                    //    Configuration = configuration;
                    //}
                    //if (prevJoints != null)
                    //    Joints = JointTarget.GetAbsoluteJoints(joints, prevJoints);
                    //else
                    //    Joints = joints;

                    Joints8 = Get8Solution(transform, out errors);

                    Errors.AddRange(errors);
                }
            }
            double[][] Get8Solution(Transform transform,out List<string> errors)
            {
                var solutions = new double[8][];
                List<string> solutionErrors;

                errors = new List<string>();

                for (int i = 0; i < 8; i++)
                {
                    solutionErrors = new List<string>();
                    solutions[i] = InverseK(transform, (RobotConfigurations)i, out solutionErrors);
                    foreach(var s in solutionErrors)
                    {
                        errors.Add(s);
                    }
                }
                return solutions;
            }
            protected override void ComputePlane()
            {
                throw new NotImplementedException();
            }
        }
        protected class SphericalWristKinematics : RobotKinematics
        {

            //   static double[] StartPosition = new double[] { 0, PI / 2, 0, 0, 0, -PI };
            internal static double[] startJoints = new double[6];

            public SphericalWristKinematics(RobotArm robot, Target target, double[] prevJoints, Plane? basePlane) : base(robot, target, prevJoints, basePlane)
            {
                startJoints = robot?.GetStartPose().Joints;
            }


            /// <summary>
            /// Inverse kinematics for a spherical wrist 6 axis robot.
            /// Code adapted from https://github.com/whitegreen/KinematikJava
            /// </summary>
            /// <param name="target">Cartesian target</param>
            /// <returns>Returns the 6 rotation values in radians.</returns>
            override protected double[] InverseKinematics(Transform transform, RobotConfigurations configuration, out List<string> errors)
            {
                errors = new List<string>();

                bool shoulder = configuration.HasFlag(RobotConfigurations.Shoulder);
                bool elbow = configuration.HasFlag(RobotConfigurations.Elbow);
                if (shoulder) elbow = !elbow;
                bool wrist = !configuration.HasFlag(RobotConfigurations.Wrist);

                bool isUnreachable = false;

                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

                Plane flange = Plane.WorldXY;
                flange.Transform(transform);

                double[] joints = new double[6];

                double l2 = Sqrt(a[2] * a[2] + d[3] * d[3]);
                double ad2 = Atan2(a[2], d[3]);
                Point3d center = flange.Origin - flange.Normal * d[5];
                joints[0] = Atan2(center.Y, center.X);
                double ll = Sqrt(center.X * center.X + center.Y * center.Y);
                Point3d p1 = new Point3d(a[0] * center.X / ll, a[0] * center.Y / ll, d[0]);

                if (shoulder)
                {
                    joints[0] += PI;
                    var rotate = Transform.Rotation(PI, new Point3d(0, 0, 0));
                    center.Transform(rotate);
                }

                double l3 = (center - p1).Length;
                double l1 = a[1];
                double beta = Acos((l1 * l1 + l3 * l3 - l2 * l2) / (2 * l1 * l3));
                if (double.IsNaN(beta))
                {
                    beta = 0;
                    isUnreachable = true;
                }
                if (elbow)
                    beta *= -1;

                double ttl = new Vector3d(center.X - p1.X, center.Y - p1.Y, 0).Length;
                // if (p1.X * (center.X - p1.X) < 0)
                if (shoulder)
                    ttl = -ttl;
                double al = Atan2(center.Z - p1.Z, ttl);

                joints[1] = beta + al;

                double gama = Acos((l1 * l1 + l2 * l2 - l3 * l3) / (2 * l1 * l2));
                if (double.IsNaN(gama))
                {
                    gama = PI;
                    isUnreachable = true;
                }
                if (elbow)
                    gama *= -1;

                joints[2] = gama - ad2 - PI / 2;

                double[] c = new double[3];
                double[] s = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    c[i] = Cos(joints[i]);
                    s[i] = Sin(joints[i]);
                }

                var arr = new Transform();
                arr[0, 0] = c[0] * (c[1] * c[2] - s[1] * s[2]); arr[0, 1] = s[0]; arr[0, 2] = c[0] * (c[1] * s[2] + s[1] * c[2]); arr[0, 3] = c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0];
                arr[1, 0] = s[0] * (c[1] * c[2] - s[1] * s[2]); arr[1, 1] = -c[0]; arr[1, 2] = s[0] * (c[1] * s[2] + s[1] * c[2]); arr[1, 3] = s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0];
                arr[2, 0] = s[1] * c[2] + c[1] * s[2]; arr[2, 1] = 0; arr[2, 2] = s[1] * s[2] - c[1] * c[2]; arr[2, 3] = a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0];
                arr[3, 0] = 0; arr[3, 1] = 0; arr[3, 2] = 0; arr[3, 3] = 1;

                arr.TryGetInverse(out var in123);

                var mr = Transform.Multiply(in123, transform);
                joints[3] = Atan2(mr[1, 2], mr[0, 2]);
                joints[4] = Acos(mr[2, 2]);
                joints[5] = Atan2(mr[2, 1], -mr[2, 0]);

                if (wrist)
                {
                    joints[3] += PI;
                    joints[4] *= -1;
                    joints[5] -= PI;
                }

                #region 判断姿态进行修正
                for (int i = 0; i < 6; i++)
                {
                    if (joints[i] - startJoints[i] > PI) joints[i] -= 2 * PI;
                    if (joints[i] - startJoints[i] < -PI) joints[i] += 2 * PI;
                }

                if (isUnreachable)
                    errors.Add($"Target out of reach");

                if (Abs(1 - mr[2, 2]) < 0.0001)
                    errors.Add($"Near wrist singularity");

                //if (new Vector3d(center.X, center.Y, 0).Length < a[0] + SingularityTol)
                //    errors.Add($"Near overhead singularity");


                for (int i = 0; i < 6; i++)
                {
                    if (double.IsNaN(joints[i])) joints[i] = 0;
                }
                #endregion
                return joints;
            }

            override protected Transform[] ForwardKinematics(double[] joints)
            {
                var transforms = new Transform[6];
                double[] c = joints.Select(x => Cos(x)).ToArray();
                double[] s = joints.Select(x => Sin(x)).ToArray();
                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

                transforms[0] = new double[4, 4] { { c[0], 0, c[0], c[0] + a[0] * c[0] }, { s[0], -c[0], s[0], s[0] + a[0] * s[0] }, { 0, 0, 0, d[0] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[1] = new double[4, 4] { { c[0] * (c[1] - s[1]), s[0], c[0] * (c[1] + s[1]), c[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] - s[1]), -c[0], s[0] * (c[1] + s[1]), s[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] + c[1], 0, s[1] - c[1], (s[1] + c[1]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[2] = new double[4, 4] { { c[0] * (c[1] * c[2] - s[1] * s[2]), s[0], c[0] * (c[1] * s[2] + s[1] * c[2]), c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] * c[2] - s[1] * s[2]), -c[0], s[0] * (c[1] * s[2] + s[1] * c[2]), s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] * c[2] + c[1] * s[2], 0, s[1] * s[2] - c[1] * c[2], a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[3] = new double[4, 4] { { c[3] - s[3], -c[3] - s[3], c[3], c[3] }, { s[3] + c[3], -s[3] + c[3], s[3], s[3] }, { 0, 0, 0, 0 + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[4] = new double[4, 4] { { c[3] * c[4] - s[3], -c[3] * c[4] - s[3], c[3] * s[4], c[3] * s[4] }, { s[3] * c[4] + c[3], -s[3] * c[4] + c[3], s[3] * s[4], s[3] * s[4] }, { -s[4], s[4], c[4], c[4] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[5] = new double[4, 4] { { c[3] * c[4] * c[5] - s[3] * s[5], -c[3] * c[4] * s[5] - s[3] * c[5], c[3] * s[4], c[3] * s[4] * d[5] }, { s[3] * c[4] * c[5] + c[3] * s[5], -s[3] * c[4] * s[5] + c[3] * c[5], s[3] * s[4], s[3] * s[4] * d[5] }, { -s[4] * c[5], s[4] * s[5], c[4], c[4] * d[5] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
                
                transforms[3] = Transform.Multiply(transforms[2], transforms[3]);
                transforms[4] = Transform.Multiply(transforms[2], transforms[4]);
                transforms[5] = Transform.Multiply(transforms[2], transforms[5]);
                
                return transforms;
            }
        }
        //UR
        protected class OffsetWristKinematics : RobotKinematics
        {
            public OffsetWristKinematics(RobotArm robot, Target target, double[] prevJoints, Plane? basePlane) : base(robot, target, prevJoints, basePlane) { }

            /// <summary>
            /// Inverse kinematics for a offset wrist 6 axis robot.
            /// Code adapted from https://github.com/ros-industrial/universal_robot/blob/indigo-devel/ur_kinematics/src/ur_kin.cpp
            /// </summary>
            /// <param name="target">Cartesian target</param>
            /// <returns>Returns the 6 rotation values in radians.</returns>
            override protected double[] InverseKinematics(Transform transform, RobotConfigurations configuration, out List<string> errors)
            {
                errors = new List<string>();

                bool shoulder = configuration.HasFlag(RobotConfigurations.Shoulder);
                bool elbow = configuration.HasFlag(RobotConfigurations.Elbow);
                if (shoulder) elbow = !elbow;
                bool wrist = !configuration.HasFlag(RobotConfigurations.Wrist);
                if (shoulder) wrist = !wrist;

                double[] joints = new double[6];
                bool isUnreachable = false;

                transform *= Transform.Rotation(PI / 2, Point3d.Origin);

                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

                // shoulder
                {
                    double A = d[5] * transform[1, 2] - transform[1, 3];
                    double B = d[5] * transform[0, 2] - transform[0, 3];
                    double R = A * A + B * B;

                    double arccos = Acos(d[3] / Sqrt(R));
                    if (double.IsNaN(arccos))
                    {
                        errors.Add($"Overhead singularity.");
                        arccos = 0;
                    }

                    double arctan = Atan2(-B, A);

                    if (!shoulder)
                        joints[0] = arccos + arctan;
                    else
                        joints[0] = -arccos + arctan;
                }

                // wrist 2
                {
                    double numer = (transform[0, 3] * Sin(joints[0]) - transform[1, 3] * Cos(joints[0]) - d[3]);
                    double div = numer / d[5];

                    double arccos = Acos(div);
                    if (double.IsNaN(arccos))
                    {
                        errors.Add($"Overhead singularity 2.");
                        arccos = PI;
                        isUnreachable = true;
                    }

                    if (!wrist)
                        joints[4] = arccos;
                    else
                        joints[4] = 2.0 * PI - arccos;
                }

                // rest
                {
                    double c1 = Cos(joints[0]);
                    double s1 = Sin(joints[0]);
                    double c5 = Cos(joints[4]);
                    double s5 = Sin(joints[4]);

                    joints[5] = Atan2(Sign(s5) * -(transform[0, 1] * s1 - transform[1, 1] * c1), Sign(s5) * (transform[0, 0] * s1 - transform[1, 0] * c1));

                    double c6 = Cos(joints[5]), s6 = Sin(joints[5]);
                    double x04x = -s5 * (transform[0, 2] * c1 + transform[1, 2] * s1) - c5 * (s6 * (transform[0, 1] * c1 + transform[1, 1] * s1) - c6 * (transform[0, 0] * c1 + transform[1, 0] * s1));
                    double x04y = c5 * (transform[2, 0] * c6 - transform[2, 1] * s6) - transform[2, 2] * s5;
                    double p13x = d[4] * (s6 * (transform[0, 0] * c1 + transform[1, 0] * s1) + c6 * (transform[0, 1] * c1 + transform[1, 1] * s1)) - d[5] * (transform[0, 2] * c1 + transform[1, 2] * s1) + transform[0, 3] * c1 + transform[1, 3] * s1;
                    double p13y = transform[2, 3] - d[0] - d[5] * transform[2, 2] + d[4] * (transform[2, 1] * c6 + transform[2, 0] * s6);
                    double c3 = (p13x * p13x + p13y * p13y - a[1] * a[1] - a[2] * a[2]) / (2.0 * a[1] * a[2]);

                    double arccos = Acos(c3);
                    if (double.IsNaN(arccos))
                    {
                        arccos = 0;
                        isUnreachable = true;
                    }

                    if (!elbow)
                        joints[2] = arccos;
                    else
                        joints[2] = 2.0 * PI - arccos;

                    double denom = a[1] * a[1] + a[2] * a[2] + 2 * a[1] * a[2] * c3;
                    double s3 = Sin(arccos);
                    double A = (a[1] + a[2] * c3);
                    double B = a[2] * s3;

                    if (!elbow)
                        joints[1] = Atan2((A * p13y - B * p13x) / denom, (A * p13x + B * p13y) / denom);
                    else
                        joints[1] = Atan2((A * p13y + B * p13x) / denom, (A * p13x - B * p13y) / denom);

                    double c23_0 = Cos(joints[1] + joints[2]);
                    double s23_0 = Sin(joints[1] + joints[2]);

                    joints[3] = Atan2(c23_0 * x04y - s23_0 * x04x, x04x * c23_0 + x04y * s23_0);
                }

                if (isUnreachable)
                    errors.Add($"Target out of reach.");

                //   joints[5] += PI / 2;

                for (int i = 0; i < 6; i++)
                {
                    if (joints[i] > PI) joints[i] -= 2.0 * PI;
                    if (joints[i] < -PI) joints[i] += 2.0 * PI;
                }

                return joints;
            }

            override protected Transform[] ForwardKinematics(double[] joints)
            {
                var transforms = new Transform[6];
                double[] c = joints.Select(x => Cos(x)).ToArray();
                double[] s = joints.Select(x => Sin(x)).ToArray();
                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();
                double s23 = Sin(joints[1] + joints[2]);
                double c23 = Cos(joints[1] + joints[2]);
                double s234 = Sin(joints[1] + joints[2] + joints[3]);
                double c234 = Cos(joints[1] + joints[2] + joints[3]);

                transforms[0] = new double[4, 4] { { c[0], 0, s[0], 0 }, { s[0], 0, -c[0], 0 }, { 0, 1, 0, d[0] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[1] = new double[4, 4] { { c[0] * c[1], -c[0] * s[1], s[0], a[1] * c[0] * c[1] }, { c[1] * s[0], -s[0] * s[1], -c[0], a[1] * c[1] * s[0] }, { s[1], c[1], 0, d[0] + a[1] * s[1] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[2] = new double[4, 4] { { c23 * c[0], -s23 * c[0], s[0], c[0] * (a[2] * c23 + a[1] * c[1]) }, { c23 * s[0], -s23 * s[0], -c[0], s[0] * (a[2] * c23 + a[1] * c[1]) }, { s23, c23, 0, d[0] + a[2] * s23 + a[1] * s[1] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[3] = new double[4, 4] { { c234 * c[0], s[0], s234 * c[0], c[0] * (a[2] * c23 + a[1] * c[1]) + d[3] * s[0] }, { c234 * s[0], -c[0], s234 * s[0], s[0] * (a[2] * c23 + a[1] * c[1]) - d[3] * c[0] }, { s234, 0, -c234, d[0] + a[2] * s23 + a[1] * s[1] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[4] = new double[4, 4] { { s[0] * s[4] + c234 * c[0] * c[4], -s234 * c[0], c[4] * s[0] - c234 * c[0] * s[4], c[0] * (a[2] * c23 + a[1] * c[1]) + d[3] * s[0] + d[4] * s234 * c[0] }, { c234 * c[4] * s[0] - c[0] * s[4], -s234 * s[0], -c[0] * c[4] - c234 * s[0] * s[4], s[0] * (a[2] * c23 + a[1] * c[1]) - d[3] * c[0] + d[4] * s234 * s[0] }, { s234 * c[4], c234, -s234 * s[4], d[0] + a[2] * s23 + a[1] * s[1] - d[4] * c234 }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[5] = new double[4, 4] { { c[5] * (s[0] * s[4] + c234 * c[0] * c[4]) - s234 * c[0] * s[5], -s[5] * (s[0] * s[4] + c234 * c[0] * c[4]) - s234 * c[0] * c[5], c[4] * s[0] - c234 * c[0] * s[4], d[5] * (c[4] * s[0] - c234 * c[0] * s[4]) + c[0] * (a[2] * c23 + a[1] * c[1]) + d[3] * s[0] + d[4] * s234 * c[0] }, { -c[5] * (c[0] * s[4] - c234 * c[4] * s[0]) - s234 * s[0] * s[5], s[5] * (c[0] * s[4] - c234 * c[4] * s[0]) - s234 * c[5] * s[0], -c[0] * c[4] - c234 * s[0] * s[4], s[0] * (a[2] * c23 + a[1] * c[1]) - d[3] * c[0] - d[5] * (c[0] * c[4] + c234 * s[0] * s[4]) + d[4] * s234 * s[0] }, { c234 * s[5] + s234 * c[4] * c[5], c234 * c[5] - s234 * c[4] * s[5], -s234 * s[4], d[0] + a[2] * s23 + a[1] * s[1] - d[4] * c234 - d[5] * s234 * s[4] }, { 0, 0, 0, 1 } }.ToTransform();

                transforms[5] *= Transform.Rotation(-PI / 2, Point3d.Origin);

                return transforms;
            }
        }
        //Aubo
        protected class AuboWristKinematics : RobotKinematics
        {
            public AuboWristKinematics(RobotArm robot, Target target, double[] prevJoints, Plane? basePlane) : base(robot, target, prevJoints, basePlane) { }

            override protected double[] InverseKinematics(Transform transform, RobotConfigurations configuration, out List<string> errors)
            {
                errors = new List<string>();

                bool shoulder = configuration.HasFlag(RobotConfigurations.Shoulder);
                bool elbow = configuration.HasFlag(RobotConfigurations.Elbow);
                if (shoulder) elbow = !elbow;
                bool wrist = !configuration.HasFlag(RobotConfigurations.Wrist);
                if (shoulder) wrist = !wrist;

                double[] joints = new double[6];
                bool isUnreachable = false;
                double ZERO_THRESH = 1e-4;

                transform *= Transform.Rotation(PI / 2, Point3d.Origin);

                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

                // shoulder
                {
                    double A = d[5] * transform[1, 2] - transform[1, 3]; // d6 * ay - py
                    double B = d[5] * transform[0, 2] - transform[0, 3]; // d6 * ax - px
                    double R1 = A * A + B * B - d[3] * d[3];

                    if(R1 < 0.0){
                        errors.Add($"Overhead singularity.");
                        R1 = 0;
                    }
                    double R12 = Sqrt(R1);

                    if (!shoulder)
                        joints[0] = Atan2(A, B) -  Atan2(d[3], R12);
                    else
                        joints[0] = Atan2(A, B) - Atan2(d[3], -R12);
                    if (joints[0] > PI) joints[0] -= 2.0 * PI;
                    if (joints[0] < -PI) joints[0] += 2.0 * PI;
                }

                // wrist 2 joint q5
                {
                    //double numer = (transform[0, 3] * Sin(joints[0]) - transform[1, 3] * Cos(joints[0]) - d[3]);
                    //double div = numer / d[5];

                    //double arccos = Acos(div);

                    //if (double.IsNaN(arccos))
                    //{
                    //    errors.Add($"Overhead singularity 2.");
                    //    arccos = PI;
                    //    isUnreachable = true;
                    //}

                    double c1 = Cos(joints[0]);
                    double s1 = Sin(joints[0]);
                    double B5 = -transform[1, 2] * c1 + transform[0, 2] * s1;
                    double M5 = -transform[1, 0] * c1 + transform[0, 0] * s1;
                    double N5 = -transform[1, 1] * c1 + transform[0, 1] * s1;
                    double R5 = Sqrt(M5 * M5 + N5 * N5);

                    if (!wrist)
                        joints[4] = Atan2(R5, B5);
                    else
                        joints[4] = Atan2(-R5, B5);
                }

                // rest
                {
                    // wrist e joint q6
                    double c1 = Cos(joints[0]), s1 = Sin(joints[0]);
                    double c5 = Cos(joints[4]), s5 = Sin(joints[4]);

                    double A6 = -transform[1, 1] * c1 + transform[0, 1] * s1;
                    double B6 = transform[1, 0] * c1 - transform[0, 0] * s1;
                    if (Abs(s5) < ZERO_THRESH)
                    {
                        return joints;
                    }
                    joints[5] = Atan2(A6 * s5, B6 * s5);

                    //   joints q3 q2 q4
                    double c6 = Cos(joints[5]), s6 = Sin(joints[5]);
                    double pp1 = c1 * (transform[0, 2] * d[5] - transform[0, 3] + d[4] * transform[0, 1] * c6 + d[4] * transform[0, 0] * s6) + s1 * (transform[1, 2] * d[5] - transform[1, 3] + d[4] * transform[1, 1] * c6 + d[4] * transform[1, 0] * s6);
                    double pp2 = -d[0] - transform[2, 2] * d[5] + transform[2, 3] - d[4] * transform[2, 1] * c6 - d[4] * transform[2, 0] * s6;
                    double B3 = (pp1 * pp1 + pp2 * pp2 - a[1] * a[1] - a[2] * a[2]) / (2 * a[1] * a[2]);
                    if ((1 - B3 * B3) < ZERO_THRESH)
                    {
                        isUnreachable = true;
                        joints[2] = 0;
                    }
                    else
                    {
                        double Sin3 = Sqrt(1 - B3 * B3);
                        if (!elbow)
                            joints[2] = Atan2(Sin3, B3);
                        else
                            joints[2] = Atan2(-Sin3, B3);
                    }

                    double c3 = Cos(joints[2]);
                    double s3 = Sin(joints[2]);
                    double A2 = pp1 * (a[1] + a[2] * c3) + pp2 * (a[2] * s3);
                    double B2 = pp2 * (a[1] + a[2] * c3) - pp1 * (a[2] * s3);

                    joints[1] = Atan2(A2, B2);

                    double c2 = Cos(joints[1]);
                    double s2 = Sin(joints[1]);

                    double A4 = -c1 * (transform[0, 1] * c6 + transform[0, 0] * s6) - s1 * (transform[1, 1] * c6 + transform[1, 0] * s6);
                    double B4 = transform[2, 1] * c6 + transform[2, 0] * s6;
                    double A41 = pp1 - a[1] * s2;
                    double B41 = pp2 - a[1] * c2;
                    joints[3] = Atan2(A4, B4) - Atan2(A41, B41);
                }

                if (isUnreachable)
                    errors.Add($"Target out of reach.");

                //   joints[5] += PI / 2;

                for (int i = 0; i < 6; i++)
                {
                    if (joints[i] > PI) joints[i] -= 2.0 * PI;
                    if (joints[i] < -PI) joints[i] += 2.0 * PI;
                }

                return joints;
            }

            override protected Transform[] ForwardKinematics(double[] joints)
            {
                var transforms = new Transform[6];
                double[] c = joints.Select(x => Cos(x)).ToArray();
                double[] s = joints.Select(x => Sin(x)).ToArray();
                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

                double c1 = c[0], c2 = c[1], c3 = c[2], c4 = c[3], c5 = c[4], c6 = c[5];
                double s1 = s[0], s2 = s[1], s3 = s[2], s4 = s[3], s5 = s[4], s6 = s[5];
                double a2 = a[1], a3 = a[2];
                double d1 = d[0], d4 = d[3], d5 = d[4], d6 = d[5];
                
                double c23 = Cos(joints[1] - joints[2]);
                double c234 = Cos(joints[1] - joints[2] + joints[3]);

                double s23 = Sin(joints[1] - joints[2]);
                double s234 = Sin(joints[1] - joints[2] + joints[3]);
                

                transforms[0] = new double[4, 4] { { c1, 0, s1, 0 }, 
                                                   { s1, 0, -c1, 0 }, 
                                                   { 0, 1, 0, d1 }, 
                                                   { 0, 0, 0, 1 } }.ToTransform();
                
                transforms[1] = new double[4, 4] { { -c1 * s2, c1 * c2, -s1, -a2 * c1 * s2 }, 
                                                   { -s1 * s2, s1 * c2, c1, -a2 * s1 * s2 }, 
                                                   { c2, s2, 0, a2 * c2 + d1 }, 
                                                   { 0, 0, 0, 1 } }.ToTransform();
                
                transforms[2] = new double[4, 4] { { -c1 * s23, -c1 * c23, s1, -a3 * c1 * s23 - a2 * c1 * s2 }, 
                                                   { -s1 * s23, -s1 * c23, -c1, -a3 * s1 * s23 - a2 * s1 * s2 },
                                                   { c23, -s23, 0, a3 * c23 + a2 * c2 + d1 }, 
                                                   { 0, 0, 0, 1 } }.ToTransform();
                
                transforms[3] = new double[4, 4] { { c1 * c234, -s1, -c1 * s234, s1 * d4 - a3 * c1 * s23 - a2 * c1 * s2 }, 
                                                    { s1 * c234, c1, -s1 * s234, -c1 * d4 - a3 * s1 * s23 - a2 * s1 * s2 }, 
                                                    { s234, 0, c234, a3 * c23 + a2 * c2 + d1 }, 
                                                    { 0, 0, 0, 1 } }.ToTransform();

                transforms[4] = new double[4, 4] { { c1 * c234 * c5 - s1 * s5, -c1 * s234, c1 * c234 * s5 + s1 * c5, -c1 * s234 * d5 + s1 * d4 - a3 * c1 * s23 - a2 * c1 * s2 }, 
                                                   { s1 * c234 * c5 + c1 * s5, -s1 * s234, s1 * c234 * s5 - c1 * c5, -s1 * s234 * d5 - c1 * d4 - a3 * s1 * s23 - a2 * s1 * s2 }, 
                                                   { s234 * c5, c234, s234 * s5, c234 * d5 + a3 * c23 + a2 * c2 + d1 }, 
                                                   { 0, 0, 0, 1 } }.ToTransform();

                transforms[5] = new double[4, 4] { { c1 * c234 * c5 * c6 - s1 * s5 * c6 - c1 * s234 * s6, s1 * s5 * s6 - c1 * c234 * c5 * s6 - c1 * s234 * c6, c1 * c234 * s5 + s1 * c5, d6 * (c1 * c234 * s5 + s1 * c5) - c1 * s234 * d5 + s1 * d4 - a3 * c1 * s23 - a2 * c1 * s2 }, 
                                                   { s1 * c234 * c5 * c6 + c1 * s5 * c6 - s1 * s234 * s6, -s6 * (s1 * c234 * c5 + c1 * s5) - s1 * s234 * c6, s1 * c234 * s5 - c1 * c5, d6 * (s1 * c234 * s5 - c1 * c5) - s1 * s234 * d5 - c1 * d4 - a3 * s1 * s23 - a2 * s1 * s2 }, 
                                                   { s234 * c5 * c6 + c234 * s6, -s234 * c5 * s6 + c234 * c6, s234 * s5, s234 * s5 * d6 + c234 * d5 + a3 * c23 + a2 * c2 + d1 }, 
                                                   { 0, 0, 0, 1 } }.ToTransform();
                transforms[5] *= Transform.Rotation(-PI / 2, Point3d.Origin);

                return transforms;
            }
        }
        protected class IK : RobotKinematics_Analytic
        {
            public IK(RobotArm robot ,Target target,double[] prevJoints, Plane? basePlane) : base(robot,target, prevJoints, basePlane) { }

            override protected double[] InverseK(Transform transform, RobotConfigurations configuration, out List<string> errors)
            {
                errors = new List<string>();

                bool shoulder = configuration.HasFlag(RobotConfigurations.Shoulder);
                bool elbow = configuration.HasFlag(RobotConfigurations.Elbow);
                if (shoulder) elbow = !elbow;
                bool wrist = !configuration.HasFlag(RobotConfigurations.Wrist);

                bool isUnreachable = false;

                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

                Plane flange = Plane.WorldXY;
                flange.Transform(transform);

                double[] joints = new double[6];

                double l2 = Sqrt(a[2] * a[2] + d[3] * d[3]);
                double ad2 = Atan2(a[2], d[3]);
                Point3d center = flange.Origin - flange.Normal * d[5];
                joints[0] = Atan2(center.Y, center.X);
                double ll = Sqrt(center.X * center.X + center.Y * center.Y);
                Point3d p1 = new Point3d(a[0] * center.X / ll, a[0] * center.Y / ll, d[0]);

                if (shoulder)
                {
                    joints[0] += PI;
                    var rotate = Transform.Rotation(PI, new Point3d(0, 0, 0));
                    center.Transform(rotate);
                }

                double l3 = (center - p1).Length;
                double l1 = a[1];
                double beta = Acos((l1 * l1 + l3 * l3 - l2 * l2) / (2 * l1 * l3));
                if (double.IsNaN(beta))
                {
                    beta = 0;
                    isUnreachable = true;
                }
                if (elbow)
                    beta *= -1;

                double ttl = new Vector3d(center.X - p1.X, center.Y - p1.Y, 0).Length;
                // if (p1.X * (center.X - p1.X) < 0)
                if (shoulder)
                    ttl = -ttl;
                double al = Atan2(center.Z - p1.Z, ttl);

                joints[1] = beta + al;

                double gama = Acos((l1 * l1 + l2 * l2 - l3 * l3) / (2 * l1 * l2));
                if (double.IsNaN(gama))
                {
                    gama = PI;
                    isUnreachable = true;
                }
                if (elbow)
                    gama *= -1;

                joints[2] = gama - ad2 - PI / 2;

                double[] c = new double[3];
                double[] s = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    c[i] = Cos(joints[i]);
                    s[i] = Sin(joints[i]);
                }

                var arr = new Transform();
                arr[0, 0] = c[0] * (c[1] * c[2] - s[1] * s[2]); arr[0, 1] = s[0]; arr[0, 2] = c[0] * (c[1] * s[2] + s[1] * c[2]); arr[0, 3] = c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0];
                arr[1, 0] = s[0] * (c[1] * c[2] - s[1] * s[2]); arr[1, 1] = -c[0]; arr[1, 2] = s[0] * (c[1] * s[2] + s[1] * c[2]); arr[1, 3] = s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0];
                arr[2, 0] = s[1] * c[2] + c[1] * s[2]; arr[2, 1] = 0; arr[2, 2] = s[1] * s[2] - c[1] * c[2]; arr[2, 3] = a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0];
                arr[3, 0] = 0; arr[3, 1] = 0; arr[3, 2] = 0; arr[3, 3] = 1;

                arr.TryGetInverse(out var in123);

                var mr = Transform.Multiply(in123, transform);
                joints[3] = Atan2(mr[1, 2], mr[0, 2]);
                joints[4] = Acos(mr[2, 2]);
                joints[5] = Atan2(mr[2, 1], -mr[2, 0]);

                if (wrist)
                {
                    joints[3] += PI;
                    joints[4] *= -1;
                    joints[5] -= PI;
                }

                #region 判断姿态进行修正
                for (int i = 0; i < 6; i++)
                {
                    if (joints[i] > PI) joints[i] -= 2 * PI;
                    if (joints[i] < -PI) joints[i] += 2 * PI;
                }

                if (isUnreachable)
                    errors.Add($"Target out of reach");

                if (Abs(1 - mr[2, 2]) < 0.0001)
                    errors.Add($"Near wrist singularity");

                //if (new Vector3d(center.X, center.Y, 0).Length < a[0] + SingularityTol)
                //    errors.Add($"Near overhead singularity");


                for (int i = 0; i < 6; i++)
                {
                    if (double.IsNaN(joints[i])) joints[i] = 0;
                }
                #endregion
                return joints;
            }

            override protected Transform[] ForwardKinematics(double[] joints)
            {
                var transforms = new Transform[6];
                double[] c = joints.Select(x => Cos(x)).ToArray();
                double[] s = joints.Select(x => Sin(x)).ToArray();
                double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
                double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

                transforms[0] = new double[4, 4] { { c[0], 0, c[0], c[0] + a[0] * c[0] }, { s[0], -c[0], s[0], s[0] + a[0] * s[0] }, { 0, 0, 0, d[0] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[1] = new double[4, 4] { { c[0] * (c[1] - s[1]), s[0], c[0] * (c[1] + s[1]), c[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] - s[1]), -c[0], s[0] * (c[1] + s[1]), s[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] + c[1], 0, s[1] - c[1], (s[1] + c[1]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[2] = new double[4, 4] { { c[0] * (c[1] * c[2] - s[1] * s[2]), s[0], c[0] * (c[1] * s[2] + s[1] * c[2]), c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] * c[2] - s[1] * s[2]), -c[0], s[0] * (c[1] * s[2] + s[1] * c[2]), s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] * c[2] + c[1] * s[2], 0, s[1] * s[2] - c[1] * c[2], a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[3] = new double[4, 4] { { c[3] - s[3], -c[3] - s[3], c[3], c[3] }, { s[3] + c[3], -s[3] + c[3], s[3], s[3] }, { 0, 0, 0, 0 + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[4] = new double[4, 4] { { c[3] * c[4] - s[3], -c[3] * c[4] - s[3], c[3] * s[4], c[3] * s[4] }, { s[3] * c[4] + c[3], -s[3] * c[4] + c[3], s[3] * s[4], s[3] * s[4] }, { -s[4], s[4], c[4], c[4] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
                transforms[5] = new double[4, 4] { { c[3] * c[4] * c[5] - s[3] * s[5], -c[3] * c[4] * s[5] - s[3] * c[5], c[3] * s[4], c[3] * s[4] * d[5] }, { s[3] * c[4] * c[5] + c[3] * s[5], -s[3] * c[4] * s[5] + c[3] * c[5], s[3] * s[4], s[3] * s[4] * d[5] }, { -s[4] * c[5], s[4] * s[5], c[4], c[4] * d[5] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();

                transforms[3] = Transform.Multiply(transforms[2], transforms[3]);
                transforms[4] = Transform.Multiply(transforms[2], transforms[4]);
                transforms[5] = Transform.Multiply(transforms[2], transforms[5]);

                return transforms;
            }

            protected override double[][] InverseKinematics(Transform transform, out List<string> errors)
            {
                throw new NotImplementedException();
            }

            protected override double[] InverseKinematics_GetOneSol(Transform transform, int solindex, out List<string> errors)
            {
                throw new NotImplementedException();
            }
        }
        #region IKFAST
        //protected class IKFast_ABB : RobotKinematics_Analytic
        //{
        //    public IKFast_ABB(RobotArm robot, Target target, double[] prevJoints, Plane? basePlane) : base(robot, target, prevJoints, basePlane) { }

        //    [DllImport("IKFast.dll", CallingConvention = CallingConvention.Cdecl)]
        //    public static extern bool ComputeIkCSharp(double[] eetrans, double[] eerot, double pfree, double[] solutions);//eetrans = 机械臂末端位置，eerot = 机械臂末端旋转矩阵，pfree = 第7自由度，solutions = 回传值

        //    [DllImport("IKFast.dll", CallingConvention = CallingConvention.Cdecl)]
        //    public static extern bool ComputeIkCSharp_GetOneSol(double[] eetrans, double[] eerot, double pfree, double[] solutions, int solindex);
        //    override protected Transform[] ForwardKinematics(double[] joints)
        //    {
        //        var transforms = new Transform[6];
        //        double[] c = joints.Select(x => Cos(x)).ToArray();
        //        double[] s = joints.Select(x => Sin(x)).ToArray();
        //        double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
        //        double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

        //        transforms[0] = new double[4, 4] { { c[0], 0, c[0], c[0] + a[0] * c[0] }, { s[0], -c[0], s[0], s[0] + a[0] * s[0] }, { 0, 0, 0, d[0] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[1] = new double[4, 4] { { c[0] * (c[1] - s[1]), s[0], c[0] * (c[1] + s[1]), c[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] - s[1]), -c[0], s[0] * (c[1] + s[1]), s[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] + c[1], 0, s[1] - c[1], (s[1] + c[1]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[2] = new double[4, 4] { { c[0] * (c[1] * c[2] - s[1] * s[2]), s[0], c[0] * (c[1] * s[2] + s[1] * c[2]), c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] * c[2] - s[1] * s[2]), -c[0], s[0] * (c[1] * s[2] + s[1] * c[2]), s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] * c[2] + c[1] * s[2], 0, s[1] * s[2] - c[1] * c[2], a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[3] = new double[4, 4] { { c[3] - s[3], -c[3] - s[3], c[3], c[3] }, { s[3] + c[3], -s[3] + c[3], s[3], s[3] }, { 0, 0, 0, 0 + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[4] = new double[4, 4] { { c[3] * c[4] - s[3], -c[3] * c[4] - s[3], c[3] * s[4], c[3] * s[4] }, { s[3] * c[4] + c[3], -s[3] * c[4] + c[3], s[3] * s[4], s[3] * s[4] }, { -s[4], s[4], c[4], c[4] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[5] = new double[4, 4] { { c[3] * c[4] * c[5] - s[3] * s[5], -c[3] * c[4] * s[5] - s[3] * c[5], c[3] * s[4], c[3] * s[4] * d[5] }, { s[3] * c[4] * c[5] + c[3] * s[5], -s[3] * c[4] * s[5] + c[3] * c[5], s[3] * s[4], s[3] * s[4] * d[5] }, { -s[4] * c[5], s[4] * s[5], c[4], c[4] * d[5] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();

        //        transforms[3] = Transform.Multiply(transforms[2], transforms[3]);
        //        transforms[4] = Transform.Multiply(transforms[2], transforms[4]);
        //        transforms[5] = Transform.Multiply(transforms[2], transforms[5]);

        //        return transforms;
        //    }

        //    protected override double[][] InverseKinematics(Transform transform, out List<string> errors)
        //    {
        //        errors = new List<string>();
        //        double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
        //        double[] rota = new double[9]//ZYX
        //        {
        //        transform[0, 0], transform[0, 1], transform[0, 2],
        //        transform[1, 0], transform[1, 1], transform[1, 2],
        //        transform[2, 0], transform[2, 1], transform[2, 2]
        //        };
        //        double[] solutions = new double[48];

        //        //Stopwatch sw = new Stopwatch();
        //        //sw.Start();
        //        bool a = ComputeIkCSharp(xyz, rota, 0, solutions);
        //        //sw.Stop();
        //        ////需要打开VS输出窗口查看
        //        //Debug.WriteLine("时间:" + sw.Elapsed);
        //        //sw.Reset();
        //        double[][] eightsolutions = new double[8][];
        //        if (!a)
        //        {
        //            errors.Add("Failed to get 8 ik solutions");
        //        }
        //        else
        //        {
        //            for (int i = 0; i < 8; i++)
        //            {
        //                double[] onesolutions = new double[6];
        //                for (int j = 0; j < 6; j++)
        //                {
        //                    double onevalue = solutions[i * 6 + j];
        //                    onesolutions[j] = onevalue;
        //                }
        //                eightsolutions[i] = onesolutions;
        //            }
        //        }
        //        return eightsolutions;
        //    }

        //    protected override double[] InverseKinematics_GetOneSol(Transform transform, int solindex, out List<string> errors)
        //    {
        //        errors = new List<string>();
        //        double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
        //        double[] rota = new double[9]//ZYX
        //        {
        //        transform[0, 0], transform[0, 1], transform[0, 2],
        //        transform[1, 0], transform[1, 1], transform[1, 2],
        //        transform[2, 0], transform[2, 1], transform[2, 2]
        //        };
        //        double[] solutions = new double[6];
        //        bool a = ComputeIkCSharp_GetOneSol(xyz, rota, 0, solutions, solindex);
        //        if (!a)
        //        {
        //            errors.Add($"Failed to get No.{solindex} ik solution");
        //        }
        //        return solutions;
        //    }
        //}
        //protected class IKFast_KUKA : RobotKinematics_Analytic
        //{
        //    public IKFast_KUKA(RobotArm robot, Target target, double[] prevJoints, Plane? basePlane) : base(robot, target, prevJoints, basePlane) { }

        //    [DllImport("IKFast_KUKA.dll", CallingConvention = CallingConvention.Cdecl)]
        //    public static extern bool ComputeIk_KRTwenty_REighteenTen(double[] eetrans, double[] eerot, double pfree, double[] solutions);//eetrans = 机械臂末端位置，eerot = 机械臂末端旋转矩阵，pfree = 第7自由度，solutions = 回传值

        //    [DllImport("IKFast_KUKA.dll", CallingConvention = CallingConvention.Cdecl)]
        //    public static extern bool ComputeIkOne_KRTwenty_REighteenTen(double[] eetrans, double[] eerot, double pfree, double[] solutions, int solindex);
        //    override protected Transform[] ForwardKinematics(double[] joints)
        //    {
        //        var transforms = new Transform[6];
        //        double[] c = joints.Select(x => Cos(x)).ToArray();
        //        double[] s = joints.Select(x => Sin(x)).ToArray();
        //        double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
        //        double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

        //        transforms[0] = new double[4, 4] { { c[0], 0, c[0], c[0] + a[0] * c[0] }, { s[0], -c[0], s[0], s[0] + a[0] * s[0] }, { 0, 0, 0, d[0] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[1] = new double[4, 4] { { c[0] * (c[1] - s[1]), s[0], c[0] * (c[1] + s[1]), c[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] - s[1]), -c[0], s[0] * (c[1] + s[1]), s[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] + c[1], 0, s[1] - c[1], (s[1] + c[1]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[2] = new double[4, 4] { { c[0] * (c[1] * c[2] - s[1] * s[2]), s[0], c[0] * (c[1] * s[2] + s[1] * c[2]), c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] * c[2] - s[1] * s[2]), -c[0], s[0] * (c[1] * s[2] + s[1] * c[2]), s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] * c[2] + c[1] * s[2], 0, s[1] * s[2] - c[1] * c[2], a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[3] = new double[4, 4] { { c[3] - s[3], -c[3] - s[3], c[3], c[3] }, { s[3] + c[3], -s[3] + c[3], s[3], s[3] }, { 0, 0, 0, 0 + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[4] = new double[4, 4] { { c[3] * c[4] - s[3], -c[3] * c[4] - s[3], c[3] * s[4], c[3] * s[4] }, { s[3] * c[4] + c[3], -s[3] * c[4] + c[3], s[3] * s[4], s[3] * s[4] }, { -s[4], s[4], c[4], c[4] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
        //        transforms[5] = new double[4, 4] { { c[3] * c[4] * c[5] - s[3] * s[5], -c[3] * c[4] * s[5] - s[3] * c[5], c[3] * s[4], c[3] * s[4] * d[5] }, { s[3] * c[4] * c[5] + c[3] * s[5], -s[3] * c[4] * s[5] + c[3] * c[5], s[3] * s[4], s[3] * s[4] * d[5] }, { -s[4] * c[5], s[4] * s[5], c[4], c[4] * d[5] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();

        //        transforms[3] = Transform.Multiply(transforms[2], transforms[3]);
        //        transforms[4] = Transform.Multiply(transforms[2], transforms[4]);
        //        transforms[5] = Transform.Multiply(transforms[2], transforms[5]);

        //        return transforms;
        //    }

        //    protected override double[][] InverseKinematics(Transform transform, out List<string> errors)
        //    {
        //        errors = new List<string>();
        //        double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
        //        double[] rota = new double[9]//ZYX
        //        {
        //        transform[0, 0], transform[0, 1], transform[0, 2],
        //        transform[1, 0], transform[1, 1], transform[1, 2],
        //        transform[2, 0], transform[2, 1], transform[2, 2]
        //        };
        //        double[] solutions = new double[48];

        //        //Stopwatch sw = new Stopwatch();
        //        //sw.Start();
        //        bool a = ComputeIk_KRTwenty_REighteenTen(xyz, rota, 0, solutions);
        //        //sw.Stop();
        //        ////需要打开VS输出窗口查看
        //        //Debug.WriteLine("时间:" + sw.Elapsed);
        //        //sw.Reset();
        //        double[][] eightsolutions = new double[8][];
        //        if (!a)
        //        {
        //            errors.Add("Failed to get 8 ik solutions");
        //        }
        //        else
        //        {
        //            for (int i = 0; i < 8; i++)
        //            {
        //                double[] onesolutions = new double[6];
        //                for (int j = 0; j < 6; j++)
        //                {
        //                    double onevalue = solutions[i * 6 + j];
        //                    onesolutions[j] = onevalue;
        //                }
        //                //onesolutions[0] = PI - onesolutions[0];
        //                //onesolutions[1] += PI;
        //                //onesolutions[2] = (PI / 2) - onesolutions[2];
        //                //onesolutions[3] = PI - onesolutions[3];
        //                //onesolutions[4] = 2 * PI - onesolutions[4];
        //                //onesolutions[5] = 2 * PI - onesolutions[5];
        //                eightsolutions[i] = onesolutions;
        //            }
        //        }
        //        return eightsolutions;
        //    }

        //    protected override double[] InverseKinematics_GetOneSol(Transform transform, int solindex, out List<string> errors)
        //    {
        //        errors = new List<string>();
        //        double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
        //        double[] rota = new double[9]//ZYX
        //        {
        //        transform[0, 0], transform[0, 1], transform[0, 2],
        //        transform[1, 0], transform[1, 1], transform[1, 2],
        //        transform[2, 0], transform[2, 1], transform[2, 2]
        //        };
        //        double[] solutions = new double[6];
        //        bool a = ComputeIkOne_KRTwenty_REighteenTen(xyz, rota, 0, solutions, solindex);
        //        if (!a)
        //        {
        //            errors.Add($"Failed to get No.{solindex} ik solution");
        //        }
        //        //solutions[0] = PI - solutions[0];
        //        //solutions[1] += PI;
        //        //solutions[1] *= -1;
        //        //solutions[2] = (PI / 2) - solutions[2];
        //        //solutions[3] = PI - solutions[3];
        //        //solutions[4] = 2*PI - solutions[4];
        //        //solutions[5] = 2*PI - solutions[5];
        //        return solutions;
        //    }
        //}
        #endregion
    }
    public class IKfast
    {
        [DllImport("IKFast.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ComputeFk(double[] joints, double[] eetrans, double[] eerot);
        
        [DllImport("IKFast.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ComputeIkCSharp(double[] eetrans, double[] eerot, double pfree, double[] solutions);//eetrans = 机械臂末端位置，eerot = 机械臂末端旋转矩阵，pfree = 第7自由度，solutions = 回传值

        [DllImport("IKFast.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ComputeIkCSharp_GetOneSol(double[] eetrans, double[] eerot, double pfree, double[] solutions,int solindex);

        public static double[][] InverseKinematics(Transform transform, out List<string> errors)
        {
            errors = new List<string>();
            double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
            double[] rota = new double[9]//ZYX
            {    
                transform[0, 0], transform[0, 1], transform[0, 2],
                transform[1, 0], transform[1, 1], transform[1, 2],
                transform[2, 0], transform[2, 1], transform[2, 2]
            };
            double[] solutions = new double[48];

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            bool a = ComputeIkCSharp(xyz, rota, 0, solutions);
            //sw.Stop();
            ////需要打开VS输出窗口查看
            //Debug.WriteLine("时间:" + sw.Elapsed);
            //sw.Reset();
            double[][] eightsolutions = new double[8][];
            if (!a)
            {
                errors.Add("Failed to get 8 ik solutions");
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    double[] onesolutions = new double[6];
                    for (int j = 0; j < 6; j++)
                    {
                        double onevalue = solutions[i * 6 + j];
                        onesolutions[j] = onevalue;
                    }
                    eightsolutions[i] = onesolutions;
                }
            }
            return eightsolutions;
        }

        public static double[] InverseKinematics_GetOneSol (Transform transform,int solindex,out List<string> errors)
        {
            errors = new List<string>();
            double[] xyz = new double[3] { transform[0, 3] / 1000, transform[1, 3] / 1000, transform[2, 3] / 1000 };//mm => m
            double[] rota = new double[9]//ZYX
            {
                transform[0, 0], transform[0, 1], transform[0, 2],
                transform[1, 0], transform[1, 1], transform[1, 2],
                transform[2, 0], transform[2, 1], transform[2, 2]
            };
            double[] solutions = new double[6];
            bool a = ComputeIkCSharp_GetOneSol(xyz, rota, 0, solutions, solindex);
            if (!a)
            {
                errors.Add($"Failed to get No.{solindex} ik solution");
            }
            return solutions;
        }
    }

    public class AllKinematics 
    {
        public double[] Joints { get; internal set; }
        public double[][] Joints8 { get; internal set; }
        public Plane[] Planes { get; internal set; }
        public List<string> Errors { get; internal set; } = new List<string>();
        public RobotConfigurations Configuration { get; internal set; }
        public Target Target { get; internal set; }

        public Mechanism mechanism { get; }

        public Dictionary<int,double[][]> IKSolutionStorage = new Dictionary<int,double[][]>();

        public List<double[]> AllJoints = new List<double[]>();
        public static bool UseIKFast { get; set; } = false;

        public Dictionary<int, Transform[]> FKSolutionStorage = new Dictionary<int, Transform[]>();

        public AllKinematics(List<Target> alltargets,Mechanism mechanism,double[] firstjoints)
        {
            this.mechanism = mechanism;
            for(int i = 0; i < alltargets.Count; i++)
            {
                Target target = alltargets[i];
                SetJoints(target, i);
            }
            double[] prevJoints = firstjoints;
            for (int i = 0; i < IKSolutionStorage.Count; i++)
            {
                double[] joints = GetClosestSolution(IKSolutionStorage[i], prevJoints, out var configuration, out var difference);
                this.Configuration = configuration;
                double[] Joints = JointTarget.GetAbsoluteJoints(joints, prevJoints);
                AllJoints.Add(Joints);
                prevJoints = Joints;
            }
            for(int i = 0; i < AllJoints.Count; i++)
            {
                SetPlanes(AllJoints[i], i);
            }
        }
        public void SetJoints(Target target,int index)
        {
            if (target is JointTarget jointTarget)
            {
                Joints = jointTarget.Joints;
                double[][] eightjoints = new double[1][] { Joints };
                IKSolutionStorage.Add(index, eightjoints);
            }
            else if (target is CartesianTarget cartesianTarget)
            {
                Plane tcp = target.Tool.Tcp;
                tcp.Rotate(PI, Vector3d.ZAxis, Point3d.Origin);

                Plane targetPlane = cartesianTarget.Plane;
                //变换到Frame坐标下，如在旋转情况下坐标值不发生改动
                if (target.Frame.IsCoupled)
                {
                    targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                }
                else
                {
                    //targetPlane.Transform(Transform.PlaneToPlane(target.Frame.Plane, Plane.WorldXY));
                    targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                }

                //targetPlane.Transform(Transform.PlaneToPlane(Plane.WorldXY, target.Frame.Plane));
                //targetPlane.Transform(target.Frame.Plane.ToTransform());//把坐标转换到Frame坐标系？

                var transform = Transform.PlaneToPlane(Planes[0], Plane.WorldXY) * Transform.PlaneToPlane(tcp, targetPlane);

                List<string> errors;
                //double[] joints;
                //if (cartesianTarget.Configuration != null || prevJoints == null)
                //{
                //    Configuration = cartesianTarget.Configuration ?? RobotConfigurations.None;
                //    joints = InverseKinematics(transform, Configuration, out errors);
                //}
                //else
                //{
                //    joints = GetClosestSolution(transform, prevJoints, out var configuration, out errors, out var difference);
                //    Configuration = configuration;
                //}
                //if (prevJoints != null)
                //    Joints = JointTarget.GetAbsoluteJoints(joints, prevJoints);
                //else
                //    Joints = joints;

                Joints8 = Get8Solution(transform, out errors);

                IKSolutionStorage.Add(index, Joints8);

                Errors.AddRange(errors);
            }
        }
        double[][] Get8Solution(Transform transform, out List<string> errors)
        {
            var solutions = new double[8][];

            errors = new List<string>();

            for (int i = 0; i < 8; i++)
            {
                List<string> solutionErrors = new List<string>();
                solutions[i] = InverseKinematics(transform, (RobotConfigurations)i, out solutionErrors);
                foreach (var s in solutionErrors)
                {
                    errors.Add(s);
                }
            }
            return solutions;
        }
        public void SetPlanes(double[] joints,int index)
        {
            var jointTransforms = ForwardKinematics(joints);
            FKSolutionStorage.Add(index, jointTransforms);
            //if (target is JointTarget)
            //{
            //    var closest = GetClosestSolution(jointTransforms[jointTransforms.Length - 1], joints, out var configuration, out var errors, out var difference);
            //    this.Configuration = difference < AngleTol ? configuration : RobotConfigurations.Undefined;
            //}

            //for (int i = 0; i < 6; i++)
            //{
            //    var plane = jointTransforms[i].ToPlane();
            //    plane.Rotate(PI, plane.ZAxis);
            //    Planes[i + 1] = plane;
            //}
        }
        double SquaredDifference(double a, double b)
        {
            double difference = Abs(a - b);
            if (difference > PI)
                difference = PI * 2 - difference;
            return difference * difference;
        }
        double[] GetClosestSolution(double[][] solutions, double[] prevJoints, out RobotConfigurations configuration, out double difference)
        {
            int closestSolutionIndex = 0;
            double closestDifference = double.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                double currentDifference = prevJoints.Zip(solutions[i], (x, y) => SquaredDifference(x, y)).Sum();

                if (currentDifference < closestDifference)
                {
                    closestSolutionIndex = i;
                    closestDifference = currentDifference;
                }
            }
            difference = closestDifference;
            configuration = (RobotConfigurations)closestSolutionIndex;
            return solutions[closestSolutionIndex];
        }
        public double[] InverseKinematics(Transform transform, RobotConfigurations configuration, out List<string> errors)
        {
            errors = new List<string>();

            bool shoulder = configuration.HasFlag(RobotConfigurations.Shoulder);
            bool elbow = configuration.HasFlag(RobotConfigurations.Elbow);
            if (shoulder) elbow = !elbow;
            bool wrist = !configuration.HasFlag(RobotConfigurations.Wrist);

            bool isUnreachable = false;

            double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
            double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

            Plane flange = Plane.WorldXY;
            flange.Transform(transform);

            double[] joints = new double[6];

            double l2 = Sqrt(a[2] * a[2] + d[3] * d[3]);
            double ad2 = Atan2(a[2], d[3]);
            Point3d center = flange.Origin - flange.Normal * d[5];
            joints[0] = Atan2(center.Y, center.X);
            double ll = Sqrt(center.X * center.X + center.Y * center.Y);
            Point3d p1 = new Point3d(a[0] * center.X / ll, a[0] * center.Y / ll, d[0]);

            if (shoulder)
            {
                joints[0] += PI;
                var rotate = Transform.Rotation(PI, new Point3d(0, 0, 0));
                center.Transform(rotate);
            }

            double l3 = (center - p1).Length;
            double l1 = a[1];
            double beta = Acos((l1 * l1 + l3 * l3 - l2 * l2) / (2 * l1 * l3));
            if (double.IsNaN(beta))
            {
                beta = 0;
                isUnreachable = true;
            }
            if (elbow)
                beta *= -1;

            double ttl = new Vector3d(center.X - p1.X, center.Y - p1.Y, 0).Length;
            // if (p1.X * (center.X - p1.X) < 0)
            if (shoulder)
                ttl = -ttl;
            double al = Atan2(center.Z - p1.Z, ttl);

            joints[1] = beta + al;

            double gama = Acos((l1 * l1 + l2 * l2 - l3 * l3) / (2 * l1 * l2));
            if (double.IsNaN(gama))
            {
                gama = PI;
                isUnreachable = true;
            }
            if (elbow)
                gama *= -1;

            joints[2] = gama - ad2 - PI / 2;

            double[] c = new double[3];
            double[] s = new double[3];
            for (int i = 0; i < 3; i++)
            {
                c[i] = Cos(joints[i]);
                s[i] = Sin(joints[i]);
            }

            var arr = new Transform();
            arr[0, 0] = c[0] * (c[1] * c[2] - s[1] * s[2]); arr[0, 1] = s[0]; arr[0, 2] = c[0] * (c[1] * s[2] + s[1] * c[2]); arr[0, 3] = c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0];
            arr[1, 0] = s[0] * (c[1] * c[2] - s[1] * s[2]); arr[1, 1] = -c[0]; arr[1, 2] = s[0] * (c[1] * s[2] + s[1] * c[2]); arr[1, 3] = s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0];
            arr[2, 0] = s[1] * c[2] + c[1] * s[2]; arr[2, 1] = 0; arr[2, 2] = s[1] * s[2] - c[1] * c[2]; arr[2, 3] = a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0];
            arr[3, 0] = 0; arr[3, 1] = 0; arr[3, 2] = 0; arr[3, 3] = 1;

            arr.TryGetInverse(out var in123);

            var mr = Transform.Multiply(in123, transform);
            joints[3] = Atan2(mr[1, 2], mr[0, 2]);
            joints[4] = Acos(mr[2, 2]);
            joints[5] = Atan2(mr[2, 1], -mr[2, 0]);

            if (wrist)
            {
                joints[3] += PI;
                joints[4] *= -1;
                joints[5] -= PI;
            }

            #region 判断姿态进行修正
            for (int i = 0; i < 6; i++)
            {
                if (joints[i] > PI) joints[i] -= 2 * PI;
                if (joints[i] < -PI) joints[i] += 2 * PI;
            }

            if (isUnreachable)
                errors.Add($"Target out of reach");

            if (Abs(1 - mr[2, 2]) < 0.0001)
                errors.Add($"Near wrist singularity");

            //if (new Vector3d(center.X, center.Y, 0).Length < a[0] + SingularityTol)
            //    errors.Add($"Near overhead singularity");


            for (int i = 0; i < 6; i++)
            {
                if (double.IsNaN(joints[i])) joints[i] = 0;
            }
            #endregion
            return joints;
        }
        public Transform[] ForwardKinematics(double[] joints)
        {
            var transforms = new Transform[6];
            double[] c = joints.Select(x => Cos(x)).ToArray();
            double[] s = joints.Select(x => Sin(x)).ToArray();
            double[] a = mechanism.Joints.Select(joint => joint.A).ToArray();
            double[] d = mechanism.Joints.Select(joint => joint.D).ToArray();

            transforms[0] = new double[4, 4] { { c[0], 0, c[0], c[0] + a[0] * c[0] }, { s[0], -c[0], s[0], s[0] + a[0] * s[0] }, { 0, 0, 0, d[0] }, { 0, 0, 0, 1 } }.ToTransform();
            transforms[1] = new double[4, 4] { { c[0] * (c[1] - s[1]), s[0], c[0] * (c[1] + s[1]), c[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] - s[1]), -c[0], s[0] * (c[1] + s[1]), s[0] * ((c[1] - s[1]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] + c[1], 0, s[1] - c[1], (s[1] + c[1]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
            transforms[2] = new double[4, 4] { { c[0] * (c[1] * c[2] - s[1] * s[2]), s[0], c[0] * (c[1] * s[2] + s[1] * c[2]), c[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * c[0] }, { s[0] * (c[1] * c[2] - s[1] * s[2]), -c[0], s[0] * (c[1] * s[2] + s[1] * c[2]), s[0] * (a[2] * (c[1] * c[2] - s[1] * s[2]) + a[1] * c[1]) + a[0] * s[0] }, { s[1] * c[2] + c[1] * s[2], 0, s[1] * s[2] - c[1] * c[2], a[2] * (s[1] * c[2] + c[1] * s[2]) + a[1] * s[1] + d[0] }, { 0, 0, 0, 1 } }.ToTransform();
            transforms[3] = new double[4, 4] { { c[3] - s[3], -c[3] - s[3], c[3], c[3] }, { s[3] + c[3], -s[3] + c[3], s[3], s[3] }, { 0, 0, 0, 0 + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
            transforms[4] = new double[4, 4] { { c[3] * c[4] - s[3], -c[3] * c[4] - s[3], c[3] * s[4], c[3] * s[4] }, { s[3] * c[4] + c[3], -s[3] * c[4] + c[3], s[3] * s[4], s[3] * s[4] }, { -s[4], s[4], c[4], c[4] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();
            transforms[5] = new double[4, 4] { { c[3] * c[4] * c[5] - s[3] * s[5], -c[3] * c[4] * s[5] - s[3] * c[5], c[3] * s[4], c[3] * s[4] * d[5] }, { s[3] * c[4] * c[5] + c[3] * s[5], -s[3] * c[4] * s[5] + c[3] * c[5], s[3] * s[4], s[3] * s[4] * d[5] }, { -s[4] * c[5], s[4] * s[5], c[4], c[4] * d[5] + d[3] }, { 0, 0, 0, 1 } }.ToTransform();

            transforms[3] = Transform.Multiply(transforms[2], transforms[3]);
            transforms[4] = Transform.Multiply(transforms[2], transforms[4]);
            transforms[5] = Transform.Multiply(transforms[2], transforms[5]);

            return transforms;
        }
    }
}