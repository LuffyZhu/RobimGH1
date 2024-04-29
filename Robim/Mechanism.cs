using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using System.Xml;
using static Robim.Util;
using static System.Math;
using System.Threading.Tasks;
using RobimRobots;

namespace Robim
{
    public abstract class Mechanism
    {
        readonly string model;
        public Manufacturers Manufacturer { get; protected set; }
        public string Model => $"{Manufacturer.ToString()}.{model}";
        public double Payload { get; }
        public Plane BasePlane { get; set; }
        public Mesh BaseMesh { get; }
        public Joint[] Joints { get; }
        public bool MovesRobot { get; }
        public Mesh DisplayMesh { get; }
        public RobimFormSystem RFS { get; }
        protected double Revolving { get; }
        protected bool IsRevolve { get; }
        public bool TrackVertical { get; }

        internal Mechanism(string model, Manufacturers manufacturer, double payload, Plane basePlane, Mesh baseMesh, IEnumerable<Joint> joints, bool movesRobot,RobimFormSystem rfs = null, double revolving = 0, bool isrevolve = false, bool trackVertical = false)
        {
            this.model = model;
            this.Manufacturer = manufacturer;
            this.Payload = payload;
            this.BasePlane = basePlane;
            this.BaseMesh = baseMesh;
            this.Joints = joints.ToArray();
            this.MovesRobot = movesRobot;
            this.DisplayMesh = CreateDisplayMesh();
            this.RFS = rfs;
            this.Revolving = revolving;
            this.IsRevolve = isrevolve;
            this.TrackVertical = trackVertical;

            // use degree as limit instead
            //for (int i = 0; i < Joints.Length; i++)
            //{
            //    Joints[i].Range = new Interval(DegreeToRadian(Joints[i].Range.T0, i), DegreeToRadian(Joints[i].Range.T1, i));
            //}

            SetStartPlanes();
        }

        Mesh CreateDisplayMesh()
        {
            var mesh = new Mesh();
            mesh.Append(BaseMesh);

            foreach (var joint in Joints)
                mesh.Append(joint.Mesh);

            mesh.Transform(BasePlane.ToTransform());
            return mesh;
        }

        static List<Mesh> GetMeshes(string model)
        {
            var meshes = new List<Mesh>();

            /*
            using (var stream = new MemoryStream(Properties.Resources.Meshes))
            {
                var formatter = new BinaryFormatter();
                JointMeshes jointMeshes = formatter.Deserialize(stream) as JointMeshes;
                index = jointMeshes.Names.FindIndex(x => x == model);
                if (index != -1) meshes = jointMeshes.Meshes[index];
            }
            */

            string folder = LibraryPath;

            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "*.3dm");

                foreach (var file in files)
                {
                    Rhino.FileIO.File3dm geometry = Rhino.FileIO.File3dm.Read(file);

                    var layer = geometry.AllLayers.FirstOrDefault(x => x.Name == model);

                    if (layer != null)
                    {
                        int i = 0;
                        while (true)
                        {
                            string name = $"{i++}";
                            var jointLayer = geometry.AllLayers.FirstOrDefault(x => (x.Name == name) && (x.ParentLayerId == layer.Id));
                            if (jointLayer == null) break;
                            var mesh = geometry.Objects.FirstOrDefault(x => x.Attributes.LayerIndex == jointLayer.Index)?.Geometry as Mesh ?? new Mesh();
                            meshes.Add(mesh);
                        }

                        return meshes;
                    }
                }

                throw new InvalidOperationException($" Robot \"{model}\" is not in the geometry file.");
            }
            else
            {
                throw new DirectoryNotFoundException($" Robim folder not found in the Documents folder.");
            }
        }

        internal static Mechanism Create(ModelProperties modelProperties ,RobimFormSystem rfs)
        {
            var modelName = modelProperties.ModelName;
            var manufacturer = modelProperties.Manufacturers;

            
            var meshes = modelProperties.LoadModel.ShowModel();

            double payload = modelProperties.Payload;

            var baseMesh = meshes[0].DuplicateMesh();
            var baseElement = modelProperties.BaseProperties;
            double x = baseElement.X;
            double y = baseElement.Y;
            double z = baseElement.Z;
            double q1 = baseElement.Q1;
            double q2 = baseElement.Q2;
            double q3 = baseElement.Q3;
            double q4 = baseElement.Q4;

            var basePlane = RobotCellAbb.QuaternionToPlane(x, y, z, q1, q2, q3, q4);

            var jointElements = modelProperties.JointProperties;
            Joint[] joints = new Joint[jointElements.Length];

            for (int i = 0; i < jointElements.Length; i++)
            {
                var jointElement = jointElements[i];
                double a = jointElement.A;
                double d = jointElement.D;
                Interval range = new Interval(jointElement.MinRange, jointElement.MaxRange);
                double maxSpeed = jointElement.MaxSpeed;
                Mesh mesh = meshes[i + 1].DuplicateMesh();
                int number = jointElement.JointNumber - 1;

                if (jointElement.JointType == JointType.Revolute)
                    joints[i] = new RevoluteJoint() { Index = i, Number = number, A = a, D = d, Range = range, MaxSpeed = maxSpeed.ToRadians(), Mesh = mesh };
                else if (jointElement.JointType == JointType.Prismatic)
                    joints[i] = new PrismaticJoint() { Index = i, Number = number, A = a, D = d, Range = range, MaxSpeed = maxSpeed, Mesh = mesh };
            }

            bool movesRobot = false;
            bool trackVertical = false;

            switch (modelProperties.LoadModel.ModelType)
            {
                case (ModelType.Robot):
                    {
                        //if (rfs.R_EulerPlane != Plane.WorldXY)
                        //    basePlane = rfs.R_EulerPlane;
                        switch (manufacturer)
                        {
                            case (Manufacturers.ABB):
                                return new RobotAbb(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.KUKA):
                                return new RobotKuka(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.UR):
                                return new RobotUR(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.FANUC):
                                return new RobotFanuc(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.Staubli):
                                return new RobotStaubli(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.Aubo):
                                return new RobotAubo(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.Estun):
                                return new RobotESTUN(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.Googol):
                                return new RobotGoogol(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.Yaskawa):
                                return new RobotYaskawa(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.Cobot):
                                return new RobotCobot(modelName, payload, basePlane, baseMesh, joints);
                            case (Manufacturers.JAKA):
                                return new RobotJAKA(modelName, payload, basePlane, baseMesh, joints);
                            default:
                                return null;
                        }
                    }
                case (ModelType.Platform):
                    if (rfs.P_EulerPlane != Plane.WorldXY)
                        basePlane = rfs.P_EulerPlane;
                    var platproperties = modelProperties as PlatformProperties;
                    double revolving = platproperties.Type;
                    bool isrevolve = platproperties.IsRevolve;
                    return new Positioner(modelName, manufacturer, payload, basePlane, baseMesh, joints, movesRobot, rfs, revolving, isrevolve);
                case (ModelType.Track):
                    if (rfs.T_EulerPlane != Plane.WorldXY)
                        basePlane = rfs.T_EulerPlane;
                    var movesRobotAttribute = modelProperties as TrackProperties;
                    movesRobot = movesRobotAttribute.MovesRobot;
                    return new Track(modelName, manufacturer, payload, basePlane, baseMesh, joints, movesRobot,rfs, trackVertical);
                case (ModelType.Custom):
                    return new Custom(modelName, manufacturer, payload, basePlane, baseMesh, joints, movesRobot,rfs);
                default:
                    return null;
            }
        }
        /*
        public static void WriteMeshes()
        {
            Rhino.FileIO.File3dm robotsGeometry = Rhino.FileIO.File3dm.Read($@"{ResourcesFolder}\robotsGeometry.3dm");
            var jointmeshes = new JointMeshes();

            foreach (var layer in robotsGeometry.Layers)
            {
                if (layer.Name == "Default" || layer.ParentLayerId != Guid.Empty) continue;
                jointmeshes.Names.Add(layer.Name);
                var meshes = new List<Mesh>();
                meshes.Add(robotsGeometry.Objects.First(x => x.Attributes.LayerIndex == layer.LayerIndex).Geometry as Mesh);

                int i = 0;
                while (true)
                {
                    string name = $"{i++ + 1}";
                    var jointLayer = robotsGeometry.Layers.FirstOrDefault(x => (x.Name == name) && (x.ParentLayerId == layer.Id));
                    if (jointLayer == null) break;
                    meshes.Add(robotsGeometry.Objects.First(x => x.Attributes.LayerIndex == jointLayer.LayerIndex).Geometry as Mesh);
                }
                jointmeshes.Meshes.Add(meshes);
            }

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, jointmeshes);
                File.WriteAllBytes($@"{ResourcesFolder}\Meshes.rob", stream.ToArray());
            }
        }
        */
        public abstract KinematicSolution Kinematics(Target target, double[] prevJoints = null, Plane? basePlane = null);
        public abstract KinematicSolution Kinematics_Analytic(Target target, double[] prevJoints = null, Plane? basePlane = null);

        protected abstract void SetStartPlanes();
        public abstract double DegreeToRadian(double degree, int i);
        public abstract double RadianToDegree(double radian, int i);
        public override string ToString() => $"{this.GetType().Name} ({Model})";

        //public static Plane coupledplane = Plane.Unset;

        protected abstract class MechanismKinematics : KinematicSolution
        {
            protected Mechanism mechanism;
            protected string[] Externaltype { get; }
            protected bool IsRevolve { get; } = false;
            protected bool TrackVertical { get; } = false;
            internal MechanismKinematics(Mechanism mechanism, Target target, double[] prevJoints, Plane? basePlane,string[] externaltype = null)
            {
                this.mechanism = mechanism; // robot 
                Mechanism = mechanism;  // robot
                int jointCount = mechanism.Joints.Length;

                this.Externaltype = externaltype;  // null
                if(mechanism.GetType() == typeof(Positioner))
                {
                    var posi = mechanism as Positioner;
                    IsRevolve = posi.IsRevolve;
                }
                // Init properties
                Joints = new double[jointCount];
                Planes = new Plane[jointCount + 1];

                // Base plane//设定原点//this BasePlane is eulerplane
                Planes[0] = mechanism.BasePlane;

                if (basePlane != null)
                {
                    Planes[0].Transform(Transform.PlaneToPlane(Plane.WorldXY, (Plane)basePlane));
                }

                if (UseIKFast)
                {
                    IKSetJoints(target, prevJoints);
                    IKSetPlanes();
                }
                else
                {
                    SetJoints(target, prevJoints);
                    JointsOutOfRange();
                    SetPlanes(target);
                }

                // Move planes to base
                var transform = Planes[0].ToTransform();

                for (int i = 1; i < jointCount + 1; i++)
                    Planes[i].Transform(transform);//结合x,y,z & a,d   // Combine/integrate x,y,z & a,d 
            }

            protected abstract void SetJoints(Target target, double[] prevJoints);
            protected abstract void SetPlanes(Target target);
            protected abstract void IKSetJoints(Target target, double[] prevJoints);
            protected abstract void IKSetPlanes();

            protected virtual void JointsOutOfRange()
            {
                string modelname = mechanism.Model;
                //mechanism.Joints.Where(x => x.Number==2).Select(x => x.Range.)

                //if (modelname.ToLower().Contains("fanuc"))
                //{

                //    double t1 = Joints[1];
                //    var j1 = mechanism.DegreeToRadian(t1, 2);
                //    mechanism.Joints[2].Range = new Interval(-t1 - 65 * PI / 180, -t1 + 245 * PI / 180);

                //}
                //if (mechanism.Joints.Length >= 4) 
                //{
                //    var currentJoints3 = mechanism.Joints[2];
                //    var checkJoint3Range = mechanism.Joints[2].Range;

                //    var checkThis3 = mechanism.RadianToDegree(Joints[currentJoints3.Index], currentJoints3.Index);
                //    //var checkThis2 = mechanism.RadianToDegree(Joints[currentJoints3.Index], currentJoints3.Index);
                //    var allJoints = mechanism.Joints;
                //}
                


                var outofRangeErrors = mechanism.Joints
                .Where(x => !x.Range.IncludesParameter(mechanism.RadianToDegree(Joints[x.Index],x.Index)))
                .Select(x => $"Axis {x.Number + 1} is outside the permitted range.");

                // ATTENTION!!! ADDED FOR STEEL WELDING
                /*
                if (Joints.Length > 2) 
                {
                    var currentJ2valueForFanuc = mechanism.RadianToDegree(Joints[1], 1);
                    var currentJ3limitForFanuc = -currentJ2valueForFanuc + 190;
                    var currentJ3valueForFanuc = mechanism.RadianToDegree(Joints[2], 2);
                    if (currentJ3valueForFanuc > currentJ3limitForFanuc)
                    {
                        var J3ErrorFanuc = "Axis 3 has reached the limit, the cable might get jammed";
                        Errors.Add(J3ErrorFanuc);
                    }
                }*/
                // ATTENTION!!! ADDED FOR STEEL WELDING

                Errors.AddRange(outofRangeErrors);
            }
        }
        protected abstract class MechanismKinematics_Analytic : KinematicSolution
        {
            protected Mechanism mechanism;

            internal MechanismKinematics_Analytic(Mechanism mechanism, Target target, double[] prevJoints, Plane? basePlane)
            {
                this.mechanism = mechanism;
                int jointCount = mechanism.Joints.Length;

                // Init properties
                Joints = new double[jointCount];
                Planes = new Plane[jointCount + 1];

                // Base plane//设定原点//this BasePlane is eulerplane
                Planes[0] = mechanism.BasePlane;

                if (basePlane != null)
                {
                    Planes[0].Transform(Transform.PlaneToPlane(Plane.WorldXY, (Plane)basePlane));
                }

                //IKSetJoints(target, prevJoints);
                //IKSetPlanes();
                ComputeJoint8Solution(target);


                // Move planes to base
                var transform = Planes[0].ToTransform();

                for (int i = 1; i < jointCount + 1; i++)
                    Planes[i].Transform(transform);//结合x,y,z & a,d
            }

            protected abstract void IKSetJoints(Target target, double[] prevJoints);
            protected abstract void IKSetPlanes();
            protected abstract void ComputeJoint8Solution(Target target);
            protected abstract void ComputePlane();
        }
    }

    public abstract class Joint
    {
        public int Index { get; set; }
        public int Number { get; set; }
        internal double A { get; set; }
        internal double D { get; set; }
        public Interval Range { get; internal set; }
        public double MaxSpeed { get; internal set; }
        public Plane Plane { get; set; }
        public Mesh Mesh { get; set; }
    }

    public class BaseJoint
    {

    }

    public class RevoluteJoint : Joint
    {

    }

    public class PrismaticJoint : Joint
    {
    }


    [Serializable]
    class JointMeshes
    {
        internal List<string> Names { get; set; } = new List<string>();
        internal List<List<Mesh>> Meshes { get; set; } = new List<List<Mesh>>();
    }
}