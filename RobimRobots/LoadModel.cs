using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RobimRobots
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
}
