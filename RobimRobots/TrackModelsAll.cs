using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum TrackModels { JiaShan , SMS , M7DM1 , YIMO , KL1000_2 , Yancheng , JiadingWelding , Xijiao , AFN , Dolomiti }
    public abstract class TrackModelsAll : TrackProperties
    {
        public TrackModelsAll(string modelname, Manufacturers manufacturers, double payload, bool movesrobot) : base(modelname, manufacturers, payload, movesrobot) { }
        public static List<string> GetNames => Enum.GetNames(typeof(TrackModels)).Select(x => $"{x}").ToList();
        public static List<int> GetJoint()
        {
            int[] joints = new int[10]
            {
                1,1,1,1,1,1,1,1,1,1
            };
            return joints.ToList();
        }
    }
    public class JiaShan : TrackModelsAll
    {
        public JiaShan() : base("JiaShan", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -9200, 9200, 1890);
            return jointProperties;
        }
    }
    public class SMS : TrackModelsAll
    {
        public SMS() : base("SMS", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -50, 9950, 1890);
            return jointProperties;
        }
    }
    public class M7DM1 : TrackModelsAll
    {
        public M7DM1() : base("M7DM1", Manufacturers.ABB, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -5200, 5200, 1890);
            return jointProperties;
        }
    }
    public class YIMO : TrackModelsAll
    {
        public YIMO() : base("YIMO", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 10, -4900, 1890);
            return jointProperties;
        }
    }
    public class KL1000_2 : TrackModelsAll
    {
        public KL1000_2() : base("KL1000_2", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 881.6, 480, 0, 10000, 1890);
            return jointProperties;
        }
    }
    public class Yancheng : TrackModelsAll
    {
        public Yancheng() : base("Yancheng", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -3000, 3000, 1890);
            return jointProperties;
        }
    }
    public class JiadingWelding : TrackModelsAll
    {
        public JiadingWelding() : base("JiadingWelding", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 0, 5000, 1890);
            return jointProperties;
        }
    }
    public class Xijiao : TrackModelsAll
    {
        public Xijiao() : base("Xijiao", Manufacturers.UR, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -1000, 1000, 1890);
            return jointProperties;
        }
    }
    public class AFN : TrackModelsAll
    {
        public AFN() : base("AFN", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -300, 7600, 1890);
            return jointProperties;
        }
    }
    public class Dolomiti : TrackModelsAll
    {
        public Dolomiti() : base("Dolomiti", Manufacturers.KUKA, 1000, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -8400, 8400, 1890);
            return jointProperties;
        }
    }
}
