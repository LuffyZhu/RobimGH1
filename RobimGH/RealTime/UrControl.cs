using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Rhino.Geometry;
using static Robim.Grasshopper.UrRead;

namespace Robim.Grasshopper
{
    public class UrControl : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the urcontrol class.
        /// </summary>
        public UrControl()
          : base("ControlRTDE", "cRTDE",
              "RTDE robot control",
              "Robim", "RealTime")
        {
        }

        //declare a new urControl object
        private static UR_Control currentURcontrol = new UR_Control();
        //declare a new reset bool
        private static bool reset = false;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            /* [0] */
            pManager.AddTextParameter("IP", "IP", "Robot IP", GH_ParamAccess.item);
            /* [1] */
            pManager.AddIntegerParameter("Frequency", "Frequency", "Frequency", GH_ParamAccess.item);
            /* [2] */
            pManager.AddBooleanParameter("Start", "Start", "Start", GH_ParamAccess.item);
            /* [3] */
            pManager.AddBooleanParameter("Reset", "RE", "Reset", GH_ParamAccess.item);

            /* [4] */
            pManager.AddNumberParameter("CPose", "CPose", "Cartesian posision values", GH_ParamAccess.list);
            pManager[4].Optional = true;
            /* [5] */
            pManager.AddNumberParameter("COri", "COri", "Cartesian orientation values", GH_ParamAccess.list);
            pManager[5].Optional = true;
            /* [6] */
            pManager.AddNumberParameter("JPose", "JPose", "Joint values", GH_ParamAccess.list);
            pManager[6].Optional = true;
            /* [7] */
            pManager.AddBooleanParameter("JMode", "JMode", "Joint mode activated? ", GH_ParamAccess.item);
            pManager[7].Optional = true;
            /* [8] */
            pManager.AddNumberParameter("Velocity", "Vel", "Assign velocity", GH_ParamAccess.item);
            pManager.AddNumberParameter("Acceleration", "ACC", "Assign Acceleration speed", GH_ParamAccess.item);
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
            //retrive the reset bool
            DA.GetData(3, ref reset);
            //if the reset bool is true then create a new urControl client
            if (reset)
            {
                if (currentURcontrol != null) { currentURcontrol.Destroy(); }
                currentURcontrol = new UR_Control();
                reset = false;
            }

            string robotIP = "192.168.1.11";
            DA.GetData(0, ref robotIP);
            int frequency = 100;
            DA.GetData(1, ref frequency);
            bool ifstart = false;
            DA.GetData(2, ref ifstart);

            List<double> cpos = new List<double>();
            List<double> cori = new List<double>();
            DA.GetDataList(3, cpos);
            DA.GetDataList(4, cori);

            List<double> jpos = new List<double>();
            DA.GetDataList(5, jpos);

            bool jmode = false;
            DA.GetData(6, ref jmode);

            double VelSpeed = 1.0;
            DA.GetData(8, ref VelSpeed);

            double AccSpeed = 1.0;
            DA.GetData(9, ref AccSpeed);

            UR_Control_Data.ip_address = robotIP;
            UR_Control_Data.time_step = frequency;

            UR_Control_Data.velocity = Convert.ToString(VelSpeed);
            UR_Control_Data.acceleration = Convert.ToString(AccSpeed);

            if (cpos.Count == 3) { UR_Control_Data.C_Position = cpos.ToArray(); }
            if (cori.Count == 3) { UR_Control_Data.C_Orientation = cori.ToArray(); }
            if (jpos.Count == 6) { UR_Control_Data.J_Orientation = jpos.ToArray(); }


            //UR_Control ur_ctrl_robot = null;
            if (ifstart)
            {
                //ur_ctrl_robot = new UR_Control();
                //ur_ctrl_robot.Start();
                currentURcontrol.UR_Control_Once(jmode);
            }
            //else { if (ur_ctrl_robot != null) { ur_ctrl_robot.Destroy(); } }
        }

        public static class UR_Control_Data
        {
            // IP Port Number and IP Address
            public static string ip_address;
            //  Real-time (Read/Write)
            public const ushort port_number = 30003;
            // Comunication Speed (ms)
            public static int time_step;
            // Home Parameters UR3/UR3e:
            //  Joint Space:
            //      Orientation {J1 .. J6} (rad)
            public static double[] J_Orientation = new double[6] { -1.6, -1.7, -2.2, -0.8, 1.59, -0.03 };
            //  Cartesian Space:
            //      Position {X, Y, Z} (mm)
            public static double[] C_Position = new double[3] { -0.11, -0.26, 0.15 };
            //      Orientation {Euler Angles} (rad):
            public static double[] C_Orientation = new double[3] { 0.0, 3.11, 0.0 };
            // Move Parameters: Velocity, Acceleration
            public static string velocity = "0.01";
            public static string acceleration = "0.1";
        }

        public class UR_Control
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

            public void UR_Control_Once(bool jmode)
            {
                try
                {
                    if (tcp_client.Connected == false)
                    {
                        // Connect to controller -> if the controller is disconnected
                        tcp_client.Connect(UR_Control_Data.ip_address, UR_Control_Data.port_number);
                    }

                    // Initialization TCP/IP Communication (Stream)
                    network_stream = tcp_client.GetStream();

                    if (jmode)
                    {
                        var jpose = FormatPose(UR_Control_Data.J_Orientation[0],
                                               UR_Control_Data.J_Orientation[1],
                                               UR_Control_Data.J_Orientation[2],
                                               UR_Control_Data.J_Orientation[3],
                                               UR_Control_Data.J_Orientation[4],
                                               UR_Control_Data.J_Orientation[5]);
                        packet_cmd = utf8.GetBytes("movej([" + jpose + "]," + "a=" + UR_Control_Data.acceleration + ", v=" + UR_Control_Data.velocity + ")" + "\n");
                    }
                    else
                    {
                        var cpose = FormatPose(UR_Control_Data.C_Position[0],
                                               UR_Control_Data.C_Position[1],
                                               UR_Control_Data.C_Position[2],
                                               UR_Control_Data.C_Orientation[0],
                                               UR_Control_Data.C_Orientation[1],
                                               UR_Control_Data.C_Orientation[2]);
                        packet_cmd = utf8.GetBytes("movel(p[" + cpose + "]," + "a=" + UR_Control_Data.acceleration + ", v=" + UR_Control_Data.velocity + ")" + "\n");
                    }

                    //  Send command to the robot
                    network_stream.Write(packet_cmd, 0, packet_cmd.Length);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }

            }

            public string FormatPose(double d1, double d2, double d3, double d4, double d5, double d6)
            {
                string s1 = d1.ToString("0.###");
                string s2 = d2.ToString("0.###");
                string s3 = d3.ToString("0.###");

                string s4 = d4.ToString("0.###");
                string s5 = d5.ToString("0.###");
                string s6 = d6.ToString("0.###");
                return s1 + "," + s2 + "," + s3 + "," + s4 + "," + s5 + "," + s6;
            }

            public void UR_Control_Thread()
            {
                try
                {
                    if (tcp_client.Connected == false)
                    {
                        // Connect to controller -> if the controller is disconnected
                        tcp_client.Connect(UR_Control_Data.ip_address, UR_Control_Data.port_number);
                    }

                    // Initialization TCP/IP Communication (Stream)
                    network_stream = tcp_client.GetStream();

                    while (exit_thread == false)
                    {


                        packet_cmd = utf8.GetBytes("movej([" + "-1.571,-1.571,-1.571,-1.571,1.571,0.000" + "],"
                                                             + "a=" + UR_Control_Data.acceleration + ", v=" + UR_Control_Data.velocity + ")" + "\n");

                        //  Send command to the robot
                        network_stream.Write(packet_cmd, 0, packet_cmd.Length);
                        //  Wait Time (5 seconds)
                        Thread.Sleep(5000);



                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
            }
            public void Start()
            {
                exit_thread = false;
                // Start a thread to control Universal Robots (UR)
                robot_thread = new Thread(new ThreadStart(UR_Control_Thread));
                robot_thread.IsBackground = true;
                robot_thread.Start();
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
                return Properties.Resources.control;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("895df3e8-4744-48ae-bebc-44028e7c11a2"); }
        }
    }
}
