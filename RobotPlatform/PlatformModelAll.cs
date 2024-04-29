using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class PlatformModelAll:PlatformProperties
    {
        public PlatformModelAll(string modelname, Manufacturers manufacturers, double payload,double type, bool isrevolve) : base(modelname, manufacturers, payload, type, isrevolve) { }
        protected override LoadModel SetLoadModel() => new LoadPlatform(Manufacturers, ModelName, JointCount);
    }
    public class LoadPlatform : LoadModel
    {
        public LoadPlatform(Manufacturers manufacturers, string modelname, int jointcount) : base(manufacturers, ModelType.Platform, modelname, jointcount) { }
        protected override Mesh[] LoadConvexHullModel(Manufacturers manufacturers, string modelname, int jointcount)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];
            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotPlatform.Properties.Resources.ResourceManager.GetString($"Platform_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }

        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];
            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotPlatform.Properties.Resources.ResourceManager.GetString($"Platform_{modelname}_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemPlatform : ModelSystem
    {
        public ModelSystemPlatform(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype0 = (PlatformModels)Enum.Parse(typeof(PlatformModels), this.ModelName);
            switch (Modeltype0)
            {
                case PlatformModels.DKP_400:
                    return new DKP_400();
                case PlatformModels.M8DM1_M9DM1:
                    return new M8DM1_M9DM1();
                case PlatformModels.SMS_P:
                    return new SMS_P();
                case PlatformModels.WoodMove:
                    return new WoodMove();
                case PlatformModels.Yancheng_P:
                    return new Yancheng_P();
                case PlatformModels.YIMO_P:
                    return new YIMO_P();
                case PlatformModels.Revolving:
                    return new Revolving();
                case PlatformModels.JiShi:
                    return new JiShi();
                case PlatformModels.XAUAT_P:
                    return new XAUAT_P();
                case PlatformModels.RP22P920600:
                    return new RP22P920600();
                default:
                    return null;
            }
        }
        public static List<int> PlatformJoints()
        {
            int[] joints = new int[10]
            {
                1,1,2,1,2,1,1,1,1,1
            };
            return joints.ToList();
        }
    }

    public class RP22P920600 : PlatformModelAll
    {
        public RP22P920600() : base("RP22P920600", Manufacturers.KUKA, 400, 2, true) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 0, double.MinValue, double.MaxValue, 94.5);
            //jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 0, -6.248279, 6.248279, 94.5);
            //jointProperties[0] = new JointProperties(JointType.Revolute, 7, 0, 0, -358.0, 358.0, 94.5);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            return null;
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
    public class Revolving : PlatformModelAll
    {
        public Revolving() : base("Revolving", Manufacturers.KUKA, 400, 1, true) { }
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
    public class JiShi : PlatformModelAll
    {
        public JiShi() : base("JiShi", Manufacturers.KUKA, 400, 2, true) { }
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

    public class XAUAT_P : PlatformModelAll
    {
        public XAUAT_P() : base("XAUAT_P", Manufacturers.Other, 400, 2, true) { }
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
}
