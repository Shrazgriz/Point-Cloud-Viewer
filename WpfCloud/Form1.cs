using System;
using System.Windows.Forms;
using AnyCAD.Presentation;
using AnyCAD.Platform;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MVUnity;
using MVUnity.PointCloud;
using MVUnity.Geometry3D;
using System.Diagnostics;

namespace AnyCADTest
{
    public partial class Form1 : Form
    {
        readonly V3 origin = new V3(0, 0, 0);
        readonly V3 scale = new V3(100, 100, 100);
        private RenderWindow3d renderView;
        private Cloud3D cloud;
        public Form1()
        {
            InitializeComponent();
            renderView = new RenderWindow3d();
            renderView.SetBounds(0, 0, canvas.Width, canvas.Height);
            canvas.Controls.Add(renderView);
        }

        private void WriteLine(string content)
        {
            string line = content + "\r\n";
            TB_output.Text += line;
        }

        private void orbitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderView.ExecuteCommand("Orbit");
        }

        private void panToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderView.ExecuteCommand("Pan");
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderView.ExecuteCommand("Zoom");
        }

        private void planRecognitionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double Tolerance = double.Parse(tb_tolerance.Text);
            renderView.ClearScene();
            Rectangle3D r3d = cloud.FitRec(Tolerance);
            TopoShape line1 = GlobalInstance.BrepTools.MakeLine(ConventTo(r3d.GetVertex(0)), ConventTo(r3d.GetVertex(1)));
            TopoShape line2 = GlobalInstance.BrepTools.MakeLine(ConventTo(r3d.GetVertex(1)), ConventTo(r3d.GetVertex(2)));
            TopoShape quad = GlobalInstance.BrepTools.Sweep(line1, line2, true);
            renderView.ShowGeometry(quad, 0);
            WriteLine(string.Format("Fitted plane norm: " + r3d.Norm.ToString("F4")));

            Plane _firstFit = cloud.FindPlane_RANSAC(5, 0.3);
            WriteLine(string.Format("Ransac plan norm: " + _firstFit.Norm.ToString("F4")));
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
                if (div < Tolerance)
                {
                    colorBuffer.Add(0f);
                    colorBuffer.Add(0f);
                    colorBuffer.Add(1f);
                }
                else if (div < Tolerance*2f)
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

        private void cloudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                string filepath = open.FileName;
                CloudReader filereader = new CloudReader();
                filereader.Scale = scale;
                List<ScanRow> rows = filereader.ReadOpton(filepath, "rcxyzRGB",150);
                List<float> pointBuffer = new List<float>();
                List<float> colorBuffer = new List<float>();
                int flag = 0;
                cloud = new Cloud3D();
                //cloud = Cloud3D.CreateCloud(rows);
                foreach (var row in rows)
                {
                    foreach (Vertex vertex in row.Vertices)
                    {
                        cloud.AddPoint(vertex);
                        pointBuffer.Add((float)vertex.X);
                        pointBuffer.Add((float)vertex.Y);
                        pointBuffer.Add((float)vertex.Z);
                        switch (flag)
                        {
                            case 0:
                                colorBuffer.Add(0f);
                                colorBuffer.Add(0f);
                                colorBuffer.Add(0f);
                                break;
                            case 1:
                                colorBuffer.Add(0f);
                                colorBuffer.Add(0f);
                                colorBuffer.Add(1f);
                                break;
                            case 2:
                                colorBuffer.Add(0f);
                                colorBuffer.Add(1f);
                                colorBuffer.Add(0f);
                                break;
                            case 3:
                                colorBuffer.Add(0f);
                                colorBuffer.Add(1f);
                                colorBuffer.Add(1f);
                                break;
                            case 4:
                                colorBuffer.Add(1f);
                                colorBuffer.Add(0f);
                                colorBuffer.Add(0f);
                                break;
                            case 5:
                                colorBuffer.Add(1f);
                                colorBuffer.Add(0f);
                                colorBuffer.Add(1f);
                                break;
                            case 6:
                                colorBuffer.Add(1f);
                                colorBuffer.Add(1f);
                                colorBuffer.Add(0f);
                                break;
                            case 7:
                                colorBuffer.Add(1f);
                                colorBuffer.Add(1f);
                                colorBuffer.Add(1f);
                                break;
                            default:
                                break;
                        }                        
                    }
                    flag ++;
                    if (flag > 7) flag -= 8;
                }
                
                PointCloudNode pcn = new PointCloudNode();
                PointStyle ps = new PointStyle();
                ps.SetMarker("plus");// circle, rect
                ps.SetPointSize(4);
                pcn.SetPointStyle(ps);
                pcn.SetPoints(pointBuffer.ToArray());
                pcn.SetColors(colorBuffer.ToArray());
                Plane fittedPlane = cloud.FindPlane_RANSAC(50, 0.2);
                Box OBB = cloud.ComputeOBB();
                TopoShape boundingbox = CreateBox(OBB);

                List<V3> points8 = OBB.Get8Points();
                PointCloudNode bp = new PointCloudNode();
                PointStyle bps = new PointStyle();
                bps.SetMarker("circle");
                bps.SetPointSize(8);
                bp.SetPointStyle(bps);
                List<float> bpBuffer = new List<float>();
                List<float> bcBuffer = new List<float>();
                foreach (V3 vert in points8)
                {
                    bpBuffer.Add((float)vert.X);
                    bpBuffer.Add((float)vert.Y);
                    bpBuffer.Add((float)vert.Z);
                    bcBuffer.Add(1f);
                    bcBuffer.Add(1f);
                    bcBuffer.Add(1f);
                }
                bp.SetPoints(bpBuffer.ToArray());
                bp.SetColors(bcBuffer.ToArray());

                renderView.SceneManager.AddNode(pcn);
                renderView.SceneManager.AddNode(bp);
                renderView.RequestDraw();
                renderView.FitAll();
                WriteLine(string.Format("OBB major dir: "+OBB.MajorOrientation.ToString("F4")));
                WriteLine(string.Format("OBB secondary dir: " + OBB.SecondaryOrientation.ToString("F4")));
                WriteLine(string.Format("OBB minor dir: " + OBB.MinorOrientation.ToString("F4")));
            }
        }

        private TopoShape CreateBox(Box box)
        {
            V3 start = box.Center - box.MajorOrientation * box.Size.X;
            V3 end = box.Center+ box.MajorOrientation * box.Size.X;
            TopoShape _b = GlobalInstance.BrepTools.MakeBox(ConventTo(start), ConventTo(end), box.Size.Y, box.Size.Z);
            return _b;
        }

        private void depthMatrixToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                string filepath = open.FileName;
                FileStream _fs = new FileStream(filepath, FileMode.Open);
                StreamReader _fr = new StreamReader(_fs);
                _fr.ReadLine();
                string line = _fr.ReadLine();
                List<float> pointBuffer = new List<float>();
                List<float> colorBuffer = new List<float>();
                PointStyle ps = new PointStyle();
                ps.SetMarker("plus");
                ps.SetPointSize(4);
                string[] _split = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                double s_phi = double.Parse(_split[3]);
                double d_phi = double.Parse(_split[4]);
                double d_theta = double.Parse(_split[5]);
                _fr.ReadLine();
                _fr.ReadLine();
                line = _fr.ReadLine();
                while ((line != "") & (line != null) & (!_fr.EndOfStream))
                {
                    _split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int c = int.Parse(_split[0]);
                    int r = int.Parse(_split[1]);
                    int d = int.Parse(_split[2]);
                    double theta = (c * d_theta) * Math.PI / 180;
                    double phi = (s_phi + r * d_phi) * Math.PI / 180;
                    Vector3 localnorm = new Vector3(Math.Sin(phi) * Math.Cos(theta), Math.Sin(phi) * Math.Sin(theta), Math.Cos(phi));
                    float x = d * (float)localnorm.X;
                    float y = d * (float)localnorm.Y;
                    float z = d * (float)localnorm.Z;
                    pointBuffer.Add(x);
                    pointBuffer.Add(y);
                    pointBuffer.Add(z);
                    colorBuffer.Add((float)localnorm.X);
                    colorBuffer.Add((float)localnorm.Y);
                    colorBuffer.Add((float)localnorm.Z);
                    line = _fr.ReadLine();
                }
                PointCloudNode pcn = new PointCloudNode();
                pcn.SetPointStyle(ps);
                pcn.SetPoints(pointBuffer.ToArray());
                pcn.SetColors(colorBuffer.ToArray());
                cloud = Cloud3D.CreateCloud(pointBuffer);

                renderView.SceneManager.AddNode(pcn);
                renderView.RequestDraw();
                renderView.FitAll();
                _fr.Close();
                _fs.Close();
            }
        }

        private void pointCloudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog()
            {
                Filter = "点云文件|*.xyz"
            };
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                FileStream _mfs = new FileStream(saveFile.FileName, FileMode.Create);
                StreamWriter _saveStream = new StreamWriter(_mfs);
                _saveStream.Close();
                _mfs.Close();
            }
        }
        
        private void cubeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process proc = Process.Start("ScriptedCAD.exe", "box;40,-20,0;1,0,0;20,40,80");
        }

        private void cylinderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process proc = Process.Start("ScriptedCAD.exe", "cyd;-40,20,0;0,0,1;30,100,60");
        }

        private void xIGESToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = "IGES文件|*.iges" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                TopoDataExchangeIges reader = new TopoDataExchangeIges();
                TopoShape shape = reader.Read(new AnyCAD.Platform.Path(dialog.FileName));
            }
        }
        private Vector3 ConventTo(V3 vect)
        {
            return new Vector3(vect.X, vect.Y, vect.Z);
        }
    }
}
