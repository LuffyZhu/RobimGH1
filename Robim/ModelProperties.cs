using System;
using System.Collections.Generic;
using System.Linq;

namespace Robim
{
    public enum Manufacturers { ABB, KUKA, UR, FANUC, Staubli, Aubo, Estun, RoboticPlus, Other, All }
    public enum ModelType { Robot, Track, Platform, Tool, Custom }
    public enum JointType { Revolute, Prismatic }
    public enum ABBRobots { IRB6700_320_150, IRB120, IRB1600_145 }

    public enum TrackModels { JiaShan, SMS, M7DM1, YIMO, KL1000_2, Yancheng, JiadingWelding, Xijiao, AFN, Dolomiti }

    public abstract class ModelProperties
    {
        public string ModelName { get; }
        public Manufacturers Manufacturers { get; }
        public double Payload { get; }
        public ModelType ModelType { get; }
        public BaseProperties BaseProperties { get; }
        public JointProperties[] JointProperties { get; }
        public int JointCount { get; }
        public IOProperties IOProperties { get; }
        public LoadModel LoadModel {get;}
        public ModelProperties(string modelname,Manufacturers manufacturers,double payload,ModelType modelType,BaseProperties baseProperties = null)
        {
            this.ModelName = modelname;
            this.Manufacturers = manufacturers;
            this.Payload = payload;
            this.ModelType = modelType;
            if (baseProperties == null)
                this.BaseProperties = new BaseProperties(0, 0, 0, 1, 0, 0, 0);
            else
                this.BaseProperties = baseProperties;
            this.JointProperties = SetJointProperties();
            this.JointCount = JointProperties.Length;
            this.IOProperties = SetIOProperties();
            this.LoadModel = SetLoadModel();
        }
        protected abstract LoadModel SetLoadModel();
        protected abstract JointProperties[] SetJointProperties();
        protected abstract IOProperties SetIOProperties();
    }
    public abstract class RobotProperties : ModelProperties
    {
        public int Group { get;}
        public RobotProperties(string modelname, Manufacturers manufacturers, double payload,int group) : base(modelname, manufacturers, payload,ModelType.Robot)
        {
            this.Group = group;
        }
        protected override LoadModel SetLoadModel() => new LoadRobot(Manufacturers, ModelName, JointCount);
    }
    public abstract class TrackProperties :ModelProperties
    {
        public bool MovesRobot { get; }
        public TrackProperties (string modelname, Manufacturers manufacturers, double payload, bool movesrobot) : base(modelname, manufacturers, payload,ModelType.Track)
        {
            this.MovesRobot = movesrobot;
        }
        protected override LoadModel SetLoadModel() => new LoadTrack(Manufacturers, ModelName, JointCount);
    }
    public abstract class PlatformProperties:ModelProperties
    {
        public double Type { get; }
        public bool IsRevolve { get; }
        public PlatformProperties(string modelname, Manufacturers manufacturers, double payload,double type, bool isrevolve) : base(modelname, manufacturers, payload,ModelType.Platform)
        {
            this.Type = type;
            this.IsRevolve = isrevolve;
        }
        protected override LoadModel SetLoadModel() => new LoadPlatform(Manufacturers, ModelName, JointCount);
    }
    public abstract class ToolProperties : ModelProperties
    {
        public double Weight { get; }
        public BaseProperties TCP { get; }
        public ToolProperties(string modelname, Manufacturers manufacturers,BaseProperties tcp,double weight) : base(modelname, manufacturers, 0, ModelType.Tool, tcp)
        {
            this.Weight = weight;
            this.TCP = tcp;
        }

        protected override LoadModel SetLoadModel() => new LoadTool(Manufacturers, ModelName, JointCount);
    }
    public class BaseProperties
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Q1 { get; }
        public double Q2 { get; }
        public double Q3 { get; }
        public double Q4 { get; }
        public BaseProperties(double x, double y, double z, double q1, double q2, double q3, double q4)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Q1 = q1;
            this.Q2 = q2;
            this.Q3 = q3;
            this.Q4 = q4;
        }
    }
    public class JointProperties
    {
        public JointType JointType { get; }
        public int JointNumber { get; }
        public double A { get; }
        public double D { get; }
        public double MinRange { get; }
        public double MaxRange { get; }
        public double MaxSpeed { get; }
        public JointProperties(JointType jointType,int jointnumber,double a,double d,double minrange,double maxrange,double maxspeed)
        {
            JointType = jointType;
            JointNumber = jointnumber;
            A = a;
            D = d;
            MinRange = minrange;
            MaxRange = maxrange;
            MaxSpeed = maxspeed;
        }
    }
    public class IOProperties
    {
        public string[] DO { get; }
        public string[] DI { get; }
        public string[] AO { get; }
        public string[] AI { get; }
        public IOProperties(string DOnames,string DInames,string AOnames,string AInames)
        {
            this.DO = DOnames.Split(',');
            this.DI = DInames.Split(',');
            this.AO = AOnames.Split(',');
            this.AI = AInames.Split(',');
        }
    }
    public abstract class ModelSystem
    {
        public string ModelName;
            
        public ModelSystem(string modelname)
        {
            ModelName = modelname;
        }
        public static List<string> RobotNames()
        {
            List<string> Names = new List<string>();
            Names.AddRange(Enum.GetNames(typeof(ABBRobots)).Select(x => $"ABB.{x}").ToList());
            Names.AddRange(RobotModelsAubo.GetNames);
            Names.AddRange(RobotModelsEstun.GetNames);
            Names.AddRange(RobotModelsFanuc.GetNames);
            Names.AddRange(RobotModelsKUKA.GetNames);
            Names.AddRange(RobotModelsStaubli.GetNames);
            Names.AddRange(RobotModelsUR.GetNames);
            return Names;
        }
        public static List<string> TrackNames(ref List<int> jointcounts)
        {
            List<string> Names = new List<string>();
            Names.AddRange(Enum.GetNames(typeof(TrackModels)).Select(x => $"{x}").ToList());
            int[] joints = new int[10]
            {
                1,1,1,1,1,1,1,1,1,1
            };
            jointcounts = joints.ToList();
            return Names;
        }
        public static List<string> PlatformNames(ref List<int> jointcounts)
        {
            List<string> Names = new List<string>();
            Names.AddRange(PlatformModelAll.GetNames);
            jointcounts = PlatformModelAll.GetJoint();
            return Names;
        }
        public static List<string> ToolNames()
        {
            List<string> Names = new List<string>();
            Names.AddRange(ToolModelsAll.GetNames);
            return Names;
        }
        public abstract ModelProperties GetModelProperties();
        public ModelProperties GetArmProperties(Manufacturers manufacturers)
        {
            switch (manufacturers)
            {
                case Manufacturers.Aubo:
                    var Modeltype1 = (AuboRobots)Enum.Parse(typeof(AuboRobots), this.ModelName);
                    switch (Modeltype1)
                    {
                        case AuboRobots.Aubo10:
                            return new Aubo10();
                        default:
                            return null;
                    }
                case Manufacturers.Estun:
                    var Modeltype2 = (EstunRobots)Enum.Parse(typeof(EstunRobots), this.ModelName);
                    switch (Modeltype2)
                    {
                        case EstunRobots.ER16:
                            return new ER16();
                        case EstunRobots.ER170_2650:
                            return new ER170_2650();
                        default:
                            return null;
                    }
                case Manufacturers.FANUC:
                    var Modeltype3 = (FanucRobots)Enum.Parse(typeof(FanucRobots), this.ModelName);
                    switch (Modeltype3)
                    {
                        case FanucRobots.M_20ia:
                            return new M_20ia();
                        case FanucRobots.M_710ic20l:
                            return new M_710ic20l();
                        case FanucRobots.M_710ic50:
                            return new M_710ic50();
                        case FanucRobots.R_2000ic125L:
                            return new R_2000ic125L();
                        case FanucRobots.R_2000ic210F:
                            return new R_2000ic210F();
                        case FanucRobots.R_2000ic270f:
                            return new R_2000ic270f();
                        default:
                            return null;
                    }
                case Manufacturers.KUKA:
                    var Modeltype4 = (KUKARobots)Enum.Parse(typeof(KUKARobots), this.ModelName);
                    switch (Modeltype4)
                    {
                        case KUKARobots.KR10_R1100:
                            return new KR10_R1100();
                        case KUKARobots.KR180_R2500_extra:
                            return new KR180_R2500_extra();
                        case KUKARobots.KR20_R1810:
                            return new KR20_R1810();
                        case KUKARobots.KR210_R2700_ultra:
                            return new KR210_R2700_ultra();
                        case KUKARobots.KR22_R1610:
                            return new KR22_R1610();
                        case KUKARobots.KR500_2:
                            return new KR500_2();
                        case KUKARobots.KR60_HA:
                            return new KR60_HA();
                        case KUKARobots.KR6_R900:
                            return new KR6_R900();
                        case KUKARobots.KR90s_Special:
                            return new KR90s_Special();
                        case KUKARobots.KR90_R3100_Extra:
                            return new KR90_R3100_Extra();
                        default:
                            return null;
                    }
                case Manufacturers.Staubli:
                    var Modeltype5 = (StaubliRobots)Enum.Parse(typeof(StaubliRobots), this.ModelName);
                    switch (Modeltype5)
                    {
                        case StaubliRobots.RX160:
                            return new RX160();
                        case StaubliRobots.RX160L:
                            return new RX160L();
                        case StaubliRobots.TX200:
                            return new TX200();
                        case StaubliRobots.TX200L:
                            return new TX200L();
                        default:
                            return null;
                    }
                case Manufacturers.UR:
                    var Modeltype6 = (URRobots)Enum.Parse(typeof(URRobots), this.ModelName);
                    switch (Modeltype6)
                    {
                        case URRobots.UR10:
                            return new UR10();
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }
        public ModelProperties GetPlatformProperties()
        {
            var Modeltype0 = (PlatformModels)Enum.Parse(typeof(PlatformModels), this.ModelName);
            switch (Modeltype0)
            {
                case PlatformModels.DKP_400:
                    return new DKP_400();
                case PlatformModels.M8DM1_M9DM1:
                    return new M8DM1_M9DM1();
                case PlatformModels.SMS_P:
                    return new SMS_P();
                case PlatformModels.WoodMove:
                    return new WoodMove();
                case PlatformModels.Yancheng_P:
                    return new Yancheng_P();
                case PlatformModels.YIMO_P:
                    return new YIMO_P();
                default:
                    return null;
            }
        }
        public ModelProperties GetToolProperties()
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
}
