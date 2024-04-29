using ConnectToDB;
using FoxLearn.License;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RobimLicenseDeveloper
{
    public partial class frmGenerate : Form
    {
        public frmGenerate()
        {
            InitializeComponent();
        }
        //需要增加使用者选择
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            KeyManager km = new KeyManager(txtProductID.Text);
            KeyValuesClass kv;
            string productKey = string.Empty;
            if(cboLicenseType.SelectedIndex == 0)
            {
                kv = new KeyValuesClass()
                {
                    Type = LicenseType.FULL,
                    Header = Convert.ToByte(9),
                    Footer = Convert.ToByte(6),
                    ProductCode = (byte)ProductCode,
                    Edition = Edition.ENTERPRISE,
                    Version = 1,
                    Expiration = DateTime.Now.Date.AddDays(Convert.ToInt32(txtExperience.Text))
                };
                if (!km.GenerateKey(kv, ref productKey))
                    txtProductKey.Text = "ERROR";
            }
            else
            {
                kv = new KeyValuesClass()
                {
                    Type = LicenseType.TRIAL,
                    Header = Convert.ToByte(9),
                    Footer = Convert.ToByte(6),
                    ProductCode = (byte)ProductCode,
                    Edition = Edition.ENTERPRISE,
                    Version = 1,
                    Expiration = DateTime.Now.Date.AddDays(Convert.ToInt32(txtExperience.Text))
                };
                if (!km.GenerateKey(kv, ref productKey))
                    txtProductKey.Text = "ERROR";
            }
            txtProductKey.Text = productKey;
            if(txtProductKey.Text != "ERROR")
            {
                string[] user = username.SelectedItem.ToString().Split('.');
                string[] pro = productsname.SelectedItem.ToString().Split('.');
                string columnvalue = "robim_user_id,product_id,license_type,experience_day,product_key,registered_day,products_id";
                string value = string.Format("'{0}','{1}','{2}','{3}','{4}','{5}','{6}'", user[0], txtProductID.Text, kv.Type.ToString(), kv.Expiration.ToString("yyyy-MM-dd HH:mm:ss"), productKey,DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss"), pro[0]);
                DBConnect.Insert("Robim", "Robim_License", columnvalue, value);
                btnGenerate.Enabled = false;
            }
        }

        const int ProductCode = 10;

        private void frmGenerate_Load(object sender, EventArgs e)
        {
            SelectUser();
            SelectProduct();
        }

        void SelectUser()
        {
            cboLicenseType.SelectedIndex = 0;
            List<string> li = DBConnect.Select("Robim", "Robim_User ORDER BY id", "id,username");
            for (int i = 0; i < li.Count; i += 2)
            {
                username.Items.Add(li[i] + ". " + li[i + 1]);
            }
            username.SelectedIndex = 0;
        }
        void SelectProduct()
        {
            cboLicenseType.SelectedIndex = 0;
            List<string> li = DBConnect.Select("Robim", "Products ORDER BY id", "id,name");
            for (int i = 0; i < li.Count; i += 2)
            {
                productsname.Items.Add(li[i] + ". " + li[i + 1]);
            }
            productsname.SelectedIndex = 0;
        }
    }
}
