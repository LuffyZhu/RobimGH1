using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum StaubliRobots { TX200, TX200L, RX160, RX160L }
    public abstract class RobotModelsStaubli : RobotProperties
    {
        public RobotModelsStaubli(string modelname, double payload, int group) : base(modelname, Manufacturers.Staubli, payload, group) { }
        public static List<string> GetNames => Enum.GetNames(typeof(StaubliRobots)).Select(x => $"Staubli.{x}").ToList();
    }
    public class TX200 : RobotModelsStaubli
    {
        public TX200() : base("TX200", 100, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 250, 0, -180, 180, 160);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 950, 0, -115, 120, 160);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 0, 0, -140, 145, 160);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 800, -270, 270, 260);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -120, 120, 260);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 194, -270, 270, 400);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "BasicIO-1\\%Q0,BasicIO-1\\%Q1,BasicIO-1\\%Q2";
            string DInames = "BasicIO-1\\%I0,BasicIO-1\\%I1,BasicIO-1\\%I2";
            string AOnames = "";
            string AInames = "";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class TX200L : RobotModelsStaubli
    {
        public TX200L() : base("TX200L", 100, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 250, 0, -180, 180, 160);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 950, 0, -115, 120, 160);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 0, 0, -140, 145, 160);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1200, -270, 270, 260);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -120, 120, 260);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 194, -270, 270, 400);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "BasicIO-1\\%Q0,BasicIO-1\\%Q1,BasicIO-1\\%Q2";
            string DInames = "BasicIO-1\\%I0,BasicIO-1\\%I1,BasicIO-1\\%I2";
            string AOnames = "";
            string AInames = "";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class RX160 : RobotModelsStaubli
    {
        public RX160() : base("RX160", 30, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 0, -160, 160, 200);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 825, 0, -137.5, 137.5, 200);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 0, 0, -150, 150, 255);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 625, -270, 270, 315);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -105, 120, 360);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 110, -270, 270, 870);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "BasicIO-1\\%Q0,BasicIO-1\\%Q1,BasicIO-1\\%Q2";
            string DInames = "BasicIO-1\\%I0,BasicIO-1\\%I1,BasicIO-1\\%I2";
            string AOnames = "";
            string AInames = "";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class RX160L : RobotModelsStaubli
    {
        public RX160L() : base("RX160L", 30, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 0, -160, 160, 200);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 825, 0, -137.5, 137.5, 200);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 0, 0, -150, 150, 255);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 925, -270, 270, 315);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -105, 120, 360);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 110, -270, 270, 870);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "BasicIO-1\\%Q0,BasicIO-1\\%Q1,BasicIO-1\\%Q2";
            string DInames = "BasicIO-1\\%I0,BasicIO-1\\%I1,BasicIO-1\\%I2";
            string AOnames = "";
            string AInames = "";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
}
