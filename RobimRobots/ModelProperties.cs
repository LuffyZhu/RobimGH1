using System;
using System.Collections.Generic;
using System.Linq;

namespace RobimRobots
{
    public enum Manufacturers { ABB, KUKA, UR, FANUC, Staubli, Aubo, Estun, RoboticPlus, Googol, Yaskawa, Cobot, JAKA, Other, All }

    public enum ModelType { Robot, Track, Platform, Tool, Custom }
    public enum JointType { Revolute, Prismatic }
    public enum ABBRobots { IRB6700_320_150, IRB120, IRB1600_145, ABB_4600 }
    public enum AuboRobots { Aubo10 }
    public enum EstunRobots { ER170_2650, ER16 }
    public enum FanucRobots { M_710ic50, M_710ic20l, R_2000ic270f, M_20ia, R_2000ic125L, R_2000ic210F, M_10iD8L, M_10iD10L, M_10iD12, M_10iD16S, M_20iB25, M_20iB35S, M_20iD12L, M_20iD25, M_20iD35, R_2000iC_165F, M_10iA_8L }
    public enum GoogolRobots { ZK_1400_06 }
    public enum KUKARobots { KR210_R2700_Extra, KR90_R3100_Extra, KR6_R900, KR500_2, KR60_HA, KR20_R1810, KR10_R1100, KR10_R1100_2, KR22_R1610, KR180_R2500_Extra, KR8_R2100_2_arc_HW, KR210_R2700_2, KR360_R2830, KR70_R2100, KR_20_R3100, KR120_2700_Extra, KR300_R2700, KR16_2C }
    public enum StaubliRobots { TX200, TX200L, RX160, RX160L }
    public enum URRobots { UR10, UR20 }
    public enum Yaskawa { GP20HL_ASSY_ASM }

    public enum JAKA { Zu12 }
    public enum Cobot { Cobot1 }

    public enum TrackModels { TJU5000, TJU3400, SWJTs4800, ZJUs6000, ZJUv2000, ZJUg6000L, ZJUg6000R  /*JiaShan, SMS, M7DM1, YIMO, KL1000_2, Yancheng, JiadingWelding, Xijiao, AFN, Dolomiti, Shandongluqiao, Capsule, JiadingTimber, ZJU_Truss, ProductWelding, SteelProductWelding, XAUAT, SteelWeldOnsiteTest, SteelWeldOnsiteUpdate*/ }
    public enum PlatformModels { SMS_P, YIMO_P, DKP_400, Yancheng_P, M8DM1_M9DM1, WoodMove, Revolving, JiShi, XAUAT_P, RP22P920600 }
    public enum ToolModels { CircularSaw, CircularSaw2 }

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
        public LoadModel LoadModel { get; }
        public ModelProperties(string modelname, Manufacturers manufacturers, double payload, ModelType modelType, BaseProperties baseProperties = null)
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
        public int Group { get; }
        public RobotProperties(string modelname, Manufacturers manufacturers, double payload, int group) : base(modelname, manufacturers, payload, ModelType.Robot)
        {
            this.Group = group;
        }
        //protected override LoadModel SetLoadModel() => new LoadRobot(Manufacturers, ModelName, JointCount);
    }
    public abstract class TrackProperties : ModelProperties
    {
        public bool MovesRobot { get; }
        public bool TrackVertical { get; }
        public TrackProperties(string modelname, Manufacturers manufacturers, double payload, bool movesrobot, bool trackVertical) : base(modelname, manufacturers, payload, ModelType.Track)
        {
            this.MovesRobot = movesrobot;
            this.TrackVertical = trackVertical;
        }
    }
    public abstract class PlatformProperties : ModelProperties
    {
        public double Type { get; }
        public bool IsRevolve { get; }
        public PlatformProperties(string modelname, Manufacturers manufacturers, double payload, double type, bool isrevolve) : base(modelname, manufacturers, payload, ModelType.Platform)
        {
            this.Type = type;
            this.IsRevolve = isrevolve;
        }
        //protected override LoadModel SetLoadModel() => new LoadPlatform(Manufacturers, ModelName, JointCount);
    }
    public abstract class ToolProperties : ModelProperties
    {
        public double Weight { get; }
        public BaseProperties TCP { get; }
        public ToolProperties(string modelname, Manufacturers manufacturers, BaseProperties tcp, double weight) : base(modelname, manufacturers, 0, ModelType.Tool, tcp)
        {
            this.Weight = weight;
            this.TCP = tcp;
        }

        //protected override LoadModel SetLoadModel() => new LoadTool(Manufacturers, ModelName, JointCount);
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
        public JointProperties(JointType jointType, int jointnumber, double a, double d, double minrange, double maxrange, double maxspeed)
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
        public IOProperties(string DOnames, string DInames, string AOnames, string AInames)
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
            Names.AddRange(Enum.GetNames(typeof(AuboRobots)).Select(x => $"Aubo.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(EstunRobots)).Select(x => $"Estun.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(FanucRobots)).Select(x => $"FANUC.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(GoogolRobots)).Select(x => $"Googol.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(KUKARobots)).Select(x => $"KUKA.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(StaubliRobots)).Select(x => $"Staubli.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(URRobots)).Select(x => $"UR.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(Yaskawa)).Select(x => $"Yaskawa.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(Cobot)).Select(x => $"Cobot.{x}").ToList());
            Names.AddRange(Enum.GetNames(typeof(JAKA)).Select(x => $"JAKA.{x}").ToList());
            return Names;
        }
        public static List<string> TrackNames()
        {
            List<string> Names = new List<string>();
            Names.AddRange(Enum.GetNames(typeof(TrackModels)).Select(x => $"{x}").ToList());
            return Names;
        }
        public static List<string> PlatformNames()
        {
            List<string> Names = new List<string>();
            Names.AddRange(Enum.GetNames(typeof(PlatformModels)).Select(x => $"{x}").ToList());
            return Names;
        }
        public static List<string> ToolNames()
        {
            List<string> Names = new List<string>();
            Names.AddRange(Enum.GetNames(typeof(ToolModels)).Select(x => $"{x}").ToList());
            return Names;
        }
        public abstract ModelProperties GetModelProperties();
    }
}
