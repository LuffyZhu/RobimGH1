using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Robim.Grasshopper
{
    static class GeometryUtil
    {
        public static List<Mesh> PoseMeshes(RobotSystem robot, List<KinematicSolution> solutions, List<Mesh[]> tools)
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

        static List<Mesh> PoseMeshes(MechanicalGroup group, IList<Plane> planes, Mesh[] tool)
        {
            planes = planes.ToList();
            var count = planes.Count - 1;
            planes.RemoveAt(count);
            planes.Add(planes[count - 1]);

            var outMeshes = group.DefaultMeshes.Select(m => m.DuplicateMesh()).ToList();
            outMeshes.AddRange(tool.Select(x => x.DuplicateMesh()));

            for (int i = 0; i < group.DefaultPlanes.Count; i++)
            {
                var s = Transform.PlaneToPlane(group.DefaultPlanes[i], planes[i]);
                outMeshes[i].Transform(s);
            }

            return outMeshes;
        }

        static List<Mesh> PoseMeshesRobot(RobotArm arm, IList<Plane> planes, Mesh[] tool)
        {
            planes = planes.ToList();
            var count = planes.Count - 1;
            planes.RemoveAt(count);
            planes.Add(planes[count - 1]);

            var defaultPlanes = arm.Joints.Select(m => m.Plane).Prepend(arm.BasePlane).Append(Plane.WorldXY).ToList();
            var defaultMeshes = arm.Joints.Select(m => m.Mesh).Prepend(arm.BaseMesh).ToList();
            defaultMeshes.AddRange(tool);
            var outMeshes = defaultMeshes.Select(m => m.DuplicateMesh()).ToList();

            for (int i = 0; i < defaultPlanes.Count; i++)
            {
                var s = Transform.PlaneToPlane(defaultPlanes[i], planes[i]);
                outMeshes[i].Transform(s);
            }

            return outMeshes;
        }
    }
}
