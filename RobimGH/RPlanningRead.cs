using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SimpleTcp;

namespace Robim.Grasshopper
{
    public class RPlanningRead : GH_Component
    {
        string output = null;
        string backjason = "null";
        bool connectionstate = false;
        bool send = false;
        string requestjson = "0";
        string ip = "0.0";
        string port = "ssss";
        bool receive = false;
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RPlanningRead()
          : base("RPlanningRead", "Nickname",
            "Description",
            "Robim", "Planning")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "Target", "T", "List of targets or toolpaths for the robot.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddParameter(new RobotSystemParameter(), "RSystem", "RS", "Robot system", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddTextParameter("JSONr", "JSONr", "JSON request for planning service", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddBooleanParameter("Send", "Send", "Send request JSON to planning service", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddTextParameter("IP", "IP", "IP address to send to", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager.AddTextParameter("Port", "Port", "Connection port number", GH_ParamAccess.item);
            pManager[5].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddIntegerParameter("tID", "tID", "Target index", GH_ParamAccess.list);
            //pManager.AddBooleanParameter("ifPTP", "ifPTP", "True if target mode is ptp", GH_ParamAccess.list);
            //pManager.AddTextParameter("Mode", "M", "Target mode (ptp or linear)", GH_ParamAccess.list);
            //pManager.AddTextParameter("Joints", "J", "List of output robot joints (6 per each target)", GH_ParamAccess.list);
            //pManager.AddPointParameter("LPos", "LO", "Linear target positions", GH_ParamAccess.list);
            //pManager.AddNumberParameter("LQ", "LQ", "Linear target quaternions", GH_ParamAccess.tree);
            pManager.AddParameter(new TargetParameter(), "Target", "T", "Target", GH_ParamAccess.item);
            //pManager.AddParameter(new TargetParameter(), "LTarget", "LT", "LTarget", GH_ParamAccess.item);

            pManager.AddTextParameter("Response", "R", "Planning service response", GH_ParamAccess.item);
            pManager.AddTextParameter("Info", "I", "result", GH_ParamAccess.item);
            pManager.AddTextParameter("bytes", "B", "Bytes", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inTargets = new List<GH_Target>();
            DA.GetDataList(0, inTargets);

            var targets = inTargets.Select(t => t.Value).ToList();

            //var cts = targets.Select(t => t as CartesianTarget).ToList();
            //var jts = targets.Select(t => t as JointTarget).ToList();

            //var targets = program.Value.Targets;
            //var pt = targets[0].ProgramTargets;

            var tool = targets[0].Tool; //pt[0].Target.Tool;
            var speed = targets[0].Speed; //pt[0].Target.Speed;
            var zone = targets[0].Zone; //pt[0].Target.Zone;
            var external = targets[0].External; //pt[0].Target.External;
            var command = targets[0].Command; //pt[0].Target.Command;
            Mesh workpiece = null;
            Frame frame = null;

            //var robotSystem = program.Value.RobotSystem;
            //GH_RobotSystem rSystem = new GH_RobotSystem();
            //rSystem.Value = robotSystem;
            GH_RobotSystem rSystem = new GH_RobotSystem();
            DA.GetData(1, ref rSystem);

            string request = null;
            DA.GetData(2, ref request);

            bool send = false;
            DA.GetData(3, ref send);

            string ip = "127.0.0.1";
            DA.GetData(4, ref ip);

            string port = "5002";
            DA.GetData(5, ref port);

            List<Target> finalTargets = new List<Target>();

            string response = null;

            string ip_send = ip + ":" + port;
            SimpleTcpClient tcpClient = new SimpleTcpClient(ip_send);
            tcpClient.Events.Connected += Events_Connected;
            tcpClient.Events.Disconnected += Events_Disconnected;
            tcpClient.Events.DataReceived += Events_DataReceived1;
            string bytes = null;

            if (send)
            {
                backjason = null;
                tcpClient.Connect();
                byte[] byteSend = System.Text.Encoding.Default.GetBytes(request);
                int length = byteSend.Length;
                byte[] byteSend_head = BitConverter.GetBytes(length);
                int newlength = BitConverter.ToInt32(byteSend_head, 0);
                output += "\n" + newlength.ToString();
                byte[] byteSend_new = new byte[byteSend.Length + byteSend_head.Length];
                byteSend_head.CopyTo(byteSend_new, 0);
                byteSend.CopyTo(byteSend_new, byteSend_head.Length);
                tcpClient.Send(byteSend_new);
                bytes = byteSend_new[0].ToString();
                //tcpClient.Disconnect();
            }

            //DA.GetData(2, ref response);
            response = backjason;

            if (response != null && response.Length > 50)
            {
                RPlanning path = JsonConvert.DeserializeObject<RPlanning>(response);
                var segments = path.Path.Segments;

                List<string> modes = new List<string>();
                List<bool> ifPTP = new List<bool>();
                //List<string> robot = new List<string>();
                //List<Point3d> lPos = new List<Point3d>();
                List<int> indices = new List<int>();

                DataTree<double> quaternions = new DataTree<double>();

                int counter = 0;
                List<JointTarget> jTargets = new List<JointTarget>();
                List<CartesianTarget> cTargets = new List<CartesianTarget>();

                foreach (Segment s in segments)
                {
                    indices.Add(Convert.ToInt32(s.OriginalIndex));
                    var currMode = s.Mode.ToString();
                    modes.Add(currMode);
                    if (currMode == "Ptp")
                    {
                        var joints = s.Target.PtpTarget.JointTarget.Robot;

                        var stDegrees = joints;
                        var stRadians = (stDegrees.Select((x, i) => (rSystem.Value).DegreeToRadian(x, i, 0))).ToArray();
                        if (rSystem.Value.Manufacturer == RobimRobots.Manufacturers.FANUC)
                        {
                            stRadians = fixRadianFanuc(stRadians);
                        }
                        //var stRadians = DegreeToRadianFormula(stDegrees);

                        var target = new JointTarget(stRadians, tool, speed, zone, command, frame, workpiece, external);
                        jTargets.Add(target);

                        var strJoints = stRadians[0].ToString() + ", " + stRadians[1].ToString() + ", " + stRadians[2].ToString() + ", " + stRadians[3].ToString() + ", " + stRadians[4].ToString() + ", " + stRadians[5].ToString();

                        ifPTP.Add(true);
                    }
                    else
                    {
                        ifPTP.Add(false);

                        var x = s.Target.Pose.Position.X;
                        var y = s.Target.Pose.Position.Y;
                        var z = s.Target.Pose.Position.Z;
                        var point = new Point3d(x, y, z);

                        var qw = s.Target.Pose.Orientation.Quaternion.W;
                        var qx = s.Target.Pose.Orientation.Quaternion.X;
                        var qy = s.Target.Pose.Orientation.Quaternion.Y;
                        var qz = s.Target.Pose.Orientation.Quaternion.Z;

                        counter++;

                        var plane = PosTransform.Quaternion2Plane(x, y, z, qw, qx, qy, qz);

                        var lTarget = new CartesianTarget(plane, null, Motions.Linear, tool, speed, zone, command, frame, workpiece, external);
                        cTargets.Add(lTarget);
                    }

                }

                var wovenTargets = weaveTargets(ifPTP, jTargets, cTargets);
                finalTargets = wovenTargets;

            }


            DA.SetDataList(0, finalTargets);
            DA.SetData(1, output);
            DA.SetData(2, backjason);
            DA.SetData(3, bytes);

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.iconCreateTarget;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c78633e2-1b0f-45c1-b058-8d2ff7018718");

        IGH_Param[] target = new IGH_Param[1]
        {
            new TargetParameter() { Name = "Target", NickName = "T", Description = "Reference target", Optional = false }
        };

        public List<Target> weaveTargets(List<bool> pattern, List<JointTarget> jTargets, List<CartesianTarget> cTargets)
        {
            List<Target> allT = new List<Target>();
            for (int i = 0; i < pattern.Count; i++)
            {
                if (pattern[i] == true)
                {
                    allT.Add(jTargets[0]);
                    jTargets.RemoveAt(0);
                }
                else
                {
                    allT.Add(cTargets[0]);
                    cTargets.RemoveAt(0);
                }
            }

            return allT;
        }

        public double[] DegreeToRadianFormula(double[] degrees)
        {
            double[] radians = new double[degrees.Length];

            for (int i = 0; i < degrees.Length; i++)
            {
                var radian = degrees[i] * Math.PI / 180;
                radians[i] = radian;
            }
            return radians;
        }

        List<double> radianToDegreesFanuc(double[] jointsRadian)
        {
            List<double> jointsDegrees = new List<double> { };
            double j1_temp = 0;

            for (int i = 0; i < 6; ++i)
            {
                double radian = jointsRadian[i];
                if (i == 1)
                {
                    radian = 0.5 * Math.PI - radian;
                    j1_temp = radian;
                }
                if (i == 2)
                {
                    radian = radian - j1_temp;
                }
                if (i == 3)
                {
                    radian = -1 * radian;
                }
                if (i == 5)
                {
                    radian = Math.PI - radian;
                }
                jointsDegrees.Add(radian * 180 / Math.PI);
            }
            return jointsDegrees;
        }

        double[] fixRadianFanuc(double[] jointsRadian)
        {
            List<double> jointsRadianF = new List<double> { };
            double j1_temp = 0;

            for (int i = 0; i < 6; ++i)
            {
                double radian = jointsRadian[i];
                if (i == 1)
                {
                    radian = 0.5 * Math.PI - radian;
                    j1_temp = radian;
                }
                if (i == 2)
                {
                    radian = radian - j1_temp;
                }
                if (i == 3)
                {
                    radian = -1 * radian;
                }
                if (i == 5)
                {
                    radian = Math.PI - radian;
                }
                jointsRadianF.Add(radian);
            }
            return jointsRadianF.ToArray();
        }

        private void Events_Disconnected(object sender, ClientDisconnectedEventArgs e)
        {
            connectionstate = false;
            //output += "\n" + e.IpPort + " ...disconnected";
            output = "\n" + e.IpPort + " ...disconnected";
        }

        private void Events_Connected(object sender, ClientConnectedEventArgs e)
        {
            //output += "\n" + e.IpPort + " ...connected";
            output = "\n" + e.IpPort + " ...connected";
        }

        private void Events_DataReceived1(object sender, DataReceivedEventArgs e)
        {
            byte[] Head = new byte[4];
            byte[] Text = new byte[e.Data.Length - 4];
            Array.Copy(e.Data, 0, Head, 0, 4);
            Array.Copy(e.Data, 4, Text, 0, e.Data.Length - 4);
            //output += "\n" + e.IpPort + ":" + System.BitConverter.ToInt32(Head, 0) + System.Text.Encoding.Default.GetString(Text);
            output = System.BitConverter.ToInt32(Head, 0) + System.Text.Encoding.Default.GetString(Text);
            if (backjason != null && backjason != "null")
            {
                backjason += System.Text.Encoding.Default.GetString(e.Data);
            }
            else { backjason = System.Text.Encoding.Default.GetString(Text); }

        }

        private void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    public partial class RPlanning
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public JPath Path { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error_info")]
        public string ErrorInfo { get; set; }
    }

    public partial class JPath
    {
        [JsonProperty("segments")]
        public Segment[] Segments { get; set; }
    }

    public partial class Segment
    {
        [JsonProperty("index")]
        public long Index { get; set; }

        [JsonProperty("mode")]
        public Mode Mode { get; set; }

        [JsonProperty("velocity")]
        public double Velocity { get; set; }

        [JsonProperty("original_index")]
        public long OriginalIndex { get; set; }

        [JsonProperty("target")]
        public JTarget Target { get; set; }
    }

    public partial class JTarget
    {
        [JsonProperty("ptp_target", NullValueHandling = NullValueHandling.Ignore)]
        public PtpJTarget PtpTarget { get; set; }

        [JsonProperty("pose", NullValueHandling = NullValueHandling.Ignore)]
        public Pose Pose { get; set; }
    }

    public partial class Pose
    {
        [JsonProperty("position")]
        public Ion Position { get; set; }

        [JsonProperty("orientation")]
        public JOrientation Orientation { get; set; }
    }

    public partial class JOrientation
    {
        [JsonProperty("quaternion")]
        public Ion Quaternion { get; set; }
    }

    public partial class Ion
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }

        [JsonProperty("w")]
        public double W { get; set; }
    }

    public partial class PtpJTarget
    {
        [JsonProperty("joint_target")]
        public JointJTarget JointTarget { get; set; }
    }

    public partial class JointJTarget
    {
        [JsonProperty("robot")]
        public double[] Robot { get; set; }

        [JsonProperty("external")]
        public object[] External { get; set; }
    }

    public enum Mode { Line, Ptp };

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                ModeConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ModeConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Mode) || t == typeof(Mode?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "line":
                    return Mode.Line;
                case "ptp":
                    return Mode.Ptp;
            }
            throw new Exception("Cannot unmarshal type Mode");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Mode)untypedValue;
            switch (value)
            {
                case Mode.Line:
                    serializer.Serialize(writer, "line");
                    return;
                case Mode.Ptp:
                    serializer.Serialize(writer, "ptp");
                    return;
            }
            throw new Exception("Cannot marshal type Mode");
        }

        public static readonly ModeConverter Singleton = new ModeConverter();
    }
    /*
            try
                    {
                        while (IsConnected())//one loop collects a piece of complete message
                        {
                            NetworkStream stream = tcpClient?.GetStream();
                            List<byte> totalBytes = new List<byte>();
                            if (stream != null && stream.DataAvailable)
                            {
                                if (HasHeader)
                                {
                                    #region data with length header
                                    byte[] countBytes = new byte[4];
                                    int headCount = await stream.ReadAsync(countBytes, 0, countBytes.Length);
                                    if (headCount != 4)
                                    {
                                        log.Warn("can't read the first 4 bytes");
                                        break;//can't read the first 4 bytes
                                    }
                                    else
        {
            int size = BitConverter.ToInt32(countBytes, 0);
            size = IPAddress.NetworkToHostOrder(size);
            if (size > 0)
            {
                while (size > 0)
                {
                    byte[] bytes;
                    if (size < quant)
                    {
                        bytes = new byte[size];
                    }
                    else
                    {
                        bytes = new byte[quant];
                    }
                    if (stream != null && stream.DataAvailable)//in case the socket is closed when running async
                    {
                        try
                        {
                            int length = await stream.ReadAsync(bytes, 0, bytes.Length);
                            totalBytes.AddRange(bytes);
                            size -= quant;
                            //log.Info("Reading");
                        }
                        catch (Exception e)
                        {
                            log.Error("Error", e);
                        }
                    }
                    else
                    {
                        //log.Info("No data avaliable for reading");
                        break;
                    }
                }
            }
            else
            {
                log.Warn("size provided by first 4 bytes is not correct");
                break;//not correct length
            }

        }
                                    #endregion
                                }
                                else
        {
            #region data without length header
            if (stream != null && stream.DataAvailable)
            {
                try
                {
                    byte[] bytes = new byte[quant];
                    int length = await stream.ReadAsync(bytes, 0, bytes.Length);
                    if (length < quant)
                    {
                        byte[] bytesCut = bytes.Take(length).ToArray();
                        totalBytes.AddRange(bytesCut);
                    }
                    else
                    {
                        totalBytes.AddRange(bytes);
                        log.Warn("Warn: In non-length-header mode, the size of buffer is less or at least equal to the data received. Data might not be received completely!");
                    }
                }
                catch (Exception e)
                {
                    log.Error("Error", e);
                }
            }
            #endregion
        }
                            }
                            else
        {
            //no data available
        }
        //finish reading, process the byte[] data
        DataReceived = totalBytes?.ToArray();
        //log.Info("data received length: " + DataReceived.Length);
        //excute delegate
        if (DataReceived != null && DataReceived.Length > 0)
        {
            if (OnReceiveMsg != null)
            {
                foreach (Func<byte[], object> dele in OnReceiveMsg.GetInvocationList())
                {
                    dele.BeginInvoke(DataReceived, null, null);
                }
            }
        }
                        }
                    }
                    catch (Exception e)
        {
            tcpClient?.Close();
            log.Error("Error", e);//to log
        }

        */

}