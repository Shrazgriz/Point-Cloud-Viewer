using System.Windows;
using System.Windows.Media;

namespace CloudHelix
{
    /// <summary>
    /// WReadCloud.xaml 的交互逻辑
    /// </summary>
    public partial class WReadCloud : Window
    {
        public Parameters Parameters { get; private set; }

        public WReadCloud(Parameters parameters)
        {
            InitializeComponent();
            Parameters = parameters;
            DataContext = Parameters;
        }

        private void BN_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BN_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BN_Color_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                ShowHelp = true,
                Color = System.Drawing.Color.Black
            };
            cd.ShowDialog();
            Parameters.PointBrush = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));
            //Rec_Brush.Fill = Parameters.PointBrush;
            Rec_Brush.Fill = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));
        }
    }
}
