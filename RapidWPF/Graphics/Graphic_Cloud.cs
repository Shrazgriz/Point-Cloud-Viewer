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
        MVUnity.Exchange.CloudReader filereader;
        System.Windows.Media.Color pColor;
        //static Parameters parameters;
        public Graphic_Cloud(MVUnity.Exchange.CloudReader cloud, System.Windows.Media.Color PointColor)
        {
            filereader = cloud;
            pColor = PointColor;
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
                List<List<ScanRow>> cloud = filereader.ReadMultipleCloudOpton();
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
                            mColors.Append(pColor.R * f / 65535f);
                            mColors.Append(pColor.G * f / 65535f);
                            mColors.Append(pColor.B * f / 65535f);
                            f--;
                            if (f == 127) f = 256;
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region 无序点云
                Point3D[] pts = filereader.ReadCloud(filereader.VertSkip);
                foreach (Point3D pt in pts)
                {
                    mPositions.Append((float)pt.X);
                    mPositions.Append((float)pt.Y);
                    mPositions.Append((float)pt.Z);
                    mColors.Append(pColor.R);
                    mColors.Append(pColor.G);
                    mColors.Append(pColor.B);
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
