using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MVUnity;
using MVUnity.PointCloud;
using AnyCAD.Foundation;
using RapidWPF.Graphics;

namespace RapidWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const int fontSize = 2;
        private Parameters parameters;
        public MainWindow()
        {
            InitializeComponent();
            parameters = new Parameters();
        }

        private void mRenderCtrl_ViewerReady()
        {
        }

        private void BN_ReadCloud_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                parameters.CloudFilePath = open.FileName;
                mRenderCtrl.ClearScene();
                WReadCloud readcloud = new WReadCloud(parameters);
                if (readcloud.ShowDialog() != true) return;
                parameters = readcloud.Parameters;
                MVUnity.Exchange.CloudReader filereader = new MVUnity.Exchange.CloudReader
                {
                    Scale = parameters.Cloudscale,
                    FileName = open.FileName,
                    Format = parameters.Cloudformat,
                    RowSkip= parameters.RowSkip,
                    VertSkip = parameters.VertexSkip
                };
                Graphic_Cloud cloud = new Graphic_Cloud(filereader,parameters);
                cloud.Run(mRenderCtrl);
                cloud.DrawBoundingBox(mRenderCtrl, fontSize);
            }
        }

        private void BN_ReadModel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_AnaPlane_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_Sew_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_Trim_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_PointsOn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_PointOff_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_Export_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
