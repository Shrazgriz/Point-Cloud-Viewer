﻿using System;
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
using MVUnity.Geometry3D;
using AnyCAD.Foundation;
using RapidWPF.Graphics;

namespace RapidWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Parameters parameters;
        public MainWindow()
        {
            InitializeComponent();
            parameters = new Parameters();
        }

        private void mRenderCtrl_ViewerReady()
        {
            AnyCAD.Forms.RenderControl render = mRenderCtrl.View3D;
            render.SetBackgroundColor(0f, 0.5f, 0.5f, 0.25f);
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
                    RowSkip = parameters.RowSkip,
                    VertSkip = parameters.VertexSkip
                };
                Graphic_Cloud cloud = new Graphic_Cloud(filereader, parameters);
                cloud.Run(mRenderCtrl);
                cloud.DrawBoundingBox(mRenderCtrl, parameters.FontSize);
            }
        }

        private void BN_ReadModel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BN_AnaPlane_Click(object sender, RoutedEventArgs e)
        {
            if (parameters.CloudFilePath == null)
            {
                throw new Exception("需要先读取点云");
            }

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

            List<Polygon3D> polygons = new List<Polygon3D>();
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Count == 1)
                {
                    polygons.Add(groups[i].First().Boundary);
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
                        polygons.AddRange(groups[i].Select(r => r.Boundary));
                    }
                    else
                    {
                        polygons.Add(bound);
                    }
                }
            }
            #endregion
            #region 平面可视化
            int id = EntityID;
            foreach (Polygon3D rect in polygons)
            {
                id++;
                TopoShape face = PolygonToFace(rect);
                RenderableGeometry entity = new RenderableGeometry();
                entity.SetGeometry(face);
                entity.SetId(id);
                EntitySceneNode node = new EntitySceneNode();
                node.SetEntity(entity);
                node.SetId(new ElementId(id));
                renderView.SceneManager.AddNode(node);
            }
            #endregion

            WriteLine(string.Format("Region count: {0}", regions.Count));
            WriteLine(string.Format("Filted count: {0}", regions.FindAll(r => r.CellCount > CloudConstants.MinRegionCellCount && r.EstimatedArea > CloudConstants.MinRegionArea).Count));
            WriteLine(string.Format("Clusters count: {0}", groups.Count));

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
