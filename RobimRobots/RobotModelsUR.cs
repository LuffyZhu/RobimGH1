using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum URRobots { UR10 }
    public abstract class RobotModelsUR : RobotProperties
    {
        public RobotModelsUR(string modelname, double payload, int group) : base(modelname, Manufacturers.UR, payload, group) { }
        public static List<string> GetNames => Enum.GetNames(typeof(URRobots)).Select(x => $"UR.{x}").ToList();
    }
    public class UR10 : RobotModelsUR
    {
        public UR10() : base("UR10", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 0, 127.3, -360, 360, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, -612, 0, -360, 360, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -572.3, 0, -360, 360, 180);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 163.941, -360, 360, 180);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 115.7, -360, 360, 180);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 92.2, -360, 360, 180);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "0,1";
            string AInames = "0,1";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
}
