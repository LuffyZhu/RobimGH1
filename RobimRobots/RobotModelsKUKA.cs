using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum KUKARobots { KR210_R2700_ultra , KR90_R3100_Extra , KR90s_Special, KR6_R900, KR500_2, KR60_HA, KR20_R1810 , KR10_R1100 , KR22_R1610, KR180_R2500_extra }
    public abstract class RobotModelsKUKA : RobotProperties
    {
        public RobotModelsKUKA(string modelname, double payload, int group) : base(modelname, Manufacturers.KUKA, payload, group) { }
        public static List<string> GetNames => Enum.GetNames(typeof(KUKARobots)).Select(x => $"KUKA.{x}").ToList();
    }
    public class KR210_R2700_ultra : RobotModelsKUKA
    {
        public KR210_R2700_ultra() : base("KR210_R2700_ultra", 125, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 350, 675, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -140, -5, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -41, 0, -120, 155, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1200, -350, 350, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -122.5, 122.5, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -350, 350, 175);
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
    public class KR90_R3100_Extra : RobotModelsKUKA
    {
        public KR90_R3100_Extra() : base("KR90_R3100_Extra", 90, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 350, 675, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1350, 0, -140, -5, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -41, 0, -120, 155, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1400, -350, 350, 136);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -122.5, 122.5, 129);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -350, 350, 206);
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
    public class KR90s_Special : RobotModelsKUKA
    {
        public KR90s_Special() : base("KR90s_Special", 90, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 350, 675, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1350, 0, -140, -5, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -41, 0, -120, 155, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1400, -350, 350, 136);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -122.5, 122.5, 129);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -350, 350, 206);
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
    public class KR6_R900 : RobotModelsKUKA
    {
        public KR6_R900() : base("KR6_R900", 6, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 25, 400, -170, 170, 360);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 455, 0, -190, 45, 300);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 35, 0, -120, 156, 360);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 420, -185, 185, 381);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -120, 120, 388);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 80, -350, 350, 615);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "1,2";
            string AInames = "1,2";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class KR500_2 : RobotModelsKUKA
    {
        public KR500_2() : base("KR500_2", 500, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 500, 1045, -185, 185, 69);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1300, 0, -130, 20, 69);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 55, 0, -94, 150, 69);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1025, -350, 350, 77);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -118, 118, 76);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 250, -350, 350, 120);
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
    public class KR60_HA : RobotModelsKUKA
    {
        public KR60_HA() : base("KR60_HA", 60, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 350, 815, -185, 185, 128);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 850, 0, -135, 35, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 145, 0, -120, 158, 128);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 820, -350, 350, 260);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -119, 119, 245);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 170, -350, 350, 322);
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
    public class KR20_R1810 : RobotModelsKUKA
    {
        public KR20_R1810() : base("KR20_R1810", 20, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 160, 520, -185, 185, 200);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 780, 0, -185, 65, 175);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 150, 0, -138, 175, 190);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 860, -350, 350, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -130, 130, 430);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 153, -350, 350, 630);
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
    public class KR10_R1100 : RobotModelsKUKA
    {
        public KR10_R1100() : base("KR10_R1100", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 25, 400, -185, 185, 128);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 560, 0, -185, 65, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 35, 0, -138, 175, 128);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 515, -350, 350, 260);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -130, 130, 245);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 80, -350, 350, 322);
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
    public class KR22_R1610 : RobotModelsKUKA
    {
        public KR22_R1610() : base("KR22_R1610", 20, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 160, 520, -185, 185, 200);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 780, 0, -185, 65, 175);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 150, 0, -138, 175, 190);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 655, -350, 350, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -130, 130, 430);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 153, -350, 350, 630);
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
    public class KR180_R2500_extra : RobotModelsKUKA
    {
        public KR180_R2500_extra() : base("KR180_R2500_extra", 180, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 350, 675, -185, 185, 123);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -140, -5, 115);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -41, 0, -120, 155, 120);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1000, -350, 350, 179);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 172);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -350, 350, 219);
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