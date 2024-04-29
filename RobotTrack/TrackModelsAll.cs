using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class TrackModelsAll : TrackProperties
    {
        public TrackModelsAll(string modelname, Manufacturers manufacturers, double payload, bool movesrobot, bool trackVertical) : base(modelname, manufacturers, payload, movesrobot, trackVertical) { }
        protected override LoadModel SetLoadModel() => new LoadTrack(Manufacturers, ModelName, JointCount);
    }
    public class LoadTrack : LoadModel
    {
        public LoadTrack(Manufacturers manufacturers, string modelname, int jointcount) : base(manufacturers, ModelType.Track, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotTrack.Properties.Resources.ResourceManager.GetString($"Track_{modelname}_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
        protected override Mesh[] LoadConvexHullModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotTrack.Properties.Resources.ResourceManager.GetString($"Track_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });

            return meshes;
        }
    }
    public class ModelSystemTrack : ModelSystem
    {
        public ModelSystemTrack(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype0 = (TrackModels)Enum.Parse(typeof(TrackModels), this.ModelName);
            switch (Modeltype0)
            {
                /*case TrackModels.AFN:
                    return new AFN();
                case TrackModels.Dolomiti:
                    return new Dolomiti();
                case TrackModels.JiadingWelding:
                    return new JiadingWelding();
                case TrackModels.JiaShan:
                    return new JiaShan();
                case TrackModels.KL1000_2:
                    return new KL1000_2();
                case TrackModels.M7DM1:
                    return new M7DM1();
                case TrackModels.SMS:
                    return new SMS();
                case TrackModels.Xijiao:
                    return new Xijiao();
                case TrackModels.Yancheng:
                    return new Yancheng();
                case TrackModels.YIMO:
                    return new YIMO();
                case TrackModels.Shandongluqiao:
                    return new Shandongluqiao();
                case TrackModels.Capsule:
                    return new Capsule();
                case TrackModels.JiadingTimber:
                    return new JiadingTimber();
                case TrackModels.ZJU_Truss:
                    return new ZJU_Truss();
                case TrackModels.ProductWelding:
                    return new ProductWelding();
                case TrackModels.SteelProductWelding:
                    return new SteelProductWelding();
                case TrackModels.XAUAT:
                    return new XAUAT();
                case TrackModels.SteelWeldOnsiteTest:
                    return new SteelWeldOnsiteTest();
                case TrackModels.SteelWeldOnsiteUpdate:
                    return new SteelWeldOnsiteUpdate();*/
                case TrackModels.TJU5000:
                    return new TJU5000();
                case TrackModels.TJU3400:
                    return new TJU3400();
                case TrackModels.SWJTs4800:
                    return new SWJTs4800();
                case TrackModels.ZJUs6000:
                    return new ZJUs6000();
                case TrackModels.ZJUv2000:
                    return new ZJUv2000();
                case TrackModels.ZJUg6000L:
                    return new ZJUg6000L();
                case TrackModels.ZJUg6000R:
                    return new ZJUg6000R();
                default:
                    return null;
            }
        }
        public static List<int> TrackJoints()
        {
            int[] joints = new int[19]
            {
                1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1
            };
            return joints.ToList();
        }
    }
    /*
    public class JiaShan : TrackModelsAll
    {
        public JiaShan() : base("JiaShan", Manufacturers.KUKA, 1000, true, false) { }
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
        public SMS() : base("SMS", Manufacturers.KUKA, 1000, true, false) { }
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
        public M7DM1() : base("M7DM1", Manufacturers.ABB, 1000, true, false) { }
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
        public YIMO() : base("YIMO", Manufacturers.KUKA, 1000, true, false) { }
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
        public KL1000_2() : base("KL1000_2", Manufacturers.KUKA, 1000, true, false) { }
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
        public Yancheng() : base("Yancheng", Manufacturers.KUKA, 1000, true, false) { }
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
        public JiadingWelding() : base("JiadingWelding", Manufacturers.KUKA, 1000, true, false) { }
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
    public class ProductWelding : TrackModelsAll
    {
        public ProductWelding() : base("ProductWelding", Manufacturers.KUKA, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            //jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -12260, 0, 1890);
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -2400, 2100, 1890);
            return jointProperties;
        }
    }
    public class Xijiao : TrackModelsAll
    {
        public Xijiao() : base("Xijiao", Manufacturers.UR, 1000, true, false) { }
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
        public AFN() : base("AFN", Manufacturers.KUKA, 1000, true, false) { }
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
        public Dolomiti() : base("Dolomiti", Manufacturers.KUKA, 1000, true, false) { }
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
    public class Shandongluqiao : TrackModelsAll
    {
        public Shandongluqiao() : base("Shandongluqiao", Manufacturers.FANUC, 1000, true) { }
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
    */
    //public class SteelProductWelding : TrackModelsAll
    //{
    //    public SteelProductWelding() : base("SteelProductWelding", Manufacturers.KUKA, 1000, true, false) { }
    //    protected override IOProperties SetIOProperties()
    //    {
    //        return null;
    //    }
    //    protected override JointProperties[] SetJointProperties()
    //    {
    //        JointProperties[] jointProperties = new JointProperties[1];
    //        jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -6800, 6800, 1890);
    //        return jointProperties;
    //    }
    //}
    /*
    public class Capsule : TrackModelsAll
    {
        public Capsule() : base("Capsule", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -3500, 0, 1890);
            return jointProperties;
        }
    }
    public class JiadingTimber : TrackModelsAll
    {
        public JiadingTimber() : base("JiadingTimber", Manufacturers.KUKA, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -4500, 0, 1890);
            return jointProperties;
        }
    }

    public class ZJU_Truss : TrackModelsAll
    {
        public ZJU_Truss() : base("ZJU_Truss", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 0, 5550, 1890);
            return jointProperties;
        }
    }

    public class SteelProductWelding : TrackModelsAll
    {
        public SteelProductWelding() : base("SteelProductWelding", Manufacturers.KUKA, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            //jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -12260, 0, 1890);        old model
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -6800, 6800, 1890);
            return jointProperties;
        }
    }

    public class XAUAT : TrackModelsAll
    {
        public XAUAT() : base("XAUAT", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -1995, 1410, 1890);
            return jointProperties;
        }
    }

    public class SteelWeldOnsiteTest : TrackModelsAll
    {
        public SteelWeldOnsiteTest() : base("SteelWeldOnsiteTest", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -5000, 5000, 1890);
            return jointProperties;
        }
    }

    public class SteelWeldOnsiteUpdate : TrackModelsAll
    {
        public SteelWeldOnsiteUpdate() : base("SteelWeldOnsiteUpdate", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -5500, 5500, 1890);
            return jointProperties;
        }
    }
    */

    public class ZJUv2000 : TrackModelsAll
    {
        public ZJUv2000() : base("ZJUv2000", Manufacturers.RoboticPlus, 1000, true, true) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 0, 2000, 1890);
            return jointProperties;
        }
    }

    public class ZJUs6000 : TrackModelsAll
    {
        public ZJUs6000() : base("ZJUs6000", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 0, 6000, 1890);
            return jointProperties;
        }
    }

    public class ZJUg6000L : TrackModelsAll
    {
        public ZJUg6000L() : base("ZJUg6000L", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, -3000, 0, 1890);
            return jointProperties;
        }
    }

    public class ZJUg6000R : TrackModelsAll
    {
        public ZJUg6000R() : base("ZJUg6000R", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 0, 3000, 1890);
            return jointProperties;
        }
    }


    public class TJU5000 : TrackModelsAll
    {
        public TJU5000() : base("TJU5000", Manufacturers.RoboticPlus, 1000, true, false) { }
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

    public class TJU3400 : TrackModelsAll
    {
        public TJU3400() : base("TJU3400", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 0, 3400, 1890);
            return jointProperties;
        }
    }
    public class SWJTs4800 : TrackModelsAll
    {
        public SWJTs4800() : base("SWJTs4800", Manufacturers.RoboticPlus, 1000, true, false) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Prismatic, 7, 0, 0, 0, 4800, 1890);
            return jointProperties;
        }
    }
}
