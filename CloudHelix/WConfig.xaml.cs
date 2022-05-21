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

namespace CloudHelix
{
    /// <summary>
    /// WConfig.xaml 的交互逻辑
    /// </summary>
    public partial class WConfig : Window
    {
        public Parameters Parameters { get; private set; }
        public CloudViewer cloudV { get; private set; }
        public WConfig(Parameters parameters, CloudViewer cd)
        {
            InitializeComponent();
            Parameters = parameters;
            cloudV = cd;
            CardCD.DataContext = cloudV;
            CardGeo.DataContext = Parameters;
            CardRec.DataContext = Parameters;
        }

        private void BN_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BN_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
