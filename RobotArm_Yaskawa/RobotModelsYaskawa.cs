using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class RobotModelsYaskawa : RobotProperties
    {
        public RobotModelsYaskawa(string modelname, double payload, int group) : base(modelname, Manufacturers.Yaskawa, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotYaskawa(ModelName, JointCount);
    }
    public class LoadRobotYaskawa : LoadModel
    {
        public LoadRobotYaskawa(string modelname, int jointcount) : base(Manufacturers.Yaskawa, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_Yaskawa.Properties.Resources.ResourceManager.GetString($"YASKAWA_{modelname}_{i}");
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
                string meshbyte = RobotArm_Yaskawa.Properties.Resources.ResourceManager.GetString($"YASKAWA_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemYaskawa : ModelSystem
    {
        public ModelSystemYaskawa(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype9 = (Yaskawa)Enum.Parse(typeof(Yaskawa), this.ModelName);
            switch (Modeltype9)
            {
                case Yaskawa.GP20HL_ASSY_ASM:
                return new GP20HL_ASSY_ASM();
                default:
                    return null;
            }
        }
    }

    public class GP20HL_ASSY_ASM : RobotModelsYaskawa
    {
        public GP20HL_ASSY_ASM() : base("GP20HL_ASSY_ASM", 125, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 145, 540, -180, 180, 180); //d
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -90, 135, 180); //d
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 250, 0, -80, 206, 180); //d
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1812, -200, 200, 400); //d
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -150, 150, 430);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -455, 455, 630); 
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
