using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class RobotModelsAubo:RobotProperties
    {
        public RobotModelsAubo(string modelname, double payload, int group) : base(modelname, Manufacturers.Aubo, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotAubo(ModelName, JointCount);
    }
    public class LoadRobotAubo : LoadModel
    {
        public LoadRobotAubo(string modelname, int jointcount) : base(Manufacturers.Aubo, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_Aubo.Properties.Resources.ResourceManager.GetString($"Aubo_{modelname}_{i}");
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
                string meshbyte = RobotArm_Aubo.Properties.Resources.ResourceManager.GetString($"Aubo_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemAubo : ModelSystem
    {
        public ModelSystemAubo(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype1 = (AuboRobots)Enum.Parse(typeof(AuboRobots), this.ModelName);
            switch (Modeltype1)
            {
                case AuboRobots.Aubo10:
                    return new Aubo10();
                default:
                    return null;
            }
        }
    }
    public class Aubo10 : RobotModelsAubo
    {
        public Aubo10() : base("Aubo10", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 0, 163.2, -175, 175, 180);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 632, 0, -175, 175, 180);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 600.5, 0, -175, 175, 150);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 201.3, -175, 175, 180);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 102.5, -175, 175, 180);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 94, -175, 175, 180);
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
