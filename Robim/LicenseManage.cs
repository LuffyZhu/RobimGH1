using ConnectToDB;
using FoxLearn.License;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Robim
{
    public class LicenseManage
    {
        public Task Task { get;internal set; }
        public bool LicensePass { get;internal set; } = false;
        public int ReCheck { get; set; } = 1;
        GH_Component GH_Component { get; }
        string ProductID = null;
        string ProductKey = null;
        string EndDate = null;
        string OpenProduct = null;
        string[] OpenProducts = new string[3]{
            "Robim & RoboMesser",
            "Robim",
            "RoboMesser"
        };

        public LicenseManage(GH_Component gH_Component)
        {
            GH_Component = gH_Component;
            Check();
        }
        public bool IsLicensePass()
        {
            if (!LicensePass)
            {
                if (Task != null)
                {
                    Task.Wait();
                    Task.Dispose();
                }
                if (!LicensePass && ReCheck > 0)
                {
                    for (int i = 0; i < ReCheck; i++)
                    {
                        Check();
                        Task.Wait();
                        Task.Dispose();
                        if (LicensePass)
                        {
                            return LicensePass;
                        }
                    }
                }
            }
            
            return LicensePass;
        }
        void Check()
        {
            Task = Task.Factory.StartNew(() =>
            {
                LicensePass = licensecheck(GH_Component);
            });
        }
        #region GetWebTime
        //public DateTime getWebTime()
        //{
        //    // default ntp server
        //    //const string ntpServer = "ntp1.aliyun.com";
        //    const string ntpServer = "time.windows.com";

        //    // NTP message size - 16 bytes of the digest (RFC 2030)
        //    byte[] ntpData = new byte[48];
        //    // Setting the Leap Indicator, Version Number and Mode values
        //    ntpData[0] = 0x1B; // LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

        //    IPAddress[] addresses = Dns.GetHostEntry(ntpServer).AddressList;
        //    // The UDP port number assigned to NTP is 123
        //    IPEndPoint ipEndPoint = new IPEndPoint(addresses[0], 123);

        //    // NTP uses UDP
        //    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //    socket.Connect(ipEndPoint);
        //    // Stops code hang if NTP is blocked
        //    socket.ReceiveTimeout = 3000;//3s
        //    socket.Send(ntpData);
        //    socket.Receive(ntpData);
        //    socket.Close();

        //    // Offset to get to the "Transmit Timestamp" field (time at which the reply 
        //    // departed the server for the client, in 64-bit timestamp format."
        //    const byte serverReplyTime = 40;
        //    // Get the seconds part
        //    ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
        //    // Get the seconds fraction
        //    ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);
        //    // Convert From big-endian to little-endian
        //    intPart = swapEndian(intPart);
        //    fractPart = swapEndian(fractPart);
        //    ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000UL);

        //    // UTC time
        //    DateTime webTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds(milliseconds);
        //    // Local time
        //    return webTime.ToLocalTime();
        //}
        // 小端存储与大端存储的转换
        //private uint swapEndian(ulong x)
        //{
        //    return (uint)(((x & 0x000000ff) << 24) +
        //    ((x & 0x0000ff00) << 8) +
        //    ((x & 0x00ff0000) >> 8) +
        //    ((x & 0xff000000) >> 24));
        //}
        #endregion
        private bool licensecheck(GH_Component gH_Component)
        {
            bool licensepass = false;
            
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Robim");
            string folder2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries\\Robim");
            //string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries\\Robim\\Download");

            if (File.Exists(folder2 + "\\Key.lic"))
            {
                licensepass = GetOnlineCheck(gH_Component, folder2);
            }
            else
            {
                if (Directory.Exists(folder))
                {
                    if (File.Exists(folder + "\\Key.lic"))
                    {
                        licensepass = GetOnlineCheck(gH_Component, folder);
                    }
                    else
                    {
                        gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Folder '{folder}' can not find license file.");
                    }
                }
                else
                {
                    gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Folder '{folder2}'or'{folder}' can not find license file.");
                    //throw new Exception($"License folder '{folder}' not found");
                }
            }
            return licensepass;
        }
        bool GetOnlineCheck(GH_Component gH_Component,string folder)
        {
            bool licensepass = false;

            string lblProductID = ComputerInfo.GetComputerId();
            //本地挡案
            KeyManager km = new KeyManager(lblProductID);
            LicenseInfo lic = new LicenseInfo();

            int value = km.LoadSuretyFile(string.Format(@"{0}\Key.lic", folder), ref lic);
            string localProductID = lic.FullName;
            string localLicenseType = "FULL";
            string localProductKey = lic.ProductKey;
            ProductID = localProductID;
            ProductKey = localProductKey;
            #region OLD
            //string productKey = lic.ProductKey;
            //if (km.ValidKey(ref productKey))
            //{
            //    KeyValuesClass kv = new KeyValuesClass();
            //    if (km.DisassembleKey(productKey, ref kv))
            //    {
            //        localProductKey = productKey;
            //        localLicenseType = kv.Type.ToString();
            //        /*if (kv.Type == LicenseType.TRIAL)
            //            localLicenseType = string.Format("{0} days", (kv.Expiration - DateTime.Now.Date).Days);
            //        else
            //            localLicenseType = "Full";*/
            //    }
            //}
            #endregion
            if (value > 0)
            {
                if (localProductID == lblProductID)
                {
                    List<string> IBI = DBConnect.SelectWhere("license_type,experience_day,product_key,products_id", "Robim", "Robim_License", "product_id", lblProductID);
                    if (IBI.Count > 0)
                    {
                        string lblLicenseType = IBI[0];
                        string lblLicenseDay = IBI[1];
                        string lblProductKey = IBI[2];
                        string lblProductsID = IBI[3];
                        OpenProduct = OpenProducts[int.Parse(lblProductsID)];
                        if (lblProductsID == "0" || localProductID == "1")
                        {
                            if (lblLicenseType == localLicenseType && lblLicenseDay != null && lblProductKey == localProductKey)
                            {
                                // add license date time
                                DateTime st = Convert.ToDateTime(lblLicenseDay);
                                //DateTime localtime = getWebTime();
                                string time = DBConnect.GetTime();
                                if (time != null)
                                {
                                    DateTime localtime = Convert.ToDateTime(time);
                                    EndDate = st.ToString();
                                    TimeSpan ts = st.Subtract(localtime);
                                    double seconds = ts.TotalSeconds;//24.
                                    if (seconds > 0)
                                    {
                                        licensepass = true;
                                    }
                                    else
                                    {
                                        gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "License use time exceeds");
                                        //throw new Exception("License use time exceeds");
                                    }
                                }
                                else
                                {
                                    gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Can't get server time.");
                                }
                            }
                            else
                            {
                                gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "License is not correct");
                                //throw new Exception("License is not correct");
                            }
                        }
                        else
                        {
                            gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Your License is not included this product(Robim)");
                        }
                    }
                    else
                    {
                        gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Network Error");
                    }
                }
                else
                {
                    gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "ProductID is Wrong");
                    //throw new Exception("ProductID is Wrong");
                }
            }
            else
            {
                gH_Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"License file 'Key.lic' not found");
                //throw new Exception($"License file 'Key.lic' not found");
            }
            return licensepass;
        }
        public override string ToString()
        {
            string log = $"Product ID : {ProductID}\nProduct Key : {ProductKey}\nEnd Date : {EndDate}\nOpen Product : {OpenProduct}";
            return log;
        }
    }
}
