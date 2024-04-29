using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Robim
{
    public abstract class LoadModel
    {
        public Manufacturers Manufacturers { get; }
        public ModelType ModelType { get; }
        public string ModelName { get; }
        public int JointCount { get; }
        public LoadModel(Manufacturers manufacturers,ModelType modelType,string modelname,int jointcount)
        {
            Manufacturers = manufacturers;
            ModelType = modelType;
            ModelName = modelname;
            JointCount = jointcount;
        }
        public Mesh[] ShowModel()
        {
            return LoadOriginModel(Manufacturers, ModelName, JointCount);
        }
        public Mesh[] GetConvexHull()
        {
            return LoadConvexHullModel(Manufacturers, ModelName, JointCount);
        }
        protected abstract Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount);
        protected abstract Mesh[] LoadConvexHullModel(Manufacturers manufacturers, string modelname, int jointcount);
    }

    public class LoadRobot : LoadModel
    {
        public LoadRobot(Manufacturers manufacturers,string modelname, int jointcount) : base(manufacturers, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            Parallel.For(0,jointcount, (i) =>
            {
                string meshbyte = Properties.Resources.ResourceManager.GetString($"{manufacturers}_{modelname}_{i}");
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
                string meshbyte = Properties.Resources.ResourceManager.GetString($"{manufacturers}_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });

            return meshes;
        }
    }
    
    public class LoadPlatform : LoadModel
    {
        public LoadPlatform(Manufacturers manufacturers, string modelname, int jointcount) : base(manufacturers, ModelType.Platform, modelname, jointcount) { }
        protected override Mesh[] LoadConvexHullModel(Manufacturers manufacturers, string modelname, int jointcount)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];
            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = Properties.Resources.ResourceManager.GetString($"Platform_{modelname}_CH_{i}");
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
                string meshbyte = Properties.Resources.ResourceManager.GetString($"Platform_{modelname}_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
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
                string meshbyte =  Properties.Resources.ResourceManager.GetString($"Tool_{modelname}_CH_{i}");
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
                string meshbyte = Properties.Resources.ResourceManager.GetString($"Tool_{modelname}_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
}
