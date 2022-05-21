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
            TB_output.Text += line;
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

                renderView.SceneManager.AddNode(pcn);
                renderView.Renderer.SetStandardView(EnumStandardView.SV_Top);
                renderView.ShowWorkingGrid(false);
                renderView.RequestDraw();
                renderView.FitAll();
            }
        }

        private void Canvas_Resize(object sender, EventArgs e)
        {

        }

        private void MenuRecoPlane_Click(object sender, RoutedEventArgs e)
        {
            renderView.ClearScene();
            Rectangle3D r3d = cloud.FitRec(Ransac_tolerance);
            WriteLine(string.Format("Fitted plane norm: " + r3d.Norm.ToString("F4")));

            Plane _firstFit = cloud.FindPlane_RANSAC(Ransac_tolerance, 0.2);
            WriteLine(string.Format("Ransac plan norm: " + _firstFit.Norm.ToString("F4")));
            V3 vert0 = _firstFit.Projection(r3d.GetVertex(0));
            V3 vert1 = _firstFit.Projection(r3d.GetVertex(1));
            V3 vert2 = _firstFit.Projection(r3d.GetVertex(2));
            TopoShape line1 = GlobalInstance.BrepTools.MakeLine(ConventTo(vert0), ConventTo(vert1));
            TopoShape line2 = GlobalInstance.BrepTools.MakeLine(ConventTo(vert1), ConventTo(vert2));
            TopoShape quad = GlobalInstance.BrepTools.Sweep(line1, line2, true);
            renderView.ShowGeometry(quad, 0);

            Dictionary<int, double> dict_id_delta = new Dictionary<int, double>();
            double sqd = 0;
            foreach (KeyValuePair<int, V3> pt in cloud.Points)
            {
                double _d = _firstFit.AbsDistance(pt.Value);
                dict_id_delta.Add(pt.Key, _d);
                sqd += _d * _d;
            }
            sqd /= cloud.Points.Count;
            double std = Math.Sqrt(sqd);

            List<float> pointBuffer = new List<float>();
            List<float> colorBuffer = new List<float>();
            foreach (KeyValuePair<int, double> pt in dict_id_delta)
            {
                pointBuffer.Add((float)cloud.Points[pt.Key].X);
                pointBuffer.Add((float)cloud.Points[pt.Key].Y);
                pointBuffer.Add((float)cloud.Points[pt.Key].Z);
                double div = dict_id_delta[pt.Key] / std;
                if (div < Filter_tolerance)
                {
                    colorBuffer.Add(0f);
                    colorBuffer.Add(0f);
                    colorBuffer.Add(1f);
                }
                else if (div < Filter_tolerance * 2f)
                {
                    colorBuffer.Add(0f);
                    colorBuffer.Add(1f);
                    colorBuffer.Add(0f);
                }
                else
                {
                    colorBuffer.Add(1f);
                    colorBuffer.Add(0f);
                    colorBuffer.Add(0f);
                }
            }
            PointCloudNode pcn = new PointCloudNode();
            PointStyle ps = new PointStyle();
            ps.SetMarker("plus");// circle, rect
            ps.SetPointSize(4);
            pcn.SetPointStyle(ps);
            pcn.SetPoints(pointBuffer.ToArray());
            pcn.SetColors(colorBuffer.ToArray());
            renderView.SceneManager.AddNode(pcn);
            renderView.RequestDraw();
            renderView.FitAll();
        }

        private void MenuNormal_Click(object sender, RoutedEventArgs e)
        {

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

        private void TB_Clear_Click(object sender, RoutedEventArgs e)
        {
            TB_output.Clear();
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

        private void MenuReadT6_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filepath = open.FileName;
                FileStream openstream = new FileStream(filepath, FileMode.Open);
                StreamReader reader = new StreamReader(openstream);
                string line = reader.ReadLine();
                bool utri = true;
                int geoID = 0;
                while (line != "" && line != null)
                {
                    string[] nodetext = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    List<Vector3> c = new List<Vector3>();
                    foreach (string node in nodetext)
                    {
                        string[] context = node.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        Vector3 coord = new Vector3(double.Parse(context[0]), double.Parse(context[1]), double.Parse(context[2]));
                        c.Add(coord);
                    }

                    if (utri)
                    {
                        TopoShape poly1 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[0], c[1], c[3] });
                        TopoShape poly2 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[1], c[2], c[4] });
                        TopoShape poly3 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[1], c[3], c[4] });
                        TopoShape poly4 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[3], c[4], c[5] });
                        renderView.ShowGeometry(poly1, geoID++);
                        renderView.ShowGeometry(poly2, geoID++);
                        renderView.ShowGeometry(poly3, geoID++);
                        renderView.ShowGeometry(poly4, geoID++);
                    }
                    else
                    {
                        TopoShape poly1 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[0], c[1], c[2] });
                        TopoShape poly2 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[1], c[2], c[4] });
                        TopoShape poly3 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[1], c[3], c[4] });
                        TopoShape poly4 = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { c[2], c[4], c[5] });
                        renderView.ShowGeometry(poly1, geoID++);
                        renderView.ShowGeometry(poly2, geoID++);
                        renderView.ShowGeometry(poly3, geoID++);
                        renderView.ShowGeometry(poly4, geoID++);
                    }
                    utri = !utri;
                    line = reader.ReadLine();
                }
                renderView.RequestDraw();
                renderView.FitAll();
            }
        }

        private void MenuT6ApexCoff_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filepath = open.FileName;
                FileStream openstream = new FileStream(filepath, FileMode.Open);
                StreamReader reader = new StreamReader(openstream);
                string line = reader.ReadLine();
                int geoID = 0;
                bool utri = true;
                while (line != "" && line != null)
                {
                    string[] cofftext = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    double[] coff = new double[cofftext.Length];
                    for (int i = 0; i < cofftext.Length; i++)
                    {
                        coff[i] = double.Parse(cofftext[i]);
                    }
                    line = reader.ReadLine();
                    string[] nodetext = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    line = reader.ReadLine();

                    List<V3> c = new List<V3>();
                    foreach (string node in nodetext)
                    {
                        string[] context = node.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        V3 coord = new V3(double.Parse(context[0]), double.Parse(context[1]), double.Parse(context[2]));
                        c.Add(coord);
                    }
                    V3 e1, e2;
                    if (utri)
                    {
                        e1 = c[2] - c[0];
                        e2 = c[5] - c[2];
                    }
                    else
                    {
                        e1 = c[3] - c[0];
                        e2 = c[5] - c[3];
                    }
                    utri = !utri;
                    List<List<Vector3>> rows = new List<List<Vector3>>();
                    for (int i = 0; i < parameters.Tesslation + 2; i++)
                    {
                        List<Vector3> row = new List<Vector3>();
                        for (int j = 0; j < i + 1; j++)
                        {
                            V3 val = c[0] + e1 * (i / (double)(parameters.Tesslation + 1)) + e2 * (j / (double)(parameters.Tesslation + 1));
                            //xy.Add(new V2(val.X, val.Y));
                            double z = OneRegionofDI.F(val.X, val.Y, coff);
                            row.Add(new Vector3(val.X, val.Y, z));
                        }
                        rows.Add(row);
                    }
                    for (int i = 0; i < parameters.Tesslation+1; i++)
                    {
                        TopoShape RowPoly1st = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { rows[i][0], rows[i + 1][0], rows[i + 1][1] });
                        renderView.ShowGeometry(RowPoly1st, geoID++);
                        for (int j = 0; j < i; j++)
                        {
                            TopoShape Utri = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { rows[i][j], rows[i][j + 1], rows[i + 1][j + 1] });
                            TopoShape Dtri = GlobalInstance.BrepTools.MakePolygon(new List<Vector3>() { rows[i][j + 1], rows[i + 1][j + 2], rows[i + 1][j + 2] });
                            renderView.ShowGeometry(Utri, geoID++);
                            renderView.ShowGeometry(Dtri, geoID++);
                        }
                    }
                }
            }
        }
    }
}
