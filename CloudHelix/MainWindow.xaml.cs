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
        private Parameters parameters;
        private const string OpenModelFilter = "3D model files (*.3ds;*.obj;*.lwo;*.stl;*.ply;)|*.3ds;*.obj;*.objz;*.lwo;*.stl;*.ply;";
        private readonly CloudReader filereader;

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
                if (readcloud.ShowDialog() != true)
                {
                    return;
                }
                filereader.Scale = V3.Identity;
                filereader.FileName = parameters.CloudFilePath;
                filereader.Format = parameters.Cloudformat;
                cd.Scale = parameters.Scale;

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
                    pointBuffer.AddRange(from MVUnity.Point3D pt in pts
                                         select new w3d.Point3D(pt.X, pt.Y, pt.Z));
                    #endregion
                }
                cd.Points = pointBuffer.ToArray();
                cd.PointColor = parameters.PointBrush.Color;
                cd.PointSize = parameters.PointSize;
                cd.Update();
                Vector3D c = 0.5 * (cd.Ceiling + cd.Floor);
                double dis = c.Length;
                w3d.Point3D center = new w3d.Point3D(c.X * cd.Scale.X, c.Y * cd.Scale.Y, c.Z * cd.Scale.Z);
                x3d.Camera.LookAt(center, 0.5f * dis, 2f);
            }
        }

        private void BN_ReadModel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog() { Filter = OpenModelFilter, Multiselect = false };
            if (open.ShowDialog() == true)
            {
                WReadCloud readcloud = new WReadCloud(parameters);
                if (readcloud.ShowDialog() != true)
                {
                    return;
                }
                Material mat = new DiffuseMaterial(parameters.PointBrush);
                ModelImporter reader = new ModelImporter();
                Model3DGroup mg = reader.Load(open.FileName);
                foreach (Model3D child in mg.Children)
                {
                    if (child is GeometryModel3D g3d)
                    {
                        g3d.Material = mat;
                        g3d.BackMaterial = mat;
                    }
                }
                ScaleTransform3D sca = new ScaleTransform3D(parameters.Scale);
                x3d.Children.Add(new ModelVisual3D() { Content = mg, Transform = sca });
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

        private void BN_Clear_Click(object sender, RoutedEventArgs e)
        {
            cd.Points = new w3d.Point3D[0];
        }

        private void BN_SetView_Click(object sender, RoutedEventArgs e)
        {            
            CameraHelper.ChangeDirection(x3d.Camera, new Vector3D(1f, 1f, -1f), new Vector3D(0, 0, 1f), 2f);
        }
    }
}
