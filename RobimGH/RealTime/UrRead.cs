using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Robim.Grasshopper
{
    public class UrRead : GH_Component
    {

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public UrRead()
          : base("ReadRTDE", "rRTDE",
              "RTDE robot read values",
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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("JPose", "JPose", "Joint position values", GH_ParamAccess.list);
            //0

            pManager.AddNumberParameter("CPose", "CPose", "Cartesian posision values", GH_ParamAccess.list);
            //1

            pManager.AddNumberParameter("COri", "COri", "Cartesian orientation values", GH_ParamAccess.list);
            //2

            pManager.AddNumberParameter("DigIn", "DigIn", "Digital input bits", GH_ParamAccess.list);
            //3

            pManager.AddNumberParameter("DigOut", "DigOut", "Digital output bits", GH_ParamAccess.list);
            //4
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
            UR_Stream ur_stream_robot = null;

            UR_Stream_Data.ip_address = robotIP;
            UR_Stream_Data.time_step = 200;  // 200ms

            if (ifstart)
            {
                ur_stream_robot = new UR_Stream();
                //ur_stream_robot.Start();
                ur_stream_robot.UR_Stream_Once();
                var j1 = UR_Stream_Data.J_Orientation[0];
                var j2 = UR_Stream_Data.J_Orientation[1];
                var j3 = UR_Stream_Data.J_Orientation[2];
                var j4 = UR_Stream_Data.J_Orientation[3];
                var j5 = UR_Stream_Data.J_Orientation[4];
                var j6 = UR_Stream_Data.J_Orientation[5];
                var jvals = new List<double>() { j1, j2, j3, j4, j5, j6 };
                DA.SetDataList(0, jvals);

                var cp1 = UR_Stream_Data.C_Position[0];
                var cp2 = UR_Stream_Data.C_Position[1];
                var cp3 = UR_Stream_Data.C_Position[2];
                var cpos = new List<double>() { cp1, cp2, cp3 };
                DA.SetDataList(1, cpos);

                var co1 = UR_Stream_Data.C_Orientation[0];
                var co2 = UR_Stream_Data.C_Orientation[1];
                var co3 = UR_Stream_Data.C_Orientation[2];
                var cori = new List<double>() { co1, co2, co3 };
                DA.SetDataList(2, cori);

                var dinput = UR_Stream_Data.DigitalInputBits;
                var decodedDI = UR_Stream.DecodeDigitalBits(Convert.ToInt32(dinput));
                DA.SetDataList(3, decodedDI);

                var doutput = UR_Stream_Data.DigitalOutputBits;
                var decodedDO = UR_Stream.DecodeDigitalBits(Convert.ToInt32(doutput));
                DA.SetDataList(4, decodedDO);
            }
            else { if (ur_stream_robot != null) { ur_stream_robot.Destroy(); } }
        }

        private static List<double> pose;
        private static DateTime _lastReceived = DateTime.Now;
        /*private static void CartesianInfoReceived(object sender, CartesianInfoPackageEventArgs e, IGH_DataAccess DA)
        {
            if ((DateTime.Now - _lastReceived).TotalMilliseconds < 200) return;
            _lastReceived = DateTime.Now;

            pose = new List<double>() { e.X, e.Y, e.Z, e.Rx, e.Ry, e.Rz };
            DA.SetDataList(0,pose);
        }*/

        public class UR_Stream
        {
            // Initialization of Class variables
            //  Thread
            private Thread robot_thread = null;
            private bool exit_thread = false;
            //  TCP/IP Communication
            private TcpClient tcp_client = new TcpClient();
            private NetworkStream network_stream = null;
            //  Packet Buffer (Read)
            private byte[] packet = new byte[1116];

            // Offset:
            //  Size of first packet in bytes (Integer)
            private const byte first_packet_size = 4;
            //  Size of other packets in bytes (Double)
            private const byte offset = 8;

            // Total message length in bytes
            private const UInt32 total_msg_length = 3288596480;

            public void UR_Stream_Once()
            {
                try
                {
                    if (tcp_client.Connected == false)
                    {
                        // Connect to controller -> if the controller is disconnected
                        tcp_client.Connect(UR_Stream_Data.ip_address, UR_Stream_Data.port_number);
                    }

                    // Initialization TCP/IP Communication (Stream)
                    network_stream = tcp_client.GetStream();

                    // Get the data from the robot
                    if (network_stream.Read(packet, 0, packet.Length) != 0)
                    {
                        //if (BitConverter.ToUInt32(packet, first_packet_size - 4) == total_msg_length)
                        if (500 == 500)
                        {

                            // Reverses the order of elements in a one-dimensional array or part of an array.
                            Array.Reverse(packet);

                            // Note:
                            //  For more information on values 32... 37, etc., see the UR Client Interface document.
                            // Read Joint Values in radians
                            UR_Stream_Data.J_Orientation[0] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (32 * offset));
                            UR_Stream_Data.J_Orientation[1] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (33 * offset));
                            UR_Stream_Data.J_Orientation[2] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (34 * offset));
                            UR_Stream_Data.J_Orientation[3] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (35 * offset));
                            UR_Stream_Data.J_Orientation[4] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (36 * offset));
                            UR_Stream_Data.J_Orientation[5] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (37 * offset));

                            // Read Cartesian (Positon) Values in metres
                            UR_Stream_Data.C_Position[0] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (56 * offset));
                            UR_Stream_Data.C_Position[1] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (57 * offset));
                            UR_Stream_Data.C_Position[2] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (58 * offset));
                            // Read Cartesian (Orientation) Values in metres 
                            UR_Stream_Data.C_Orientation[0] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (59 * offset));
                            UR_Stream_Data.C_Orientation[1] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (60 * offset));
                            UR_Stream_Data.C_Orientation[2] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (61 * offset));

                            // Current state of the digital inputs. NOTE: these are bits encoded as int64_t, e.g.a value of 5 corresponds to bit 0 and bit 2 set high
                            UR_Stream_Data.DigitalInputBits = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (86 * offset));
                            UR_Stream_Data.DigitalOutputBits = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (131 * offset));

                            // Current state of the digital inputs. NOTE: these are bits encoded as int64_t, e.g.a value of 5 corresponds to bit 0 and bit 2 set high
                            UR_Stream_Data.RobotMode = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (95 * offset));

                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
            }

            /// <summary>
            /// decode which digital inputs have been activated based on "sum" signal from the robot
            /// </summary>
            /// <param name="sum"></param>
            /// <returns></returns>
            public static List<int> DecodeDigitalBits(int sum)
            {
                int[] values = new int[] { 1, 2, 4, 8, 16, 32, 64, 128 }; // digital input values 0 1 2 3 4 5 6 7
                List<int> decodedValues = new List<int>();
                if (sum == 0)
                {
                    // base case: the sum has been decoded
                    return new List<int>();
                }
                else
                {
                    for (int i = values.Length - 1; i >= 0; i--)
                    {
                        if (sum < values[i])
                        {
                            // if the sum is smaller than the current value, skip it
                            continue;
                        }

                        if (decodedValues.Contains(Convert.ToInt32(Math.Log(values[i], 2))))
                        {
                            // if the value has already been added to the decoded values, skip it
                            continue;
                        }

                        // add the value to the decoded values list and recurse on the remaining sum
                        decodedValues.Add(Convert.ToInt32(Math.Log(values[i], 2)));
                        sum = sum - values[i];
                    }
                    return decodedValues;
                }
            }

            public void UR_Stream_Thread()
            {
                try
                {
                    if (tcp_client.Connected == false)
                    {
                        // Connect to controller -> if the controller is disconnected
                        tcp_client.Connect(UR_Stream_Data.ip_address, UR_Stream_Data.port_number);
                    }

                    // Initialization TCP/IP Communication (Stream)
                    network_stream = tcp_client.GetStream();

                    // Initialization timer
                    var t = new Stopwatch();

                    while (exit_thread == false)
                    {
                        // Get the data from the robot
                        if (network_stream.Read(packet, 0, packet.Length) != 0)
                        {
                            if (BitConverter.ToUInt32(packet, first_packet_size - 4) == total_msg_length)
                            {
                                // t_{0}: Timer start.
                                t.Start();

                                // Reverses the order of elements in a one-dimensional array or part of an array.
                                Array.Reverse(packet);

                                // Note:
                                //  For more information on values 32... 37, etc., see the UR Client Interface document.
                                // Read Joint Values in radians
                                UR_Stream_Data.J_Orientation[0] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (32 * offset));
                                UR_Stream_Data.J_Orientation[1] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (33 * offset));
                                UR_Stream_Data.J_Orientation[2] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (34 * offset));
                                UR_Stream_Data.J_Orientation[3] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (35 * offset));
                                UR_Stream_Data.J_Orientation[4] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (36 * offset));
                                UR_Stream_Data.J_Orientation[5] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (37 * offset));

                                // Read Cartesian (Positon) Values in metres
                                UR_Stream_Data.C_Position[0] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (56 * offset));
                                UR_Stream_Data.C_Position[1] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (57 * offset));
                                UR_Stream_Data.C_Position[2] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (58 * offset));
                                // Read Cartesian (Orientation) Values in metres 
                                UR_Stream_Data.C_Orientation[0] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (59 * offset));
                                UR_Stream_Data.C_Orientation[1] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (60 * offset));
                                UR_Stream_Data.C_Orientation[2] = BitConverter.ToDouble(packet, packet.Length - first_packet_size - (61 * offset));

                                // t_{1}: Timer stop.
                                t.Stop();

                                // Recalculate the time: t = t_{1} - t_{0} -> Elapsed Time in milliseconds
                                if (t.ElapsedMilliseconds < UR_Stream_Data.time_step)
                                {
                                    Thread.Sleep(UR_Stream_Data.time_step - (int)t.ElapsedMilliseconds);
                                }

                                // Reset (Restart) timer.
                                t.Restart();
                            }
                        }
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
                robot_thread = new Thread(new ThreadStart(UR_Stream_Thread));
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
        public static class UR_Stream_Data
        {
            // IP Port Number and IP Address
            public static string ip_address;
            //  Real-time (Read Only)
            public const ushort port_number = 30013;
            // Comunication Speed (ms)
            public static int time_step;
            // Joint Space:
            //  Orientation {J1 .. J6} (rad)
            public static double[] J_Orientation = new double[6];
            // Cartesian Space:
            //  Position {X, Y, Z} (mm)
            public static double[] C_Position = new double[3];
            //  Orientation {Euler Angles} (rad):
            public static double[] C_Orientation = new double[3];

            public static double DigitalInputBits = 0;
            public static double DigitalOutputBits = 0;
            public static double RobotMode = 0;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.read;
            }
        }
        public override Guid ComponentGuid => new Guid("40707487-91fd-40f1-8d90-4570ed1077dd");

    }
}
