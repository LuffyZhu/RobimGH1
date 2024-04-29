using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum EstunRobots { ER170_2650, ER16 }
    public abstract class RobotModelsEstun : RobotProperties
    {
        public RobotModelsEstun(string modelname, double payload, int group) : base(modelname, Manufacturers.Estun, payload, group) { }
        public static List<string> GetNames => Enum.GetNames(typeof(EstunRobots)).Select(x => $"Estun.{x}").ToList();
    }
    public class ER170_2650 : RobotModelsEstun
    {
        public ER170_2650() : base("ER170_2650", 125, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 260, 645.5, -180, 180, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -60, 80, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 230, 0, -95, 80, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1245, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 220, -360, 360, 175);
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
    public class ER16 : RobotModelsEstun
    {
        public ER16() : base("ER16", 125, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 160, 412, -180, 180, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 680, 0, -60, 140, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 130, 0, -170, 80, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 750, -360, 360, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -130, 120, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 95, -360, 360, 175);
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
