using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class RobotModelsUR : RobotProperties
    {
        public RobotModelsUR(string modelname, double payload, int group) : base(modelname, Manufacturers.UR, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotUR(ModelName, JointCount);
    }
    public class LoadRobotUR : LoadModel
    {
        public LoadRobotUR(string modelname, int jointcount) : base(Manufacturers.UR, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_UR.Properties.Resources.ResourceManager.GetString($"UR_{modelname}_{i}");
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
                string meshbyte = RobotArm_UR.Properties.Resources.ResourceManager.GetString($"UR_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemUR : ModelSystem
    {
        public ModelSystemUR(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype6 = (URRobots)Enum.Parse(typeof(URRobots), this.ModelName);
            switch (Modeltype6)
            {
                case URRobots.UR10:
                    return new UR10();
                case URRobots.UR20:
                    return new UR20();
                default:
                    return null;
            }
        }
    }
    public class UR10 : RobotModelsUR
    {
        public UR10() : base("UR10", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 0, 127.3, -360, 360, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, -612, 0, -360, 360, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -572.3, 0, -360, 360, 180);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 163.941, -360, 360, 180);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 115.7, -360, 360, 180);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 92.2, -360, 360, 180);
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
    public class UR20 : RobotModelsUR
    {
        public UR20() : base("UR20", 20, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 0, 236.3, -360, 360, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, -862, 0, -360, 360, 120);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, -728.7, 0, -360, 360, 150);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 201, -360, 360, 210);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 159.3, -360, 360, 210);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 154.3, -360, 360, 210);
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
