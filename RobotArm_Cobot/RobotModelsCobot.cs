using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RobimRobots
{
    public abstract class RobotModelsCobot : RobotProperties
    {
        public RobotModelsCobot(string modelname, double payload, int group) : base(modelname, Manufacturers.Cobot, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotCobot(ModelName, JointCount);
    }
    public class LoadRobotCobot : LoadModel
    {
        public LoadRobotCobot(string modelname, int jointcount) : base(Manufacturers.Cobot, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_Cobot.Properties.Resources.ResourceManager.GetString($"Cobot_{modelname}_{i}");
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
                string meshbyte = RobotArm_Cobot.Properties.Resources.ResourceManager.GetString($"Cobot_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemCobot : ModelSystem
    {
        public ModelSystemCobot(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype6 = (Cobot)Enum.Parse(typeof(Cobot), this.ModelName);
            switch (Modeltype6)
            {
                case Cobot.Cobot1:
                    return new Cobot1();
                default:
                    return null;
            }
        }
    }
    public class Cobot1 : RobotModelsCobot
    {
        public Cobot1() : base("Cobot1", 1, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            //jointProperties[0] = new JointProperties(JointType.Revolute, 1, 32.56, 70, -360, 360, 120);
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 32.56, 3, -360, 360, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 61.56, 32, -360, 360, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 110.4, 4, -360, 360, 180);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 96, 5, -360, 360, 180);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 6, 48, -360, 360, 180);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 7, 47, -360, 360, 180);
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
