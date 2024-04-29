using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Robim;

namespace RC
{
    public static class GJK
    {
        public static bool IsInSameDirection(this Rhino.Geometry.Vector3d vector, Rhino.Geometry.Vector3d otherVector)
        {
            return Rhino.Geometry.Vector3d.Multiply(vector, otherVector) > 0;
        }
        public static bool IsInOppositeDirection(this Rhino.Geometry.Vector3d vector, Rhino.Geometry.Vector3d otherVector)
        {
            return Rhino.Geometry.Vector3d.Multiply(vector, otherVector) < 0;
        }
        public interface IConvexRegion
        {
            /// <summary>
            /// Calculates the furthest point on the region 
            /// along a given direction.
            /// </summary>
            Rhino.Geometry.Vector3d GetFurthestPoint(Rhino.Geometry.Vector3d direction);
        }
    }
    class Simplex
    {
        List<Rhino.Geometry.Vector3d> _vertices = new List<Rhino.Geometry.Vector3d>();

        public int Count
        {
            get { return _vertices.Count; }
        }

        public Rhino.Geometry.Vector3d this[int i]
        {
            get { return _vertices[i]; }
        }

        public Simplex(params Rhino.Geometry.Vector3d[] vertices)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                _vertices.Add(vertices[i]);
            }
        }

        public void Add(Rhino.Geometry.Vector3d vertex)
        {
            _vertices.Add(vertex);
        }

        public void Remove(Rhino.Geometry.Vector3d vertex)
        {
            _vertices.Remove(vertex);
        }
    }
    public static class GJKAlgorithm
    {
        public static bool Intersects(GJK.IConvexRegion regioneOne, GJK.IConvexRegion regionTwo)
        {
            //Get an initial point on the Minkowski difference.
            Rhino.Geometry.Vector3d OneV = new Rhino.Geometry.Vector3d(1, 1, 1);
            Rhino.Geometry.Vector3d s = Support(regioneOne, regionTwo, OneV);

            //Create our initial simplex.
            Simplex simplex = new Simplex(s);

            //Choose an initial direction toward the origin.
            Rhino.Geometry.Vector3d d = -s;

            //Choose a maximim number of iterations to avoid an 
            //infinite loop during a non-convergent search.
            int maxIterations = 50;

            for (int i = 0; i < maxIterations; i++)
            {
                //Get our next simplex point toward the origin.
                Rhino.Geometry.Vector3d a = Support(regioneOne, regionTwo, d);

                //If we move toward the origin and didn't pass it 
                //then we never will and there's no intersection.
                if (a.IsInOppositeDirection(d))
                {
                    return false;
                }
                //otherwise we add the new
                //point to the simplex and
                //process it.
                simplex.Add(a);
                //Here we either find a collision or we find the closest feature of
                //the simplex to the origin, make that the new simplex and update the direction
                //to move toward the origin from that feature.
                if (ProcessSimplex(ref simplex, ref d))
                {
                    return true;
                }
            }
            //If we still couldn't find a simplex 
            //that contains the origin then we
            //"probably" have an intersection.
            return false;
        }

        /// <summary>
        ///Either finds a collision or the closest feature of the simplex to the origin, 
        ///and updates the simplex and direction.
        /// </summary>
        static bool ProcessSimplex(ref Simplex simplex, ref Rhino.Geometry.Vector3d direction)
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
                return ProcessTetrehedron(ref simplex, ref direction);
            }
        }

        /// <summary>
        /// Determines which Veronoi region of a line segment 
        /// the origin is in, utilizing the preserved winding
        /// of the simplex to eliminate certain regions.
        /// </summary>
        static bool ProcessLine(ref Simplex simplex, ref Rhino.Geometry.Vector3d direction)
        {
            Rhino.Geometry.Vector3d a = simplex[1];
            Rhino.Geometry.Vector3d b = simplex[0];
            Rhino.Geometry.Vector3d ab = b - a;
            Rhino.Geometry.Vector3d aO = -a;

            if (ab.IsInSameDirection(aO))
            {
                double dot = Rhino.Geometry.Vector3d.Multiply(ab, aO);
                float angle = (float)Math.Acos(dot / (ab.Length * aO.Length));
                direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ab, aO), ab);
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
        static bool ProcessTriangle(ref Simplex simplex, ref Rhino.Geometry.Vector3d direction)
        {
            Rhino.Geometry.Vector3d a = simplex[2];
            Rhino.Geometry.Vector3d b = simplex[1];
            Rhino.Geometry.Vector3d c = simplex[0];
            Rhino.Geometry.Vector3d ab = b - a;
            Rhino.Geometry.Vector3d ac = c - a;
            Rhino.Geometry.Vector3d abc = Rhino.Geometry.Vector3d.CrossProduct(ab, ac);
            Rhino.Geometry.Vector3d aO = -a;
            Rhino.Geometry.Vector3d acNormal = Rhino.Geometry.Vector3d.CrossProduct(abc, ac);
            Rhino.Geometry.Vector3d abNormal = Rhino.Geometry.Vector3d.CrossProduct(ab, abc);

            if (acNormal.IsInSameDirection(aO))
            {
                if (ac.IsInSameDirection(aO))
                {
                    simplex.Remove(b);
                    direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ac, aO), ac);
                }
                else
                {
                    if (ab.IsInSameDirection(aO))
                    {
                        simplex.Remove(c);
                        direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ab, aO), ab);
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
                        direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ab, aO), ab);
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
                        direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(abc, aO), abc);
                    }
                    else
                    {
                        direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(-abc, aO), -abc);
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
        static bool ProcessTetrehedron(ref Simplex simplex, ref Rhino.Geometry.Vector3d direction)
        {
            Rhino.Geometry.Vector3d a = simplex[3];
            Rhino.Geometry.Vector3d b = simplex[2];
            Rhino.Geometry.Vector3d c = simplex[1];
            Rhino.Geometry.Vector3d d = simplex[0];
            Rhino.Geometry.Vector3d ac = c - a;
            Rhino.Geometry.Vector3d ad = d - a;
            Rhino.Geometry.Vector3d ab = b - a;
            Rhino.Geometry.Vector3d bc = c - b;
            Rhino.Geometry.Vector3d bd = d - b;

            Rhino.Geometry.Vector3d acd = Rhino.Geometry.Vector3d.CrossProduct(ad, ac);
            Rhino.Geometry.Vector3d abd = Rhino.Geometry.Vector3d.CrossProduct(ab, ad);
            Rhino.Geometry.Vector3d abc = Rhino.Geometry.Vector3d.CrossProduct(ac, ab);

            Rhino.Geometry.Vector3d aO = -a;

            if (abc.IsInSameDirection(aO))
            {
                if (Rhino.Geometry.Vector3d.CrossProduct(abc, ac).IsInSameDirection(aO))
                {
                    simplex.Remove(b);
                    simplex.Remove(d);
                    direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ac, aO), ac);
                }
                else if (Rhino.Geometry.Vector3d.CrossProduct(ab, abc).IsInSameDirection(aO))
                {
                    simplex.Remove(c);
                    simplex.Remove(d);
                    direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ab, aO), ab);
                }
                else
                {
                    simplex.Remove(d);
                    direction = abc;
                }
            }
            else if (acd.IsInSameDirection(aO))
            {
                if (Rhino.Geometry.Vector3d.CrossProduct(acd, ad).IsInSameDirection(aO))
                {
                    simplex.Remove(b);
                    simplex.Remove(c);
                    direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ad, aO), ad);
                }
                else if (Rhino.Geometry.Vector3d.CrossProduct(ac, acd).IsInSameDirection(aO))
                {
                    simplex.Remove(b);
                    simplex.Remove(d);
                    direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ac, aO), ac);
                }
                else
                {
                    simplex.Remove(b);
                    direction = acd;
                }
            }
            else if (abd.IsInSameDirection(aO))
            {
                if (Rhino.Geometry.Vector3d.CrossProduct(abd, ab).IsInSameDirection(aO))
                {
                    simplex.Remove(c);
                    simplex.Remove(d);
                    direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ab, aO), ab);
                }
                else if (Rhino.Geometry.Vector3d.CrossProduct(ad, abd).IsInSameDirection(aO))
                {
                    simplex.Remove(b);
                    simplex.Remove(c);
                    direction = Rhino.Geometry.Vector3d.CrossProduct(Rhino.Geometry.Vector3d.CrossProduct(ad, aO), ad);
                }
                else
                {
                    simplex.Remove(c);
                    direction = abd;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the furthest point on the Minkowski 
        /// difference along a given direction.
        /// </summary>
        static Rhino.Geometry.Vector3d Support(GJK.IConvexRegion regionOne,GJK.IConvexRegion regionTwo,Rhino.Geometry.Vector3d direction)
        {
            return regionOne.GetFurthestPoint(direction) - regionTwo.GetFurthestPoint(-direction);
        }
    }
    public class Sphere : GJK.IConvexRegion
    {
        public Rhino.Geometry.Vector3d Center;
        public float Radius;

        public Sphere(Rhino.Geometry.Vector3d center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public Rhino.Geometry.Vector3d GetFurthestPoint(Rhino.Geometry.Vector3d direction)
        {
            if (direction != Rhino.Geometry.Vector3d.Zero)
            {
                direction.Unitize();
            }
            return Center + Radius * direction;
        }
    }
    public class Box : GJK.IConvexRegion
    {
        public Rhino.Geometry.Vector3d Center;
        Rhino.Geometry.Vector3d _halfDimensions = new Rhino.Geometry.Vector3d(1, 1, 1);
        Rhino.Geometry.Quaternion _orientation = Rhino.Geometry.Quaternion.Identity;

        public Rhino.Geometry.Vector3d Dimensions
        {
            get { return 2f * _halfDimensions; }
        }
        public Box(Rhino.Geometry.Box box) : this(box.BoundingBox.Diagonal/2, (float)box.X.Length, (float)box.Z.Length, (float)box.Y.Length) { }
        public Box(Rhino.Geometry.Vector3d center): this(center, 1f, 1f, 1f) { }
        public Box(Rhino.Geometry.Vector3d center,float width,float height,float depth) : this(center, width, height, depth, Transform.Identity) { }
        public Box(Rhino.Geometry.Vector3d center,float width,float height,float depth,Transform rotationMatrix)
        {
            Center = center;
            _halfDimensions = new Rhino.Geometry.Vector3d(width / 2f, height / 2f,depth / 2f);
            _orientation = Util.GetRotation(rotationMatrix.ToPlane());
        }

        public Rhino.Geometry.Vector3d GetFurthestPoint(Rhino.Geometry.Vector3d direction)
        {
            Rhino.Geometry.Vector3d BackV = new Rhino.Geometry.Vector3d(0, 0, -1);
            Rhino.Geometry.Vector3d halfHeight = _halfDimensions.Y * Rhino.Geometry.Vector3d.YAxis;
            Rhino.Geometry.Vector3d halfWidth = _halfDimensions.X * Rhino.Geometry.Vector3d.XAxis;
            Rhino.Geometry.Vector3d halfDepth = _halfDimensions.Z * BackV;

            Rhino.Geometry.Vector3d[] vertices = new Rhino.Geometry.Vector3d[8];
            vertices[0] = halfWidth + halfHeight + halfDepth;
            vertices[1] = -halfWidth + halfHeight + halfDepth;
            vertices[2] = halfWidth - halfHeight + halfDepth;
            vertices[3] = halfWidth + halfHeight - halfDepth;
            vertices[4] = -halfWidth - halfHeight + halfDepth;
            vertices[5] = halfWidth - halfHeight - halfDepth;
            vertices[6] = -halfWidth + halfHeight - halfDepth;
            vertices[7] = -halfWidth - halfHeight - halfDepth;

            Transform rotationTransform = _orientation.MatrixForm();
            Transform translation = Transform.Translation(Center);
            Transform world = rotationTransform * translation;

            vertices[0].Transform(world);
            Rhino.Geometry.Vector3d furthestPoint = vertices[0];
            double maxDot = Rhino.Geometry.Vector3d.Multiply(furthestPoint, direction);
            for (int i = 1; i < 8; i++)
            {
                vertices[i].Transform(world);
                Rhino.Geometry.Vector3d vertex = vertices[i];
                double dot = Rhino.Geometry.Vector3d.Multiply(vertex, direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    furthestPoint = vertex;
                }
            }
            return furthestPoint;
        }

        public Transform CalculateWorld()
        {
            Vector3 dimensions = new Vector3((float)Dimensions.X, (float)Dimensions.Y, (float)Dimensions.Z);
            Matrix4x4 matrix4X4 = Matrix4x4.CreateScale(dimensions);
            Transform transform = new double[4, 4]
            {
                { matrix4X4.M11, matrix4X4.M12, matrix4X4.M13, matrix4X4.M14 },
                { matrix4X4.M21, matrix4X4.M22, matrix4X4.M23, matrix4X4.M24 },
                { matrix4X4.M31, matrix4X4.M32, matrix4X4.M33, matrix4X4.M34 },
                { matrix4X4.M41, matrix4X4.M42, matrix4X4.M43, matrix4X4.M44}
            }.ToTransform();
            return  transform * _orientation.MatrixForm() * Transform.Translation(Center);
        }
    }
}