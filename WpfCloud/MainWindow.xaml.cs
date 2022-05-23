using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using AnyCAD.Platform;
using AnyCAD.Presentation;
using MVUnity;
using MVUnity.PointCloud;
using MVUnity.Geometry3D;
using System.Linq;

namespace WpfCloud
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private RenderWindow3d renderView;
        private Parameters parameters;
        private Cloud3D cloud;
        bool m_PickPoint = true;
        double Ransac_tolerance = 5;
        double Filter_tolerance = 0.6;
        Color[] colors = new Color[6] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple, Color.Aqua };
        //private GeneralParas paras;
        public MainWindow()
        {
            InitializeComponent();
            renderView = new RenderWindow3d();
            renderView.Dock = DockStyle.Fill;
            renderView.MouseClick += new MouseEventHandler(OnRenderWindow_MouseClick);
            parameters = new Parameters();
        }

        private void WriteLine(string content)
        {
            string line = content + "\r\n";
            System.Diagnostics.Debug.WriteLine(line);
        }
        private Vector3 ConventTo(V3 vect)
        {
            return new Vector3(vect.X, vect.Y, vect.Z);
        }

        private void WindowsFormsHost_Loaded(object sender, RoutedEventArgs e)
        {
            renderView.SetBounds(0, 0, canvas.Width, canvas.Height);

            canvas.Controls.Add(renderView);
            renderView.SetBackgroundColor(ColorValue.WHITE, ColorValue.WHITE, ColorValue.WHITE);
        }

        private void Wfhost_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void MenuReadCloud_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                WReadcloud readcloud = new WReadcloud(parameters);
                if (readcloud.ShowDialog() != true) return;
                
                MVUnity.Exchange.CloudReader filereader = new MVUnity.Exchange.CloudReader
                {
                    Scale = readcloud.Parameters.Cloudscale,
                    FileName = open.FileName,
                    Format = readcloud.Parameters.Cloudformat
                };

                List<float> pointBuffer = new List<float>();
                List<float> colorBuffer = new List<float>();
                if (filereader.Format.Contains('r') & filereader.Format.Contains('c'))
                {
                    #region 有序点云         
                    List<List<ScanRow>> cloud = filereader.ReadMultipleCloudOpton(parameters.RowSkip, parameters.VertexSkip);
                    int colorID = 0;
                    foreach (List<ScanRow> rows in cloud)
                    {
                        foreach (ScanRow row in rows)
                        {
                            int f = 256;
                            foreach (Vertex vertex in row.Vertices)
                            {
                                pointBuffer.Add((float)vertex.X);
                                pointBuffer.Add((float)vertex.Y);
                                pointBuffer.Add((float)vertex.Z);
                                colorBuffer.Add(colors[0].R * f / 65535f);
                                colorBuffer.Add(colors[0].G * f / 65535f);
                                colorBuffer.Add(colors[0].B * f / 65535f);
                                f--;
                                if (f == 1) f = 256;
                            }
                            colorID++;
                            if (colorID >= colors.Length) colorID -= colors.Length;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 无序点云
                    Point3D[] pts = filereader.ReadCloud(parameters.VertexSkip);
                    System.Windows.Media.Color color = parameters.PointBrush.Color;
                    foreach (Point3D pt in pts)
                    {
                        pointBuffer.Add((float)pt.X);
                        pointBuffer.Add((float)pt.Y);
                        pointBuffer.Add((float)pt.Z);
                        colorBuffer.Add(color.R);
                        colorBuffer.Add(color.G);
                        colorBuffer.Add(color.B);
                    }
                    #endregion
                }
                PointCloudNode pcn = new PointCloudNode();
                PointStyle ps = new PointStyle();
                ps.SetMarker("rect");// circle, rect
                ps.SetPointSize(4);
                pcn.SetPointStyle(ps);
                pcn.SetPoints(pointBuffer.ToArray());
                pcn.SetColors(colorBuffer.ToArray());
                V3 target = 0.5 * (filereader.Min + filereader.Max);
                V3 came = target + V3.Identity * 200;
                renderView.SceneManager.AddNode(pcn);
                renderView.View3d.SetOrbitCenter(FromV3(target));
                renderView.Renderer.LookAt(FromV3(target), FromV3(came), Vector3.UNIT_Z);                
                renderView.ShowWorkingGrid(false);
                renderView.RequestDraw();
                renderView.FitAll();
            }
        }

        private void Canvas_Resize(object sender, EventArgs e)
        {

        }

        private void MenuNormal_Click(object sender, RoutedEventArgs e)
        {

        }
        private Vector3 FromV3(V3 Value)
        {
            return new Vector3(Value.X, Value.Y, Value.Z);
        }

        private void OnRenderWindow_MouseClick(object sender, MouseEventArgs e)
        {
            if (!m_PickPoint)
                return;

            // Try the grid
            Vector3 pt = renderView.HitPointOnGrid(e.X, e.Y);
            if (pt != null)
            {
                WriteLine(string.Format("Click on X: {0}, Y:{1}", e.X, e.Y));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            renderView.SetStandardView(EnumStandardView.SV_Top);
            renderView.RequestDraw();
        }

        private void MenuExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog()
            {
                Filter = "点云文件|*.xyz"
            };
            if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filepath = saveFile.FileName;
                FileStream save = new FileStream(filepath, FileMode.Create);
                StreamWriter writer = new StreamWriter(save);

                foreach (var point in cloud.Points)
                {
                    if (point.Value.Z == -1) continue;
                    writer.WriteLine(string.Format("{0} {1} {2} {3} {4} {5}", point.Value.X, point.Value.Y, point.Value.Z, cloud.Colors[point.Key].R, cloud.Colors[point.Key].G, cloud.Colors[point.Key].B));
                }

                writer.Close();
                save.Close();
            }
        }        
    }
}
