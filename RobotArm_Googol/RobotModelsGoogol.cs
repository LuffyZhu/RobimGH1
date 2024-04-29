using Grasshopper.Kernel;
using Grasshopper;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class RobotModelsGoogol : RobotProperties
    {
        public RobotModelsGoogol(string modelname, double payload, int group) : base(modelname, Manufacturers.Googol, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotGoogol(ModelName, JointCount);
    }
    public class LoadRobotGoogol : LoadModel
    {
        public LoadRobotGoogol(string modelname, int jointcount) : base(Manufacturers.Googol, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_Googol.Properties.Resources.ResourceManager.GetString($"Googol_{modelname}_{i}");
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
                string meshbyte = RobotArm_Googol.Properties.Resources.ResourceManager.GetString($"Googol_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemGoogol : ModelSystem
    {
        public ModelSystemGoogol(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype9 = (GoogolRobots)Enum.Parse(typeof(GoogolRobots), this.ModelName);
            switch (Modeltype9)
            {
                case GoogolRobots.ZK_1400_06:
                    return new ZK_1400_06();
                default:
                    return null;
            }
        }
    }
    public class ZK_1400_06 : RobotModelsGoogol
    {
        public ZK_1400_06() : base("ZK_1400_06", 125, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 100, 480, -180, 180, 176);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 580, 0, -70, 110, 173);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 170, 0, -120, 70, 170);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 670, -150, 150, 295);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -110, 105, 390);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 150, -320, 320, 307);  //130
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