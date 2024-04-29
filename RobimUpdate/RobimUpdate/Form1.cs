using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace RobimUpdate
{
    public partial class Form1 : Form
    {
        //int thisversion = 0;
        string ver = "0.0.0.0";
        GetABC getABC;
        public Form1()
        {
            InitializeComponent();
        }
        public Form1(string nowver)
        {
            InitializeComponent();
            //thisversion = int.Parse(nowver);
            ver = nowver;
        }
        UpdateItem UpdateInfo = new UpdateItem();

        private void Form1_Load(object sender, EventArgs e)
        {
            //version.Text = "ver_" + thisversion.ToString();
            version.Text = "ver_" + ver;
        }

        List<string> AllFiles = new List<string>();
        string DownloadPath = "";
        List<string> allfilepathes = new List<string>();

        private void GetUpdate(object sender, EventArgs e)
        {
            getABC = new GetABC();
            Output.Text = "正在查询更新";
            string URLString = "http://106.14.212.171/Robim/Update.xml";
            //WebClient client = new WebClient();
            XmlTextReader reader = new XmlTextReader(URLString);
            Task task = Task.Factory.StartNew(() =>
            {
                while (reader.Read())
                {
                    #region Example
                    /*switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            Console.Write("<" + reader.Name);

                            while (reader.MoveToNextAttribute()) // Read the attributes.
                                Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                            Console.Write(">");
                            Console.WriteLine(">");
                            break;
                        case XmlNodeType.Text: //Display the text in each element.
                            Console.WriteLine(reader.Value);
                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            Console.Write("</" + reader.Name);
                            Console.WriteLine(">");
                            break;
                    }*/
                    #endregion
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            switch (reader.Name)
                            {
                                case "Version":
                                    reader.Read();
                                    UpdateInfo.Version = reader.Value;
                                    break;
                                case "UpdateContent":
                                    reader.Read();
                                    UpdateInfo.UpdateContent = reader.Value;
                                    break;
                                //case "DownloadUri":
                                //    reader.Read();
                                //    UpdateInfo.DownloadUri = reader.Value;
                                //    break;
                                //case "DllUri":
                                //    reader.Read();
                                //    UpdateInfo.DllUri = reader.Value;
                                //    break;
                                //case "GhaUri":
                                //    reader.Read();
                                //    UpdateInfo.GhaUri = reader.Value;
                                //    break;
                                case "FolderUri":
                                    reader.Read();
                                    UpdateInfo.FolderUri = reader.Value;
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            break;
                    }
                }
            });
            task.Wait();
            string[] vers = ver.Split('.');//1.2020.09.09
            string[] onlinevers = UpdateInfo.Version.Split('.');//1.2021.01.01
            bool hasnew = false;
            if(int.Parse(vers[1]) < int.Parse(onlinevers[1]))
            {
                hasnew = true;
            }
            else if(int.Parse(vers[1]) == int.Parse(onlinevers[1]))
            {
                if(int.Parse(vers[2]) < int.Parse(onlinevers[2]))
                {
                    hasnew = true;
                }
                else if (int.Parse(vers[2]) == int.Parse(onlinevers[2]))
                {
                    if (int.Parse(vers[3]) < int.Parse(onlinevers[3]))
                    {
                        hasnew = true;
                    }
                }
            }
            if(hasnew)
            {
                Output.Text = "有更新版:Ver_" + UpdateInfo.Version;
                string[] text = UpdateInfo.UpdateContent.Split('\n');
                string[] print = new string[0];
                for(int i = 0; i < text.Length; i++)
                {
                    string str = text[i].Trim();
                    str = str.Replace("\r", "");
                    if (str != "")
                    {
                        UpdateContext.Text += "\n" + str;
                    }
                }

                AllFiles = new List<string>(500);
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UpdateInfo.FolderUri);
                //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                //{
                //    using (StreamReader streamreader = new StreamReader(response.GetResponseStream()))
                //    {
                //        string html = streamreader.ReadToEnd();

                //        Regex regex = new Regex("<a href=\".*\">(?<name>.*)</a>");
                //        MatchCollection matches = regex.Matches(html);

                //        if (matches.Count > 0)
                //        {
                //            foreach (Match match in matches)
                //            {
                //                if (match.Success)
                //                {
                //                    string[] matchData = match.Groups[0].ToString().Split('\"');
                //                    if (matchData[1].Contains("gha") || matchData[1].Contains("dll"))
                //                        AllFiles.Add(matchData[1]);
                //                }
                //            }
                //        }
                //    }
                //}
                List<string> allfileuri = new List<string>();
                double Totalsize = 0;
                using (SftpClient sftpClient = new SftpClient(getABC.A, getABC.B, getABC.C))
                {
                    sftpClient.Connect();
                    if (sftpClient.IsConnected)
                    {
                        var files = sftpClient.ListDirectory(UpdateInfo.FolderUri);
                        foreach(var file in files)
                        {
                            if (!file.IsDirectory)
                            {
                                AllFiles.Add(file.Name);
                                allfileuri.Add(file.FullName);
                                Totalsize += file.Length;
                            }
                        }
                    }
                }
                //List<Task> Tasks = new List<Task>(AllFiles.Count);
                //foreach (string onefile in AllFiles)
                //{
                //    string path = Path.Combine(UpdateInfo.FolderUri, onefile);
                //    allfileuri.Add(path);
                //    Task task1 = Task.Factory.StartNew(() =>
                //    {
                //        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(path);
                //        req.Method = "HEAD";
                //        using (HttpWebResponse resp = (HttpWebResponse)(req.GetResponse()))
                //        {
                //            Totalsize += resp.ContentLength / 1024.0 / 1024;
                //        }
                //    });
                //    Tasks.Add(task1);
                //}
                //Task.WaitAll(Tasks.ToArray(),-1);
                //Tasks.ForEach(x => x.Dispose());
                UpdateInfo.AllFileUri = allfileuri;
                UpdateInfo.Size = Totalsize;
                Install.Enabled = true;
            }
            else
            {
                Output.Text = "已经是最新版";
                Done.Enabled = true;
            }
        }

        private void Form1_HasRead(long alllength, double percentage)
        {
            double a = percentage * 4.85;
            try
            {
                this.Invoke((MethodInvoker)delegate {
                    downloadcount.Text = percentage.ToString("f0") + "% (" + (alllength / 1024.0 / 1024).ToString("f3") + "MB / " + (UpdateInfo.Size / 1024.0 / 1024).ToString("f3") + "MB)"; //输出：已下载/总大小
                    slider.Width = (int)a;
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void Done_Click(object sender, EventArgs e)
        {
            string appname = "Rhino";
            Process[] processes = Process.GetProcessesByName(appname);
            if(processes.Length == 0)
            {
                Process.Start(@"C:\Program Files\Rhino 6\System\Rhino.exe");//A程序完整路径，不是这个路径不会开
            }
            Environment.Exit(0);
        }
        private void Install_Click(object sender, EventArgs e)
        {
            InstallFile();
            this.HasRead += Form1_HasRead;
            Install.Enabled = false;
        }

        delegate void HasReadOneByteHandler(long alllength, double percentage);
        event HasReadOneByteHandler HasRead;
        long allbytes = 0;
        int current = 0;
        void SendPercentage()
        {
            allbytes += 1;//呼叫一次 +1

            double percentage = (allbytes * 100) / UpdateInfo.Size;

            if((int)percentage != current)
            {
                if (HasRead != null)
                {
                    HasRead(allbytes, percentage);
                }
                current = (int)percentage;
            }
        }
        public async void InstallFile()
        {
            string appname = "Rhino";
            Process[] processes = Process.GetProcessesByName(appname);
            foreach (var p in processes)
                p.Kill();
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries\\Robim\\Download");
            DownloadPath = dir;
            Directory.CreateDirectory(dir);
            allfilepathes = new List<string>();
            foreach (string file in AllFiles)
            {
                allfilepathes.Add(Path.Combine(dir, file));
            }
            //string dllfile = Path.Combine(dir, "Robim.dll");
            //string ghafile = Path.Combine(dir, "Robim.gha");

            //UpdateInfo.Size = ((new FileInfo(UpdateInfo.DllUri).Length) / 1024.0 / 1024).ToString("f2");

            //Output.Text = "正在下载新版本文件，请耐心等待 0 MB /" + UpdateInfo.Size;

            Output.Text = "正在下载新版本文件，请耐心等待";

            Bar.Visible = true;

            bool[] downloadsuccess = new bool[allfilepathes.Count];
            for (int i = 0; i < allfilepathes.Count; i++)
            {
                downloadsuccess[i] = await Task.Run(() =>
                {
                    try
                    {
                        using (SftpClient sftp = new SftpClient(getABC.A, getABC.B, getABC.C))
                        {
                            sftp.Connect();
                            if (sftp.IsConnected)
                            {
                                using (var sftpstream = sftp.OpenRead(UpdateInfo.AllFileUri[i]))
                                {
                                    FileStream fileStream = new FileStream(allfilepathes[i], FileMode.Create, FileAccess.Write);
                                    int length = (int)sftpstream.Length;
                                    for (int j = 0; j < length; j++)
                                    {
                                        int by = sftpstream.ReadByte();
                                        fileStream.WriteByte(Convert.ToByte(by));
                                        SendPercentage();
                                    }
                                    fileStream.Dispose();
                                }
                            }
                        }
                        return true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return false;
                    }
                });
            }
            //bool success = await Task.Run(() =>
            //{
            //    try
            //    {
            //        for (int i = 0; i < allfilepathes.Count; i++)
            //        {
            //            using (SftpClient sftp = new SftpClient(getABC.A, getABC.B, getABC.C))
            //            {
            //                sftp.Connect();
            //                if (sftp.IsConnected)
            //                {
            //                    using (var sftpstream = sftp.OpenRead(UpdateInfo.AllFileUri[i]))
            //                    {
            //                        FileStream fileStream = new FileStream(allfilepathes[i], FileMode.Create, FileAccess.Write);
            //                        int length = (int)sftpstream.Length;
            //                        for (int j = 0; j < length; j++)
            //                        {
            //                            int by = sftpstream.ReadByte();
            //                            fileStream.WriteByte(Convert.ToByte(by));
            //                            SendPercentage(j);
            //                        }
            //                        fileStream.Dispose();
            //                    }
            //                }
            //            }
            //        }
            //        return true;
            //    }
            //    catch (Exception e)
            //    {
            //        MessageBox.Show(e.Message);
            //        return false;
            //    }
            //});
            if (downloadsuccess.All(x => x == true))
            {
                downloadcount.Text = "Done";
                slider.Width = 485;
                Output.Text = "文件已下载，正在复制文件";
                MoveFiles();
            }
            else
            {
                Output.Text = "下载新版本文件失败，请重试";
                Install.Enabled = true;
                Done.Enabled = true;
                return;
            }
        }
        async void MoveFiles()
        {
            bool success2 = await Task.Run(() =>
            {
                try
                {
                    string path = DownloadPath.Replace("\\Download", "");
                    //ZipFile.ExtractToDirectory(zipfile, extractPath);
                    foreach (string item in Directory.GetFiles(DownloadPath))
                        File.Copy(item, Path.Combine(path, Path.GetFileName(item)), true);
                    //File.Delete(zipfile);
                    DirectoryInfo di = new DirectoryInfo(DownloadPath);
                    di.Delete(true);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
            
            if (success2)
            {
                string dir_old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries");
                string dllfile_old = Path.Combine(dir_old, "Robim.dll");
                string ghafile_old = Path.Combine(dir_old, "Robim.gha");
                if (File.Exists(dllfile_old))
                {
                    File.Delete(dllfile_old);
                }
                if (File.Exists(ghafile_old))
                {
                    File.Delete(ghafile_old);
                }
                ver = UpdateInfo.Version;
                version.Text = "ver_" + ver;
                Output.Text = "更新完成，您可以点击下方按钮启动应用";
                Done.Enabled = true;
            }
            else
            {
                Output.Text = "复制更新文件出错，请重试";
                Install.Enabled = true;
                Done.Enabled = true;
            }
        }
    }

    public class UpdateItem
    {
        public string Version { get; set; } = "0.0000.00.00";  //版本号
        public string UpdateContent { get; set; }  //更新信息
        public string DownloadUri { get; set; }  //更新包的下载地址
        public string DllUri { get; set; }  //更新包的下载地址
        public string GhaUri { get; set; }  //更新包的下载地址
        /// <summary>
        /// 更新包folder的下载地址
        /// </summary>
        public string FolderUri { get; set; }
        public List<string> AllFileUri { get; set; }
        public string Time { get; set; }  //更新时间
        public double Size { get; set; }  //更新包大小
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

