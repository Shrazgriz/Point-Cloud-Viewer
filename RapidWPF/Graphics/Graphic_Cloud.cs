using System.Collections.Generic;
using System.Linq;
using AnyCAD.Foundation;
using AnyCAD.WPF;
using MVUnity;
using MVUnity.PointCloud;

namespace RapidWPF.Graphics
{
    public class Graphic_Cloud
    {
        static Float32Buffer mPositions;
        static Float32Buffer mColors;
        static MVUnity.Exchange.CloudReader filereader;
        static Parameters parameters;
        public Graphic_Cloud(MVUnity.Exchange.CloudReader cloud)
        {
            filereader = cloud;
            parameters = new Parameters();
        }
        bool ReadData()
        {
            if (mPositions != null)
                return true;
            mPositions = new Float32Buffer(0);
            mColors = new Float32Buffer(0);

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
                            mPositions.Append((float)vertex.X);
                            mPositions.Append((float)vertex.Y);
                            mPositions.Append((float)vertex.Z);
                            mColors.Append(parameters.PointColor.R * f / 65535f);
                            mColors.Append(parameters.PointColor.G * f / 65535f);
                            mColors.Append(parameters.PointColor.B * f / 65535f);
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
                    mPositions.Append((float)pt.X);
                    mPositions.Append((float)pt.Y);
                    mPositions.Append((float)pt.Z);
                    mColors.Append(color.R);
                    mColors.Append(color.G);
                    mColors.Append(color.B);
                }
                #endregion
            }
            return true;
        }
        public void Run(RenderControl render)
        {
            if (!ReadData())
                return;
            PointCloud node = PointCloud.Create(mPositions, mColors, 1);
            render.ShowSceneNode(node);
        }
    }
}
