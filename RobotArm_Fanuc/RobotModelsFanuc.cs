using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobimRobots
{
    public abstract class RobotModelsFanuc : RobotProperties
    {
        public RobotModelsFanuc(string modelname, double payload, int group) : base(modelname, Manufacturers.FANUC, payload, group) { }
        protected override LoadModel SetLoadModel() => new LoadRobotFanuc(ModelName, JointCount);
    }
    public class LoadRobotFanuc : LoadModel
    {
        public LoadRobotFanuc(string modelname, int jointcount) : base(Manufacturers.FANUC, ModelType.Robot, modelname, jointcount) { }
        protected override Mesh[] LoadOriginModel(Manufacturers manufacturers, string modelname, int jointcount = 6)
        {
            //basemodel
            jointcount += 1;

            Mesh[] meshes = new Mesh[jointcount];

            if (modelname == " M_10iD10L")
            {
                modelname = modelname.Remove(0, 1);
            }

            Parallel.For(0, jointcount, (i) =>
            {
                string meshbyte = RobotArm_Fanuc.Properties.Resources.ResourceManager.GetString($"FANUC_{modelname}_{i}");
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
                string meshbyte = RobotArm_Fanuc.Properties.Resources.ResourceManager.GetString($"FANUC_{modelname}_CH_{i}");
                meshes.SetValue(GH_Convert.ByteArrayToCommonObject<Mesh>(Convert.FromBase64String(meshbyte)), i);
            });
            return meshes;
        }
    }
    public class ModelSystemFanuc : ModelSystem
    {
        public ModelSystemFanuc(string modelname) : base(modelname) { }
        public override ModelProperties GetModelProperties()
        {
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

                case FanucRobots.M_10iD8L:
                    return new M_10iD8L();

                case FanucRobots.M_10iD10L:
                    return new M_10iD10L();
                case FanucRobots.M_10iD12:
                    return new M_10iD12();
                case FanucRobots.M_10iD16S:
                    return new M_10iD16S();
                case FanucRobots.M_20iB25:
                    return new M_20iB25();
                case FanucRobots.M_20iB35S:
                    return new M_20iB35S();
                case FanucRobots.M_20iD12L:
                    return new M_20iD12L();
                case FanucRobots.M_20iD25:
                    return new M_20iD25();
                case FanucRobots.M_20iD35:
                    return new M_20iD35();

                case FanucRobots.R_2000iC_165F:
                    return new R_2000iC_165F();

                case FanucRobots.M_10iA_8L:
                    return new M_10iA_8L();

                default:
                    return null;
            }
        }
    }
    public class M_710ic50 : RobotModelsFanuc
    {
        public M_710ic50() : base("M_710ic50", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 565, -180, 180, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 870, 0, -56, 80, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 170, 0, -145, 270, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1016, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 175, -450, 450, 175);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "0,1,2,3,4";
            string AInames = "0,1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class M_710ic20l : RobotModelsFanuc
    {
        public M_710ic20l() : base("M_710ic20l", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 565, -180, 180, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1150, 0, -110, 110, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 190, 0, -162, 270, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1800, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -450, 450, 175);
            return jointProperties;
        }
        protected override IOProperties SetIOProperties()
        {
            string DOnames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string DInames = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16";
            string AOnames = "0,1,2,3,4";
            string AInames = "0,1,2,3,4";
            return new IOProperties(DOnames, DInames, AOnames, AInames);
        }
    }
    public class R_2000ic270f : RobotModelsFanuc
    {
        public R_2000ic270f() : base("R_2000ic270f", 270, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 312, 670, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1075, 0, -60, 76, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 225, 0, -120, 155, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1280, -350, 350, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -122.5, 122.5, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 240, -350, 350, 175);
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
    public class M_20ia : RobotModelsFanuc
    {
        public M_20ia() : base("M_20ia", 270, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            //  normal Fanuc
            /*
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 525, -185, 185, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 790, 0, -100, 160, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 250, 0, -285, 275.6, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 835, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -450, 450, 175);*/

            //changed for steel welding
            
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 525, -170, 170, 105);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 790, 0, -100, 128, 101);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 250, 0, -185, 273.1, 107);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 835, -200, 200, 122);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 113);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -270, 270, 175);
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
    public class R_2000ic125L : RobotModelsFanuc
    {
        public R_2000ic125L() : base("R_2000ic125L", 125, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 312, 670, -185, 185, 130);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1075, 0, -56, 80, 115);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 225, 0, -68.1, 231.9, 125);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1730, -350, 350, 180);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 180);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -360, 360, 260);
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
    public class R_2000ic210F : RobotModelsFanuc
    {
        public R_2000ic210F() : base("R_2000ic210F", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 312, 670, -185, 185, 120);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1075, 0, -56, 80, 105);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 225, 0, -68.1, 241.9, 110);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1280, -360, 360, 140);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 140);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -360, 360, 220);
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

    //done
    public class M_10iD8L : RobotModelsFanuc  
    {
        public M_10iD8L() : base("M_10iD8L", 8, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 450, -170, 170, 210);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 840, 0, -117, 117, 210);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 195, 0, -227, 227, 220);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1100, -190, 190, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -180, 180, 450);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 75, -450, 450, 720);
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

    /*
    public class ARCMate120iD_M_20iD_35_v01 : RobotModelsFanuc  //done
    {
        public ARCMate120iD_M_20iD_35_v01() : base("ARCMate120iD_M_20iD_35_v01", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 425, -170, 170, 210);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 840, 0, -130, 130, 210);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 215, 0, -229, 229, 265);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 890, -200, 200, 420);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 420);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 90, -270, 270, 720);
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

    public class ARCMate120iD_prototype : RobotModelsFanuc //done
    {
        public ARCMate120iD_prototype() : base("ARCMate120iD_prototype", 210, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 425, -170, 170, 210);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 840, 0, -130, 130, 210);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 215, 0, -229, 229, 265);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 890, -200, 200, 420);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 420);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 90, -270, 270, 720);
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
    */

    //done !!
    public class M_10iD10L : RobotModelsFanuc  
    {
        public M_10iD10L() : base(" M_10iD10L", 10, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 450, -170, 170, 260);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 640, 0, -117, 117, 240);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 195, 0, -227, 227, 260);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 900, -190, 190, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -180, 180, 450);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 75, -450, 450, 720);
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

    //done
    public class M_10iD12 : RobotModelsFanuc
    {
        public M_10iD12() : base("M_10iD12", 12, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 450, -170, 170, 260);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 640, 0, -117, 117, 240);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 195, 0, -227, 227, 260);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 700, -190, 190, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -180, 180, 450);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 75, -450, 450, 720);
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

    //done
    public class M_10iD16S : RobotModelsFanuc  
    {
        public M_10iD16S() : base("M_10iD16S", 16, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 450, -170, 170, 290);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 440, 0, -117, 117, 270);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 195, 0, -227, 227, 270);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 555, -190, 190, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -180, 180, 450);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 90, -450, 450, 730);
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

    //done
    public class M_20iB25 : RobotModelsFanuc  
    {
        public M_20iB25() : base("M_20iB25", 25, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 650, -170, 170, 205);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 905, 0, -120, 120, 205);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 120, 0, -151, 151, 260);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 865, -200, 200, 415);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -145, 145, 415);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -270, 270, 880);
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

    //done
    public class M_20iB35S : RobotModelsFanuc  
    {
        public M_20iB35S() : base("M_20iB35S", 35, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 650, -170, 170, 205);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 637, 0, -120, 120, 205);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 120, 0, -150, 150, 260);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 723, -200, 200, 415);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -130, 130, 415);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -270, 270, 880);
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

    //done
    public class M_20iD12L : RobotModelsFanuc  
    {
        public M_20iD12L() : base("M_20iD12L", 12, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 425, -170, 170, 210);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 840, 0, -130, 130, 210);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 215, 0, -237, 237, 265);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1340, -200, 200, 420);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -180, 180, 450);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 75, -450, 450, 720);
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

    //done
    public class M_20iD25 : RobotModelsFanuc 
    {
        public M_20iD25() : base("M_20iD25", 25, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 425, -170, 170, 210);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 840, 0, -130, 130, 210);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 215, 0, -229, 229, 265);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 890, -200, 200, 420);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -180, 180, 420);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 90, -450, 450, 720);
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

    //done
    public class M_20iD35 : RobotModelsFanuc  
    {
        public M_20iD35() : base("M_20iD35", 35, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 75, 425, -170, 170, 180);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 840, 0, -130, 130, 180);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 215, 0, -229, 229, 200);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 890, -200, 200, 350);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -140, 140, 350);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 90, -270, 270, 400);
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

    public class R_2000iC_165F : RobotModelsFanuc
    {
        public R_2000iC_165F() : base("R_2000iC_165F", 50, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 312, 670, -185, 185, 180);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 1075, 0, -60, 76, 180);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 225, 0, -72, 180, 200);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1280, -360, 360, 350);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -125, 125, 350);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 215, -360, 360, 400);
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

    public class M_10iA_8L : RobotModelsFanuc 
    {
        public M_10iA_8L() : base("M_10iA_8L", 8, 0) { }

        protected override JointProperties[] SetJointProperties()
        {
            JointProperties[] jointProperties = new JointProperties[6];
            jointProperties[0] = new JointProperties(JointType.Revolute, 1, 150, 450, -370, 340, 200);
            jointProperties[1] = new JointProperties(JointType.Revolute, 2, 780, 0, -255, 255, 200);
            jointProperties[2] = new JointProperties(JointType.Revolute, 3, 200, 0, -462, 462, 210);
            jointProperties[3] = new JointProperties(JointType.Revolute, 4, 0, 1080, -400, 400, 430);
            jointProperties[4] = new JointProperties(JointType.Revolute, 5, 0, 0, -360, 280, 430);
            jointProperties[5] = new JointProperties(JointType.Revolute, 6, 0, 100, -900, 540, 630);
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
