using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using static Robim.Util;
using static System.Math;
using RobimRobots;

namespace Robim
{
    public class Positioner : Mechanism
    {
        //旋转type only for platform external
        //public double Revolving { get; internal set; }
        //public bool IsRevolve { get; internal set; }
        string[] externaltype { get; }
        internal Positioner(string model, Manufacturers manufacturer, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints, bool movesRobot,RobimFormSystem rfs, double revolving, bool isrevolve) : base(model, manufacturer, payload, basePlane, baseMesh, joints, movesRobot, rfs, revolving, isrevolve)
        {
            externaltype = rfs.External_Type;
        }
        
        //public static Plane coupledplane = Plane.Unset;
        protected override void SetStartPlanes()
        {
            switch (this.Revolving)
            {
                case 0:
                    Joints[0].Plane = new Plane(new Point3d(0, 0, Joints[0].D), Vector3d.XAxis, Vector3d.ZAxis);
                    break;
                case 1://第一种旋转模式Y-axis
                    Joints[0].Plane = new Plane(new Point3d(0, 0, Joints[0].D), Vector3d.XAxis, Vector3d.ZAxis);
                    break;
                case 2://第二种旋转模式Z-axis && 导轨型(木加工平台导轨)
                    Joints[0].Plane = new Plane(new Point3d(Joints[0].A, 0, Joints[0].D), Vector3d.XAxis, Vector3d.YAxis);
                    break;
                case 3://第三种旋转模式Y-axis & Z-axis
                    Joints[0].Plane = new Plane(new Point3d(0, 0, Joints[0].D), Vector3d.XAxis, Vector3d.ZAxis);
                    Joints[1].Plane = new Plane(new Point3d(0, Joints[1].A, Joints[0].D + Joints[1].D), Vector3d.XAxis, Vector3d.YAxis);
                    break;
            }
            //初始耦合面 get origin coupled plane
            DigitalCoupledPlane.Set("Platform",RFS.R_EulerPlane, BasePlane, Joints,RFS.C_EulerPlane);
            // Base plane//设定原点
            /*Plane plane1 = RobotSystem.robotbaseplane;
            Plane plane2 = BasePlane;//external eulerplane
            if (plane1 != Plane.Unset)
            {
                plane2.Transform(Transform.PlaneToPlane(Plane.WorldXY, (Plane)plane1));
            }
            // Move planes to base
            var transform = plane2.ToTransform();
            if (coupledplane == Plane.Unset)
            {
                Plane plane = Joints.Last().Plane;//不转动时的初始Plane
                plane.Transform(transform);
                coupledplane = plane;
            }*/
            #region Old
            //先暂时这么处理之后引入外部轴名称，根据名称作plan方向的判断            
            /*if (Joints.Length == 1)
            {
                Joints[0].Plane = new Plane(new Point3d(Joints[0].A, 0, Joints[0].D), Vector3d.XAxis, Vector3d.YAxis);
            }
            else
            {
                Joints[0].Plane = new Plane(new Point3d(0, 0, Joints[0].D), Vector3d.XAxis, Vector3d.ZAxis);
                Joints[1].Plane = new Plane(new Point3d(0, Joints[1].A, Joints[0].D + Joints[1].D), Vector3d.XAxis, Vector3d.YAxis);
            }*/
            /*if (Joints.Length == 1)
            {
                Joints[0].Plane = new Plane(new Point3d(0, 0, Joints[0].D), Vector3d.XAxis, Vector3d.ZAxis);
            }
            else
            {
                Joints[0].Plane = new Plane(new Point3d(0, 0, Joints[0].D), Vector3d.XAxis, Vector3d.ZAxis);
                Joints[1].Plane = new Plane(new Point3d(0, Joints[1].A, Joints[0].D + Joints[1].D), Vector3d.XAxis, Vector3d.YAxis);
            }*/
            #endregion
        }

        public override double DegreeToRadian(double degree, int i)
        {
            if (this.IsRevolve) return degree * (PI / 180);
            else return degree;
        }
        public override double RadianToDegree(double radian, int i)
        {
            if (this.IsRevolve) return radian * (180 / PI);
            else return radian;
        }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? basePlane = null) => new PositionerKinematics(this, target, prevJoints, basePlane, externaltype);

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }

        class PositionerKinematics : MechanismKinematics
        {
            string[] externals { get; set; }
            internal PositionerKinematics(Positioner positioner, Target target, double[] prevJoints, Plane? basePlane,string[] externaltype) : base(positioner, target, prevJoints, basePlane, externaltype) { }
            protected override void SetJoints(Target target, double[] prevJoints)
            {
                externals = Externaltype;
                for (int i = 0, j = 0; i < externals.Length; i++)
                {
                    if (externals[i].Contains("Platform"))
                    {
                        int externalNum = mechanism.Joints[j].Number - 6;
                        if (target.External.Length - 1 < externalNum)
                            Errors.Add($"Positioner external axis not configured on this target.");
                        else
                        {
                            Joints[j] = target.External[i];
                            j += 1;
                        }
                    }
                }

                #region Old 如果有两个外部轴，旋转平台为E2的值
                /*if(target.External.Length == 1)
                {
                    for (int i = 0; i < mechanism.Joints.Length; i++)
                    {
                        int externalNum = mechanism.Joints[i].Number - 6;
                        if (target.External.Length - 1 < externalNum)
                            Errors.Add($"Positioner external axis not configured on this target.");
                        else
                            Joints[i] = target.External[0];
                    }
                }
                else
                {
                    for (int i = 0; i < mechanism.Joints.Length; i++)
                    {
                        int externalNum = mechanism.Joints[i].Number - 6;
                        if (target.External.Length - 1 < externalNum)
                            Errors.Add($"Positioner external axis not configured on this target.");
                        else
                            Joints[i] = target.External[1];
                    }
                }*/
                #endregion
                if (prevJoints != null)
                    Joints = JointTarget.GetAbsoluteJoints(Joints, prevJoints);
            }
            protected override void SetPlanes(Target target)
            {
                int jointCount = mechanism.Joints.Length;

                if (!this.IsRevolve)//不旋转
                {
                    Planes[1] = mechanism.Joints[0].Plane;
                    Planes[1].Origin += Planes[1].YAxis * Joints[0];//Planes[1].Origin += Planes[1].XAxis * Joints[0] 为嘉善工厂临时改动，导轨在y轴移动，机械臂屁股朝-x方向 
                    //Temporary modification for the Jiashan factory, the guide rail moves on the y-axis, and the butt of the robot arm faces the -x direction
                    if (mechanism.Joints.Length == 1) return;
                }
                else
                {
                    for (int i = 0; i < jointCount; i++)
                    {
                        Planes[i + 1] = mechanism.Joints[i].Plane;

                        for (int j = i; j >= 0; j--)
                            Planes[i + 1].Rotate(Joints[j], mechanism.Joints[j].Plane.Normal, mechanism.Joints[j].Plane.Origin);
                    }
                }
            }
            protected override void IKSetJoints(Target target, double[] prevJoints)
            {
                throw new NotImplementedException();
            }
            protected override void IKSetPlanes()
            {
                throw new NotImplementedException();
            }
        }
    }

    public class Custom : Mechanism
    {
        internal Custom(string model, Manufacturers manufacturer, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints, bool movesRobot,RobimFormSystem rfs)
            : base(model, manufacturer, payload, basePlane, baseMesh, joints, movesRobot,rfs) { }

        protected override void SetStartPlanes()
        {
            var plane = Plane.WorldXY;
            foreach (var joint in Joints)
            {
                joint.Plane = plane;
            }
        }

        public override double DegreeToRadian(double degree, int i) => degree;
        public override double RadianToDegree(double radian, int i) => radian;

        public override KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? basePlane = null)
            => new CustomKinematics(this, target, prevJoints, basePlane);

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }

        class CustomKinematics : MechanismKinematics
        {
            internal CustomKinematics(Custom custom, Target target, double[] prevJoints, Plane? basePlane)
                : base(custom, target, prevJoints, basePlane) { }

            protected override void SetJoints(Target target, double[] prevJoints)
            {
                for (int i = 0; i < mechanism.Joints.Length; i++)
                {
                    int externalNum = mechanism.Joints[i].Number - 6;

                    if (target.External.Length < externalNum + 1)
                        Errors.Add($"Custom external axis not configured on this target.");
                    else
                    {
                        double value = target.External[externalNum];
                        Joints[i] = prevJoints == null ? value : value;
                    }
                }
            }

            protected override void SetPlanes(Target target)
            {
                for (int i = 0; i < Planes.Length; i++)
                {
                    Planes[i] = Plane.WorldXY;
                }
            }
            protected override void IKSetJoints(Target target, double[] prevJoints)
            {
                throw new NotImplementedException();
            }
            protected override void IKSetPlanes()
            {
                throw new NotImplementedException();
            }
        }
    }
    public enum TrackHangUpSideDown { No ,X_axis ,Y_axis}
    public class Track : Mechanism
    {
        string[] externaltypes { get; }
        internal Track(string model, Manufacturers manufacturer, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints, bool movesRobot,RobimFormSystem rfs, bool trackVertical) : base(model, manufacturer, payload, basePlane, baseMesh, joints, movesRobot, rfs, trackVertical: trackVertical)
        {
            externaltypes = rfs.External_Type;
            if (model == "ZJUv2000")
            {
                trackVertical = true;
            }
        }
        
        public static Plane coupledplane = Plane.Unset;

        protected override void SetStartPlanes()
        {
            //var plane = Plane.WorldXY;
            foreach (var joint in Joints)
            {
                joint.Plane = new Plane(new Point3d(joint.A, 0, joint.D), Vector3d.XAxis, Vector3d.YAxis);
                //plane.Origin = plane.Origin + plane.XAxis * joint.A + plane.ZAxis * joint.D;
                //joint.Plane = plane;
            }

            /*
            if (Joints.Length == 3)
            {
                plane = Joints[2].Plane;
                plane.Rotate(PI, plane.XAxis);
                Joints[2].Plane = plane;
            }
            */
            //DigitalCoupledPlane.Set("Track", BasePlane, Joints);
        }

        public override double DegreeToRadian(double degree, int i) => degree;
        public override double RadianToDegree(double radian, int i) => radian;

        public override KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? basePlane = null) => new TrackKinematics(this, target, basePlane, externaltypes);

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }

        class TrackKinematics : MechanismKinematics
        {
            string[] externals { get; set; }

            internal TrackKinematics(Track track, Target target, Plane? basePlane, string[] externaltype) : base(track, target, null, basePlane, externaltype) { }
            
            protected override void SetJoints(Target target, double[] prevJoints)
            {
                externals = Externaltype;
                for (int i = 0, j = 0; i < externals.Length; i++)
                {
                    if (externals[i].Contains("Track"))
                    {
                        int externalNum = mechanism.Joints[j].Number - 6;
                        if (target.External.Length - 1 < externalNum)
                            Errors.Add($"Track external axis not configured on this target.");
                        else
                        {
                            Joints[j] = target.External[i];
                            j += 1;
                        }
                    }
                }
                #region Old
                /*for (int i = 0; i < mechanism.Joints.Length; i++)
                {
                    int externalNum = mechanism.Joints[i].Number - 6;
                    if (target.External.Length < externalNum + 1) Errors.Add($"Track external axis not configured on this target.");
                    else Joints[i] = target.External[externalNum];
                }*/
                #endregion
            }

            protected override void SetPlanes(Target target)
            {
                if(mechanism.Manufacturer  == Manufacturers.UR)
                {
                    Planes[1] = mechanism.Joints[0].Plane;
                    Planes[1].Origin += Planes[1].ZAxis * Joints[0];//UR改动，导轨在Z轴移动
                    if (mechanism.Joints.Length == 1) return;
                }

                Planes[1] = mechanism.Joints[0].Plane;
                if (this.mechanism.TrackVertical == false)
                {
                    Planes[1].Origin += Planes[1].YAxis * Joints[0];//Planes[1].Origin += Planes[1].XAxis * Joints[0] 为嘉善工厂临时改动，导轨在y轴移动，机械臂屁股朝-x方向
                    //Temporary modification for the Jiashan factory, the guide rail moves on the y-axis, and the butt of the robot arm faces the -x direction
                }
                else
                {
                    Planes[1].Origin += Planes[1].ZAxis * Joints[0];
                }


                if (mechanism.Joints.Length == 1) return;

                Planes[2] = mechanism.Joints[1].Plane;
                Planes[2].Origin += Planes[1].Origin + Planes[2].YAxis * Joints[1];
                if (mechanism.Joints.Length == 2) return;

                Planes[3] = mechanism.Joints[2].Plane;
                Planes[3].Origin += Planes[2].Origin + Planes[3].ZAxis * Joints[2];
                if (mechanism.Joints.Length == 3) return;
            }
            protected override void IKSetJoints(Target target, double[] prevJoints)
            {
                throw new NotImplementedException();
            }
            protected override void IKSetPlanes()
            {
                throw new NotImplementedException();
            }
        }
    }

    public class DigitalCoupledPlane
    {
        /// <summary>
        /// 导轨初始面
        /// </summary>
        public Plane T_CoupledPlane { get; set; }
        /// <summary>
        /// 旋转平台初始面
        /// </summary>
        public Plane P_CoupledPlane { get; set; }
        /// <summary>
        /// 使用者输入的耦合面
        /// </summary>
        public Plane CustomPlane { get; set; }
        public DigitalCoupledPlane(Plane t_coupledplane,Plane p_coupledplane,Plane custom)
        {
            this.T_CoupledPlane = t_coupledplane;
            this.P_CoupledPlane = p_coupledplane;
            this.CustomPlane = custom;
        }
        public static DigitalCoupledPlane DCP = new DigitalCoupledPlane(Plane.Unset, Plane.Unset, Plane.Unset);
        public static DigitalCoupledPlane Set(string Type,Plane robotbase, Plane externaleulerplane,Joint[] Joints,Plane custom)
        {
            //输入的耦合面
            DCP.CustomPlane = custom;
            //初始耦合面 get origin coupled plane
            // Base plane//设定原点
            Plane plane1 = robotbase;
            Plane plane2 = externaleulerplane;//external eulerplane
            if (plane1 != Plane.WorldXY)
            {
                plane2.Transform(Transform.PlaneToPlane(Plane.WorldXY, plane1));
            }
            // Move planes to base
            var transform = plane2.ToTransform();
            Plane plane = Joints.Last().Plane;//不转动时的初始Plane
            plane.Transform(transform);
            if (Type.Contains("Track"))
                DCP.T_CoupledPlane = plane;
            else
                DCP.P_CoupledPlane = plane;

            return DCP;
        }
    }
}