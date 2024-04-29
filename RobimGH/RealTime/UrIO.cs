using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Robim.Grasshopper
{
    public class UrIO : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the urio class.
        /// </summary>
        public UrIO()
          : base("IORTDE", "IORTDE",
              "RTDE robot IO control",
              "Robim", "RealTime")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("IP", "IP", "Robot IP", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Frequency", "Frequency", "Frequency", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Start", "Start", "Start", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IOstate", "IOstate", "IOstate", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string robotIP = "192.168.1.11";
            DA.GetData(0, ref robotIP);
            int frequency = 100;
            DA.GetData(1, ref frequency);
            bool ifstart = false;
            DA.GetData(2, ref ifstart);
            bool iostate = false;
            DA.GetData(3, ref iostate);

            UR_IO_Data.ip_address = robotIP;
            UR_IO_Data.time_step = 200;  // 200ms

            UR_IO_Data.velocity = "0.02";
            UR_IO_Data.acceleration = "0.1";


            UR_Control_IO ur_ctrl_robot = null;
            if (ifstart)
            {
                ur_ctrl_robot = new UR_Control_IO();
                //ur_ctrl_robot.Start();
                ur_ctrl_robot.UR_Control_Once(iostate);
            }
            else { if (ur_ctrl_robot != null) { ur_ctrl_robot.Destroy(); } }
        }

        public static class UR_IO_Data
        {
            // IP Port Number and IP Address
            public static string ip_address;
            //  Real-time (Read/Write)
            public const ushort port_number = 30003;
            // Comunication Speed (ms)
            public static int time_step;

            // Move Parameters: Velocity, Acceleration
            public static string velocity = "0.01";
            public static string acceleration = "0.1";
        }

        public class UR_Control_IO
        {
            // Initialization of Class variables
            //  Thread
            private Thread robot_thread = null;
            private bool exit_thread = false;
            //  TCP/IP Communication
            private TcpClient tcp_client = new TcpClient();
            private NetworkStream network_stream = null;
            //  Packet Buffer (Write)
            private byte[] packet_cmd;
            //  Encoding
            private UTF8Encoding utf8 = new UTF8Encoding();

            public void UR_Control_Once(bool dostate)
            {
                try
                {
                    if (tcp_client.Connected == false)
                    {
                        // Connect to controller -> if the controller is disconnected
                        tcp_client.Connect(UR_IO_Data.ip_address, UR_IO_Data.port_number);
                    }

                    // Initialization TCP/IP Communication (Stream)
                    network_stream = tcp_client.GetStream();
                    var check = "set_digital_out(0, " + FirstCharToUpper(dostate.ToString()) + ")" + "\n";
                    packet_cmd = utf8.GetBytes(check);

                    //packet_cmd = utf8.GetBytes("movel(p[" + "-0.158,-0.688,0.447,-0.012,3.138,0.000" + "],"
                    //                                         + "a=" + UR_Control_Data.acceleration + ", v=" + UR_Control_Data.velocity + ")" + "\n");



                    //"set_io(FUN_SET_DIGITAL_OUT, 0, true)"

                    //  Send command to the robot
                    network_stream.Write(packet_cmd, 0, packet_cmd.Length);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }

            }

            public string FirstCharToUpper(string input)
            {
                switch (input)
                {
                    case null: throw new ArgumentNullException(nameof(input));
                    case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                    default: return input[0].ToString().ToUpper() + input.Substring(1);
                }
            }

            public void Stop()
            {
                exit_thread = true;
                // Stop a thread
                Thread.Sleep(100);
            }
            public void Destroy()
            {
                // Start a thread and disconnect tcp/ip communication
                Stop();
                if (tcp_client.Connected == true)
                {
                    network_stream.Dispose();
                    tcp_client.Close();
                }
                Thread.Sleep(100);
            }
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.io;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("72ea9d5d-1e5a-4063-aee2-861e4b8d7434"); }
        }
    }
}
