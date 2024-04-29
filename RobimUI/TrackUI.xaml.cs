using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Robim.RobimUI
{
    /// <summary>
    /// Interaction logic for TrackUI.xaml
    /// </summary>
    public partial class TrackUI : Window
    {
        public TrackUI()
        {
            InitializeComponent();
        }
        public void CustomAnchors_Load(object sender, EventArgs e)
        {

        }

        private void Control_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        public TextBox trackLengthInput
        {
            get { return TLbox; }
        }

        public TextBox robotDomainInput
        {
            get { return RDbox; }
        }
        public TextBox zeroPosInput
        {
            get { return ZPbox; }
        }

        public Button Closebtn
        {
            get { return this.CloseButton; }
        }

        public Button Savebtn
        {
            get { return this.SaveButton; }
        }
    }
}
