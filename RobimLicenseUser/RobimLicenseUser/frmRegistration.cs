using FoxLearn.License;
using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace RobimLicenseUser
{
    public partial class frmRegistration : Form
    {
        public frmRegistration()
        {
            InitializeComponent();
        }
        const int ProductCode = 1;

        private void btnOK_Click(object sender, EventArgs e)
        {
            KeyManager km = new KeyManager(txtProductID.Text);
            string productKey = txtProductKey.Text;
            if(km.ValidKey(ref productKey))
            {
                KeyValuesClass kv = new KeyValuesClass();
                if(km.DisassembleKey(productKey, ref kv))
                {
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
                    if (Directory.Exists(folder))
                    {
                        km.SaveSuretyFile(string.Format(@"{0}\Key.lic", folder), lic);
                        MessageBox.Show("Successfully Registered!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($" Folder '{folder}' not found");
                    }
                    /*XmlDocument xmlDoc = new XmlDocument();
                    XmlNode LicenseNode = xmlDoc.CreateElement("License");
                    xmlDoc.AppendChild(LicenseNode);

                    XmlNode ImformationNode = xmlDoc.CreateElement("Imformation");
                    //XmlAttribute attribute1 = xmlDoc.CreateAttribute("username");
                    //attribute1.Value = "RP_Robots";
                    XmlAttribute attribute2 = xmlDoc.CreateAttribute("product_id");
                    attribute2.Value = ComputerInfo.GetComputerId();
                    XmlAttribute attribute3 = xmlDoc.CreateAttribute("license_type");
                    attribute3.Value = kv.Type.ToString();
                    XmlAttribute attribute4 = xmlDoc.CreateAttribute("product_key");
                    attribute4.Value = lic.ProductKey;
                    //ImformationNode.Attributes.Append(attribute1);
                    ImformationNode.Attributes.Append(attribute2);
                    ImformationNode.Attributes.Append(attribute3);
                    ImformationNode.Attributes.Append(attribute4);
                    LicenseNode.AppendChild(ImformationNode);
                    if (Directory.Exists(folder))
                    {
                        xmlDoc.Save(folder + "\\License.xml");
                        MessageBox.Show("Successfully Registered!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($" Folder '{folder}' not found");
                    }*/
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Sorry, your product key is invalid.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void frmRegistration_Load(object sender, EventArgs e)
        {
            txtProductID.Text = ComputerInfo.GetComputerId();
        }
    }
}
