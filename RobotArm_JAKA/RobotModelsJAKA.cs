using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RobimRobots
{
    public abstract class RobotModelsJAKA : RobotProperties
    {
        public RobotModelsJAKA(string modelname, double payload, int group) : base(modelname, Manufacturers.JAKA, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotJAKA(ModelName, JointCount);
    }
    public class LoadRobotJAKA : LoadModel
    {
        public LoadRobotJAKA(string modelname, int jointcount) : base(Manufacturers.JAKA, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_JAKA.Properties.Resources.ResourceManager.GetString($"JAKA_{modelname}_{i}");
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
                string meshbyte = RobotArm_JAKA.Properties.Resources.ResourceManager.GetString($"JAKA_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemJAKA : ModelSystem
    {
        public ModelSystemJAKA(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype6 = (JAKA)Enum.Parse(typeof(JAKA), this.ModelName);
            switch (Modeltype6)
            {
                case JAKA.Zu12:
                    return new Zu12();
                default:
                    return null;
            }
        }
    }
    public class Zu12 : RobotModelsJAKA
    {
        public Zu12() : base("Zu12", 12, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 0, 140.6, -270, 270, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, -595, 0, -85, 265, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -574.5, 0, -175, 175, 120);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 163.1, -85, 265, 180);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 112, -270, 270, 180);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100.5, -270, 270, 180);
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
