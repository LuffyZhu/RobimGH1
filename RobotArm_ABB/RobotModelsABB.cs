using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace RobimRobots
{
    public abstract class RobotModelsABB : RobotProperties
    {
        public RobotModelsABB(string modelname, double payload, int group) : base(modelname, Manufacturers.ABB, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotABB(ModelName, JointCount);
    }
    public class LoadRobotABB : LoadModel
    {
        public LoadRobotABB(string modelname, int jointcount) : base(Manufacturers.ABB, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_ABB.Properties.Resources.ResourceManager.GetString($"ABB_{modelname}_{i}");
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
                string meshbyte = RobotArm_ABB.Properties.Resources.ResourceManager.GetString($"ABB_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });

            return meshes;
        }
    }
    public class ModelSystemABB : ModelSystem
    {
        public ModelSystemABB(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype0 = (ABBRobots)Enum.Parse(typeof(ABBRobots), this.ModelName);
            switch (Modeltype0)
            {
                case ABBRobots.IRB6700_320_150:
                    return new IRB6700_320_150();
                case ABBRobots.IRB120:
                    return new IRB120();
                case ABBRobots.IRB1600_145:
                    return new IRB1600_145();
                case ABBRobots.ABB_4600:
                    return new ABB_4600();
                default:
                    return null;
            }
        }
    }
    public class IRB6700_320_150:RobotModelsABB
    {
        public IRB6700_320_150() : base("IRB6700_320_150", 235, 0) { }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 320, 780, -180, 180, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1280, 0, -110, 110, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 200, 0, -162, 270, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1592.5, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 200, -450, 450, 175);
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
    public class IRB120 : RobotModelsABB
    {
        public IRB120() : base("IRB120", 3, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 0, 290, -165, 165, 250);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 270, 0, -110, 110, 250);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 70, 0, -110, 70, 250);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 302, -160, 160, 320);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -120, 120, 320);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 72, -400, 400, 420);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "DO10_1,DO10_2,DO10_3,DO10_4,DO10_5,DO10_6,DO10_7,DO10_8,DO10_9,DO10_10,DO10_11,DO10_12,DO10_13,DO10_14,DO10_15,DO10_16";
            string DInames = "DI10_1,DI10_2,DI10_3,DI10_4,DI10_5,DI10_6,DI10_7,DI10_8,DI10_9,DI10_10,DI10_11,DI10_12,DI10_13,DI10_14,DI10_15,DI10_16";
            string AOnames = "";
            string AInames = "";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class IRB1600_145 : RobotModelsABB
    {
        public IRB1600_145() : base("IRB1600_145", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 486.5, -180, 180, 180);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 700, 0, -90, 150, 180);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 0, 0, -245, 65, 185);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 600, -200, 200, 385);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -115, 115, 400);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 65, -400, 400, 460);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "DO10_1,DO10_2,DO10_3,DO10_4,DO10_5,DO10_6,DO10_7,DO10_8,DO10_9,DO10_10,DO10_11,DO10_12,DO10_13,DO10_14,DO10_15,DO10_16";
            string DInames = "DI10_1,DI10_2,DI10_3,DI10_4,DI10_5,DI10_6,DI10_7,DI10_8,DI10_9,DI10_10,DI10_11,DI10_12,DI10_13,DI10_14,DI10_15,DI10_16";
            string AOnames = "AO10_1,AO10_2";
            string AInames = "AI10_1,AI10_2";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }

    public class ABB_4600 : RobotModelsABB
    {
        public ABB_4600() : base("ABB_4600", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 175, 495, -180, 180, 180);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1095, 0, -90, 150, 180);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 175, 0, -180, 75, 185);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1230.5, -400, 400, 385);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 120, 400);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 85, -400, 400, 460);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "DO10_1,DO10_2,DO10_3,DO10_4,DO10_5,DO10_6,DO10_7,DO10_8,DO10_9,DO10_10,DO10_11,DO10_12,DO10_13,DO10_14,DO10_15,DO10_16";
            string DInames = "DI10_1,DI10_2,DI10_3,DI10_4,DI10_5,DI10_6,DI10_7,DI10_8,DI10_9,DI10_10,DI10_11,DI10_12,DI10_13,DI10_14,DI10_15,DI10_16";
            string AOnames = "AO10_1,AO10_2";
            string AInames = "AI10_1,AI10_2";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
}
