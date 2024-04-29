using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public enum ToolModels { CircularSaw , CircularSaw2 }
    public abstract class ToolModelsAll : ToolProperties
    {
        public ToolModelsAll(string modelname, Manufacturers manufacturers, BaseProperties tcp ,double weight) : base(modelname, manufacturers, tcp, weight) { }
        public static List<string> GetNames => Enum.GetNames(typeof(ToolModels)).Select(x => $"{x}").ToList();
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
