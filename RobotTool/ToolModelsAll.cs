using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class ToolModelsAll : ToolProperties
    {
        public ToolModelsAll(string modelname, Manufacturers manufacturers, BaseProperties tcp ,double weight) : base(modelname, manufacturers, tcp, weight) { }
        protected override LoadModel SetLoadModel() => new LoadTool(Manufacturers, ModelName, JointCount);
    }
    public class LoadTool : LoadModel
    {
        public LoadTool(Manufacturers manufacturers, string modelname, int jointcount) : base(manufacturers, ModelType.Tool, modelname, jointcount) { }
        protected override Mesh[] LoadConvexHullModel(Manufacturers manufacturers, string modelname, int jointcount)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];
            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotTool.Properties.Resources.ResourceManager.GetString($"Tool_{modelname}_CH_{i}");
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
                string meshbyte = RobotTool.Properties.Resources.ResourceManager.GetString($"Tool_{modelname}_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemTool : ModelSystem
    {
        public ModelSystemTool(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
            var Modeltype0 = (ToolModels)Enum.Parse(typeof(ToolModels), this.ModelName);
            switch (Modeltype0)
            {
                case ToolModels.CircularSaw:
                    return new CircularSaw();
                case ToolModels.CircularSaw2:
                    return new CircularSaw2();
                default:
                    return null;
            }
        }
    }
    public class CircularSaw : ToolModelsAll
    {
        public CircularSaw() : base("CircularSaw", Manufacturers.RoboticPlus, SetTCP(), 10) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            return new JointProperties[0];
        }
        static BaseProperties SetTCP()
        {
            return new BaseProperties(-404.972233, -0.071936, 241.308294, -0.499801, -0.499801, 0.500199, 0.500199);
        }
    }
    public class CircularSaw2 : ToolModelsAll
    {
        public CircularSaw2() : base("CircularSaw2", Manufacturers.RoboticPlus, SetTCP(), 10) { }
        protected override IOProperties SetIOProperties()
        {
            return null;
        }
        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[1];
            jointProperties[0] = new JointProperties(JointType.Revolute, 7, -271.272983, 240.839352, double.MinValue, double.MaxValue, 94.5);
            return jointProperties;
        }
        static BaseProperties SetTCP()
        {
            return new BaseProperties(-404.972233, -0.071936, 241.308294, -0.499801, -0.499801, 0.500199, 0.500199);
        }
    }
}
