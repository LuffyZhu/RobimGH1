using Rhino.DocObjects;
using Rhino.Input.Custom;
using Rhino.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace Robim.Cloud
{
    public sealed class GH_CloudGetter
    {
        private static bool m_reference = true;

        public GH_CloudGetter() => GH_CloudGetter.m_reference = true;

        public static GH_Cloud GetCloud()
        {
            GetObject getObject;
            while (true)
            {
                getObject = new GetObject();
                if (GH_CloudGetter.m_reference)
                {
                    getObject.SetCommandPrompt("Cloud to reference");
                    getObject.AddOption("Mode", "Reference");
                }
                else
                {
                    getObject.SetCommandPrompt("Cloud to copy");
                    getObject.AddOption("Mode", "Copy");
                }
                getObject.GeometryFilter = ObjectType.PointSet;
                switch (getObject.Get())
                {
                    case GetResult.Option:
                        GH_CloudGetter.m_reference = !GH_CloudGetter.m_reference;
                        continue;
                    case GetResult.Object:
                        goto label_6;
                    default:
                        goto label_7;
                }
            }
        label_6:
            GH_Cloud cloud = !GH_CloudGetter.m_reference ? new GH_Cloud(getObject.Object(0).PointCloud()) : new GH_Cloud(getObject.Object(0).ObjectId);
            goto label_8;
        label_7:
            cloud = (GH_Cloud)null;
        label_8:
            return cloud;
        }

        

        public static List<GH_Cloud> GetClouds()
        {
            GetObject getObject;
            while (true)
            {
                getObject = new GetObject();
                if (GH_CloudGetter.m_reference)
                {
                    getObject.SetCommandPrompt("Clouds to reference");
                    getObject.AddOption("Mode", "Reference");
                }
                else
                {
                    getObject.SetCommandPrompt("Clouds to copy");
                    getObject.AddOption("Mode", "Copy");
                }
                getObject.GeometryFilter = ObjectType.PointSet;
                switch (getObject.GetMultiple(1, 0))
                {
                    case GetResult.Option:
                        GH_CloudGetter.m_reference = !GH_CloudGetter.m_reference;
                        continue;
                    case GetResult.Object:
                        goto label_6;
                    default:
                        goto label_12;
                }
            }
        label_6:
            List<GH_Cloud> ghCloudList = new List<GH_Cloud>();
            int num = checked(getObject.ObjectCount - 1);
            int index = 0;
            do
            {
                if (GH_CloudGetter.m_reference)
                    ghCloudList.Add(new GH_Cloud(getObject.Object(index).ObjectId));
                else
                    ghCloudList.Add(new GH_Cloud(getObject.Object(index).PointCloud()));
                checked { ++index; }
            }
            while (index <= num);
            List<GH_Cloud> clouds = ghCloudList;
            goto label_13;
        label_12:
            clouds = (List<GH_Cloud>)null;
        label_13:
            return clouds;
        }
    }
}
