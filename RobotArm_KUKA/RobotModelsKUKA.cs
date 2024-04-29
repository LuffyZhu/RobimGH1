using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class RobotModelsKUKA : RobotProperties
    {
        public RobotModelsKUKA(string modelname, double payload, int group) : base(modelname, Manufacturers.KUKA, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotKUKA(ModelName, JointCount);
    }
    public class LoadRobotKUKA : LoadModel
    {
        public LoadRobotKUKA(string modelname, int jointcount) : base(Manufacturers.KUKA, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_KUKA.Properties.Resources.ResourceManager.GetString($"KUKA_{modelname}_{i}");
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
                string meshbyte = RobotArm_KUKA.Properties.Resources.ResourceManager.GetString($"KUKA_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemKUKA : ModelSystem
    {
        public ModelSystemKUKA(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype4 = (KUKARobots)Enum.Parse(typeof(KUKARobots), this.ModelName);
            switch (Modeltype4)
            {
                case KUKARobots.KR10_R1100:
                    return new KR10_R1100();
                case KUKARobots.KR10_R1100_2:
                    return new KR10_R1100_2();
                case KUKARobots.KR180_R2500_Extra:
                    return new KR180_R2500_Extra();
                case KUKARobots.KR20_R1810:
                    return new KR20_R1810();
                case KUKARobots.KR210_R2700_Extra:
                    return new KR210_R2700_Extra();
                case KUKARobots.KR22_R1610:
                    return new KR22_R1610();
                case KUKARobots.KR500_2:
                    return new KR500_2();
                case KUKARobots.KR60_HA:
                    return new KR60_HA();
                case KUKARobots.KR6_R900:
                    return new KR6_R900();
                case KUKARobots.KR90_R3100_Extra:
                    return new KR90_R3100_Extra();
                case KUKARobots.KR8_R2100_2_arc_HW:
                    return new KR8_R2100_2_arc_HW();
                case KUKARobots.KR210_R2700_2:
                    return new KR210_R2700_2();
                case KUKARobots.KR360_R2830:
                    return new KR360_R2830();
                case KUKARobots.KR70_R2100:
                    return new KR70_R2100();
                case KUKARobots.KR_20_R3100:
                    return new KR_20_R3100();
                case KUKARobots.KR120_2700_Extra:
                    return new KR120_2700_Extra();
                case KUKARobots.KR300_R2700:
                    return new KR300_R2700();
                case KUKARobots.KR16_2C:
                    return new KR16_2C();
                default:
                    return null;
            }
        }
    }

    public class KR300_R2700 : RobotModelsKUKA
    {
        public KR300_R2700() : base("KR300_R2700", 370, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 330, 645, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -140, -5, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 115, 0, -120, 168, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1220, -350, 350, 140);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -122, 122, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 240, -350, 350, 180);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string AOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string AInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }


    public class KR120_2700_Extra : RobotModelsKUKA
    {
        public KR120_2700_Extra() : base("KR120_2700_Extra", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 350, 675, -185, 185, 123);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -140, -5, 115);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -41, 0, -120, 155, 112);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1200, -350, 350, 179);
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


    public class KR210_R2700_Extra : RobotModelsKUKA
    {
        public KR210_R2700_Extra() : base("KR210_R2700_Extra", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 350, 675, -185, 185, 123);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -140, -5, 115);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -41, 0, -120, 155, 112);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1200, -350, 350, 179);
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
    #region No model,No use
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
    #endregion
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
            string DOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string AOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string AInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class KR10_R1100 : RobotModelsKUKA
    {
        public KR10_R1100() : base("KR10_R1100", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 25, 400, -170, 170, 128);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 560, 0, -190, 45, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 35, 0, -120, 156, 128);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 515, -185, 185, 260);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -120, 120, 245);
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

    /// <summary>
    /// Create Date : 2021/09/24
    /// Description : KR10_R1100_2
    /// </summary>
    public class KR10_R1100_2 : RobotModelsKUKA
    {
        public KR10_R1100_2() : base("KR10_R1100_2", 11.1, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 25, 400, -170, 170, 128);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 560, 0, -190, 45, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 25, 0, -120, 156, 128);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 515, -185, 185, 260);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -120, 120, 245);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 90, -350, 350, 322);
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
    public class KR180_R2500_Extra : RobotModelsKUKA
    {
        public KR180_R2500_Extra() : base("KR180_R2500_extra", 180, 0) { }

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
    public class KR8_R2100_2_arc_HW : RobotModelsKUKA
    {
        public KR8_R2100_2_arc_HW() : base("KR8_R2100_2_arc_HW", 9.3, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 160, 520, -185, 185, 200);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 980, 0, -185, 65, 175);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 220, 380, -138, 175, 190);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 934.5, -165, 165, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -115, 140, 430);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 80.5, -350, 350, 630);
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
    public class KR210_R2700_2 : RobotModelsKUKA
    {
        public KR210_R2700_2() : base("KR210_R2700_2", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 330, 645, -185, 185, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -140, -5, 115);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 115, 0, -120, 168, 112);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1220, -350, 350, 179);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 172);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -350, 350, 220);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string DInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string AOnames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            string AInames = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class KR360_R2830 : RobotModelsKUKA
    {
        public KR360_R2830() : base("KR360_R2830", 360, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 500, 1045, -185, 185, 100);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1300, 0, -130, 20, 90);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -55, 0, -100, 144, 90);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1025, -350, 350, 120);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -120, 120, 110);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 290, -350, 350, 160);
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

    public class KR70_R2100 : RobotModelsKUKA
    {
        public KR70_R2100() : base("KR70_R2100", 85, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 175, 575, -185, 185, 180);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 890, 0, -175, 60, 158);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 50, 0, -120, 165, 160);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1035, -180, 180, 230);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 230);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 185, -350, 350, 320);
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

    public class KR_20_R3100 : RobotModelsKUKA //all
    {
        public KR_20_R3100() : base("KR_20_R3100", 85, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 175, 575, -185, 185, 180);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1290, 0, -175, 60, 165);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 50, 0, -120, 170, 160);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1635, -350, 350, 360);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -130, 130, 360);
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

    public class KR16_2C : RobotModelsKUKA //all
    {
        public KR16_2C() : base("KR16_2C", 85, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 160, 520, -185, 185, 180); //
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 780, 0, -175, 60, 165);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 150, 0, -120, 170, 160);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 655, -350, 350, 360);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -130, 130, 360);
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
}