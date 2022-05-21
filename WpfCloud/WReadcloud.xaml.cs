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

namespace WpfCloud
{
    /// <summary>
    /// WReadcloud.xaml 的交互逻辑
    /// </summary>
    public partial class WReadcloud : Window
    {
        public Parameters Parameters { get; private set; }
        public WReadcloud(Parameters parameters)
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
            Rec_Brush.Fill = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));
        }
    }
}

