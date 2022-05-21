using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using w3d = System.Windows.Media.Media3D;
using MVUnity;
using MVUnity.Exchange;
using MVUnity.PointCloud;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;

namespace CloudHelix
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Parameters parameters;
        private const string OpenModelFilter = "3D model files (*.3ds;*.obj;*.lwo;*.stl;*.ply;)|*.3ds;*.obj;*.objz;*.lwo;*.stl;*.ply;";
        private CloudReader filereader;

        public MainWindow()
        {
            InitializeComponent();
            filereader = new CloudReader();
            parameters = new Parameters();
        }

        private void BN_ReadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == true)
            {
                parameters.CloudFilePath = open.FileName;
                WReadCloud readcloud = new WReadCloud(parameters);
                if (readcloud.ShowDialog() != true) return;
                filereader.Scale = parameters.Cloudscale;
                filereader.FileName = parameters.CloudFilePath;
                filereader.Format = parameters.Cloudformat;

                List<w3d.Point3D> pointBuffer = new List<w3d.Point3D>();

                if (filereader.Format.Contains('r') & filereader.Format.Contains('c'))
                {
                    #region 有序点云         
                    List<List<ScanRow>> cloud = filereader.ReadMultipleCloudOpton(parameters.RowSkip, parameters.VertexSkip);
                    foreach (List<ScanRow> rows in cloud)
                    {
                        foreach (ScanRow row in rows)
                        {
                            pointBuffer.AddRange(from Vertex vert in row.Vertices
                                                 select new w3d.Point3D(vert.X, vert.Y, vert.Z));
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 无序点云
                    MVUnity.Point3D[] pts = filereader.ReadCloud(parameters.VertexSkip);
                    foreach (var pt in pts)
                    {
                        pointBuffer.Add(new w3d.Point3D(pt.X, pt.Y, pt.Z));
                    }
                    #endregion
                }
                cd.Points = pointBuffer.ToArray();
                cd.PointColor = parameters.PointBrush.Color;
                cd.PointSize = parameters.PointSize;
                cd.Update();
                V3 c = 0.5 * (filereader.Max + filereader.Min);
                w3d.Point3D center = new w3d.Point3D(c.X, c.Y, c.Z);
                x3d.Camera.LookAt(center, 2f);
            }
        }

        private void BN_ReadModel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog() { Filter = OpenModelFilter, Multiselect = false };
            if (open.ShowDialog() == true)
            {
                ModelImporter reader = new ModelImporter();
                var mg = reader.Load(open.FileName);

                x3d.Children.Add(new ModelVisual3D() { Content = mg });
            }
        }

        private void BN_Config_Click(object sender, RoutedEventArgs e)
        {
            WConfig config = new WConfig(parameters, cd);
            if (config.ShowDialog() == true)
            {
                parameters = config.Parameters;
                cd.Update();
            }
        }
    }
}
