using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Robim
{
    public class FtpHelper
    {
        public string FTPPath { get; set; }
        public string FilePath { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<string> Msg { get; set; }
        private bool UpLoaded { get; set; } = false;
        public string Path { get; set; }

        public string[] GetFolderAndFileList()
        {
            string[] getfolderandfilelist;
            FtpWebRequest request;
            StringBuilder sb = new StringBuilder();
            try
            {
                request = (FtpWebRequest)FtpWebRequest.Create(new Uri(FTPPath));
                request.UseBinary = true;
                request.Credentials = new NetworkCredential(Username, Password);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.UseBinary = true;
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadLine();
                while (line != null)
                {
                    sb.Append(line);
                    sb.Append("\n");
                    Console.WriteLine(line);
                    line = reader.ReadLine();
                }
                sb.Remove(sb.ToString().LastIndexOf('\n'), 1);
                reader.Close();
                response.Close();
                string[] folderList = sb.ToString().Split('\n');
                return folderList;
            }
            catch
            {
                //Console.WriteLine("获取FTP上面的文件夹和文件：" + ex.Message);
                getfolderandfilelist = null;
                return getfolderandfilelist;
            }
        }


        public void FileUpLoad()
        {

            try
            {
                string url = FTPPath;
                try
                {
                    FtpWebRequest request = null;
                    try
                    {
                        FileInfo fi = new FileInfo(FilePath);
                        using (FileStream fs = fi.OpenRead())
                        {
                            request = (FtpWebRequest)FtpWebRequest.Create(new Uri(url + fi.Name));
                            string a = url + fi.Name;
                            request.Credentials = new NetworkCredential(Username, Password);
                            request.KeepAlive = false;
                            request.Method = WebRequestMethods.Ftp.UploadFile;
                            request.ContentLength = fi.Length;
                            request.UseBinary = true;
                            using (Stream stream = request.GetRequestStream())
                            {
                                int bufferLength = 5120;
                                byte[] buffer = new byte[bufferLength];
                                int i;
                                while ((i = fs.Read(buffer, 0, bufferLength)) > 0)
                                {
                                    stream.Write(buffer, 0, i);
                                }
                                stream.Close();
                                fs.Close();
                                //Console.WriteLine("FTP上传文件succesful");
                                Msg.Add("FTP upload succesfully");
                                UpLoaded = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("FTP upload: " + ex.Message);
                        Msg.Add("FTP upload: " + ex.Message);
                    }
                    finally
                    {

                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("FTP upload: " + ex.Message);
                    Msg.Add("FTP upload: " + ex.Message);
                }
                finally
                {

                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("FTP upload: " + ex.Message);
                Msg.Add("FTP upload: " + ex.Message);
            }
            if (!UpLoaded)
            {
                try
                {
                    Msg.Clear();

                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
                    p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                    p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                    p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                    p.StartInfo.CreateNoWindow = true;//不显示程序窗口
                    p.Start();//启动程序

                    //向cmd窗口发送输入信息
                    p.StandardInput.WriteLine("ftp" + Path + "&exit");

                    p.StandardInput.AutoFlush = true;
                    //p.StandardInput.WriteLine("exit");
                    //向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
                    //同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令

                    p.StandardInput.WriteLine("anonymous" + "&exit");
                    p.StandardInput.WriteLine("cd md:/" + "&exit");
                    p.StandardInput.WriteLine("put" + $"\"{FilePath}\"" + "&exit");
                    string output = p.StandardOutput.ReadLine();

                    Msg.Add($"FTP upload succesfully: {output}");
                    UpLoaded = true;
                    p.StandardInput.WriteLine("quit");
                    p.WaitForExit();//等待程序执行完退出进程
                    p.Close();
                }
                catch (Exception ex)
                {
                    Msg.Add("FTP upload: " + ex.Message);
                }
            }

        }



    }
}
