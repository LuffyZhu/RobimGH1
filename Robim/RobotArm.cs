using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using static Robim.Util;
using static System.Math;
using RobimRobots;

namespace Robim
{
    public abstract partial class RobotArm : Mechanism
    {
        internal RobotArm(string model, Manufacturers manufactuer, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, manufactuer, payload, basePlane, baseMesh, joints, false) { }

        protected override void SetStartPlanes()
        {
            KinematicSolution kinematics = Kinematics(GetStartPose());
            for (int i = 0; i < Joints.Length; i++)
            {
                Plane plane = kinematics.Planes[i + 1];
                plane.Transform(Transform.PlaneToPlane(this.BasePlane, Plane.WorldXY));
                Joints[i].Plane = plane;
            }
        }

        protected abstract JointTarget GetStartPose();
    }

    public class RobotAbb : RobotArm
    {
        internal RobotAbb(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.ABB, payload, basePlane, baseMesh, joints) { }

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, 0 });
        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new SphericalWristKinematics(this, target, prevJoints, basePlane);
        public static double ABBDegreeToRadian(double degree, int i)
        {
            double radian = degree.ToRadians();
            if (i == 1) radian = -radian + PI * 0.5;
            if (i == 2) radian *= -1;
            if (i == 4) radian *= -1;
            return radian;
        }

        public override double DegreeToRadian(double degree, int i)
        {
            return ABBDegreeToRadian(degree, i);
        }

        public override double RadianToDegree(double radian, int i)
        {
            if (i == 1) { radian -= PI * 0.5; radian = -radian; }
            if (i == 2) radian *= -1;
            if (i == 4) radian *= -1;
            return radian.ToDegrees();
        }

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null) => new IK(this, target, prevJoints, basePlane);
    }

    public class RobotKuka : RobotArm
    {
        internal RobotKuka(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.KUKA, payload, basePlane, baseMesh, joints) { }

        //protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, -PI });
        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, 0 });
        public override KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? basePlane = null) => new SphericalWristKinematics(this, target, prevJoints, basePlane);

        public override double DegreeToRadian(double degree, int i)
        {
            double radian = degree.ToRadians();
            if (i == 2) radian -= 0.5 * PI;
            radian = -radian;
            return radian;
        }

        public override double RadianToDegree(double radian, int i)
        {
            radian = -radian;
            if (i == 2) radian += 0.5 * PI;
            return radian.ToDegrees();
        }

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null) => new IK(this, target, prevJoints, basePlane);
    }

    public class RobotUR : RobotArm
    {
        internal RobotUR(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.UR, payload, basePlane, baseMesh, joints) { }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new OffsetWristKinematics(this, target, prevJoints, basePlane);

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, -PI / 2, 0, -PI / 2, 0, 0 });
        public override double DegreeToRadian(double degree, int i) => degree * (PI / 180);
        public override double RadianToDegree(double radian, int i) => radian * (180 / PI);

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
    }

    public class RobotJAKA : RobotArm
    {
        internal RobotJAKA(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.JAKA, payload, basePlane, baseMesh, joints) { }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new OffsetWristKinematics(this, target, prevJoints, basePlane);

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, -PI / 2, 0, -PI / 2, 0, 0 });
        public override double DegreeToRadian(double degree, int i) => degree * (PI / 180);
        public override double RadianToDegree(double radian, int i) => radian * (180 / PI);

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
    }

    public class RobotStaubli : RobotArm
    {
        internal RobotStaubli(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.Staubli, payload, basePlane, baseMesh, joints) { }

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, PI / 2, 0, 0, 0 });

        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new SphericalWristKinematics(this, target, prevJoints, basePlane);

        public override double DegreeToRadian(double degree, int i)
        {
            double radian = degree.ToRadians();
            if (i == 1) radian = -radian + PI * 0.5;
            if (i == 2) radian *= -1;
            if (i == 2) radian += PI * 0.5;
            if (i == 4) radian *= -1;
            return radian;
        }

        public override double RadianToDegree(double radian, int i)
        {
            if (i == 1) { radian -= PI * 0.5; radian = -radian; }
            if (i == 2) radian -= PI * 0.5;
            if (i == 2) radian *= -1;
            if (i == 4) radian *= -1;
            return radian.ToDegrees();
        }

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
    }

    public class RobotAubo : RobotArm
    {
        internal RobotAubo(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.Aubo, payload, basePlane, baseMesh, joints) { }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new AuboWristKinematics(this, target, prevJoints, basePlane);

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, 0, 0, 0, 0, 0 });
        public override double DegreeToRadian(double degree, int i) => degree * (PI / 180);
        public override double RadianToDegree(double radian, int i) => radian * (180 / PI);

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
    }

    public class RobotFanuc : RobotArm
    {
        double j1_temp = 0;
        internal RobotFanuc(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.FANUC, payload, basePlane, baseMesh, joints) { }

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, PI });
        //protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, 0 }); // { 0, PI / 2, 0, 0, 0, -PI });

        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new SphericalWristKinematics(this, target, prevJoints, basePlane);

        public override double DegreeToRadian(double degree, int i)
        {
            double radian = degree.ToRadians();
            if (i == 1) { j1_temp = radian; radian = -radian + PI * 0.5; }
            if (i == 2) { radian = j1_temp + radian; }
            if (i == 3) { radian = -1 * radian; }
            if (i == 5) { radian = PI - 1 * radian; }
            return radian;
        }

        public override double RadianToDegree(double radian, int i)
        {
            if (i == 1) { radian = -radian + PI * 0.5; j1_temp = radian; }
            if (i == 2) { radian = radian - j1_temp; }
            if (i == 3) { radian = -1 * radian; }
            if (i == 5) { radian = PI - 1 * radian; }
            //if (radian - GetStartPose().Joints[i] > PI) radian -= 2 * PI;
            //if (radian - GetStartPose().Joints[i] < -PI) radian += 2 * PI;
            return radian.ToDegrees();
        }

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
    }
    public class RobotESTUN : RobotArm
    {
        internal RobotESTUN(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.Estun, payload, basePlane, baseMesh, joints) { }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new SphericalWristKinematics(this, target, prevJoints, basePlane);

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, -PI });
        public override double DegreeToRadian(double degree, int i)
        {
            double radian = degree.ToRadians();
            if (i == 1) radian -= 0.5 * PI;
            //if (i == 2) radian -= 0.5 * PI;
            radian = -radian;
            return radian;
        }

        public override double RadianToDegree(double radian, int i)
        {
            radian = -radian;
            if (i == 1) radian += 0.5 * PI;
            return radian.ToDegrees();
        }

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
    }

    public class RobotGoogol : RobotArm
    {
        internal RobotGoogol(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.Googol, payload, basePlane, baseMesh, joints) { }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? basePlane = null) => new SphericalWristKinematics(this, target, prevJoints, basePlane);
        /*protected override void SetStartPlanes()
        {
            KinematicSolution kinematics = Kinematics(GetStartPose());
            for (int i = 0; i < Joints.Length - 1; i++)
            {
                Plane plane = kinematics.Planes[i + 1];
                plane.Transform(Transform.PlaneToPlane(this.BasePlane, Plane.WorldXY));
                Joints[i].Plane = plane;
            }
            Plane offPlane = kinematics.Planes[Joints.Length];
            //offPlane.Translate(new Vector3d(0, -45, 45));
            offPlane.Transform(Transform.PlaneToPlane(this.BasePlane, Plane.WorldXY));
            Joints[Joints.Length-1].Plane = offPlane;
        }*/
        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, -PI });
        public override double DegreeToRadian(double degree, int i)
        {
            double radian = degree.ToRadians();
            if (i == 0)
            {
                radian = -radian;
            }
            if (i == 1) radian -= 0.5 * PI;
            if (i == 3)
            {
                radian = -radian;
            }
            if (i == 5)
            {
                radian = -radian;
            }
            //if (i == 2) radian -= 0.5 * PI;
            radian = -radian;
            return radian;
        }

        public override double RadianToDegree(double radian, int i)
        {
            radian = -radian;
            if (i == 0)
            {
                radian = -radian;
            }
            if (i == 1) radian += 0.5 * PI;
            if (i == 3)
            {
                radian = -radian;
            }
            if (i == 5)
            {
                radian = -radian;
            }
            return radian.ToDegrees();
        }

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
        //public override MechanismKinematics_All Set_MechanismKinematics_All(Target target, Plane? basePlane = null) => new SphericalWristKinematics_8Solution(this, target, basePlane);
    }

    public class RobotYaskawa : RobotArm
    {
        internal RobotYaskawa(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.Yaskawa, payload, basePlane, baseMesh, joints) { }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? basePlane = null) => new SphericalWristKinematics(this, target, prevJoints, basePlane);
        /*protected override void SetStartPlanes()
        {
            KinematicSolution kinematics = Kinematics(GetStartPose());
            for (int i = 0; i < Joints.Length - 1; i++)
            {
                Plane plane = kinematics.Planes[i + 1];
                plane.Transform(Transform.PlaneToPlane(this.BasePlane, Plane.WorldXY));
                Joints[i].Plane = plane;
            }
            Plane offPlane = kinematics.Planes[Joints.Length];
            //offPlane.Translate(new Vector3d(0, -45, 45));
            offPlane.Transform(Transform.PlaneToPlane(this.BasePlane, Plane.WorldXY));
            Joints[Joints.Length-1].Plane = offPlane;
        }*/
        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, PI / 2, 0, 0, 0, -PI });
        public override double DegreeToRadian(double degree, int i)
        {
            double radian = degree.ToRadians();
            if (i == 0)
            {
                radian = -radian;
            }
            if (i == 1) radian -= 0.5 * PI;
            if (i == 3)
            {
                radian = -radian;
            }
            if (i == 5)
            {
                radian = -radian;
            }
            //if (i == 2) radian -= 0.5 * PI;
            radian = -radian;
            return radian;
        }

        public override double RadianToDegree(double radian, int i)
        {
            radian = -radian;
            if (i == 0)
            {
                radian = -radian;
            }
            if (i == 1) radian += 0.5 * PI;
            if (i == 3)
            {
                radian = -radian;
            }
            if (i == 5)
            {
                radian = -radian;
            }
            return radian.ToDegrees();
        }

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
        //public override MechanismKinematics_All Set_MechanismKinematics_All(Target target, Plane? basePlane = null) => new SphericalWristKinematics_8Solution(this, target, basePlane);
    }

    public class RobotCobot : RobotArm
    {
        internal RobotCobot(string model, double payload, Plane basePlane, Mesh baseMesh, Joint[] joints) : base(model, Manufacturers.Cobot, payload, basePlane, baseMesh, joints) { }

        public override KinematicSolution Kinematics(Target target, double[] prevJoints, Plane? basePlane = null) => new AuboWristKinematics(this, target, prevJoints, basePlane);

        protected override JointTarget GetStartPose() => new JointTarget(new double[] { 0, 0, 0, 0, 0, 0 });
        public override double DegreeToRadian(double degree, int i) => degree * (PI / 180);
        public override double RadianToDegree(double radian, int i) => radian * (180 / PI);

        public override KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null)
        {
            throw new NotImplementedException();
        }
    }
}