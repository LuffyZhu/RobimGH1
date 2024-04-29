using ConnectToDB;
using FoxLearn.License;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Robim
{
    public partial class LicenseForm : Form
    {
        public LicenseForm()
        {
            InitializeComponent();
        }
        private void LicenseForm_Load(object sender, EventArgs e)
        {
            txtProductID.Text = ComputerInfo.GetComputerId();
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            KeyManager km = new KeyManager(txtProductID.Text);
            string productKey = txtProductKey.Text;
            LicenseInfo lic = new LicenseInfo();
            lic.ProductKey = productKey;
            lic.FullName = txtProductID.Text;
            /*if(kv.Type == LicenseType.TRIAL)
            {
                lic.Day = kv.Expiration.Day;
                lic.Month = kv.Expiration.Month;
                lic.Year = kv.Expiration.Year;
            }*/
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Robim");
            string folder2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grasshopper\\Libraries\\Robim");
            List<string> IBI = DBConnect.SelectWhere("experience_day,product_key,products_id", "Robim", "Robim_License", "product_id", txtProductID.Text);
            /*if (Directory.Exists(folder2))
            {
                km.SaveSuretyFile(string.Format(@"{0}\Key.lic", folder2), lic);
                MessageBox.Show("Successfully Registered!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($" Folder '{folder2}' not found");
            }*/
            if (IBI.Count > 0)
            {
                string lblLicenseDay = IBI[0];
                string lblProductKey = IBI[1];
                string lblProductsID = IBI[2];
                if (lblProductKey == txtProductKey.Text)
                {
                    if (lblProductsID == "0" || lblProductsID == "1")
                    {
                        DateTime st = Convert.ToDateTime(lblLicenseDay);
                        string time = DBConnect.GetTime();
                        if (time != null)
                        {
                            DateTime localTime = Convert.ToDateTime(time);
                            string EndDate = st.ToString();
                            TimeSpan ts = st.Subtract(localTime);
                            double seconds = ts.TotalSeconds;
                            if (seconds > 0)
                            {
                                System.Windows.Forms.MessageBox.Show("Successfully Registered!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                if (!Directory.Exists(folder2))
                                {
                                    Directory.CreateDirectory(folder2);
                                    lic.FullName = txtProductID.Text;
                                    lic.ProductKey = txtProductKey.Text;
                                    km.SaveSuretyFile(string.Format(@"{0}\Key.lic", folder2), lic);
                                }
                            }
                            else
                            {
                                System.Windows.Forms.MessageBox.Show("License use time exceeds! Registration failed!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Can't get server time! Registration failed!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Your License is not valid for this product! Registration failed!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Product Key is not correct! Registration failed!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Network Error, Registration failed!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            this.Close();
            //if (km.ValidKey(ref productKey))
            //{
            //    LicenseInfo lic = new LicenseInfo();
            //    lic.ProductKey = productKey;
            //    lic.FullName = txtProductID.Text;
            //    /*if(kv.Type == LicenseType.TRIAL)
            //    {
            //        lic.Day = kv.Expiration.Day;
            //        lic.Month = kv.Expiration.Month;
            //        lic.Year = kv.Expiration.Year;
            //    }*/
            //    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Robim");
            //    if (Directory.Exists(folder))
            //    {
            //        km.SaveSuretyFile(string.Format(@"{0}\Key.lic", folder), lic);
            //        MessageBox.Show("Successfully Registered!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    }
            //    else
            //    {
            //        MessageBox.Show($" Folder '{folder}' not found");
            //    }
            //    this.Close();
            //    KeyValuesClass kv = new KeyValuesClass();
            //    if (km.DisassembleKey(productKey, ref kv))
            //    {
                    
            //    }
            //}
            /*else
            {
                MessageBox.Show("Sorry, your product key is invalid.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }*/
        }
    }
}
