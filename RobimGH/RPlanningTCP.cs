using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using SimpleTcp;


namespace Robim.Grasshopper
{
    public class RPlanningTCP : GH_Component
    {
        string output = "output";
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
        public RPlanningTCP()
          : base("RPlanningTCP", "Nickname",
            "Description",
            "Robim", "Planning")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("JSONr", "JSONr", "JSON request for planning service", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Send", "Send", "Send request JSON to planning service", GH_ParamAccess.item);
            pManager.AddTextParameter("IP", "IP", "IP address to send to", GH_ParamAccess.item);
            pManager.AddTextParameter("Port", "Port", "Connection port number", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
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
            string request = null;
            DA.GetData(0, ref request);

            bool send = false;
            DA.GetData(1, ref send);

            string ip = "localhost";
            DA.GetData(2, ref ip);

            string port = "5002";
            DA.GetData(3, ref port);

            //var clientW = new KVPTcpClient();

            string ip_send = ip + ":" + port;
            SimpleTcpClient tcpClient = new SimpleTcpClient(ip_send);
            tcpClient.Events.Connected += Events_Connected;
            tcpClient.Events.Disconnected += Events_Disconnected;
            tcpClient.Events.DataReceived += Events_DataReceived1;
            string bytes = null;

            if (send)
            {
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

            //string response = null;
            DA.SetData(1, output);
            DA.SetData(0, backjason);
            DA.SetData(2, bytes);
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
        public override Guid ComponentGuid => new Guid("0f907e60-a7f3-4f7b-b5df-8d2b3bf423a9");

        private void Events_Disconnected(object sender, ClientDisconnectedEventArgs e)
        {
            connectionstate = false;
            output += "\n" + e.IpPort + " ...disconnected";


        }

        private void Events_Connected(object sender, ClientConnectedEventArgs e)
        {
            output += "\n" + e.IpPort + " ...connected";
        }

        private void Events_DataReceived1(object sender, DataReceivedEventArgs e)
        {
            byte[] Head = new byte[4];
            byte[] Text = new byte[e.Data.Length - 4];
            Array.Copy(e.Data, 0, Head, 0, 4);
            Array.Copy(e.Data, 4, Text, 0, e.Data.Length - 4);
            output += "\n" + e.IpPort + ":" + System.BitConverter.ToInt32(Head, 0) + System.Text.Encoding.Default.GetString(Text);
            backjason = System.Text.Encoding.Default.GetString(Text);


        }

        private void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

    }
}