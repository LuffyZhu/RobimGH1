using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
using Quaternion = System.Numerics.Quaternion;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Robim;

namespace Robim
{
    public static class GJK
    {
        public static bool IsInSameDirection(this Vector3d vector, Vector3d otherVector)
        {
            return Vector3d.Multiply(vector, otherVector) > 0;
        }
        public static bool IsInOppositeDirection(this Vector3d vector, Vector3d otherVector)
        {
            return Vector3d.Multiply(vector, otherVector) < 0;
        }
        public interface IConvexRegion
        {
            Point3d[] Points { get; set; }
            Point3d CenterPoint { get; set; }
            /// <summary>
            /// Calculates the furthest point on the region 
            /// along a given direction.
            /// </summary>
            Vector3d GetFurthestPoint(Vector3d direction);
        }
    }
    class Simplex
    {
        List<Vector3d> _vertices = new List<Vector3d>();

        public int Count
        {
            get { return _vertices.Count; }
        }

        public Vector3d this[int i]
        {
            get { return _vertices[i]; }
        }

        public Simplex(params Vector3d[] vertices)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                _vertices.Add(vertices[i]);
            }
        }

        public void Add(Vector3d vertex)
        {
            _vertices.Add(vertex);
        }

        public void Remove(Vector3d vertex)
        {
            _vertices.Remove(vertex);
        }
    }
    public static class GJKAlgorithm
    {
        enum EvolveResult
        {
            NoIntersection,
            FoundIntersection,
            StillEvolving,
        }
        public static bool Intersects(GJK.IConvexRegion regioneOne, GJK.IConvexRegion regionTwo)
        {
            //first
            Vector3d OneV = regionTwo.CenterPoint - regioneOne.CenterPoint;
            Vector3d C = Support(regioneOne, regionTwo, OneV);
            Simplex simplex = new Simplex(C);
            //second
            Vector3d TwoV = OneV * -1;
            Vector3d B = Support(regioneOne, regionTwo, TwoV);
            simplex.Add(B);
            //third -- from inside line bc
            Vector3d cb = B - C;
            Vector3d c0 = Vector3d.Zero - C;
            Vector3d cbxc0 = Vector3d.CrossProduct(cb, c0);
            Vector3d ThreeV = Vector3d.CrossProduct(cbxc0, cb);
            Vector3d A = Support(regioneOne, regionTwo, ThreeV);
            simplex.Add(A);
            //fourth -- from inside triangle abc
            Vector3d AB = B - C;
            Vector3d AC = A - C;
            Vector3d a0 = Vector3d.Zero - A;
            Vector3d FourV = Vector3d.CrossProduct(AB, AC);
            if (FourV.IsInOppositeDirection(a0))
            {
                FourV *= -1;
            }
            Vector3d s4 = Support(regioneOne, regionTwo, FourV);
            simplex.Add(s4);
            Vector3d d = Vector3d.Unset;
            if (ProcessSimplex(regioneOne,regionTwo,ref simplex, ref d))
            {
                return true;
            }
            else
            {
                return false;
            }
            ////Get an initial point on the Minkowski difference.
            //Vector3d OneV = new Vector3d(1, 1, 1);
            //Vector3d OneV = regionTwo.CenterPoint - regioneOne.CenterPoint;
            //Vector3d s = Support(regioneOne, regionTwo, OneV);

            ////Create our initial simplex.
            //Simplex simplex = new Simplex(s);

            ////Choose an initial direction toward the origin.
            //Vector3d d = -s;

            ////Choose a maximim number of iterations to avoid an 
            ////infinite loop during a non-convergent search.
            //int maxIterations = 50;

            //for (int i = 0; i < maxIterations; i++)
            //{
            //    //Get our next simplex point toward the origin.
            //    Vector3d a = Support(regioneOne, regionTwo, d);

            //    //If we move toward the origin and didn't pass it 
            //    //then we never will and there's no intersection.
            //    if (a.IsInOppositeDirection(d))
            //    {
            //        return false;
            //    }
            //    //otherwise we add the new
            //    //point to the simplex and
            //    //process it.
            //    simplex.Add(a);
            //    //Here we either find a collision or we find the closest feature of
            //    //the simplex to the origin, make that the new simplex and update the direction
            //    //to move toward the origin from that feature.
            //    if (ProcessSimplex(ref simplex, ref d))
            //    {
            //        return true;
            //    }
            //}
            ////If we still couldn't find a simplex 
            ////that contains the origin then we
            ////"probably" have an intersection.
            //return true;
        }

        /// <summary>
        ///Either finds a collision or the closest feature of the simplex to the origin, 
        ///and updates the simplex and direction.
        /// </summary>
        static bool ProcessSimplex(GJK.IConvexRegion regionOne, GJK.IConvexRegion regionTwo,ref Simplex simplex, ref Vector3d direction)
        {
            if (simplex.Count == 2)
            {
                return ProcessLine(ref simplex, ref direction);
            }
            else if (simplex.Count == 3)
            {
                return ProcessTriangle(ref simplex, ref direction);
            }
            else
            {
                EvolveResult evolveResult;
                int pointcount = regionOne.Points.Length + regionTwo.Points.Length;
                for (int i = 0; i < pointcount; i++)
                {
                    evolveResult = ProcessTetrehedron(regionOne, regionTwo, ref simplex, ref direction);
                    if (evolveResult == EvolveResult.FoundIntersection)
                    {
                        return true;
                    }
                    else if (evolveResult == EvolveResult.NoIntersection)
                    {
                        return false;
                    }
                    else//EvolveResult.StillEvolving
                    {
                        //
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Determines which Veronoi region of a line segment 
        /// the origin is in, utilizing the preserved winding
        /// of the simplex to eliminate certain regions.
        /// </summary>
        static bool ProcessLine(ref Simplex simplex, ref Vector3d direction)
        {
            Vector3d a = simplex[1];
            Vector3d b = simplex[0];
            Vector3d ab = b - a;
            Vector3d aO = -a;

            if (ab.IsInSameDirection(aO))
            {
                double dot = Vector3d.Multiply(ab, aO);
                float angle = (float)Math.Acos(dot / (ab.Length * aO.Length));
                direction = Vector3d.CrossProduct(Vector3d.CrossProduct(ab, aO), ab);
            }
            else
            {
                simplex.Remove(b);
                direction = aO;
            }
            return false;
        }

        /// <summary>
        /// Determines which Veronoi region of a triangle 
        /// the origin is in, utilizing the preserved winding
        /// of the simplex to eliminate certain regions.
        /// </summary>
        static bool ProcessTriangle(ref Simplex simplex, ref Vector3d direction)
        {
            Vector3d a = simplex[2];
            Vector3d b = simplex[1];
            Vector3d c = simplex[0];
            Vector3d ab = b - a;
            Vector3d ac = c - a;
            Vector3d abc = Vector3d.CrossProduct(ab, ac);
            Vector3d aO = -a;
            Vector3d acNormal = Vector3d.CrossProduct(abc, ac);
            Vector3d abNormal = Vector3d.CrossProduct(ab, abc);

            if (acNormal.IsInSameDirection(aO))
            {
                if (ac.IsInSameDirection(aO))
                {
                    simplex.Remove(b);
                    direction = Vector3d.CrossProduct(Vector3d.CrossProduct(ac, aO), ac);
                }
                else
                {
                    if (ab.IsInSameDirection(aO))
                    {
                        simplex.Remove(c);
                        direction = Vector3d.CrossProduct(Vector3d.CrossProduct(ab, aO), ab);
                    }
                    else
                    {
                        simplex.Remove(b);
                        simplex.Remove(c);
                        direction = aO;
                    }
                }
            }
            else
            {
                if (abNormal.IsInSameDirection(aO))
                {
                    if (ab.IsInSameDirection(aO))
                    {
                        simplex.Remove(c);
                        direction = Vector3d.CrossProduct(Vector3d.CrossProduct(ab, aO), ab);
                    }
                    else
                    {
                        simplex.Remove(b);
                        simplex.Remove(c);
                        direction = aO;
                    }
                }
                else
                {
                    if (abc.IsInSameDirection(aO))
                    {
                        direction = Vector3d.CrossProduct(Vector3d.CrossProduct(abc, aO), abc);
                    }
                    else
                    {
                        direction = Vector3d.CrossProduct(Vector3d.CrossProduct(-abc, aO), -abc);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines which Veronoi region of a tetrahedron
        /// the origin is in, utilizing the preserved winding
        /// of the simplex to eliminate certain regions.
        /// </summary>
        static EvolveResult ProcessTetrehedron(GJK.IConvexRegion regionOne , GJK.IConvexRegion regionTwo, ref Simplex simplex, ref Vector3d direction)
        {
            Vector3d oc = simplex[2];
            Vector3d ob = simplex[1];
            Vector3d oa = simplex[0];
            Vector3d od = simplex[3];

            Vector3d ac = oa - oc;
            Vector3d ad = od - oc;
            Vector3d ab = ob - oc;
            Vector3d bc = oa - ob;
            Vector3d bd = od - ob;
            Vector3d cd = od - oa;
            Vector3d cb = ob - oa;
            Vector3d a0 = Vector3d.Zero - oc;
            Vector3d c0 = Vector3d.Zero - oa;

            //triangle abd
            Vector3d abxad = Vector3d.CrossProduct(ab, ad);
            //triangle adc
            Vector3d adxac = Vector3d.CrossProduct(ad, ac);
            //triangle cdb
            Vector3d cdxcb = Vector3d.CrossProduct(cd, cb);

            if (abxad.IsInSameDirection(a0))
            {
                // the origin is outside line ab
                simplex.Remove(oa);
                direction = abxad;
                Vector3d Nc = Support(regionOne, regionTwo, direction);
                if (Nc.IsInOppositeDirection(direction))
                {
                    return EvolveResult.NoIntersection;
                }
                simplex.Add(Nc);
                return EvolveResult.StillEvolving;
            }
            else if (adxac.IsInSameDirection(a0))
            {
                // the origin is outside line ac
                simplex.Remove(ob);
                direction = adxac;
                Vector3d Nb = Support(regionOne, regionTwo, direction);
                if (Nb.IsInOppositeDirection(direction))
                {
                    return EvolveResult.NoIntersection;
                }
                simplex.Add(Nb);
                return EvolveResult.StillEvolving;
            }
            else if (cdxcb.IsInSameDirection(c0))
            {
                simplex.Remove(oc);
                direction = cdxcb;
                Vector3d Na = Support(regionOne, regionTwo, direction);
                if (Na.IsInOppositeDirection(direction))
                {
                    return EvolveResult.NoIntersection;
                }
                simplex.Add(Na);
                return EvolveResult.StillEvolving;
            }
            else
            {
                // the origin is inside both abd , adc and cdb
                // so it must be inside the Tetrehedron!
                return EvolveResult.FoundIntersection;
            }
        }

        /// <summary>
        /// Calculates the furthest point on the Minkowski 
        /// difference along a given direction.
        /// </summary>
        static Vector3d Support(GJK.IConvexRegion regionOne,GJK.IConvexRegion regionTwo,Vector3d direction)
        {
            return regionOne.GetFurthestPoint(direction) - regionTwo.GetFurthestPoint(-direction);
        }
    }

    public class Sphere : GJK.IConvexRegion
    {
        public Vector3d Center;
        public float Radius;
        public Point3d CenterPoint { get; set; }
        public Point3d[] Points { get; set; }

        public Sphere(Vector3d center, float radius)
        {
            Center = center;
            CenterPoint = new Point3d(center.X, center.Y, center.Z);
            Radius = radius;
        }


        public Vector3d GetFurthestPoint(Vector3d direction)
        {
            if (direction != Vector3d.Zero)
            {
                direction.Unitize();
            }
            return Center + Radius * direction;
        }
    }
    public class Box : GJK.IConvexRegion
    {
        public Vector3d Center;
        Vector3d _halfDimensions = new Vector3d(1, 1, 1);
        Quaternion _orientation = Quaternion.Identity;
        public Point3d CenterPoint { get; set; }

        public Vector3d Dimensions
        {
            get { return 2f * _halfDimensions; }
        }
        public Point3d[] Points { get; set; }

        public Box(Vector3d center)
            : this(center, 1f, 1f, 1f) { }

        public Box(Vector3d center,
            float width,
            float height,
            float depth)
            : this(center, width, height, depth, Matrix4x4.Identity) { }

        public Box(Vector3d center,
            float width,
            float height,
            float depth,
            Matrix4x4 rotationMatrix)
        {
            Center = center;
            _halfDimensions = new Vector3d(
                width / 2f,
                height / 2f,
                depth / 2f);
            _orientation = System.Numerics.Quaternion.CreateFromRotationMatrix(rotationMatrix);
        }

        public Vector3d GetFurthestPoint(Vector3d direction)
        {
            Vector3d BackV = new Vector3d(0, 0, -1);
            Vector3d halfHeight = _halfDimensions.Y * Vector3d.YAxis;
            Vector3d halfWidth = _halfDimensions.X * Vector3d.XAxis;
            Vector3d halfDepth = _halfDimensions.Z * BackV;

            Vector3d[] vertices = new Vector3d[8];
            vertices[0] = halfWidth + halfHeight + halfDepth;
            vertices[1] = -halfWidth + halfHeight + halfDepth;
            vertices[2] = halfWidth - halfHeight + halfDepth;
            vertices[3] = halfWidth + halfHeight - halfDepth;
            vertices[4] = -halfWidth - halfHeight + halfDepth;
            vertices[5] = halfWidth - halfHeight - halfDepth;
            vertices[6] = -halfWidth + halfHeight - halfDepth;
            vertices[7] = -halfWidth - halfHeight - halfDepth;

            Matrix4x4 rotationTransform = Matrix4x4.CreateFromQuaternion(_orientation);
            Vector3 center = new Vector3((float)Center.X, (float)Center.Y, (float)Center.Z);
            Matrix4x4 translation = Matrix4x4.CreateTranslation(center);
            Matrix4x4 world = rotationTransform * translation;

            double[,] worldvectors = new double[4, 4]
            {
                { world.M11,world.M12,world.M13,world.M14 },
                { world.M21,world.M22,world.M23,world.M24 },
                { world.M31,world.M32,world.M33,world.M34 },
                { world.M41,world.M42,world.M43,world.M44 }
            };
            Transform worldT = worldvectors.ToTransform();
            vertices[0].Transform(worldT);
            Vector3d furthestPoint = vertices[0];
            double maxDot = Vector3d.Multiply(furthestPoint, direction);
            for (int i = 1; i < 8; i++)
            {
                vertices[i].Transform(worldT);
                Vector3d vertex = vertices[i];
                double dot = Vector3d.Multiply(vertex, direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    furthestPoint = vertex;
                }
            }
            return furthestPoint;
        }

        public Matrix4x4 CalculateWorld()
        {
            Vector3 center = new Vector3((float)Center.X, (float)Center.Y, (float)Center.Z);
            Vector3 dimensions = new Vector3((float)Dimensions.X, (float)Dimensions.Y, (float)Dimensions.Z);

            return Matrix4x4.CreateScale(dimensions) * Matrix4x4.CreateFromQuaternion(_orientation) * Matrix4x4.CreateTranslation(center);
        }
    }
    public class ConvexHull : GJK.IConvexRegion
    {
        public Vector3d Center;
        Vector3d _halfDimensions = new Vector3d(1, 1, 1);
        public Point3d[] Points { get; set; }
        Vector3d[] Vertices;
        public Vector3d Dimensions
        {
            get { return 2f * _halfDimensions; }
        }

        public Point3d CenterPoint { get; set; }

        public ConvexHull(Point3d[] points)
        {
            this.Points = points;
            Vertices = new Vector3d[Points.Length];
            for (int i = 0; i < Points.Length; i++)
            {
                Vertices[i] = points[i] - Point3d.Origin;
                CenterPoint += points[i];
            }
            CenterPoint /= points.Length;
        }

        public ConvexHull(Vector3d center,float width,float height,float depth)
        {
            Center = center;
            CenterPoint = new Point3d(center.X, center.Y, center.Z);
            _halfDimensions = new Vector3d(width / 2f, height / 2f, depth / 2f);
        }
        
        public Vector3d GetFurthestPoint(Vector3d direction)
        {
            double furthestDistance = double.MinValue;
            Vector3d furthestPoint = Vector3d.Unset;

            foreach (var v in Vertices)
            {
                var currentDistance = Vector3d.Multiply(v, direction);

                if (currentDistance > furthestDistance)
                {
                    furthestDistance = currentDistance;
                    furthestPoint = v;
                }
            }
            return furthestPoint;
        }
    }
}