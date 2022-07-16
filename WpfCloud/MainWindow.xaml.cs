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
using System.Configuration;
using System.Collections.ObjectModel;

namespace WpfCloud
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const int BoundBoxID = 2;
        const int PointCloudID = 1;
        const int EntityID = 100;
        private const string OpenModelFilter = "3D model files (*.3ds;*.obj;*.lwo;*.stl;*.ply;)|*.3ds;*.obj;*.objz;*.lwo;*.stl;*.ply;";
        private RenderWindow3d renderView;
        private Parameters parameters;
        private Cloud3D cloud;
        private PointCloudNode CloudNode;
        bool mousPick = false;

        Color[] colors = new Color[6] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple, Color.Aqua };
        private ObservableCollection<NodeInfo> UserSelection;
        public MainWindow()
        {
            InitializeComponent();
            renderView = new RenderWindow3d();
            renderView.Dock = DockStyle.Fill;
            renderView.MouseClick += new MouseEventHandler(OnRenderWindow_MouseClick);
            parameters = new Parameters();
            GlobalInstance.EventListener.OnSelectElementEvent += OnSelectElement;
            UserSelection = new ObservableCollection<NodeInfo>();
            LV_Selection.ItemsSource = UserSelection;
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

        private void OnSelectElement(SelectionChangeArgs args)
        {
            if (!args.IsHighlightMode())
            {
                SelectedShapeQuery query = new SelectedShapeQuery();
                renderView.QuerySelection(query);
                var shape = query.GetGeometry();
                if (shape != null)
                {
                    GeomCurve curve = new GeomCurve();
                    if (curve.Initialize(shape))
                    {
                        TopoShapeProperty property = new TopoShapeProperty();
                        property.SetShape(shape);
                        Console.WriteLine("Edge Length {0}", property.EdgeLength());
                    }
                }
            }
        }

        private void MenuReadCloud_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                parameters.CloudFilePath = open.FileName;
                renderView.SceneManager.ClearNodes();
                WReadCloud readcloud = new WReadCloud(parameters);
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
                    //int colorID = 0;
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
                                colorBuffer.Add(parameters.PointColor.R * f / 65535f);
                                colorBuffer.Add(parameters.PointColor.G * f / 65535f);
                                colorBuffer.Add(parameters.PointColor.B * f / 65535f);
                                f--;
                                if (f == 127) f = 256;
                            }
                            //colorID++;
                            //if (colorID >= colors.Length) colorID -= colors.Length;
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
                CloudNode = new PointCloudNode();
                CloudNode.SetId(new ElementId(PointCloudID));
                CloudNode.SetPickable(false);
                PointStyle ps = new PointStyle();
                ps.SetMarker("rect");// circle, rect
                ps.SetPointSize(4);
                CloudNode.SetPointStyle(ps);
                CloudNode.SetPoints(pointBuffer.ToArray());
                CloudNode.SetColors(colorBuffer.ToArray());
                #region
                GroupSceneNode bbox = DrawBoundBox(V3toVect(filereader.Min), V3toVect(filereader.Max), parameters, 12);
                renderView.SceneManager.AddNode(bbox);
                #endregion
                V3 target = 0.5 * (filereader.Min + filereader.Max);
                V3 came = target + V3.Identity * 200;
                renderView.SceneManager.AddNode(CloudNode);
                renderView.View3d.SetOrbitCenter(FromV3(target));
                renderView.Renderer.LookAt(FromV3(target), FromV3(came), Vector3.UNIT_Z);                
                //renderView.ShowWorkingGrid(false);
                renderView.RequestDraw();
                renderView.FitAll();
            }
        }

        private Vector3 V3toVect(V3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }
        private GroupSceneNode DrawBoundBox(Vector3 MinPt, Vector3 MaxPt, Parameters parameters, int FontSize)
        {
            int IntervalX = parameters.IntervalX;
            int IntervalY = parameters.IntervalY;
            int IntervalZ = parameters.IntervalZ;
            GroupSceneNode plotModel = new GroupSceneNode();
            plotModel.SetId(new ElementId(BoundBoxID));
            double minX = Math.Ceiling(MinPt.X / IntervalX) * IntervalX;
            double minY = Math.Ceiling(MinPt.Y / IntervalY) * IntervalY;
            double minZ = Math.Ceiling(MinPt.Z / IntervalZ) * IntervalZ;
            for (double x = minX; x <= MaxPt.X; x += IntervalX)
            {
                TextNode label = new TextNode();
                label.SetText(x.ToString("F0"));
                label.SetPickable(false);
                label.SetTextColor(ColorValue.BLACK);
                label.SetPosition(new Vector3(x , MinPt.Y  - (FontSize * 2.5), MinPt.Z ));
                //TextCreator.CreateTextLabelModel3D(x.ToString("F0"), Brushes.Black, true, FontSize,
                //                                                           new Point3D(x * Scale.X, Floor.Y * Scale.Y - (FontSize * 2.5), Floor.Z * Scale.Z),
                //                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.AddNode(label);
            }

            {
                TextNode label = new TextNode();
                label.SetText("X(mm)");
                label.SetPickable(false);
                label.SetTextColor(ColorValue.BLACK);
                label.SetPosition(new Vector3((MinPt.X + MaxPt.X) *  0.5, MinPt.Y - (FontSize * 6), MinPt.Z ));
                //GeometryModel3D label = TextCreator.CreateTextLabelModel3D("X (mm)", Brushes.Black, true, FontSize,
                //                                                           new Point3D((Floor.X + Ceiling.X) * 0.5 * Scale.X, Floor.Y * Scale.Y - (FontSize * 6), Floor.Z * Scale.Z),
                //                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.AddNode(label);
            }

            for (double y = minY; y <= MaxPt.Y; y += IntervalY)
            {
                TextNode label = new TextNode();
                label.SetText(y.ToString("F0"));
                label.SetPickable(false);
                label.SetTextColor(ColorValue.BLACK);
                label.SetPosition(new Vector3(MinPt.X  - FontSize * 3, y , MinPt.Z ));
                //GeometryModel3D label = TextCreator.CreateTextLabelModel3D(y.ToString("F0"), Brushes.Black, true, FontSize,
                //                                                           new Point3D(Floor.X * Scale.X - (FontSize * 3), y * Scale.Y, Floor.Z * Scale.Z),
                //                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.AddNode(label);
            }
            {
                TextNode label = new TextNode();
                label.SetText("Y(mm)");
                label.SetPickable(false);
                label.SetTextColor(ColorValue.BLACK);
                label.SetPosition(new Vector3(MinPt.X  - FontSize * 6, (MinPt.Y + MaxPt.Y) * 0.5, MinPt.Z ));
                //GeometryModel3D label = TextCreator.CreateTextLabelModel3D("Y (mm)", Brushes.Black, true, FontSize,
                //                                                           new Point3D(Floor.X * Scale.X - (FontSize * 10), (Floor.Y + Ceiling.Y) * 0.5 * Scale.Y, Floor.Z * Scale.Z),
                //                                                           new Vector3D(0, 1, 0), new Vector3D(-1, 0, 0));
                plotModel.AddNode(label);
            }
            for (double z = minZ; z <= MaxPt.Z + double.Epsilon; z += IntervalZ)
            {
                TextNode label = new TextNode();
                label.SetText(z.ToString("F0"));
                label.SetPickable(false);
                label.SetTextColor(ColorValue.BLACK);
                label.SetPosition(new Vector3(MinPt.X  - FontSize * 3, MaxPt.Y , z ));
                //GeometryModel3D label = TextCreator.CreateTextLabelModel3D(z.ToString("F0"), Brushes.Black, true, FontSize,
                //                                                           new Point3D(Floor.X * Scale.X - (FontSize * 3), Ceiling.Y * Scale.Y, z * Scale.Z),
                //                                                           new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
                plotModel.AddNode(label);
            }
            {
                TextNode label = new TextNode();
                label.SetText("Z(mm)");
                label.SetPickable(false);
                label.SetTextColor(ColorValue.BLACK);
                label.SetPosition(new Vector3(MinPt.X  - FontSize * 6, MinPt.Y , (MinPt.Z + MaxPt.Z)  * 0.5));
                //GeometryModel3D label = TextCreator.CreateTextLabelModel3D("Z (mm)", Brushes.Black, true, FontSize,
                //                                                           new Point3D(Floor.X * Scale.X - (FontSize * 10), Ceiling.Y * Scale.Y, (Floor.Z + Ceiling.Z) * 0.5 * Scale.Z),
                //                                                           new Vector3D(0, 0, 1), new Vector3D(1, 0, 0));
                plotModel.AddNode(label);
            }

            LineStyle lineStyle = new LineStyle();
            lineStyle.SetColor(new ColorValue(0f, 0f, 0f));
            lineStyle.SetLineWidth(2);
            lineStyle.SetPatternStyle((int)EnumLinePattern.LP_None);
            {
                //Z
                LineNode lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MinPt.Y, MinPt.Z), new Vector3(MinPt.X, MinPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MaxPt.X, MinPt.Y, MinPt.Z), new Vector3(MaxPt.X, MinPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MaxPt.X, MaxPt.Y, MinPt.Z), new Vector3(MaxPt.X, MaxPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MaxPt.Y, MinPt.Z), new Vector3(MinPt.X, MaxPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
            }
            {
                //Y
                LineNode lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MinPt.Y, MinPt.Z), new Vector3(MinPt.X, MaxPt.Y, MinPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MaxPt.X, MinPt.Y, MinPt.Z), new Vector3(MaxPt.X, MaxPt.Y, MinPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MaxPt.X, MinPt.Y, MaxPt.Z), new Vector3(MaxPt.X, MaxPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MinPt.Y, MaxPt.Z), new Vector3(MinPt.X, MaxPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
            }
            {
                //X
                LineNode lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MinPt.Y, MinPt.Z), new Vector3(MaxPt.X, MinPt.Y, MinPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MaxPt.Y, MinPt.Z), new Vector3(MaxPt.X, MaxPt.Y, MinPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MinPt.Y, MaxPt.Z), new Vector3(MaxPt.X, MinPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
                lineNode = new LineNode();
                lineNode.Set(new Vector3(MinPt.X, MaxPt.Y, MaxPt.Z), new Vector3(MaxPt.X, MaxPt.Y, MaxPt.Z));
                lineNode.SetLineStyle(lineStyle);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
            }
            plotModel.SetName("BoundingBox");
            //Rect3D bb = new Rect3D(Floor.X * Scale.X, Floor.Y * Scale.Y, Floor.Z * Scale.Z,
            //    (Ceiling.X - Floor.X) * Scale.X, (Ceiling.Y - Floor.Y) * Scale.Y, (Ceiling.Z - Floor.Z) * Scale.Z);
            //axesMeshBuilder.AddBoundingBox(bb, LineThickness);
            //GeometryModel3D axesModel = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Black);
            //axesModel.SetName(AxisName);
            return plotModel;
        }

        private void Canvas_Resize(object sender, EventArgs e)
        {

        }

        private Vector3 FromV3(V3 Value)
        {
            return new Vector3(Value.X, Value.Y, Value.Z);
        }

        private void OnRenderWindow_MouseClick(object sender, MouseEventArgs e)
        {
            if (mousPick)
            {
                PickHelper pickHelper = renderView.PickShape(e.X, e.Y);
                if (pickHelper != null)
                {
                    SceneNode pNode = pickHelper.GetSceneNode();
                    TopoShape pGeo = pickHelper.GetGeometry();
                    NodeInfo info = new NodeInfo(pNode);
                    info.Name = pGeo.ToString();
                    UserSelection.Add(info);
                }
            }
            else
            {
                PickHelper pickHelper = renderView.PickShape(e.X, e.Y);
                if (pickHelper == null)
                {
                    UserSelection.Clear();
                }
            }
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

        private void BN_ReadModel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog() { Filter = OpenModelFilter, Multiselect = false };
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                WReadCloud readcloud = new WReadCloud(parameters);
                if (readcloud.ShowDialog() != true)
                {
                    return;
                }
                //TODO
                                
            }
        }

        private void BN_AnalyzePlane_Click(object sender, RoutedEventArgs e)
        {
            if (parameters.CloudFilePath != null)
            {
                #region 区分异源点云
                MVUnity.Exchange.CloudReader filereader = new MVUnity.Exchange.CloudReader
                {
                    Scale = parameters.Cloudscale,
                    FileName = parameters.CloudFilePath,
                    Format = parameters.Cloudformat
                };
                List<List<ScanRow>> diffRows = filereader.ReadMultipleCloudOpton(parameters.RowSkip, parameters.VertexSkip);

                #endregion
                #region 编制扫描线
                List<CompiledRegion> regions = new List<CompiledRegion>();
                List<CompiledRegion> regBuff = new List<CompiledRegion>();
                foreach (List<ScanRow> rowlist in diffRows)
                {
                    int id1 = 0;
                    int id2 = 0;
                    List<ScanRow> raw2 = new List<ScanRow>();
                    List<ScanRow> raw1 = new List<ScanRow>();

                    for (int i = 1; i < rowlist.Count; i++)
                    {
                        ScanRow[] output = ScanRow.SplitOnPerspective(rowlist[i], 2, 12);
                        //此处node 需要重新编号
                        raw1.Add(output[0].ReNumber(id1));
                        raw2.Add(output[1].ReNumber(id2));
                        id1 += output[0].Count;
                        id2 += output[1].Count;
                    }
                    #region 编制断点前部分
                    int cellid1 = 0;
                    List<CompiledRegion> reg1 = new List<CompiledRegion>();
                    CellGridRow LastRow = CellGridRow.CompileT3_Delaunay(raw1[0], raw1[1], 0, cellid1);
                    CellRLRow clustedRow = CellRLRow.Clustering_SimilarNorm(LastRow);
                    cellid1 += LastRow.Count;
                    foreach (CellRunLength rl in clustedRow.GetAllCellRL())
                    {
                        reg1.Add(CompiledRegion.CompiledFrom(rl));
                    }
                    for (int i = 1; i < raw1.Count - 1; i++)
                    {
                        CellGridRow CurrentRow = CellGridRow.CompileT3_Delaunay(raw1[i], raw1[i + 1], i, cellid1);
                        LastRow.Connect(CurrentRow);
                        clustedRow = CellRLRow.Clustering_SimilarNorm(CurrentRow);
                        cellid1 += CurrentRow.Count;
                        LastRow = CurrentRow;
                        List<CellRunLength> clusters = clustedRow.GetAllCellRL();
                        foreach (CellRunLength similarCells in clusters)
                        {
                            bool find = false;
                            foreach (CompiledRegion reg in reg1.FindAll(r => r.State == RegionState.Open))
                            {
                                if (reg.Append(similarCells))
                                {
                                    find = true;
                                    break;
                                }
                            }
                            if (!find)
                            {
                                regBuff.Add(CompiledRegion.CompiledFrom(similarCells));
                            }
                        }
                        foreach (CompiledRegion reg in reg1.FindAll(r => r.State == RegionState.Open))
                        {
                            reg.MoveToNext();
                        }
                        reg1.AddRange(regBuff);
                        regBuff = new List<CompiledRegion>();
                    }
                    regions.AddRange(reg1);
                    #endregion
                    #region 编制断点后部分
                    int cellid2 = 0;
                    List<CompiledRegion> reg2 = new List<CompiledRegion>();
                    LastRow = CellGridRow.CompileT3_Delaunay(raw2[0], raw2[1], 0, cellid2);
                    clustedRow = CellRLRow.Clustering_SimilarNorm(LastRow);
                    cellid2 += LastRow.Count;
                    foreach (CellRunLength rl in clustedRow.GetAllCellRL())
                    {
                        reg2.Add(CompiledRegion.CompiledFrom(rl));
                    }
                    for (int i = 1; i < raw2.Count - 1; i++)
                    {
                        CellGridRow CurrentRow = CellGridRow.CompileT3_Delaunay(raw2[i], raw2[i + 1], i, cellid2);
                        LastRow.Connect(CurrentRow);
                        clustedRow = CellRLRow.Clustering_SimilarNorm(CurrentRow);
                        cellid2 += CurrentRow.Count;
                        LastRow = CurrentRow;
                        List<CellRunLength> clusters = clustedRow.GetAllCellRL();
                        foreach (CellRunLength similarCells in clusters)
                        {
                            bool find = false;
                            foreach (CompiledRegion reg in reg2.FindAll(r => r.State == RegionState.Open))
                            {
                                if (reg.Append(similarCells))
                                {
                                    find = true;
                                    break;
                                }
                            }
                            if (!find)
                            {
                                regBuff.Add(CompiledRegion.CompiledFrom(similarCells));
                            }
                        }
                        foreach (CompiledRegion reg in reg2.FindAll(r => r.State == RegionState.Open))
                        {
                            reg.MoveToNext();
                        }
                        reg2.AddRange(regBuff);
                        regBuff = new List<CompiledRegion>();
                    }
                    regions.AddRange(reg2);
                    #endregion
                }
                #endregion

                #region 合并相同平面
                foreach (CompiledRegion r in regions)
                {
                    if (r.State == RegionState.Open) r.State = RegionState.Completed;
                }
                List<List<CompiledRegion>> groups = DBScan.ClusteringRegions(regions.FindAll(r => r.CellCount > CloudConstants.MinRegionCellCount).ToList());

                List<Polygon3D> rects = new List<Polygon3D>();
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].Count == 1)
                    {
                        rects.Add(groups[i].First().Boundary);
                        continue;
                    }
                    else
                    {
                        List<V3> recBounds = new List<V3>();
                        foreach (CompiledRegion reg in groups[i])
                        {
                            recBounds.AddRange(reg.Boundary.GetAllVertice());
                        }
                        Polygon3D bound = Polygon3D.BoundingPoly(recBounds);

                        double area1 = bound.GetArea();
                        double area2 = groups[i].Select(r => r.Boundary.GetArea()).Sum();
                        if (area1 * parameters.MergeRation > area2)
                        {
                            rects.AddRange(groups[i].Select(r => r.Boundary));
                        }
                        else
                        {
                            rects.Add(bound);
                        }
                    }
                }
                #endregion
                #region 平面可视化
                int id = EntityID;
                foreach (Polygon3D rect in rects)
                {
                    TopoShape face = PolygonToFace(rect);
                    RenderableGeometry entity = new RenderableGeometry();
                    entity.SetGeometry(face);
                    entity.SetId(++id);
                    EntitySceneNode node = new EntitySceneNode();
                    node.SetEntity(entity);
                    renderView.SceneManager.AddNode(node);
                    //renderView.ShowGeometry(face, ++id);
                }                                
                #endregion

                WriteLine(string.Format("Region count: {0}", regions.Count));
                WriteLine(string.Format("Filted count: {0}", regions.FindAll(r => r.CellCount > CloudConstants.MinRegionCellCount && r.EstimatedArea > CloudConstants.MinRegionArea).Count));
                WriteLine(string.Format("Clusters count: {0}", groups.Count));
            }
        }

        private TopoShape PolygonToFace( Polygon3D Poly)
        {
            List<Vector3> points = Poly.GetAllVertice().Select(v => new Vector3(v.X, v.Y, v.Z)).ToList();
            TopoShape plygon = GlobalInstance.BrepTools.MakePolygon(points);
            TopoShape face = GlobalInstance.BrepTools.MakeFace(plygon);
            return face;
        }

        private void MainForm_Closed(object sender, EventArgs e)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings["CloudFormat"].Value = parameters.Cloudformat;
            cfa.AppSettings.Settings["CloudScale"].Value = parameters.Cloudscale.ToString("F2");
            cfa.AppSettings.Settings["RhoTolerance"].Value = parameters.RhoTolerance.ToString("F2");
            cfa.AppSettings.Settings["MaxSewGap"].Value = parameters.MaxGap.ToString("F2");
            cfa.AppSettings.Settings["CloudFormat"].Value = parameters.Cloudformat;
            cfa.AppSettings.Settings["CloudScale"].Value = parameters.Cloudscale.ToString("F2");
            cfa.AppSettings.Settings["PlaneNormTolerance"].Value = parameters.PlaneNormTol.ToString("F2");
            cfa.AppSettings.Settings["PlaneRhoTolerance"].Value = parameters.PlaneRhoTol.ToString("F2");
            cfa.AppSettings.Settings["CellNormTolerance"].Value = parameters.CellNormTol.ToString("F2");
            cfa.AppSettings.Settings["CellRhoTolerance"].Value = parameters.CellRhoTol.ToString("F2");
            cfa.AppSettings.Settings["StiffResolutionX"].Value = parameters.StiffResX.ToString();
            cfa.AppSettings.Settings["StiffResolutionY"].Value = parameters.StiffResY.ToString();
            cfa.AppSettings.Settings["RowSkip"].Value = parameters.RowSkip.ToString();
            cfa.AppSettings.Settings["VertexSkip"].Value = parameters.VertexSkip.ToString();
            cfa.AppSettings.Settings["PointSize"].Value = parameters.PointSize.ToString();
            cfa.AppSettings.Settings["PointBrush"].Value = parameters.PointBrush.ToString();
            cfa.AppSettings.Settings["MinRegionArea"].Value = parameters.MinRegionArea.ToString();
            cfa.AppSettings.Settings["MinRegionCellCount"].Value = parameters.MinRegionCellCount.ToString();
            cfa.AppSettings.Settings["MergeRation"].Value = parameters.MergeRation.ToString();
            cfa.AppSettings.Settings["BoundarySearchDistance"].Value = parameters.BoundarySearchWidth.ToString();
            //cfa.AppSettings.Settings["SimplifyAreaRation"].Value = parameters.SimplifyAreaRation.ToString();
            cfa.Save();
        }

        private void BN_PointsOn_Click(object sender, RoutedEventArgs e)
        {
            CloudNode.SetVisible(true);
        }

        private void BN_PointOff_Click(object sender, RoutedEventArgs e)
        {
            CloudNode.SetVisible(false);
        }

        private void BN_Sew_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_Trim_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainForm_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.LeftCtrl:
                    mousPick = true;
                    break;
                case System.Windows.Input.Key.RightCtrl:
                    mousPick = true;
                    break;                
                default:
                    break;
            }
        }

        private void MainForm_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.LeftCtrl:
                    mousPick = false;
                    break;
                case System.Windows.Input.Key.RightCtrl:
                    mousPick = false;
                    break;
                default:
                    break;
            }
        }
    }
}
