using Grasshopper.Kernel.Types.Transforms;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robim
{
    public class TargetCurve
    {
        public static TargetCurve Default { get; }
        public Curve Curve { get; set; }
        public Curve[] Curves { get; set; }
        public Line[] Lines { get; set; }
        public Arc[] Arcs { get; set; }
        public Plane PlaneofStartPoint { get; set; }
        public Plane[] PlanesofLines { get; set; }
        public Plane[] PlanesofArcs { get; set; }
        public int[] Pattern { get; set; }
        public int[] PatternB { get; set; }

        public TargetCurve(Curve curve, Curve[] curves, Line[] lines, Arc[] arcs, Plane pstart, Plane[] plines, Plane[] parcs, int[] pattern,int [] patternB)
        {
            this.Curve = curve;
            this.Curves = curves;
            this.Lines = lines;
            this.Arcs = arcs;
            this.PlaneofStartPoint = pstart;
            this.PlanesofLines = plines;
            this.PlanesofArcs = parcs;
            this.Pattern = pattern;
            this.PatternB = patternB;
        }
        
        public static void SplitCurve(Curve curve,CurveSetting curveSetting,Plane standardplane, double tor, double ator, double min, double max, out TargetCurve circularTransfer)
        {
            Plane beginplane = Plane.Unset;
            int z = 0;
            List<Line> lines = new List<Line>();
            List<Arc> arcs = new List<Arc>();
            List<Plane> LinesP = new List<Plane>();
            List<Plane> ArcsP = new List<Plane>();
            List<int> pattern = new List<int>();
            List<int> patternB = new List<int>();
            PolyCurve polyCurve = curve.ToArcsAndLines(tor, ator, min, max);
            Curve[] curves = polyCurve.Explode();
            //Plane startp = new Plane(curve.PointAtStart, standardplane.XAxis, standardplane.YAxis);
            Plane startp = GetTangentPlane(curve, curve.PointAtStart, curveSetting);
            if(z == 0)
            {
                beginplane = startp;
                z = 1;
            }
            for (int i = 0; i < curves.Length; i++)
            {
                if (curves[i].TryGetArc(out Arc arc))
                {
                    arcs.Add(arc);
                    //Plane midp = new Plane(arc.MidPoint, standardplane.XAxis, standardplane.YAxis);
                    //Plane endp = new Plane(arc.EndPoint, standardplane.XAxis, standardplane.YAxis);
                    //Plane midp = GetTangentPlane(curve, arc.MidPoint, curveSetting);
                    //Plane endp = GetTangentPlane(curve, arc.EndPoint, curveSetting);
                    double startt = arc.ClosestParameter(arc.StartPoint);
                    double midt = arc.ClosestParameter(arc.MidPoint);
                    double endt = arc.ClosestParameter(arc.EndPoint);
                    Plane startbasep = new Plane(arc.StartPoint, arc.TangentAt(startt));
                    Plane midbasep = new Plane(arc.MidPoint, arc.TangentAt(midt)); 
                    Plane endbasep = new Plane(arc.EndPoint, arc.TangentAt(endt));
                    Plane midp = beginplane;
                    midp.Transform(Transform.PlaneToPlane(startbasep, midbasep));
                    Plane endp = beginplane;
                    endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));
                    ArcsP.Add(midp);
                    ArcsP.Add(endp);
                    pattern.Add(1);
                    pattern.Add(1);
                    patternB.Add(1);
                    beginplane = endp;
                }
                else if (curves[i].TryGetPolyline(out Polyline polyline))
                {
                    Line[] lines1 = polyline.GetSegments();
                    for (int j = 0; j < lines1.Length; j++)
                    {
                        lines.Add(lines1[j]);
                        //Plane endp = new Plane(lines1[j].To, standardplane.XAxis, standardplane.YAxis);
                        //Plane endp = GetTangentPlane(curve, lines1[j].To, curveSetting);
                        Plane startbasep = new Plane(lines1[j].From, lines1[j].UnitTangent);
                        Plane endbasep = new Plane(lines1[j].To, lines1[j].UnitTangent);
                        Plane endp = beginplane;
                        endp.Transform(Transform.PlaneToPlane(startbasep,endbasep));
                        LinesP.Add(endp);
                        pattern.Add(0);
                        patternB.Add(0);
                        beginplane = endp;
                    }
                }
                else//变成polyline后就不会进来
                {
                    Line line = new Line(curves[i].PointAtStart, curves[i].PointAtEnd);
                    lines.Add(line);
                    Plane startbasep = new Plane(line.From, line.UnitTangent);
                    Plane endbasep = new Plane(line.To, line.UnitTangent);
                    Plane endp = beginplane;
                    endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));
                    //Plane endp = new Plane(curves[i].PointAtEnd, standardplane.XAxis, standardplane.YAxis);
                    //Plane endp = GetTangentPlane(curve, curves[i].PointAtEnd, curveSetting);
                    LinesP.Add(endp);
                    pattern.Add(0);
                    patternB.Add(0);
                    beginplane = endp;
                }
            }
            circularTransfer = new TargetCurve(curve, curves, lines.ToArray(), arcs.ToArray(), startp, LinesP.ToArray(), ArcsP.ToArray(), pattern.ToArray(), patternB.ToArray());
        }
        public static void SplitListCurve_V(Curve[] curves, CurveSetting curveSetting, double tor, double ator, double min, double max, out TargetCurve[] circularTransfers)
        {
            circularTransfers = new TargetCurve[curves.Length];
            Plane beginplane =  Plane.Unset;
            int firststrp = 0;
            
            for(int m = 0; m < curves.Length; m++)
            {
                Curve curve = curves[m];
                List<Line> lines = new List<Line>();
                List<Arc> arcs = new List<Arc>();
                List<Plane> LinesP = new List<Plane>();
                List<Plane> ArcsP = new List<Plane>();
                List<int> pattern = new List<int>();
                List<int> patternB = new List<int>();
                PolyCurve polyCurve = curve.ToArcsAndLines(tor, ator, min, max);
                Curve[] explodecurves = polyCurve.Explode();

                Plane startp = Plane.Unset;
                if(beginplane.Origin != curve.PointAtStart)
                {
                    firststrp = 0;
                }
                if (firststrp == 0)
                {
                    curve.ClosestPoint(curve.PointAtStart, out double t);
                    curve.PerpendicularFrameAt(t, out Plane plane);
                    Plane plane2 = plane;
                    plane2.Rotate(curveSetting.Xangle, plane.XAxis);
                    plane2.Rotate(curveSetting.Yangle, plane.YAxis);
                    plane2.Rotate(curveSetting.Zangle, plane.ZAxis);
                    beginplane = startp = plane2;
                    firststrp = 1;
                }
                else
                {
                    startp = beginplane;
                }

                for (int i = 0; i < explodecurves.Length; i++)
                {
                    if (explodecurves[i].TryGetArc(out Arc arc))
                    {
                        arcs.Add(arc);

                        double startt = arc.ClosestParameter(arc.StartPoint);
                        double midt = arc.ClosestParameter(arc.MidPoint);
                        double endt = arc.ClosestParameter(arc.EndPoint);
                        Plane startbasep = new Plane(arc.StartPoint, arc.TangentAt(startt));
                        Plane midbasep = new Plane(arc.MidPoint, arc.TangentAt(midt));
                        Plane endbasep = new Plane(arc.EndPoint, arc.TangentAt(endt));
                        Plane midp = beginplane;
                        midp.Transform(Transform.PlaneToPlane(startbasep, midbasep));
                        Plane endp = beginplane;
                        endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));

                        ArcsP.Add(midp);
                        ArcsP.Add(endp);
                        pattern.Add(1);
                        pattern.Add(1);
                        patternB.Add(1);
                        beginplane = endp;
                    }
                    else if (explodecurves[i].TryGetPolyline(out Polyline polyline))
                    {
                        Line[] lines1 = polyline.GetSegments();
                        for (int j = 0; j < lines1.Length; j++)
                        {
                            lines.Add(lines1[j]);

                            Plane startbasep = new Plane(lines1[j].From, lines1[j].UnitTangent);
                            Plane endbasep = new Plane(lines1[j].To, lines1[j].UnitTangent);
                            Plane endp = beginplane;
                            endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));

                            LinesP.Add(endp);
                            pattern.Add(0);
                            patternB.Add(0);
                            beginplane = endp;
                        }
                    }
                    else//变成polyline后就不会进来
                    {
                        Line line = new Line(explodecurves[i].PointAtStart, explodecurves[i].PointAtEnd);
                        lines.Add(line);

                        Plane startbasep = new Plane(line.From, line.UnitTangent);
                        Plane endbasep = new Plane(line.To, line.UnitTangent);
                        Plane endp = beginplane;
                        endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));

                        LinesP.Add(endp);
                        pattern.Add(0);
                        patternB.Add(0);
                        beginplane = endp;
                    }
                }
                circularTransfers[m] = new TargetCurve(curve, explodecurves, lines.ToArray(), arcs.ToArray(), startp, LinesP.ToArray(), ArcsP.ToArray(), pattern.ToArray(), patternB.ToArray());
            }
        }
        public static void SplitListCurve_S(Curve[] curves, CurveSetting curveSetting, double tor, double ator, double min, double max, out TargetCurve[] circularTransfers)
        {
            circularTransfers = new TargetCurve[curves.Length];
            Plane plane2 = curveSetting.Plane;
            plane2.Rotate(curveSetting.Xangle, curveSetting.Plane.XAxis);
            plane2.Rotate(curveSetting.Yangle, curveSetting.Plane.YAxis);
            plane2.Rotate(curveSetting.Zangle, curveSetting.Plane.ZAxis);
            Plane standardplane = plane2;

            for (int m = 0; m < curves.Length; m++)
            {
                Curve curve = curves[m];
                List<Line> lines = new List<Line>();
                List<Arc> arcs = new List<Arc>();
                List<Plane> LinesP = new List<Plane>();
                List<Plane> ArcsP = new List<Plane>();
                List<int> pattern = new List<int>();
                List<int> patternB = new List<int>();
                PolyCurve polyCurve = curve.ToArcsAndLines(tor, ator, min, max);
                Curve[] explodecurves = polyCurve.Explode();

                Plane startp = new Plane(curve.PointAtStart, standardplane.XAxis, standardplane.YAxis);

                for (int i = 0; i < explodecurves.Length; i++)
                {
                    if (explodecurves[i].TryGetArc(out Arc arc))
                    {
                        arcs.Add(arc);

                        Plane midp = new Plane(arc.MidPoint, standardplane.XAxis, standardplane.YAxis);
                        Plane endp = new Plane(arc.EndPoint, standardplane.XAxis, standardplane.YAxis);

                        ArcsP.Add(midp);
                        ArcsP.Add(endp);
                        pattern.Add(1);
                        pattern.Add(1);
                        patternB.Add(1);
                    }
                    else if (explodecurves[i].TryGetPolyline(out Polyline polyline))
                    {
                        Line[] lines1 = polyline.GetSegments();
                        for (int j = 0; j < lines1.Length; j++)
                        {
                            lines.Add(lines1[j]);
                            Plane endp = new Plane(lines1[j].To, standardplane.XAxis, standardplane.YAxis);
                            LinesP.Add(endp);
                            pattern.Add(0);
                            patternB.Add(0);
                        }
                    }
                    else//变成polyline后就不会进来
                    {
                        Line line = new Line(explodecurves[i].PointAtStart, explodecurves[i].PointAtEnd);
                        lines.Add(line);
                        Plane endp = new Plane(line.To, standardplane.XAxis, standardplane.YAxis);
                        LinesP.Add(endp);
                        pattern.Add(0);
                        patternB.Add(0);
                    }
                }
                circularTransfers[m] = new TargetCurve(curve, explodecurves, lines.ToArray(), arcs.ToArray(), startp, LinesP.ToArray(), ArcsP.ToArray(), pattern.ToArray(), patternB.ToArray());
            }
        }
        public static void SplitCurve_V(Curve curve, CurveSetting curveSetting, double tor, double ator, double min, double max, out TargetCurve circularTransfer)
        {
            Plane beginplane = Plane.Unset;
            int firststrp = 0;
            List<Line> lines = new List<Line>();
            List<Arc> arcs = new List<Arc>();
            List<Plane> LinesP = new List<Plane>();
            List<Plane> ArcsP = new List<Plane>();
            List<int> pattern = new List<int>();
            List<int> patternB = new List<int>();
            PolyCurve polyCurve = curve.ToArcsAndLines(tor, ator, min, max);
            Curve[] explodecurves = polyCurve.Explode();

            Plane startp = Plane.Unset;
            if (beginplane.Origin != curve.PointAtStart)
            {
                firststrp = 0;
            }
            if (firststrp == 0)
            {
                curve.ClosestPoint(curve.PointAtStart, out double t);
                curve.PerpendicularFrameAt(t, out Plane plane);
                Plane plane2 = plane;
                plane2.Rotate(curveSetting.Xangle, plane.XAxis);
                plane2.Rotate(curveSetting.Yangle, plane.YAxis);
                plane2.Rotate(curveSetting.Zangle, plane.ZAxis);
                beginplane = startp = plane2;
                firststrp = 1;
            }
            else
            {
                startp = beginplane;
            }

            for (int i = 0; i < explodecurves.Length; i++)
            {
                if (explodecurves[i].TryGetArc(out Arc arc))
                {
                    arcs.Add(arc);

                    double startt = arc.ClosestParameter(arc.StartPoint);
                    double midt = arc.ClosestParameter(arc.MidPoint);
                    double endt = arc.ClosestParameter(arc.EndPoint);
                    Plane startbasep = new Plane(arc.StartPoint, arc.TangentAt(startt));
                    Plane midbasep = new Plane(arc.MidPoint, arc.TangentAt(midt));
                    Plane endbasep = new Plane(arc.EndPoint, arc.TangentAt(endt));
                    Plane midp = beginplane;
                    midp.Transform(Transform.PlaneToPlane(startbasep, midbasep));
                    Plane endp = beginplane;
                    endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));

                    ArcsP.Add(midp);
                    ArcsP.Add(endp);
                    pattern.Add(1);
                    pattern.Add(1);
                    patternB.Add(1);
                    beginplane = endp;
                }
                else if (explodecurves[i].TryGetPolyline(out Polyline polyline))
                {
                    Line[] lines1 = polyline.GetSegments();
                    for (int j = 0; j < lines1.Length; j++)
                    {
                        lines.Add(lines1[j]);

                        Plane startbasep = new Plane(lines1[j].From, lines1[j].UnitTangent);
                        Plane endbasep = new Plane(lines1[j].To, lines1[j].UnitTangent);
                        Plane endp = beginplane;
                        endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));

                        LinesP.Add(endp);
                        pattern.Add(0);
                        patternB.Add(0);
                        beginplane = endp;
                    }
                }
                else//变成polyline后就不会进来
                {
                    Line line = new Line(explodecurves[i].PointAtStart, explodecurves[i].PointAtEnd);
                    lines.Add(line);

                    Plane startbasep = new Plane(line.From, line.UnitTangent);
                    Plane endbasep = new Plane(line.To, line.UnitTangent);
                    Plane endp = beginplane;
                    endp.Transform(Transform.PlaneToPlane(startbasep, endbasep));

                    LinesP.Add(endp);
                    pattern.Add(0);
                    patternB.Add(0);
                    beginplane = endp;
                }
            }
            circularTransfer = new TargetCurve(curve, explodecurves, lines.ToArray(), arcs.ToArray(), startp, LinesP.ToArray(), ArcsP.ToArray(), pattern.ToArray(), patternB.ToArray());
        }
        public static void SplitCurve_S(Curve curve, CurveSetting curveSetting, double tor, double ator, double min, double max, out TargetCurve circularTransfer)
        {
            Plane plane2 = curveSetting.Plane;
            plane2.Rotate(curveSetting.Xangle, curveSetting.Plane.XAxis);
            plane2.Rotate(curveSetting.Yangle, curveSetting.Plane.YAxis);
            plane2.Rotate(curveSetting.Zangle, curveSetting.Plane.ZAxis);
            Plane standardplane = plane2;

            List<Line> lines = new List<Line>();
            List<Arc> arcs = new List<Arc>();
            List<Plane> LinesP = new List<Plane>();
            List<Plane> ArcsP = new List<Plane>();
            List<int> pattern = new List<int>();
            List<int> patternB = new List<int>();
            PolyCurve polyCurve = curve.ToArcsAndLines(tor, ator, min, max);
            Curve[] explodecurves = polyCurve.Explode();

            Plane startp = new Plane(curve.PointAtStart, standardplane.XAxis, standardplane.YAxis);

            for (int i = 0; i < explodecurves.Length; i++)
            {
                if (explodecurves[i].TryGetArc(out Arc arc))
                {
                    arcs.Add(arc);

                    Plane midp = new Plane(arc.MidPoint, standardplane.XAxis, standardplane.YAxis);
                    Plane endp = new Plane(arc.EndPoint, standardplane.XAxis, standardplane.YAxis);

                    ArcsP.Add(midp);
                    ArcsP.Add(endp);
                    pattern.Add(1);
                    pattern.Add(1);
                    patternB.Add(1);
                }
                else if (explodecurves[i].TryGetPolyline(out Polyline polyline))
                {
                    Line[] lines1 = polyline.GetSegments();
                    for (int j = 0; j < lines1.Length; j++)
                    {
                        lines.Add(lines1[j]);
                        Plane endp = new Plane(lines1[j].To, standardplane.XAxis, standardplane.YAxis);
                        LinesP.Add(endp);
                        pattern.Add(0);
                        patternB.Add(0);
                    }
                }
                else//变成polyline后就不会进来
                {
                    Line line = new Line(explodecurves[i].PointAtStart, explodecurves[i].PointAtEnd);
                    lines.Add(line);
                    Plane endp = new Plane(line.To, standardplane.XAxis, standardplane.YAxis);
                    LinesP.Add(endp);
                    pattern.Add(0);
                    patternB.Add(0);
                }
            }
            circularTransfer = new TargetCurve(curve, explodecurves, lines.ToArray(), arcs.ToArray(), startp, LinesP.ToArray(), ArcsP.ToArray(), pattern.ToArray(), patternB.ToArray());
        }
        private static Plane GetTangentPlane(Curve curve,Point3d curvepoint,CurveSetting curveSetting)
        {
            Plane plane1 = Plane.Unset;
            if (curveSetting.Variable)
            {
                curve.ClosestPoint(curvepoint, out double t);
                curve.PerpendicularFrameAt(t, out Plane plane);
                Plane plane2 = plane;
                plane2.Rotate(curveSetting.Xangle, plane.XAxis);
                plane2.Rotate(curveSetting.Yangle, plane.YAxis);
                plane2.Rotate(curveSetting.Zangle, plane.ZAxis);
                plane1 = plane2;
            }
            else
            {
                plane1 = new Plane(curvepoint, curveSetting.Plane.XAxis, curveSetting.Plane.YAxis);
            }
            return plane1;
        }
    }
    public class CurveSetting
    {
        public Plane Plane { get; set; }
        public bool Variable { get; set; }
        public double Xangle { get; set; }
        public double Yangle { get; set; }
        public double Zangle { get; set; }

        public CurveSetting(Plane standardplane,bool variable ,double Xangle ,double Yangle,double Zangle)
        {
            this.Plane = standardplane;
            this.Variable = variable;
            this.Xangle = Xangle * Math.PI / 180;
            this.Yangle = Yangle * Math.PI / 180;
            this.Zangle = Zangle * Math.PI / 180;
        }
    }
}
