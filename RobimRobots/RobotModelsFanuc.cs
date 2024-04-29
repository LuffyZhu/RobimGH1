using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum FanucRobots { M_710ic50, M_710ic20l, R_2000ic270f, M_20ia, R_2000ic125L, R_2000ic210F }
    public abstract class RobotModelsFanuc : RobotProperties
    {
        public RobotModelsFanuc(string modelname, double payload, int group) : base(modelname, Manufacturers.FANUC, payload, group) { }
        public static List<string> GetNames => Enum.GetNames(typeof(FanucRobots)).Select(x => $"FANUC.{x}").ToList();
    }
    public class M_710ic50 : RobotModelsFanuc
    {
        public M_710ic50() : base("M_710ic50", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 565, -180, 180, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 870, 0, -110, 110, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 170, 0, -162, 270, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1016, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 175, -450, 450, 175);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "0,1,2,3,4";
            string AInames = "0,1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class M_710ic20l : RobotModelsFanuc
    {
        public M_710ic20l() : base("M_710ic20l", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 565, -180, 180, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -110, 110, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 190, 0, -162, 270, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1800, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -450, 450, 175);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "0,1,2,3,4";
            string AInames = "0,1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class R_2000ic270f : RobotModelsFanuc
    {
        public R_2000ic270f() : base("R_2000ic270f", 270, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 312, 670, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1075, 0, -60, 76, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 225, 0, -120, 155, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1280, -350, 350, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -122.5, 122.5, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 240, -350, 350, 175);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "1,2,3,4";
            string AInames = "1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class M_20ia : RobotModelsFanuc
    {
        public M_20ia() : base("M_20ia", 270, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 525, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 790, 0, -100, 160, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 250, 0, -285, 275.6, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 835, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -450, 450, 175);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "1,2,3,4";
            string AInames = "1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class R_2000ic125L : RobotModelsFanuc
    {
        public R_2000ic125L() : base("R_2000ic125L", 125, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 312, 670, -185, 185, 130);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1075, 0, -56, 80, 115);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 225, 0, -68.1, 231.9, 125);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1730, -350, 350, 180);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 180);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -360, 360, 260);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "1,2,3,4";
            string AInames = "1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class R_2000ic210F : RobotModelsFanuc
    {
        public R_2000ic210F() : base("R_2000ic210F", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 312, 670, -185, 185, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1075, 0, -56, 80, 105);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 225, 0, -68.1, 241.9, 110);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1280, -360, 360, 140);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 140);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -360, 360, 220);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "1,2,3,4";
            string AInames = "1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
}
