using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.ApplicationSettings;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Robim.Cloud
{
    public class GH_Cloud : GH_GeometricGoo<PointCloud>, IGH_PreviewData, IGH_BakeAwareData
    {
        private Guid ReferenceGuid;
        private PointCloud DisplayCloud;
        private Plane ScanPos;

        public override BoundingBox Boundingbox => this.m_value == null ? BoundingBox.Empty : this.m_value.GetBoundingBox(true);

        public BoundingBox ClippingBox => this.Boundingbox;

        public override bool IsGeometryLoaded => this.m_value != null;

        public override bool IsValid => this.m_value != null && this.m_value.IsValid;

        public override string IsValidWhyNot => "I really don't know, sorry.";

        public override Guid ReferenceID
        {
            get => this.ReferenceGuid;
            set => this.ReferenceGuid = value;
        }

        public Plane ScannerPosition
        {
            get => this.ScanPos;
            set => this.ScanPos = value;
        }

        public override string TypeDescription => "Point Cloud wrapper";

        public override string TypeName => "Cloud";

        public GH_Cloud()
        {
            this.ScanPos = Plane.WorldXY;
            this.ReferenceGuid = Guid.Empty;
            this.ScanPos = Plane.WorldXY;
        }

        public GH_Cloud(GH_Cloud other)
        {
            this.ScanPos = Plane.WorldXY;
            this.ReferenceGuid = Guid.Empty;
            this.ReferenceGuid = other != null ? other.ReferenceGuid : throw new ArgumentException(nameof(other));
            if (other.m_value != null)
                this.m_value = (PointCloud)other.m_value.Duplicate();
            this.ScanPos = other.ScanPos;
        }

        public GH_Cloud(PointCloud c)
          : base(c)
        {
            this.ScanPos = Plane.WorldXY;
            this.ReferenceGuid = Guid.Empty;
            this.ScanPos = Plane.WorldXY;
        }

        public GH_Cloud(Guid RefGuid)
        {
            this.ScanPos = Plane.WorldXY;
            this.ReferenceGuid = Guid.Empty;
            this.ReferenceGuid = RefGuid;
            this.ScanPos = Plane.WorldXY;
        }

        public override bool CastFrom(object source)
        {
            GH_Cloud target = this;
            return GH_CloudConvert.ToGHCloud(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(source)), GH_Conversion.Both, ref target);
        }
        /*
        private T DirectCast<T>(object o) where T : class => o is T obj || o == null ? obj : throw new InvalidCastException();

        new virtual bool GH_Goo<PointCloud>.CastTo<Q>(ref Q target)
        {
            bool flag;
            if (!typeof(Q).IsAssignableFrom(typeof(PointCloud)))
                flag = false;
            else if (this.m_value != null)
            {
                RhinoApp.WriteLine("Hi");
                object obj = (object)this.m_value;
                target = (Q)obj;
                flag = true;
            }
            else
                flag = false;
            return flag;
        }*/

        public override void ClearCaches()
        {
            if (!this.IsReferencedGeometry)
                return;
            this.m_value = (PointCloud)null;
        }

        private List<Line> CreateAxes(double S)
        {
            List<Line> axes = new List<Line>()
      {
        new Line(new Point3d(0.0, 0.0, 0.0), new Point3d(S * 1.5, 0.0, 0.0)),
        new Line(new Point3d(0.0, 0.0, 0.0), new Point3d(0.0, S * 1.5, 0.0)),
        new Line(new Point3d(0.0, 0.0, 0.0), new Point3d(0.0, 0.0, S * 1.5))
      };
            Rhino.Geometry.Transform plane = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.ScanPos);
            int num = checked(axes.Count - 1);
            int index = 0;
            while (index <= num)
            {
                Line line = axes[index];
                line.Transform(plane);
                axes[index] = line;
                checked { ++index; }
            }
            return axes;
        }

        private List<Line> CreatePosLines(double S)
        {
            List<Line> posLines = new List<Line>();
            double num1 = S;
            double num2 = 2.0 * S;
            bool flag = num2 >= 0.0;
            for (double num3 = -S; (flag ? (num3 > num1 ? 1 : 0) : (num3 < num1 ? 1 : 0)) == 0; num3 += num2)
            {
                Line line1 = new Line(new Point3d(-S, num3, 0.0), new Point3d(S, num3, 0.0));
                Line line2 = new Line(new Point3d(num3, -S, 0.0), new Point3d(num3, S, 0.0));
                Rhino.Geometry.Transform plane = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.ScanPos);
                line1.Transform(plane);
                line2.Transform(plane);
                posLines.Add(line1);
                posLines.Add(line2);
            }
            return posLines;
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (this.m_value == null)
                return;
            if (Settings_Global.DisplayPositions)
            {
                args.Pipeline.DrawLines((IEnumerable<Line>)this.CreatePosLines(CentralSettings.PreviewPlaneRadius), args.Color, checked(args.Thickness * 2));
                List<Line> axes = this.CreateAxes(CentralSettings.PreviewPlaneRadius);
                Color color = Instances.ActiveCanvas.Document.PreviewColourSelected;
                color = Color.FromArgb((int)byte.MaxValue, (int)color.R, (int)color.G, (int)color.B);
                if (color != args.Color)
                {
                    args.Pipeline.DrawLine(axes[0], AppearanceSettings.GridXAxisLineColor, checked(args.Thickness * 2));
                    args.Pipeline.DrawLine(axes[1], AppearanceSettings.GridYAxisLineColor, checked(args.Thickness * 2));
                    args.Pipeline.DrawLine(axes[2], AppearanceSettings.GridZAxisLineColor, checked(args.Thickness * 2));
                }
                else
                {
                    args.Pipeline.DrawLine(axes[0], args.Color, checked(args.Thickness * 2));
                    args.Pipeline.DrawLine(axes[1], args.Color, checked(args.Thickness * 2));
                    args.Pipeline.DrawLine(axes[2], args.Color, checked(args.Thickness * 2));
                }
            }
            if (Settings_Global.DisplayDynamic)
            {
                if (!args.Pipeline.IsDynamicDisplay)
                {
                    args.Pipeline.DrawPointCloud(this.m_value, Settings_Global.DisplayRadius, args.Color);
                }
                else
                {
                    this.ResolveDisplay();
                    args.Pipeline.DrawPointCloud(this.DisplayCloud, checked(Settings_Global.DisplayRadius + 1), args.Color);
                }
            }
            else
                args.Pipeline.DrawPointCloud(this.m_value, Settings_Global.DisplayRadius, args.Color);
        }

        public override IGH_Goo Duplicate() => (IGH_Goo)this.DuplicateCloud();

        public GH_Cloud DuplicateCloud() => new GH_Cloud(this);

        public override IGH_GeometricGoo DuplicateGeometry() => (IGH_GeometricGoo)this.DuplicateCloud();

        public override IGH_GooProxy EmitProxy() => (IGH_GooProxy)new GH_Cloud.GH_CloudProxy(this);

        public override BoundingBox GetBoundingBox(Rhino.Geometry.Transform xform) => this.m_value == null ? BoundingBox.Empty : this.m_value.GetBoundingBox(xform);

        public override bool LoadGeometry(RhinoDoc doc)
        {
            RhinoObject rhinoObject = doc.Objects.Find(this.ReferenceID);
            bool flag;
            if (rhinoObject == null || rhinoObject.Geometry.ObjectType != ObjectType.PointSet || !(rhinoObject.Geometry is PointCloud))
            {
                flag = false;
            }
            else
            {
                this.m_value = (PointCloud)rhinoObject.Geometry.DuplicateShallow();
                this.ScanPos = Plane.WorldXY;
                flag = true;
            }
            return flag;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            IGH_GeometricGoo ghGeometricGoo;
            if (this.IsValid)
            {
                double num = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 1000.0;
                Plane worldXy = Plane.WorldXY;
                Plane plane = new Plane(new Point3d(0.0, 0.0, 0.0), new Vector3d(0.0, -1.0, 0.0), new Vector3d(1.0, 0.0, 0.0));
                Point3d origin = this.ScannerPosition.Origin;
                worldXy.Translate(new Vector3d(-num, 0.0, 0.0));
                plane.Translate(new Vector3d(0.0, num, 0.0));
                Circle circle1 = new Circle(worldXy, num);
                Circle circle2 = new Circle(plane, num);
                circle1.Transform(Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.ScannerPosition));
                circle2.Transform(Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.ScannerPosition));
                Curve nurbsCurve1 = (Curve)circle1.ToNurbsCurve();
                Curve nurbsCurve2 = (Curve)circle2.ToNurbsCurve();
                xmorph.Morph((GeometryBase)nurbsCurve1);
                xmorph.Morph((GeometryBase)nurbsCurve2);
                this.ScannerPosition = new Plane(xmorph.MorphPoint(origin), nurbsCurve2.TangentAt(0.0), nurbsCurve1.TangentAt(0.0));
                xmorph.Morph((GeometryBase)this.m_value);
                this.ReferenceID = Guid.Empty;
                ghGeometricGoo = (IGH_GeometricGoo)this;
            }
            else
                ghGeometricGoo = (IGH_GeometricGoo)null;
            return ghGeometricGoo;
        }

        public override bool Read(GH_IReader reader)
        {
            this.ReferenceGuid = Guid.Empty;
            this.m_value = (PointCloud)null;
            this.ReferenceGuid = reader.GetGuid("RefID");
            if (reader.ItemExists("ON_Data"))
                this.m_value = GH_Convert.ByteArrayToCommonObject<PointCloud>(reader.GetByteArray("ON_Data"));
            return true;
        }

        private void ResolveDisplay()
        {
            Random random = new Random();
            this.DisplayCloud = (PointCloud)null;
            this.DisplayCloud = new PointCloud();
            if (this.m_value.Count < 1000)
            {
                this.DisplayCloud.AddRange((IEnumerable<Point3d>)this.m_value.GetPoints());
            }
            else
            {
                int num1 = checked(this.m_value.Count - 1);
                int num2 = 0;
                while (num2 <= num1)
                {
                    long index = (long)random.Next(0, checked(this.m_value.Count<PointCloudItem>() - 1));
                    this.DisplayCloud.Add(this.m_value[checked((int)index)].Location);
                    this.DisplayCloud[checked(this.DisplayCloud.Count<PointCloudItem>() - 1)].Color = this.m_value[checked((int)index)].Color;
                    checked { num2 += 1000; }
                }
            }
        }

        public override string ToString() => string.Format("PointCloud {0} HasNormals {1} HasColors {2}", (object)this.m_value.Count, (object)this.m_value.ContainsNormals, (object)this.m_value.ContainsColors).ToString();

        public override IGH_GeometricGoo Transform(Rhino.Geometry.Transform xform)
        {
            IEnumerator<PointCloudItem> enumerator = (IEnumerator<PointCloudItem>)null;
            IGH_GeometricGoo ghGeometricGoo;
            if (this.IsValid)
            {
                this.m_value.Transform(xform);
                if (this.m_value.ContainsNormals)
                {
                    try
                    {
                        foreach (PointCloudItem pointCloudItem in this.m_value)
                        {
                            Vector3d normal = pointCloudItem.Normal;
                            normal.Transform(xform);
                            pointCloudItem.Normal = normal;
                        }
                    }
                    finally
                    {
                        enumerator?.Dispose();
                    }
                }
                this.ReferenceID = Guid.Empty;
                this.ScanPos.Transform(xform);
                ghGeometricGoo = (IGH_GeometricGoo)this;
            }
            else
                ghGeometricGoo = (IGH_GeometricGoo)null;
            return ghGeometricGoo;
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetGuid("RefID", this.ReferenceGuid);
            Point3d point3d1 = new Point3d(this.ScanPos.Origin + this.ScanPos.XAxis);
            Point3d point3d2 = new Point3d(this.ScanPos.Origin + this.ScanPos.YAxis);
            GH_Point3D ghPoint3D1 = new GH_Point3D(point3d1.X, point3d1.Y, point3d1.Z);
            GH_Point3D ghPoint3D2 = new GH_Point3D(point3d2.X, point3d2.Y, point3d2.Z);
            GH_Point3D ghPoint3D3 = new GH_Point3D(this.ScanPos.Origin.X, this.ScanPos.Origin.Y, this.ScanPos.Origin.Z);
            if (this.ReferenceID == Guid.Empty && this.m_value != null)
            {
                byte[] byteArray = GH_Convert.CommonObjectToByteArray((CommonObject)this.m_value);
                if (byteArray != null)
                    writer.SetByteArray("ON_Data", byteArray);
            }
            return true;
        }

        public bool BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
        {
            obj_guid = new Guid();
            bool flag;
            if (this.IsValid)
            {
                obj_guid = doc.Objects.AddPointCloud(this.m_value, att);
                flag = true;
            }
            else
                flag = false;
            return flag;
        }

        public class GH_CloudProxy : GH_GooProxy<GH_Cloud>
        {
            public string ObjectID
            {
                get => this.Owner.IsReferencedGeometry ? string.Format("{0}", (object)this.Owner.ReferenceID) : "none";
                set {
                    if (!this.Owner.IsReferencedGeometry)
                        return;
                    try
                    {
                        this.Owner.ReferenceID = new Guid(value);
                        this.Owner.ClearCaches();
                        this.Owner.LoadGeometry();
                        this.Owner.ScanPos = Plane.WorldXY;
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine(ex.ToString());
                    }
                }
            }

            public string Type => this.Owner.Value == null ? "No cloud" : (this.Owner.Value == null ? "Other" : "Point Cloud");

            public GH_CloudProxy(GH_Cloud owner)
              : base(owner)
            {
            }

            public override void Construct()
            {
                try
                {
                    Instances.DocumentEditorFadeOut();
                    GH_Cloud ghCloud = (GH_Cloud)null;
                    if (ghCloud == null)
                        return;
                    this.Owner.m_value = ghCloud.m_value;
                    this.Owner.ReferenceGuid = ghCloud.ReferenceGuid;
                    this.Owner.LoadGeometry();
                    this.Owner.ScanPos = Plane.WorldXY;
                }
                finally
                {
                    Instances.DocumentEditorFadeIn();
                }
            }

            public override bool FromString(string @in) => false;
        }
    }
}
