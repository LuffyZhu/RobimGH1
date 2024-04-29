using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using static Robim.Util;
using RobimRobots;

namespace Robim
{

    public abstract class RobotSystem
    {
        
        #region ExternalExtraSetting in RobotCell.cs
        //public string[] externaltype { get; internal set; }
        //public bool[] externaldirection { get; internal set; }
        #endregion
        public string Name { get; }
        public Manufacturers Manufacturer { get; }
        public IO IO { get; }
        public Plane BasePlane { get; }
        public Mesh Environment { get; }
        public Mesh DisplayMesh { get; set; }
        public IRemote Remote { get; protected set; }
        public RobimFormSystem RobimFormSystem { get; internal set; }

        static RobotSystem()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public RobotSystem(string name, Manufacturers manufacturer, IO io, Plane basePlane, Mesh environment,RobimFormSystem robimFormSystem,double revolving = 0)
        {
            this.Name = name;
            this.Manufacturer = manufacturer;
            this.IO = io;
            this.BasePlane = basePlane;
            this.Environment = environment;
            this.RobimFormSystem = robimFormSystem;
        }

        /// <summary>
        /// Quaternion interpolation based on: http://www.grasshopper3d.com/group/lobster/forum/topics/lobster-reloaded
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public virtual Plane CartesianLerp(Plane a, Plane b, double t, double min, double max)
        {
            t = (t - min) / (max - min);
            if (double.IsNaN(t)) t = 0;
            var newOrigin = a.Origin * (1 - t) + b.Origin * t;

            //  Quaternion q = Quaternion.Rotation(a, b);
            // var q = Quaternion.Identity.Rotate(a).Rotate(b);

            var q = Slerp(GetRotation(a), GetRotation(b), t);

            //  q.GetRotation(out var angle, out var axis);
            // angle = (angle > PI) ? angle - 2 * PI : angle;
            //  a.Rotate(t * angle, axis, a.Origin);

            a = TransformFromQuaternion(q).ToPlane();

            a.Origin = newOrigin;
            return a;
        }

        internal abstract void SaveCode(Program program, string folder);
        internal abstract List<List<List<string>>> Code(Program program);
        internal abstract double Payload(int group);
        internal abstract Joint[] GetJoints(int group);
        public abstract List<KinematicSolution> Kinematics(IEnumerable<Target> target, IEnumerable<double[]> prevJoints = null);
        public abstract double DegreeToRadian(double degree, int i, int group = 0);
        public abstract double RadianToDegree(double radian, int i, int group = 0);
        public abstract double[] PlaneToNumbers(Plane plane);
        public abstract Plane NumbersToPlane(double[] numbers);

        public static List<string> ListRobotSystems()
        {
            return ModelSystem.RobotNames();
            #region XML
            //var names = new List<string>();
            //var elements = new List<XElement>();
            ////string folder = $@"{AssemblyDirectory}\robots";
            //string folder = LibraryPath;

            //if (Directory.Exists(folder))
            //{
            //    var files = Directory.GetFiles(folder, "*.xml");
            //    foreach (var file in files)
            //    {
            //        var element = XElement.Load(file);
            //        if (element.Name.LocalName == "RobotSystems")
            //            elements.AddRange(element.Elements());
            //    }
            //}
            //else
            //{
            //    throw new DirectoryNotFoundException($" Folder '{folder}' not found");
            //}

            ////  elements.AddRange(XElement.Parse(Properties.Resources.robotsData).Elements());

            //foreach (var element in elements)
            //    names.Add($"{element.Attribute(XName.Get("name")).Value}");

            //return names;
            #endregion
        }
        //tracklist
        public static List<string> ListTrackSystems(ref List<int> jointcounts)
        {
            jointcounts = ModelSystemTrack.TrackJoints();
            return ModelSystem.TrackNames();
            #region XML
            //var names = new List<string>();
            //var elements = new List<XElement>();
            ////string folder = $@"{AssemblyDirectory}\robots";
            //string folder = LibraryPath;

            //if (Directory.Exists(folder))
            //{
            //    var files = Directory.GetFiles(folder, "*.xml");
            //    foreach (var file in files)
            //    {
            //        var element = XElement.Load(file);

            //        if (element.Name.LocalName == "TrackSystems")
            //            elements.AddRange(element.Elements());
            //    }
            //}
            //else
            //{
            //    throw new DirectoryNotFoundException($" Folder '{folder}' not found");
            //}

            ////  elements.AddRange(XElement.Parse(Properties.Resources.robotsData).Elements());
            //foreach (var element in elements)
            //{
            //    names.Add($"{element.Attribute(XName.Get("model")).Value}");
            //    jointcounts.Add(element.Element(XName.Get("Joints")).Descendants().ToArray().Length);
            //}
            //return names;
            #endregion
        }
        //platformlist
        public static List<string> ListPlatformSystems(ref List<int> jointcounts)
        {
            jointcounts = ModelSystemPlatform.PlatformJoints();
            return ModelSystem.PlatformNames();
            #region XML
            //var names = new List<string>();
            //var elements = new List<XElement>();
            //string folder = LibraryPath;

            //if (Directory.Exists(folder))
            //{
            //    var files = Directory.GetFiles(folder, "*.xml");
            //    foreach (var file in files)
            //    {
            //        var element = XElement.Load(file);

            //        if (element.Name.LocalName == "RevolverSystems")
            //            elements.AddRange(element.Elements());
            //    }
            //}
            //else
            //{
            //    throw new DirectoryNotFoundException($" Folder '{folder}' not found");
            //}
            //foreach (var element in elements)
            //{
            //    names.Add($"{element.Attribute(XName.Get("model")).Value}");
            //    jointcounts.Add(element.Element(XName.Get("Joints")).Descendants().ToArray().Length);
            //}
            //return names;
            #endregion
        }

        public static RobotSystem Load(GH_Component gH_Component,string name, RobimFormSystem rfs, string trackName = null, string revolverName = null)
        {
            ModelProperties RobotProperties = null;
            ModelProperties TrackProperties = null;
            ModelProperties PlatformProperties = null;
            if (name != null)
            {
                string[] Robotnames = name.Split('.');
                var manufacturer = (Manufacturers)Enum.Parse(typeof(Manufacturers), Robotnames[0]);
                switch (manufacturer)
                {
                    case Manufacturers.ABB:
                        RobotProperties = new ModelSystemABB(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.KUKA):
                        RobotProperties = new ModelSystemKUKA(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.UR):
                        RobotProperties = new ModelSystemUR(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.FANUC):
                        RobotProperties = new ModelSystemFanuc(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.Staubli):
                        RobotProperties = new ModelSystemStaubli(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.Aubo):
                        RobotProperties = new ModelSystemAubo(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.Estun):
                        RobotProperties = new ModelSystemEstun(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.Googol):
                        RobotProperties = new ModelSystemGoogol(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.Yaskawa):
                        RobotProperties = new ModelSystemYaskawa(Robotnames[1]).GetModelProperties();
                        break;
                    case (Manufacturers.Cobot):
                        RobotProperties = new ModelSystemCobot(Robotnames[1]).GetModelProperties();
                        break;
                    /*case (Manufacturers.JAKA):
                        RobotProperties = new ModelSystemJAKA(Robotnames[1]).GetModelProperties();
                        break;*/

                    default:
                        return null;
                }
                rfs.R_EulerPlane = Stringtoplane(rfs.R_Eulerangle, manufacturer);
            }
            if (trackName != null)
            {
                TrackProperties = new ModelSystemTrack(trackName).GetModelProperties();
                var manufacturer = TrackProperties.Manufacturers;
                rfs.T_EulerPlane = Stringtoplane(rfs.T_Eulerangle, manufacturer);
            }
            if (revolverName != null)
            {
                PlatformProperties = new ModelSystemPlatform(revolverName).GetModelProperties();
                //var platproperties = PlatformProperties as PlatformProperties;
                //revolving = platproperties.Type;
                //isrevolve = platproperties.IsRevolve;
                var manufacturer = PlatformProperties.Manufacturers;
                rfs.P_EulerPlane = Stringtoplane(rfs.P_Eulerangle, manufacturer);
                rfs.C_EulerPlane = Stringtoplane(rfs.C_Eulerangle, manufacturer);
            }
            if (RobotProperties == null)
            {
                gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $" RobotSystem \"{name}\" not found");
                return null;
            }
            #region XML
            //XElement trackElement = null;
            //XElement revolverElement = null;
            //XElement element = null;
            //string folder = LibraryPath;

            //if (Directory.Exists(folder))
            //{
            //    var files = Directory.GetFiles(folder, "*.xml");//�ɸĳ�ץ�ض�file with Robot manufacturer or Track or Platform

            //    foreach (var file in files)//ÿһ��ģ�͵�
            //    {
            //        XElement data = XElement.Load(file);

            //        if (data.Name.LocalName == "RobotSystems" && hasRobot == false)
            //        {
            //            element = data.Elements().FirstOrDefault(x => name == $"{x.Attribute(XName.Get("name")).Value}");
            //            if (element != null)//find model file
            //            {
            //                var manufacturer = (Manufacturers)Enum.Parse(typeof(Manufacturers), element.Attribute(XName.Get("manufacturer")).Value);
            //                rfs.R_EulerPlane = Stringtoplane(rfs.R_Eulerangle, manufacturer);
            //                hasRobot = true;
            //            }
            //        }
            //        else if (data.Name.LocalName == "TrackSystems" && hasTrack == false && trackName != null)
            //        {
            //            trackElement = data.Elements().FirstOrDefault(x => trackName == $"{x.Attribute(XName.Get("model")).Value}");
            //            if (trackElement != null)//find model file
            //            {
            //                var manufacturer = (Manufacturers)Enum.Parse(typeof(Manufacturers), trackElement.Attribute(XName.Get("manufacturer")).Value);
            //                rfs.T_EulerPlane = Stringtoplane(rfs.T_Eulerangle, manufacturer);
            //                hasTrack = true;
            //            }
            //        }
            //        //��δ���
            //        else if (data.Name.LocalName == "RevolverSystems" && hasRevolver == false && revolverName != null)
            //        {
            //            revolverElement = data.Elements().FirstOrDefault(x => revolverName == $"{x.Attribute(XName.Get("model")).Value}");
            //            if (revolverElement != null)//find model file
            //            {
            //                XElement baseElement = revolverElement.Element(XName.Get("Base"));
            //                revolving = XmlConvert.ToDouble(baseElement.Attribute(XName.Get("type")).Value);
            //                isrevolve = XmlConvert.ToBoolean(baseElement.Attribute(XName.Get("isrevolve")).Value);
            //                var manufacturer = (Manufacturers)Enum.Parse(typeof(Manufacturers), revolverElement.Attribute(XName.Get("manufacturer")).Value);
            //                rfs.P_EulerPlane = Stringtoplane(rfs.P_Eulerangle, manufacturer);
            //                rfs.C_EulerPlane = Stringtoplane(rfs.C_Eulerangle, manufacturer);
            //                hasRevolver = true;
            //            }
            //        }
            //        else
            //        {
            //            continue;
            //        }
            //        /*if ((data.Name.LocalName != "RobotSystems") & (data.Name.LocalName != "TrackSystems") & (data.Name.LocalName != "RevolverSystems")) continue;
            //          if (elements != null)
            //            break;*/
            //    }
            //    //�ϲ�XML
            //    if (hasTrack)
            //    {
            //        element.Element("Mechanisms").Element("RobotArm").AddAfterSelf(trackElement);//RobotArm,Track
            //    }
            //    if (hasRevolver)
            //    {
            //        element.Element("Mechanisms").Element("RobotArm").AddAfterSelf(revolverElement);//RobotArm,Platform,Track
            //    }
            //}
            //else
            //{
            //    gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $" Folder '{folder}' not found");
            //    return null;
            //    //throw new InvalidOperationException($" Folder '{folder}' not found");
            //}

            //if (element == null)
            //{
            //    gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $" RobotSystem \"{name}\" not found");
            //    return null;
            //    //throw new InvalidOperationException($" RobotSystem \"{name}\" not found");
            //}
            /*
            if (element == null)
            {
                XElement data = XElement.Parse(Properties.Resources.robotsData);
                try
                {
                    element = data.Elements().First(x => name == $"{x.Attribute(XName.Get("name")).Value}");
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException($" RobotSystem \"{name}\" not found");
                }
            }
            */
            #endregion
            return Create(RobotProperties, TrackProperties, PlatformProperties, rfs);
        }
        private static RobotSystem Create(ModelProperties RobotProperties, ModelProperties TrackProperties, ModelProperties PlatformProperties, RobimFormSystem rfs)
        {
            var name = RobotProperties.ModelName;
            var manufacturer = RobotProperties.Manufacturers;

            var mechanicalGroups = new List<MechanicalGroup>();
            ModelProperties[] modelProperties = new ModelProperties[3] { RobotProperties, TrackProperties, PlatformProperties };

            mechanicalGroups.Add(MechanicalGroup.Create(modelProperties, rfs));

            IO io = null;
            var ioElement = RobotProperties.IOProperties;
            if (ioElement != null)
            {
                string[] doNames = ioElement.DO;
                string[] diNames = ioElement.DI;
                string[] aoNames = ioElement.AO;
                string[] aiNames = ioElement.AI;
                io = new IO() { DO = doNames, DI = diNames, AO = aoNames, AI = aiNames };
            }
            Mesh environment = null;
            Plane basePlane = rfs.R_EulerPlane;
            switch (manufacturer)
            {
                case (Manufacturers.ABB):
                    return new RobotCellAbb(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.KUKA):
                    return new RobotCellKuka(name, mechanicalGroups, io, basePlane, environment,rfs);
                case (Manufacturers.UR):
                    return new RobotCellUR(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.FANUC):
                    return new RobotCellFanuc(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.Staubli):
                    return new RobotCellStaubli(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.Aubo):
                    return new RobotCellAubo(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.Estun):
                    return new RobotCellEstun(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.Googol):
                    return new RobotCellGoogol(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.Yaskawa):
                    return new RobotCellYaskawa(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.Cobot):
                    return new RobotCellCobot(name, mechanicalGroups, io, basePlane, environment, rfs);
                case (Manufacturers.JAKA):
                    return new RobotCellJAKA(name, mechanicalGroups, io, basePlane, environment, rfs);
            }
            return null;
        }

        public override string ToString() => $"{this.GetType().Name} ({Name})";

        public static Plane Stringtoplane(string str , Manufacturers manufacturers)
        {
            Plane basePlane = Plane.WorldXY;
            //���������ŷ����
            if (str != null)
            {
                string[] a = str.Split(',');
                double[] b = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    b[i] = double.Parse(a[i]);
                }
                switch (manufacturers)
                {
                    case (Manufacturers.ABB):
                        //externalplane = RobotCellAbb.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        basePlane = RobotCellKuka.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    case (Manufacturers.KUKA):
                        basePlane = RobotCellKuka.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    case (Manufacturers.UR):
                        //externalplane = RobotCellUR.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        basePlane = RobotCellKuka.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    case (Manufacturers.FANUC):
                        basePlane = RobotCellFanuc.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    case (Manufacturers.Staubli):
                        basePlane = RobotCellStaubli.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    case (Manufacturers.Aubo):
                        basePlane = RobotCellAubo.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    case (Manufacturers.Estun):
                        basePlane = RobotCellEstun.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    case Manufacturers.RoboticPlus:
                        basePlane = RobotCellKuka.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                    default:
                        basePlane = RobotCellKuka.EulerToPlane(b[0], b[1], b[2], b[3], b[4], b[5]);
                        break;
                }
            }
            return basePlane;
        }
    }

    public class IO
    {
        public string[] DO { get; internal set; }
        public string[] DI { get; internal set; }
        public string[] AO { get; internal set; }
        public string[] AI { get; internal set; }
    }

    public class RobimFormSystem
    {
        public string R_Name { get; } = null;
        public string T_Name { get; } = null;
        public string P_Name { get; } = null;
        public string R_Eulerangle { get; set; } = null;
        public string T_Eulerangle { get; set; } = null;
        public string P_Eulerangle { get; } = null;
        public string C_Eulerangle { get; } = null;
        public bool R_HasEulerangle { get; set; } = false;
        public bool T_HasEulerangle { get; set; } = false;
        public bool P_HasEulerangle { get; } = false;
        public bool C_HasEulerangle { get; } = false;
        public Plane R_EulerPlane { get; set; }
        public Plane T_EulerPlane { get; set; }
        public Plane P_EulerPlane { get; set; }
        public Plane C_EulerPlane { get; set; }
        public string[] Externalextrasetting { get; } = null;
        public string Print { get; } = null;
        public TrackHangUpSideDown trackHangUpSideDown { get; } = TrackHangUpSideDown.No;

        #region ExternalExtraSetting in RobotCell.cs & External.cs
        public string[] External_Type { get; internal set; }
        public bool[] External_Direction { get; internal set; }
        #endregion

        public RobimFormSystem(string[] names, string[] eulerangles, bool[] haseulerangles, string[] externalextrasetting,TrackHangUpSideDown trackhangupsidedown)
        {
            if (names != null)
            {
                R_Name = names[0];
                T_Name = names[1];
                P_Name = names[2];
            }
            if (eulerangles != null)
            {
                R_Eulerangle = eulerangles[0];
                T_Eulerangle = eulerangles[1];
                P_Eulerangle = eulerangles[2];
                C_Eulerangle = eulerangles[3];
            }
            if (haseulerangles != null)
            {
                R_HasEulerangle = haseulerangles[0];
                T_HasEulerangle = haseulerangles[1];
                P_HasEulerangle = haseulerangles[2];
                C_HasEulerangle = haseulerangles[3];
            }
            if (externalextrasetting != null)
            {
                Externalextrasetting = externalextrasetting;

                External_Type = new string[externalextrasetting.Length / 2];
                External_Direction = new bool[externalextrasetting.Length / 2];
                for (int i = 0, j = 0; i < externalextrasetting.Length; i += 2, j++)
                {
                    External_Type.SetValue(externalextrasetting[i], j);
                    External_Direction.SetValue(Convert.ToBoolean(externalextrasetting[i + 1]), j);
                }

                for (int i = 0; i < externalextrasetting.Length; i++)
                {
                    Print += externalextrasetting[i] + ',';
                }
                if (Print != null)
                    Print.Remove(Print.Length - 1, 1);//���Ķ���
            }
            this.trackHangUpSideDown = trackhangupsidedown;
        }
        public override string ToString() => $"{this.GetType().Name}\n ({R_Name},{R_HasEulerangle},{R_Eulerangle})\n ({T_Name},{T_HasEulerangle},{T_Eulerangle})\n ({P_Name},{P_HasEulerangle},{P_Eulerangle}|{C_HasEulerangle},{C_Eulerangle})\n {Print}\n {trackHangUpSideDown}";
    }
}
