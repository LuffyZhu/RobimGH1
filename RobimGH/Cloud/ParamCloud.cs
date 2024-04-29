using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Robim.Cloud
{
    public class Param_Cloud :
      GH_PersistentGeometryParam<GH_Cloud>,
      IGH_BakeAwareObject,
      IGH_PreviewObject
    {
        private bool m_hidden;

        public BoundingBox ClippingBox => this.Preview_ComputeClippingBox();

        public override Guid ComponentGuid => new Guid("{e3002fb4-402f-4fd4-83dc-1b87946c626b}");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public bool Hidden { get; set; }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.cloud;


        public bool IsBakeCapable => !this.m_data.IsEmpty;

        public bool IsPreviewCapable => true;

        public override string TypeName => "Cloud";

        public Param_Cloud()
          : base(new GH_InstanceDescription("PC", "PC",
            "Point cloud retriever",
            "Robim", "Vision"))
        {
            this.m_hidden = false;
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            GH_DocumentObject.Menu_AppendSeparator((ToolStrip)menu);
        }

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) => this.BakeGeometry(doc, (ObjectAttributes)null, obj_ids);

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            Guid obj_guid = new Guid();
            IEnumerator enumerator = (IEnumerator)null;
            if (att == null)
                att = doc.CreateDefaultAttributes();
            try
            {
                foreach (IGH_BakeAwareData ghBakeAwareData in this.m_data)
                {
                    if (ghBakeAwareData != null && ghBakeAwareData.BakeGeometry(doc, att, out obj_guid))
                        obj_ids.Add(obj_guid);
                }
            }
            finally
            {
                if (enumerator is IDisposable)
                    (enumerator as IDisposable).Dispose();
            }
        }

        private void DecreaseRadius()
        {
            if (Settings_Global.DisplayRadius > 1)
                checked { --Settings_Global.DisplayRadius; }
            this.ExpirePreview(true);
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
        }

        public void DrawViewportWires(IGH_PreviewArgs args) => this.Preview_DrawWires(args);

        private void DynamicSwitch()
        {
            Settings_Global.DisplayDynamic = !Settings_Global.DisplayDynamic;
            int num = Settings_Global.DisplayDynamic ? 1 : 0;
        }

        private void IncreaseRadius()
        {
            checked { ++Settings_Global.DisplayRadius; }
            this.ExpirePreview(true);
        }

        protected override GH_Cloud InstantiateT() => new GH_Cloud();

        private void PositionSwitch()
        {
            Settings_Global.DisplayPositions = !Settings_Global.DisplayPositions;
            this.ExpirePreview(true);
        }

        protected override GH_Cloud PreferredCast(object data) => !(data is PointCloud) ? (GH_Cloud)null : new GH_Cloud((PointCloud)data);

        protected override GH_GetterResult Prompt_Plural(ref List<GH_Cloud> values)
        {
            values = GH_CloudGetter.GetClouds();
            return values != null && values.Count != 0 ? GH_GetterResult.success : GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref GH_Cloud value)
        {
            value = GH_CloudGetter.GetCloud();
            return value == null ? GH_GetterResult.cancel : GH_GetterResult.success;
        }

        public override string ToString() => "Cloud";
    }
}
