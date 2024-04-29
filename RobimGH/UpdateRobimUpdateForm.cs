using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Robim
{
    public partial class UpdateRobimUpdateForm : Form
    {
        string Ver = "";
        int Version { get; set; }
        GetABC getABC;
        public UpdateRobimUpdateForm(string ver)
        {
            InitializeComponent();
            Ver = ver;
        }
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries\\Robim\\RobimUpdate.exe");
        string dir_old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries\\RobimUpdate.exe");

        private void UpdateRobimUpdateForm_Load(object sender, EventArgs e)
        {
            this.HasRead += UpdateRobimUpdateForm_HasRead;
            getABC = new GetABC();
            using (SftpClient sftpClient = new SftpClient(getABC.A, getABC.B, getABC.C))
            {
                sftpClient.Connect();
                if (sftpClient.IsConnected)
                {
                    var folders = sftpClient.ListDirectory("/RobimUpdate/");
                    foreach(var folder in folders)
                    {
                        if (folder.IsDirectory)
                        {
                            if (!folder.Name.Contains('.'))
                            {
                                var onlinever = int.Parse(folder.Name.Remove(0, 3));//Ver
                                if (onlinever > Version)
                                    Version = onlinever;
                            }
                        }
                    }
                }
            }
            int major = 0;
            if (File.Exists(dir_old))
            {
                major = FileVersionInfo.GetVersionInfo(dir_old).FileMajorPart;
                File.Delete(dir_old);
            }
            else
            {
                major = FileVersionInfo.GetVersionInfo(dir).FileMajorPart;
            }
            if (Version > major)
            {
                InstallFile();
            }
            else
            {
                this.Close();
            }
        }

        private void UpdateRobimUpdateForm_HasRead(int percentage)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate {
                    progressBar1.Value = percentage;
                    Output.Text = $"{percentage}%";
                });
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        
        delegate void HasReadOneByteHandler(int percentage);
        event HasReadOneByteHandler HasRead;
        void SendPercentage(int now,int all)
        {
            int i = now * 100 / all;
            if(HasRead != null)
            {
                HasRead(i);
            }
        }
        public async void InstallFile()
        {
            Output.Text = "正在下载新版本文件，请耐心等待";
            bool success = await Task.Run(() =>
            {
                try
                {
                    using(SftpClient sftpClient = new SftpClient(getABC.A, getABC.B, getABC.C))
                    {
                        sftpClient.Connect();
                        if (sftpClient.IsConnected)
                        {
                            using(var sftp = sftpClient.OpenRead($"/RobimUpdate/Ver{Version}/RobimUpdate.exe"))
                            {
                                FileStream fileStream = new FileStream(dir, FileMode.Truncate, FileAccess.Write);
                                int length = (int)sftp.Length;
                                for(int i = 0; i < length; i++)
                                {
                                    int by = sftp.ReadByte();
                                    fileStream.WriteByte(Convert.ToByte(by));
                                    SendPercentage(i, length);
                                }
                                fileStream.Dispose();
                            }
                        }
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
            if (success)
            {
                Output.Text = "文件已下载，正在复制文件";
                this.Close();
            }
            else
            {
                Output.Text = "下载新版本文件失败，将使用旧版本";
                this.ControlBox = true;
                return;
            }
        }

        private void UpdateRobimUpdateForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Process.Start(dir, Ver);//A程序完整路径
        }
    }
    public class GetABC
    {
        public string A { get; }
        public string B { get; }
        public string C { get; }
        public GetABC()
        {
            StringSupport.Crypto.DES_Crypto dES_Crypto = new StringSupport.Crypto.DES_Crypto(Properties.Settings.Default.B, false, 1);
            StringSupport.Crypto.DES_Crypto dES_Crypto1 = new StringSupport.Crypto.DES_Crypto(Properties.Settings.Default.A, false, int.Parse(dES_Crypto.Result));
            string[] abc = dES_Crypto1.Result.Split('|');
            A = abc[0];
            B = abc[1];
            C = abc[2];
        }
    }
}
