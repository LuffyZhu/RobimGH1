using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum PlatformModels { SMS_P , YIMO_P , DKP_400, Yancheng_P, M8DM1_M9DM1 , WoodMove }

    public abstract class PlatformModelAll:PlatformProperties
    {
        public PlatformModelAll(string modelname, Manufacturers manufacturers, double payload,double type, bool isrevolve) : base(modelname, manufacturers, payload, type, isrevolve) { }
        public static List<string> GetNames => Enum.GetNames(typeof(PlatformModels)).Select(x => $"{x}").ToList();
        public static List<int> GetJoint()
        {
            int[] joints = new int[6]
            {
                1,1,2,1,2,1
            };
            return joints.ToList();
        }
    }
    public class SMS_P:PlatformModelAll
    {
        public SMS_P() : base("SMS_P", Manufacturers.KUKA, 400, 2, true) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 0, double.MinValue, double.MaxValue, 94.5);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
    }
    public class YIMO_P : PlatformModelAll
    {
        public YIMO_P() : base("YIMO_P", Manufacturers.KUKA, 400, 2, true) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 0, double.MinValue, double.MaxValue, 94.5);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
    }
    public class DKP_400 : PlatformModelAll
    {
        public DKP_400() : base("DKP_400", Manufacturers.KUKA, 400, 3, true) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[2];
            jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 510, -90, 90, 94.5);
            jointProperties[1] = new JointProperties(JointType.Revolute, 8, 0, 347, double.MinValue, double.MaxValue, 126);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
    }
    public class Yancheng_P : PlatformModelAll
    {
        public Yancheng_P() : base("Yancheng_P", Manufacturers.KUKA, 1000, 2, true) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 0, double.MinValue, double.MaxValue, 94.5);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
    }
    public class M8DM1_M9DM1 : PlatformModelAll
    {
        public M8DM1_M9DM1() : base("M8DM1_M9DM1", Manufacturers.ABB, 1000, 3, true) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[2];
            jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 500, -90, 90, 94.5);
            jointProperties[1] = new JointProperties(JointType.Revolute, 8, 0, 300, double.MinValue, double.MaxValue, 126);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
    }
    public class WoodMove : PlatformModelAll
    {
        public WoodMove() : base("WoodMove", Manufacturers.KUKA, 1000, 2, false) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 500, -5200, 5200, 1890);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
    }
}
