using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnyCAD.Foundation;
using AnyCAD.WPF;
using MVUnity;
using MVUnity.PointCloud;
using MVUnity.Geometry3D;

namespace RapidWPF.Graphics
{
    public class Graphic_Polygons
    {
        const ulong PolyonID = 100;
        private static List<Polygon3D> polygons;
        private double mergeRation;
        private MVUnity.Exchange.CloudReader filereader;

        public Graphic_Polygons(MVUnity.Exchange.CloudReader reader, Parameters parameter)
        {
            filereader = new MVUnity.Exchange.CloudReader
            {
                Scale = reader.Scale,
                FileName = reader.FileName,
                Format = reader.Format,
                RowSkip = reader.RowSkip,
                VertSkip = reader.VertSkip
            };
            mergeRation = parameter.MergeRation;
        }

        private bool ReadData()
        {
            if (filereader.FileName == null)
            {
                return false;
            }
            if (Graphic_Polygons.polygons == null)
            {
                Graphic_Polygons.polygons = new List<Polygon3D>();
            }
            else { Graphic_Polygons.polygons.Clear(); }
            #region 区分异源点云
            List<List<ScanRow>> diffRows = filereader.ReadMultipleCloudOpton(filereader.RowSkip, filereader.VertSkip);

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
                if (r.State == RegionState.Open)
                {
                    r.State = RegionState.Completed;
                }
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
                    if (area1 * mergeRation > area2)
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
            return true;
        }

        private void DrawPolys(RenderControl render)
        {
            #region 平面可视化
            ulong id = PolyonID;
            foreach (Polygon3D rect in polygons)
            {
                id++;
                TopoShape face = ConvertPoly3.ToShape(rect);
                MeshPhongMaterial material = MeshPhongMaterial.Create("phong.bspline");
                BrepSceneNode entity = BrepSceneNode.Create(face, material, material);
                entity.SetUserId(id);
                render.ShowSceneNode(entity);
            }
            #endregion
        }
        public void Run(RenderControl render)
        {
            if (!ReadData())
                return;
            DrawPolys(render);
        }
    }
}
