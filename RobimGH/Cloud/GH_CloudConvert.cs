using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Runtime.CompilerServices;

namespace Robim.Cloud
{
    internal static class GH_CloudConvert
    {
        public static bool ToCloud(object data, ref PointCloud rpc)
        {
            Guid guid1 = new Guid("ac8a38b7-f577-4220-8a23-689a67146fde");
            if (data == null)
                return false;
            Guid guid2 = data.GetType().GUID;
            if (guid2 == guid1)
            {
                rpc = (PointCloud)data;
                return true;
            }
            if (guid2 != GH_TypeLib.id_gh_curve)
            {
                if (!(data is Curve))
                    return false;
                rpc = (PointCloud)data;
                return true;
            }
            rpc = ((GH_Goo<PointCloud>)data).Value;
            return true;
        }

        public static bool ToGHCloud(object data, GH_Conversion conversion_level, ref GH_Cloud target)
        {
            bool ghCloud;
            switch (conversion_level)
            {
                case GH_Conversion.Primary:
                    ghCloud = GH_CloudConvert.ToGHCloud_Primary(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(data)), ref target);
                    break;
                case GH_Conversion.Secondary:
                    ghCloud = GH_CloudConvert.ToGHCloud_Secondary(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(data)), ref target);
                    break;
                case GH_Conversion.Both:
                    ghCloud = GH_CloudConvert.ToGHCloud_Primary(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(data)), ref target) || GH_CloudConvert.ToGHCloud_Secondary(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(data)), ref target);
                    break;
                default:
                    ghCloud = false;
                    break;
            }
            return ghCloud;
        }

        public static bool ToGHCloud_Primary(object data, ref GH_Cloud target)
        {
            Guid guid1 = new Guid("6d618a76-c2d9-413a-af2f-5b6a680fd453");
            bool ghCloudPrimary;
            if (data != null)
            {
                Guid guid2 = data.GetType().GUID;
                if (guid2 == guid1)
                {
                    if (target != null)
                    {
                        target.ReferenceID = Guid.Empty;
                        target.Value = (PointCloud)data;
                    }
                    else
                        target = new GH_Cloud((PointCloud)data);
                    ghCloudPrimary = true;
                }
                else if (guid2 != guid1)
                    ghCloudPrimary = false;
                else if (target != null)
                {
                    target.Value = ((GH_Goo<PointCloud>)data).Value;
                    target.ReferenceID = ((GH_GeometricGoo<PointCloud>)data).ReferenceID;
                    ghCloudPrimary = true;
                }
                else
                {
                    target = (GH_Cloud)data;
                    ghCloudPrimary = true;
                }
            }
            else
                ghCloudPrimary = false;
            return ghCloudPrimary;
        }

        public static bool ToGHCloud_Secondary(object data, ref GH_Cloud target)
        {
            Guid destination1 = new Guid();
            bool ghCloudSecondary;
            if (data == null)
                ghCloudSecondary = false;
            else if (!GH_Convert.ToGUID_Primary(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(data)), ref destination1))
            {
                string destination2 = (string)null;
                if (GH_Convert.ToString_Primary(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(data)), ref destination2))
                {
                    RhinoObject objectByNameAndType = GH_Convert.FindRhinoObjectByNameAndType(destination2, ObjectType.PointSet);
                    if (objectByNameAndType != null)
                    {
                        if (target == null)
                            target = new GH_Cloud();
                        target.ReferenceID = objectByNameAndType.Id;
                        target.ClearCaches();
                        target.LoadGeometry();
                        return target.IsValid;
                    }
                }
                PointCloud rpc = (PointCloud)null;
                if (GH_CloudConvert.ToCloud(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(data)), ref rpc))
                {
                    if (target != null)
                    {
                        target.Value = rpc;
                        target.ReferenceID = Guid.Empty;
                    }
                    else
                        target = new GH_Cloud(rpc);
                    ghCloudSecondary = true;
                }
                else
                    ghCloudSecondary = false;
            }
            else
            {
                if (target != null)
                    target.ReferenceID = destination1;
                else
                    target = new GH_Cloud(destination1);
                target.ClearCaches();
                target.LoadGeometry();
                ghCloudSecondary = target.IsValid;
            }
            return ghCloudSecondary;
        }
    }
}
