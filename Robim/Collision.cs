#define RHINOCOMMON

using IronPython.Runtime;
using Microsoft.Scripting;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Math;
using Rhino;

namespace Robim
{
    public class Collision
    {
        Program program;
        RobotSystem robotSystem;
        double linearStep;
        double angularStep;
        IEnumerable<int> first;
        IEnumerable<int> second;
        Mesh environment;
        int environmentPlane;

        bool _onlyOne;
        int _oneFirst;
        int _oneSecond;

        public bool HasCollision { get; private set; } = false;
        public Mesh[] Meshes { get; internal set; }
        public CellTarget CollisionTarget { get; private set; }
        public int Count { get; set; }
        public static bool EmergenceStop { get; set; }
        public Mesh[] CH_meshes { get; internal set; }

        public Collision(Program program, IEnumerable<int> first, IEnumerable<int> second, Mesh environment, int environmentPlane, double linearStep, double angularStep)
        {
            #if RHINOCOMMON
            this.program = program;
            this.robotSystem = program.RobotSystem;
            this.linearStep = linearStep;
            this.angularStep = angularStep;
            this.first = first;
            this.second = second;
            this.environment = environment;
            this.environmentPlane = environmentPlane;

            if (first.Count() == 1 && second.Count() == 1)
            {
                _onlyOne = true;
                _oneFirst = first.First();
                _oneSecond = second.First();
            }
        }
        public void RunAllCompute()
        {
            //Collide();

            Count = 0;

            List<CellTarget> cellTargets;
            BeforeCompute(out cellTargets);
            var tools = cellTargets[0].ProgramTargets.Select(p => p.Target.Tool.ConvexHullMesh).ToList();
            if (tools.Count < 1)
            {
                tools = cellTargets[0].ProgramTargets.Select(p => p.Target.Tool.Mesh_).ToList();
            }
            var meshes = GetDefaultMeshes(robotSystem, tools);
            //CH_meshes = MeshToConvexHull(meshes);
            CH_meshes = meshes.ToArray();

            StartCompute(cellTargets);
#else
            throw new NotImplementedException(" Collisions have to be reimplemented.");
#endif
        }
        public static void StopCompute()
        {
            EmergenceStop = true;
        }
        void Collide()
        {
            Parallel.ForEach(program.Targets, (cellTarget, state) =>
            {
                if (cellTarget.Index == 0) return;
                var prevcellTarget = program.Targets[cellTarget.Index - 1];

                double divisions = 1;

                int groupCount = cellTarget.ProgramTargets.Count;

                for (int group = 0; group < groupCount; group++)
                {
                    var target = cellTarget.ProgramTargets[group];
                    var prevTarget = prevcellTarget.ProgramTargets[group];

                    double distance = prevTarget.WorldPlane.Origin.DistanceTo(target.WorldPlane.Origin);
                    double linearDivisions = Ceiling(distance / linearStep);// mm/step

                    double maxAngle = target.Kinematics.Joints.Zip(prevTarget.Kinematics.Joints, (x, y) => Abs(x - y)).Max();
                    double angularDivisions = Ceiling(maxAngle / angularStep);

                    double tempDivisions = Max(linearDivisions, angularDivisions);
                    if (tempDivisions > divisions) divisions = tempDivisions;
                }

                var meshes = new List<Mesh>();

                int j = (cellTarget.Index == 1) ? 0 : 1;

                for (int i = j; i < divisions; i++)
                {
                    double t = (double)i / (double)divisions;//celltarget count / (mm/step)
                    var kineTargets = cellTarget.Lerp(prevcellTarget, robotSystem, t, 0.0, 1.0);
                    var kinematics = program.RobotSystem.Kinematics(kineTargets);

                    meshes.Clear();

                    // TODO: Meshes not a property of KinematicSolution anymore
                    // meshes.AddRange(kinematics.SelectMany(x => x.Meshes)); 
                    var tools = cellTarget.ProgramTargets.Select(p => p.Target.Tool.Mesh_).ToList();
                    var robotMeshes = PoseMeshes(program.RobotSystem, kinematics, tools);
                    meshes.AddRange(robotMeshes);

                    //robot meshs + environment mesh
                    if (this.environment != null)
                    {
                        if (this.environmentPlane != -1)
                        {
                            Mesh currentEnvironment = this.environment.DuplicateMesh();
                            currentEnvironment.Transform(Transform.PlaneToPlane(Plane.WorldXY, kinematics.SelectMany(x => x.Planes).ToList()[environmentPlane]));
                            meshes.Add(currentEnvironment);
                        }
                        else
                        {
                            meshes.Add(this.environment);
                        }
                    }

                    if (_onlyOne)
                    {
                        var meshA = meshes[_oneFirst];
                        var meshB = meshes[_oneSecond];
                        
                        var meshClash = Rhino.Geometry.Intersect.Intersection.MeshMeshFast(meshA, meshB);

                        if (meshClash.Length > 0 && (!HasCollision || CollisionTarget.Index > cellTarget.Index))
                        {
                            HasCollision = true;
                            //Meshes = new Mesh[] { meshA, meshB };
                            this.CollisionTarget = cellTarget;
                            //state.Break();
                            state.Stop();
                        }
                    }
                    else
                    {
                        var setA = first.Select(x => meshes[x]);
                        var setB = second.Select(x => meshes[x]);

                        var meshClash = Rhino.Geometry.Intersect.MeshClash.Search(setA, setB, 1, 1);

                        if (meshClash.Length > 0 && (!HasCollision || CollisionTarget.Index > cellTarget.Index))
                        {
                            HasCollision = true;
                            //Meshes = new Mesh[] { meshClash[0].MeshA, meshClash[0].MeshB };
                            this.CollisionTarget = cellTarget;
                            //state.Break();
                            state.Stop();
                        }
                    }
                }
            });
        }
        void BeforeCompute(out List<CellTarget> cellTargets)
        {
            #region
            /*int index3 = 0;
            double length = 0;
            for(int i = 0; i < program.Targets.Count; i++)
            {
                Point3d point = program.Targets[i].ProgramTargets[0].Plane.Origin;
                Point3d point1 = environment.ClosestPoint(point);
                Polyline point3Ds = new Polyline();
                point3Ds.Add(point);
                point3Ds.Add(point1);
                double d = point3Ds.Length;
                if(i == 0)
                {
                    length = d;
                }
                else if (length > d)
                {
                    length = d;
                    index3 = i;
                }
            }*/
            #endregion
            cellTargets = new List<CellTarget>();
            int index1 = program.Targets.Count / 10;//分十等份
            double i = program.Targets.Count % 10;//余数(第十一份)
            for (int j = 0; j < index1; j++)//一个等份有几个点
            {
                for (int k = 0; k < 10; k++)//10份
                {
                    int indexs = index1 * k + j;
                    cellTargets.Add(program.Targets[indexs]);
                }
            }
            for (int l = 0; l < i; l++)
            {
                int indexs = index1 * 10 + l;//第11份
                cellTargets.Add(program.Targets[indexs]);
            }
        }
        void StartCompute(List<CellTarget> cellTargets)
        {
            EmergenceStop = false;
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            //Rhino.UI.StatusBar.ShowProgressMeter(0, program.Targets.Count, "Computing", true, true);
            //foreach (CellTarget cellTarget in program.Targets)
            
            foreach (CellTarget cellTarget in cellTargets)
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    NewCollide(cellTarget);
                    //Rhino.UI.StatusBar.UpdateProgressMeter(cellTarget.Index, true);
                }, token);
                task.Wait();
                Count++;
                if (HasCollision || EmergenceStop)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    break;
                }
            }
            tokenSource.Dispose();
        }
        void NewCollide(CellTarget cellTarget)
        {
            if (cellTarget.Index == 0) return;
            var prevcellTarget = program.Targets[cellTarget.Index - 1];

            double divisions = 1;

            int groupCount = cellTarget.ProgramTargets.Count;

            for (int group = 0; group < groupCount; group++)
            {
                var target = cellTarget.ProgramTargets[group];
                var prevTarget = prevcellTarget.ProgramTargets[group];

                double distance = prevTarget.WorldPlane.Origin.DistanceTo(target.WorldPlane.Origin);
                double linearDivisions = Ceiling(distance / linearStep);// mm/step

                double maxAngle = target.Kinematics.Joints.Zip(prevTarget.Kinematics.Joints, (x, y) => Abs(x - y)).Max();
                double angularDivisions = Ceiling(maxAngle / angularStep);

                double tempDivisions = Max(linearDivisions, angularDivisions);
                if (tempDivisions > divisions) divisions = tempDivisions;
            }

            var meshes = new List<Mesh>();

            int j = (cellTarget.Index == 1) ? 0 : 1;
            if(j == divisions)
            {
                divisions += 1; 
            }
            for (int i = j; i < divisions; i++)
            {
                double t = (double)i / (double)divisions;//celltarget count / (mm/step)
                var kineTargets = cellTarget.Lerp(prevcellTarget, robotSystem, t, 0.0, 1.0);
                var kinematics = program.RobotSystem.Kinematics(kineTargets);

                meshes.Clear();

                // TODO: Meshes not a property of KinematicSolution anymore
                // meshes.AddRange(kinematics.SelectMany(x => x.Meshes)); 
                var tools = cellTarget.ProgramTargets.Select(p => p.Target.Tool.Mesh_).ToList();
                var robotMeshes = PoseMeshes(program.RobotSystem, kinematics, tools);
                meshes.AddRange(robotMeshes);

                Mesh workpiece = kineTargets.SingleOrDefault().Workpiece;
                if (workpiece != null)
                {
                    if (first.Contains(meshes.Count) || second.Contains(meshes.Count))
                    {
                        var obj = CoupledPlaneShow(cellTarget, workpiece);
                        meshes.Add(obj as Mesh);
                    }
                    else
                    {
                        meshes.Add(workpiece);
                    }
                }

                //robot meshs + environment mesh
                if (this.environment != null)
                {
                    if (this.environmentPlane != -1)
                    {
                        Mesh currentEnvironment = this.environment.DuplicateMesh();
                        currentEnvironment.Transform(Transform.PlaneToPlane(Plane.WorldXY, kinematics.SelectMany(x => x.Planes).ToList()[environmentPlane]));
                        meshes.Add(currentEnvironment);
                    }
                    else
                    {
                        meshes.Add(this.environment);
                    }
                }

                if (_onlyOne)
                {
                    var meshA = meshes[_oneFirst];
                    var meshB = meshes[_oneSecond];

                    #region rhino document
                    var meshClash = Intersection.MeshMeshFast(meshA, meshB);

                    //if (meshClash.Length > 0 && (!HasCollision || CollisionTarget.Index > cellTarget.Index))
                    if (meshClash.Length > 0 && !HasCollision)
                    {
                        HasCollision = true;
                        Meshes = new Mesh[] { meshA, meshB };
                        this.CollisionTarget = cellTarget;
                        //tokenSource.Cancel();
                    }
                    #endregion
                    #region GJK
                    //ConvexHull meshtopointA = new ConvexHull(ConvexHullPoints(meshA));
                    //ConvexHull meshtopointB = new ConvexHull(ConvexHullPoints(meshB));
                    //HasCollision = GJKAlgorithm.Intersects(meshtopointA, meshtopointB);
                    //if (HasCollision)
                    //{
                    //    Meshes = new Mesh[] { meshA, meshB };
                    //    this.CollisionTarget = cellTarget;
                    //}
                    #endregion
                }
                else
                {
                    var setA = first.Select(x => meshes[x]);
                    var setB = second.Select(x => meshes[x]);

                    var meshClash = Rhino.Geometry.Intersect.MeshClash.Search(setA, setB, 1, 1);
                    //if (meshClash.Length > 0 && (!HasCollision || CollisionTarget.Index > cellTarget.Index))
                    if (meshClash.Length > 0 && !HasCollision)
                    {
                        HasCollision = true;
                        Meshes = new Mesh[] { meshClash[0].MeshA, meshClash[0].MeshB };
                        this.CollisionTarget = cellTarget;
                        //tokenSource.Cancel();
                    }
                }
            }
        }
        public List<Mesh> PoseMeshes(RobotSystem robot, List<KinematicSolution> solutions, List<Mesh[]> tools)
        {
            var cell = robot as RobotCell;

            if (cell != null)
            {
                var meshes = solutions.SelectMany((_, i) => PoseMeshes(cell.MechanicalGroups[i], solutions[i].Planes, tools[i])).ToList();
                return meshes;
            }
            else
            {
                var ur = robot as RobotCellUR;
                var meshes = PoseMeshesRobot(ur.Robot, solutions[0].Planes, tools[0]);
                return meshes;
            }
        }
        List<Mesh> PoseMeshes(MechanicalGroup group, IList<Plane> planes, Mesh[] tool)
        {
            planes = planes.ToList();
            var count = planes.Count - 1;
            planes.RemoveAt(count);
            planes.Add(planes[count - 1]);
            //var outMeshes = group.DefaultMeshes.Select(m => m.DuplicateMesh()).Append(tool.DuplicateMesh()).ToList();
            var outMeshes = CH_meshes.Select(m=>m.DuplicateMesh()).ToList();
            for (int i = 0; i < group.DefaultPlanes.Count; i++)
            {
                var s = Transform.PlaneToPlane(group.DefaultPlanes[i], planes[i]);
                outMeshes[i].Transform(s);
            }
            return outMeshes.ToList();
        }
        List<Mesh> PoseMeshesRobot(RobotArm arm, IList<Plane> planes, Mesh[] tool)
        {
            planes = planes.ToList();
            var count = planes.Count - 1;
            planes.RemoveAt(count);
            planes.Add(planes[count - 1]);

            var defaultPlanes = arm.Joints.Select(m => m.Plane).Prepend(arm.BasePlane).Append(Plane.WorldXY).ToList();
            //var defaultMeshes = arm.Joints.Select(m => m.Mesh).Prepend(arm.BaseMesh).Append(tool);
            //var outMeshes = defaultMeshes.Select(m => m.DuplicateMesh()).ToList();
            var outMeshes = CH_meshes.Select(m => m.DuplicateMesh()).ToList();

            for (int i = 0; i < defaultPlanes.Count; i++)
            {
                var s = Transform.PlaneToPlane(defaultPlanes[i], planes[i]);
                outMeshes[i].Transform(s);
            }

            return outMeshes.ToList();
        }

        public List<Mesh> GetDefaultMeshes(RobotSystem robot, List<Mesh[]> tools)
        {
            var cell = robot as RobotCell;
            List<Mesh> meshes = new List<Mesh>();
            if (cell != null)
            {
                for (int i = 0; i < cell.MechanicalGroups.Count; i++)
                {
                    var group = cell.MechanicalGroups[i];
                    if (group.Externals.Count == 1)
                    {
                        var modelproperties = group.ModelProperties.Skip(1).TakeWhile(x => x != null).FirstOrDefault();
                        meshes.AddRange(modelproperties.LoadModel.GetConvexHull().Select(x => x.DuplicateMesh()));
                    }
                    else if (group.Externals.Count == 2)
                    {
                        if (group.Externals[0].GetType() == typeof(Track))
                        {
                            meshes.AddRange(group.ModelProperties[1].LoadModel.GetConvexHull().Select(x => x.DuplicateMesh()));
                            meshes.AddRange(group.ModelProperties[2].LoadModel.GetConvexHull().Select(x => x.DuplicateMesh()));
                        }
                        else
                        {
                            meshes.AddRange(group.ModelProperties[2].LoadModel.GetConvexHull().Select(x => x.DuplicateMesh()));
                            meshes.AddRange(group.ModelProperties[1].LoadModel.GetConvexHull().Select(x => x.DuplicateMesh()));
                        }
                    }
                    meshes.AddRange(group.ModelProperties[0].LoadModel.GetConvexHull().Select(x => x.DuplicateMesh()));
                    if (tools[i] != null)
                        meshes.AddRange(tools[i].Select(x => x.DuplicateMesh()));
                    else
                        meshes.Add(new Mesh());
                }
                return meshes;
            }
            else
            {
                var ur = robot as RobotCellUR;
                var arm = ur.Robot;
                var defaultMeshes = arm.Joints.Select(m => m.Mesh).Prepend(arm.BaseMesh);
                meshes = defaultMeshes.Select(m => m.DuplicateMesh()).ToList();
                return meshes;
            }
        }
        
        public Mesh[] MeshToConvexHull(List<Mesh> meshes)
        {
            //Task task = null;
            List<Mesh> convexhulls = new List<Mesh>();
            for (int j = 0;j < meshes.Count; j++)
            {
                Mesh mesh = meshes[j];
                var calc = new ConvexHullCalculator();

                List<Vector3d> verts = new List<Vector3d>();
                List<int> tris = new List<int>();
                List<Vector3d> normals = new List<Vector3d>();

                var points = mesh.Vertices.Distinct().Select(x=>new Vector3d(x)).ToList();
                
                calc.GenerateHull(points, true, ref verts, ref tris, ref normals);

                Mesh convexhull = new Mesh();

                int trisLen = tris.Count;
                for (int i = 0; i < trisLen / 3; i++)
                {
                    convexhull.Faces.AddFace(tris[i * 3 + 0], tris[i * 3 + 1], tris[i * 3 + 2]);
                }
                
                for (int i = 0; i < verts.Count; i++)
                {
                    convexhull.Vertices.Add(verts[i].X, verts[i].Y, verts[i].Z);
                }
                convexhulls.Add(convexhull);
            }
            return convexhulls.ToArray();
        }
        Point3d[] ConvexHullPoints(Mesh mesh)
        {
            #region GetConvexHullMesh

            //var calc = new ConvexHullCalculator();

            //var verts = new List<Rhino.Geometry.Vector3d>();
            //var tris = new List<int>();
            //var normals = new List<Rhino.Geometry.Vector3d>();

            //var points = mesh.Vertices.ToPoint3dArray().ToList();
            //var pointsvector = new List<Rhino.Geometry.Vector3d>();
            //pointsvector = points.Distinct().Select(x => x - Point3d.Origin).ToList();
            //calc.GenerateHull(pointsvector, true, ref verts, ref tris, ref normals);
            //var vectorpoints = verts.Distinct().Select(x => new Point3d(x));
            var vectorpoints = mesh.Vertices.Distinct().Select(x => new Point3d(x));

            return vectorpoints.ToArray();
            #endregion
        }
        public object CoupledPlaneShow(CellTarget currentTarget,object wanttrans)
        {
            var planes = currentTarget.ProgramTargets.SelectMany(x => x.Kinematics.Planes).ToList();//外部轴耦合面旋转面

            Frame frame = currentTarget.ProgramTargets[0].Target.Frame;
            if (frame != null && frame.IsCoupled)//有外部轴才有coupled plane
            {
                string[] externaltype = program.RobotSystem.RobimFormSystem.External_Type;
                int j = 0;
                foreach (string str in externaltype)
                {
                    if (str.Contains("Track"))
                        j += 2;
                    if (str.Contains("Platform"))//一般只有1个，变位机有2个
                    {
                        j += 1;
                    }
                }
                Plane rotateplane = DigitalCoupledPlane.DCP.CustomPlane;
                rotateplane.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.P_CoupledPlane, planes[j]));//Positioner.coupledplane == 初始耦合面

                Mesh mesh = null;
                Plane[] planes1 = new Plane[0];
                if (wanttrans.GetType() == typeof(Mesh))
                {
                    mesh = wanttrans as Mesh;
                    Mesh workpiece = mesh.DuplicateMesh();
                    workpiece.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, rotateplane));
                    return workpiece;
                }
                else if (wanttrans.GetType() == typeof(Plane[]))
                {
                    planes1 = wanttrans as Plane[];
                    Plane[] planes2 = new Plane[planes1.Length];
                    for (int i = 0; i < planes1.Length; i++)
                    {
                        Plane plane1 = planes1[i].Clone();
                        plane1.Transform(Transform.PlaneToPlane(Plane.WorldXY, rotateplane));
                        //plane1.Transform(Transform.PlaneToPlane(DigitalCoupledPlane.DCP.CustomPlane, rotateplane));
                        planes2.SetValue(plane1, i);
                    }
                    return planes2;
                }
            }
            return wanttrans;
        }
    }
}