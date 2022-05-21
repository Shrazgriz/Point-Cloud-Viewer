using System.Windows;

namespace CloudHelix
{
    /// <summary>
    /// WConfig.xaml 的交互逻辑
    /// </summary>
    public partial class WConfig : Window
    {
        public Parameters Parameters { get; private set; }
        public CloudViewer CloudV { get; private set; }
        public WConfig(Parameters parameters, CloudViewer cd)
        {
            InitializeComponent();
            Parameters = parameters;
            CloudV = cd;
            CardCD.DataContext = CloudV;
            CardRec.DataContext = CloudV;
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
