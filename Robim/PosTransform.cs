using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using static Robim.Util;
using static System.Math;
namespace Robim
{
    public static class PosTransform
    {
        public const double _EPS = 0.00001;

        private static readonly Dictionary<string, int[]> _AXES2TUPLE = new Dictionary<string, int[]>()
        {
            {"sxyz", new int[] {0,0,0,0 } },{"sxyx",new int[] { 0,0,1,0} },{"sxzy", new int[]{ 0,1,0,0} },
            {"sxzx", new int[] {0,1,1,0} },{"syzx", new int[]{1,0,0,0}},{"syzy",new int[]{ 1,0,1,0} },
            {"syxz",new int[]{ 1,1,0,0} },{"syxy", new int[]{ 1,1,1,0} },{"szxy", new int[]{ 2,0,0,0} },
            {"szxz", new int[]{ 2,0,1,0} },{"szyx", new int[]{ 2,1,0,0} },{"szyz", new int[]{ 2,1,1,0} },
            {"rzyx",new int[]{0, 0, 0, 1 } }, {"rxyx", new int[]{0, 0, 1, 1 } }, {"ryzx", new int[]{0, 1, 0, 1 } },
            {"rxzx",new int[]{ 0, 1, 1, 1 } }, {"rxzy", new int[] {1, 0, 0, 1 } }, {"ryzy", new int[]{1, 0, 1, 1 } },
            {"rzxy",new int[]{1, 1, 0, 1 } }, {"ryxy", new int[] {1, 1, 1, 1 } }, {"ryxz", new int[]{2, 0, 0, 1 } },
            {"rzxz", new int[]{2, 0, 1, 1 } }, {"rxyz", new int[]{2, 1, 0, 1}}, {"rzyz", new int[]{2, 1, 1, 1}}

        };

        private static readonly int[] _NEXT_AXIS = new int[] { 1, 2, 0, 1 };
        public static Transform Plane2Matrix(Plane inputplane)
        {
            Transform matrix = Transform.PlaneToPlane(Plane.WorldXY, inputplane);
            return matrix;
        }

        public static Plane Matrix2Plane(Transform matrix)
        {
            return matrix.ToPlane();
        }

        public static Quaternion Plane2Quaternion(Plane inputplane)
        {
            var q = GetRotation(inputplane);
            return q;
        }

        public static Plane Quaternion2Plane(double x, double y, double z, double q1, double q2, double q3, double q4)
        {
            var point = new Point3d(x, y, z);
            var quaternion = new Quaternion(q1, q2, q3, q4);  // qw,qx,qy,qz
            quaternion.GetRotation(out Plane plane);
            plane.Origin = point;
            return plane;
        }


        public static double[] Plane2Euler(Plane inputplane, string axes = "rzyx")
        {
            int firstaxis, parity, repetition, frame;
            double ax, ay, az;
            try
            {
                var tuple = _AXES2TUPLE[axes.ToLower()];
                firstaxis = tuple[0];
                parity = tuple[1];
                repetition = tuple[2];
                frame = tuple[3];
            }
            catch (KeyNotFoundException ex)
            {
                throw ex;
            }
            int i = firstaxis;
            int j = _NEXT_AXIS[i + parity];
            int k = _NEXT_AXIS[i - parity + 1];
            Transform M = Transform.PlaneToPlane(Plane.WorldXY, inputplane);
            if (repetition > 0)
            {
                double sy = Math.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
                if (sy > _EPS)
                {
                    ax = Math.Atan2(M[i, j], M[i, k]);
                    ay = Math.Atan2(sy, M[i, i]);
                    az = Math.Atan2(M[j, i], -M[k, i]);

                }
                else
                {
                    ax = Math.Atan2(-M[j, k], M[j, j]);
                    ay = Math.Atan2(sy, M[i, i]);
                    az = 0.0;
                }
            }
            else
            {
                double cy = Math.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
                if (cy > _EPS)
                {
                    ax = Math.Atan2(M[k, j], M[k, k]);
                    ay = Math.Atan2(-M[k, i], cy);
                    az = Math.Atan2(M[j, i], M[i, i]);
                }
                else
                {
                    ax = Math.Atan2(-M[j, k], M[j, j]);
                    ay = Math.Atan2(-M[k, i], cy);
                    az = 0.0;
                }

            }
            if (parity > 0)
            {
                ax = -ax;
                ay = -ay;
                az = -az;
            }
            if (frame > 0)
            {
                double tmp = ax;
                ax = az;
                az = tmp;
            }
            return new double[] { ax, ay, az };
        }

        public static Plane Euler2Plane(double x, double y, double z, double ai, double aj, double ak, string axes = "rzyx")
        {
            int firstaxis, parity, repetition, frame;
            //double ax, ay, az;
            try
            {
                var tuple = _AXES2TUPLE[axes.ToLower()];
                firstaxis = tuple[0];
                parity = tuple[1];
                repetition = tuple[2];
                frame = tuple[3];
            }
            catch (KeyNotFoundException ex)
            {
                throw ex;
            }
            int i = firstaxis;
            int j = _NEXT_AXIS[i + parity];
            int k = _NEXT_AXIS[i - parity + 1];
            if (frame > 0)
            {
                double temp = ai;
                ai = ak;
                ak = temp;
            }
            if (parity > 0)
            {
                ai = -ai;
                aj = -aj;
                ak = -ak;
            }
            double si = Math.Sin(ai);
            double sj = Math.Sin(aj);
            double sk = Math.Sin(ak);
            double ci = Math.Cos(ai);
            double cj = Math.Cos(aj);
            double ck = Math.Cos(ak);
            double cc = ci * ck;
            double cs = ci * sk;
            double sc = si * ck;
            double ss = si * sk;
            var M = new Transform(1);
            if (repetition > 0)
            {
                M[i, i] = cj;
                M[i, j] = sj * si;
                M[i, k] = sj * ci;
                M[j, i] = sj * sk;
                M[j, j] = -cj * ss + cc;
                M[j, k] = -cj * cs - sc;
                M[k, i] = -sj * ck;
                M[k, j] = cj * sc + cs;
                M[k, k] = cj * cc - ss;
            }
            else
            {
                M[i, i] = cj * ck;
                M[i, j] = sj * sc - cs;
                M[i, k] = sj * cc + ss;
                M[j, i] = cj * sk;
                M[j, j] = sj * ss + cc;
                M[j, k] = sj * cs - sc;
                M[k, i] = -sj;
                M[k, j] = cj * si;
                M[k, k] = cj * ci;
            }
            var plane = M.ToPlane();
            plane.Origin = new Point3d(x, y, z);
            return plane;
        }

        public static Quaternion Euler2Quaternion(double x, double y, double z, double ai, double aj, double ak, string axes = "rzyx")
        {
            Plane plane = Euler2Plane(x, y, z, ai, aj, ak, axes);
            return Plane2Quaternion(plane);

        }

        public static double[] Quaternion2Euler(double x, double y, double z, double q1, double q2, double q3, double q4, string axes = "rzyx")
        {
            Plane plane = Quaternion2Plane(x, y, z, q1, q2, q3, q4);
            return Plane2Euler(plane, axes);
        }

        public static double[] Plane2AxisAngle(Plane plane)
        {
            Vector3d vector;
            Transform matrix = Transform.PlaneToPlane(Plane.WorldXY, plane);

            double[][] m = new double[3][];
            m[0] = new double[] { matrix[0, 0], matrix[0, 1], matrix[0, 2] };
            m[1] = new double[] { matrix[1, 0], matrix[1, 1], matrix[1, 2] };
            m[2] = new double[] { matrix[2, 0], matrix[2, 1], matrix[2, 2] };

            double angle, x, y, z; // variables for result
            double epsilon = 0.01; // margin to allow for rounding errors
            double epsilon2 = 0.1; // margin to distinguish between 0 and 180 degrees
                                   // optional check that input is pure rotation, 'isRotationMatrix' is defined at:
                                   // http://www.euclideanspace.com/maths/algebra/matrix/orthogonal/rotation/
                                   // assert isRotationMatrix(m) : "not valid rotation matrix";// for debugging
            if ((Abs(m[0][1] - m[1][0]) < epsilon)
              && (Abs(m[0][2] - m[2][0]) < epsilon)
            && (Abs(m[1][2] - m[2][1]) < epsilon))
            {
                // singularity found
                // first check for identity matrix which must have +1 for all terms
                //  in leading diagonal and zero in other terms
                if ((Abs(m[0][1] + m[1][0]) < epsilon2)
                  && (Abs(m[0][2] + m[2][0]) < epsilon2)
                  && (Abs(m[1][2] + m[2][1]) < epsilon2)
                && (Abs(m[0][0] + m[1][1] + m[2][2] - 3) < epsilon2))
                {
                    // this singularity is identity matrix so angle = 0
                    return new double[] { plane.OriginX, plane.OriginY, plane.OriginZ, 0, 0, 0 }; // zero angle, arbitrary axis
                }
                // otherwise this singularity is angle = 180
                angle = PI;
                double xx = (m[0][0] + 1) / 2;
                double yy = (m[1][1] + 1) / 2;
                double zz = (m[2][2] + 1) / 2;
                double xy = (m[0][1] + m[1][0]) / 4;
                double xz = (m[0][2] + m[2][0]) / 4;
                double yz = (m[1][2] + m[2][1]) / 4;
                if ((xx > yy) && (xx > zz))
                { // m[0][0] is the largest diagonal term
                    if (xx < epsilon)
                    {
                        x = 0;
                        y = 0.7071;
                        z = 0.7071;
                    }
                    else
                    {
                        x = Sqrt(xx);
                        y = xy / x;
                        z = xz / x;
                    }
                }
                else if (yy > zz)
                { // m[1][1] is the largest diagonal term
                    if (yy < epsilon)
                    {
                        x = 0.7071;
                        y = 0;
                        z = 0.7071;
                    }
                    else
                    {
                        y = Sqrt(yy);
                        x = xy / y;
                        z = yz / y;
                    }
                }
                else
                { // m[2][2] is the largest diagonal term so base result on this
                    if (zz < epsilon)
                    {
                        x = 0.7071;
                        y = 0.7071;
                        z = 0;
                    }
                    else
                    {
                        z = Sqrt(zz);
                        x = xz / z;
                        y = yz / z;
                    }
                }
                vector = new Vector3d(x, y, z);
                vector.Unitize();
                vector *= angle;
                return new double[] { plane.OriginX, plane.OriginY, plane.OriginZ, vector.X, vector.Y, vector.Z }; // return 180 deg rotation
            }
            // as we have reached here there are no singularities so we can handle normally
            double s = Sqrt((m[2][1] - m[1][2]) * (m[2][1] - m[1][2])
              + (m[0][2] - m[2][0]) * (m[0][2] - m[2][0])
              + (m[1][0] - m[0][1]) * (m[1][0] - m[0][1])); // used to normalise
            if (Abs(s) < 0.001) s = 1;
            // prevent divide by zero, should not happen if matrix is orthogonal and should be
            // caught by singularity test above, but I've left it in just in case
            angle = Acos((m[0][0] + m[1][1] + m[2][2] - 1) / 2);
            x = (m[2][1] - m[1][2]) / s;
            y = (m[0][2] - m[2][0]) / s;
            z = (m[1][0] - m[0][1]) / s;
            vector = new Vector3d(x, y, z);
            vector.Unitize();
            vector *= angle;
            return new double[] { vector.X, vector.Y, vector.Z }; // return 180 deg rotation
        }

        public static Plane AxisAngle2Plane(double x, double y, double z, double vx, double vy, double vz)
        {
            var matrix = Transform.Identity;
            var vector = new Vector3d(vx, vy, vz);
            double angle = vector.Length;
            vector.Unitize();

            double c = Cos(angle);
            double s = Sin(angle);
            double t = 1.0 - c;

            matrix.M00 = c + vector.X * vector.X * t;
            matrix.M11 = c + vector.Y * vector.Y * t;
            matrix.M22 = c + vector.Z * vector.Z * t;

            double tmp1 = vector.X * vector.Y * t;
            double tmp2 = vector.Z * s;
            matrix.M10 = tmp1 + tmp2;
            matrix.M01 = tmp1 - tmp2;
            tmp1 = vector.X * vector.Z * t;
            tmp2 = vector.Y * s;
            matrix.M20 = tmp1 - tmp2;
            matrix.M02 = tmp1 + tmp2; tmp1 = vector.Y * vector.Z * t;
            tmp2 = vector.X * s;
            matrix.M21 = tmp1 + tmp2;
            matrix.M12 = tmp1 - tmp2;

            Plane plane = Plane.WorldXY;
            plane.Transform(matrix);
            plane.Origin = new Point3d(x, y, z);
            return plane;
        }
    }
}
