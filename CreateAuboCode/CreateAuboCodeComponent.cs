using Grasshopper;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Robim;
using System;
using System.Collections.Generic;

namespace Robim.Grasshopper
{
    public class CreateAuboCodeComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CreateAuboCodeComponent()
          : base("CreateAuboCodeComponent", "CAC",
            "Create codes for Aubo robots.",
            "Robim", "Components")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Robim.Grasshopper.TargetParameter(), "Targets", "T", "targets as list", GH_ParamAccess.list);
            //pManager.AddNumberParameter("Speed","S","global speed control",GH_ParamAccess.item, 0.2);
            pManager.AddTextParameter("File path", "Path", "path to save your codes", GH_ParamAccess.item);
            pManager.AddTextParameter("File name", "Name", "file name", GH_ParamAccess.item, "DefaultCode");
            pManager.AddBooleanParameter("Save", "Save", "Save", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "C", "aubo code", GH_ParamAccess.item);
            pManager.AddTextParameter("error", "error", "error", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Robim.Grasshopper.GH_Target> targets = new List<Robim.Grasshopper.GH_Target>();
            double speed = 0;
            string file_path = "";
            string file_name = "";
            bool save = false;
            string Aubo_code = "";
            string error_msg = "";
            // Then we need to access the input parameters individually. 
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetDataList("Targets", targets)) return;
            //if (!DA.GetData(1, ref speed)) return;
            if (!DA.GetData(1, ref file_path)) return;
            if (!DA.GetData(2, ref file_name)) return;
            if (!DA.GetData(3, ref save)) return;
            if (RhinoDoc.ActiveDoc.ModelUnitSystem.ToString() == "Millimeters")
            {
                try
                {
                    List<string> code = new List<string>();
                    code.Add("init_global_move_profile()\r\nset_joint_maxvelc({1.298089,1.298089,1.298089,1.555088,1.555088,1.555088})\r\nset_joint_maxacc({8.654390,8.654390,8.654390,10.368128,10.368128,10.368128})\r\nset_end_maxacc(1.000000)");
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var type = targets[i].GetType();
                        if (type == typeof(JointTarget))

                        {

                            Robim.JointTarget jtarget = targets[i].Value as Robim.JointTarget;
                            //IGH_Param jtarget =  as IGH_Param;
                            //jtarget.v
                            //(jtarget.ToString());
                            string joint_all = jtarget.Joints[0].ToString();
                            for (int j = 1; j < 6; j++)
                            {
                                joint_all += ", " + jtarget.Joints[j].ToString();
                                //Print(joint_all);
                            }
                            speed = jtarget.Speed.TranslationSpeed / 1000;
                            code.Add("init_global_move_profile()");
                            code.Add("set_end_maxvelc(" + speed + ")");
                            //code.Add("set_joint_maxvelc({1.298089,1.298089,1.298089,1.555088,1.555088,1.555088})");
                            //code.Add("set_joint_maxacc({8.654390,8.654390,8.654390,10.368128,10.368128,10.368128})");
                            code.Add("move_joint" + "({" + joint_all + "}, true)");
                        }
                        else
                        {
                            Robim.CartesianTarget ctarget = targets[i].Value as Robim.CartesianTarget;
                            Point3d target_ori = ctarget.Plane.Origin;
                            target_ori.X = Math.Round(target_ori.X / 1000, 6);
                            target_ori.Y = Math.Round(target_ori.Y / 1000, 6);
                            target_ori.Z = Math.Round(target_ori.Z / 1000, 6);
                            //Print(ctarget.Plane.Origin.ToString());
                            //Print(Robim.Util.GetRotation(ctarget.Plane).ToString());
                            /*var func_info = Rhino.NodeInCode.Components.FindComponent("Robim.PlaneToEuler");
                            if(func_info == null)
                            {
                              Print("Error finding function");
                              return;
                            }
                            var func=func_info.Delegate as dynamic;
                            var eu = func(ctarget.Plane);*/
                            double target_qw = Math.Round(Robim.Util.GetRotation(ctarget.Plane).A, 6);
                            double target_qx = Math.Round(Robim.Util.GetRotation(ctarget.Plane).B, 6);
                            double target_qy = Math.Round(Robim.Util.GetRotation(ctarget.Plane).C, 6);
                            double target_qz = Math.Round(Robim.Util.GetRotation(ctarget.Plane).D, 6);
                            string target_quaternion = target_qw.ToString() + ", " + target_qx.ToString() + ", " + target_qy.ToString() + ", " + target_qz.ToString();

                            string motion = ctarget.Motion.ToString();
                            //Print(ctarget.Motion.ToString());
                            Point3d tcp_ori = ctarget.Tool.Tcp.Origin;
                            tcp_ori.X = Math.Round(tcp_ori.X / 1000, 6);
                            tcp_ori.Y = Math.Round(tcp_ori.Y / 1000, 6);
                            tcp_ori.Z = Math.Round(tcp_ori.Z / 1000, 6);
                            double tcp_qw = Math.Round(Robim.Util.GetRotation(ctarget.Tool.Tcp).A, 6);
                            double tcp_qx = Math.Round(Robim.Util.GetRotation(ctarget.Tool.Tcp).B, 6);
                            double tcp_qy = Math.Round(Robim.Util.GetRotation(ctarget.Tool.Tcp).C, 6);
                            double tcp_qz = Math.Round(Robim.Util.GetRotation(ctarget.Tool.Tcp).D, 6);
                            string tcp_quaternion = tcp_qw.ToString() + ", " + tcp_qx.ToString() + ", " + tcp_qy.ToString() + ", " + tcp_qz.ToString();
                            //Print(ctarget.Tool.Tcp.Origin.ToString());
                            //Print(ctarget.Speed.ToString());
                            string tmp = "get_target_pose({" + target_ori.ToString() + "}, {" + target_quaternion + "}, false, {" + tcp_ori + "}, {" + tcp_quaternion + "}), true)";
                            string move_motion = "";
                            if (motion != "Joint")
                            {
                                move_motion = "move_line";
                            }
                            else
                            {
                                move_motion = "move_joint";
                            }

                            tmp = move_motion + "(" + tmp;
                            speed = ctarget.Speed.TranslationSpeed / 1000;
                            code.Add("init_global_move_profile()");
                            code.Add("set_end_maxvelc(" + speed + ")");
                            //code.Add("set_end_maxacc(1.000000)");
                            code.Add(tmp);
                            if (ctarget.Command.ToString() != null)
                            {
                                string target_command = ctarget.Command.ToString();
                                //Print(ctarget.Command.ToString());
                                string command_IO = "";
                                if (target_command.Contains("DO"))
                                {
                                    string DO_number = System.Text.RegularExpressions.Regex.Replace(target_command, @"[^0-9]+", "");
                                    string DO_name = "U_DO_" + DO_number.PadLeft(2, '0');
                                    command_IO = "set_robot_io_status(RobotIOType.RobotBoardUserDO, " + '"' + DO_name + '"';
                                    if (target_command.Contains("True"))
                                    {
                                        command_IO += ", 1)";
                                    }
                                    else
                                    {
                                        command_IO += ", 0)";
                                    }
                                    code.Add(command_IO);
                                    //Print(command_IO);
                                }
                                if (target_command.Contains("Wait"))
                                {
                                    double wait_time = Double.Parse(System.Text.RegularExpressions.Regex.Replace(target_command, @"[^0-9]+", ""));
                                    //Print(wait_time.ToString());
                                    if (wait_time > 0)
                                    {
                                        string sleep = "sleep(" + wait_time + ")";
                                        //Print(sleep);
                                        code.Add(sleep);
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 0; i < code.Count; i++)
                    {
                        Aubo_code += code[i] + Environment.NewLine;
                    }
                    if (save)
                    {
                        file_path = file_path + "\\" + file_name + ".aubo";
                        //Print(file_path);
                        System.IO.File.WriteAllText(@file_path, Aubo_code.ToString());
                    }
                    DA.SetData(0, Aubo_code);
                }
                catch (Exception ex)
                {
                    error_msg += ex.Message;
                    DA.SetData(1, error_msg);
                }
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "请设置模型单位为毫米！");
                error_msg = "请设置模型单位为毫米！";
                DA.SetData(1, error_msg);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            //get 
            {
                //return Properties.Resources.CACicon; 
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("f83f5555-140d-409e-8274-88f000e345d9");
    }
}