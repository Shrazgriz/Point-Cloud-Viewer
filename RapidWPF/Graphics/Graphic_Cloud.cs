using System;
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
        const int CloudID = 1;
        const int BoxID = 2;
        static Float32Buffer mPositions;
        static Float32Buffer mColors;
        MVUnity.Exchange.CloudReader filereader;
        System.Windows.Media.Color pColor;
        int Size;
        public int IntervalX;
        public int IntervalY;
        public int IntervalZ;
        public Graphic_Cloud(MVUnity.Exchange.CloudReader cloud, Parameters parameter)
        {
            filereader = cloud;
            pColor = parameter.PointColor;
            Size = parameter.PointSize;
            IntervalX = parameter.IntervalX;
            IntervalY = parameter.IntervalY;
            IntervalZ = parameter.IntervalZ;
        }
        bool ReadData()
        {
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
            PointCloud node = PointCloud.Create(mPositions, mColors, Size);
            node.SetUserId(CloudID);
            render.ShowSceneNode(node);
        }

        public void DrawBoundingBox(RenderControl render, float FontSize)
        {
            if (filereader == null)
            {
                throw new Exception("需要先指定点云来源");
            }
            var boxNode = DrawBoundBox(filereader, FontSize);
            boxNode.SetUserId(BoxID);
            render.ShowSceneNode(boxNode);
        }

        private GroupSceneNode DrawBoundBox(MVUnity.Exchange.CloudReader cloud, float FontSize)
        {
            GroupSceneNode plotModel = new GroupSceneNode();
            Vector3 Floor = ConvertV3.ToVector3(cloud.Min);
            Vector3 Ceiling = ConvertV3.ToVector3(cloud.Max);
            float minX = (float)Math.Ceiling(Floor.x / IntervalX) * IntervalX;
            float minY = (float)Math.Ceiling(Floor.y / IntervalY) * IntervalY;
            float minZ = (float)Math.Ceiling(Floor.z / IntervalZ) * IntervalZ;
            MeshPhongMaterial material = MeshPhongMaterial.Create("phong.bspline");
            material.SetUniform("diffuse", Uniform.Create(new Vector3(1, 0, 1)));
            material.SetFaceSide(EnumFaceSide.DoubleSide);

            for (float x = minX; x <= Ceiling.x; x += IntervalX)
            {
                BufferGeometry geometry = FontManager.Instance().CreateMesh(x.ToString("F0"));
                TextSceneNode label = new TextSceneNode(geometry, material, FontSize, true);
                label.SetWorldTransform(Matrix4.makeTranslation(x, Floor.y - (FontSize * 2.5f), Floor.z) * Matrix4.makeRotationAxis(Vector3.UNIT_Z, (float)(-0.5f * Math.PI)) *
                    Matrix4.makeScale(FontSize * 0.1f, FontSize * 0.1f, FontSize * 0.1f));
                label.SetPickable(false);
                plotModel.AddNode(label);
            }

            {
                BufferGeometry geometry = FontManager.Instance().CreateMesh("X (mm)");
                TextSceneNode label = new TextSceneNode(geometry, material, FontSize, true);
                label.SetPickable(false);
                label.SetWorldTransform(Matrix4.makeTranslation((Floor.x + Ceiling.x) * 0.5f, Floor.y - FontSize * 6, Floor.z)
                    * Matrix4.makeRotationAxis(Vector3.UNIT_Z, (float)(-0.5f * Math.PI)) * Matrix4.makeScale(FontSize * 0.1f, FontSize * 0.1f, FontSize * 0.1f));
                plotModel.AddNode(label);
            }

            for (float y = minY; y <= Ceiling.y; y += IntervalY)
            {
                BufferGeometry geometry = FontManager.Instance().CreateMesh(y.ToString("F0"));
                TextSceneNode label = new TextSceneNode(geometry, material, FontSize, true);
                label.SetPickable(false);
                label.SetWorldTransform(Matrix4.makeTranslation(Floor.x + FontSize * 3, y, Floor.z)
                    * Matrix4.makeScale(FontSize * 0.1f, FontSize * 0.1f, FontSize * 0.1f));
                plotModel.AddNode(label);
            }
            {
                BufferGeometry geometry = FontManager.Instance().CreateMesh("Y (mm)");
                TextSceneNode label = new TextSceneNode(geometry, material, FontSize, true);
                label.SetPickable(false);
                label.SetWorldTransform(Matrix4.makeTranslation(Floor.x + FontSize * 6, (Floor.y + Ceiling.y) * 0.5f, Floor.z)
                    * Matrix4.makeScale(FontSize * 0.1f, FontSize * 0.1f, FontSize * 0.1f));
                plotModel.AddNode(label);
            }

            for (float z = minZ; z <= Ceiling.z + double.Epsilon; z += IntervalZ)
            {
                BufferGeometry geometry = FontManager.Instance().CreateMesh(z.ToString("F0"));
                TextSceneNode label = new TextSceneNode(geometry, material, FontSize, true);
                label.SetPickable(false);
                label.SetWorldTransform(Matrix4.makeTranslation(Floor.x + FontSize * 3, Ceiling.y, z)
                    * Matrix4.makeRotationAxis(Vector3.UNIT_X, (float)(0.5f * Math.PI)) * Matrix4.makeScale(FontSize * 0.1f, FontSize * 0.1f, FontSize * 0.1f));
                plotModel.AddNode(label);
            }
            {
                BufferGeometry geometry = FontManager.Instance().CreateMesh("Z (mm)");
                TextSceneNode label = new TextSceneNode(geometry, material, FontSize, true);
                label.SetPickable(false);
                label.SetWorldTransform(Matrix4.makeTranslation(Floor.x + FontSize * 6, Ceiling.y, (Floor.z + Ceiling.z) * 0.5f)
                    * Matrix4.makeRotationAxis(Vector3.UNIT_X, (float)(0.5f * Math.PI)) * Matrix4.makeScale(FontSize * 0.1f, FontSize * 0.1f, FontSize * 0.1f));
                plotModel.AddNode(label);
            }
            {
                //Z
                
                TopoShape line = SketchBuilder.MakeLine(new GPnt(Floor.x, Floor.y, Floor.z), new GPnt(Floor.x, Floor.y, Ceiling.z));
                BrepSceneNode lineNode = BrepSceneNode.Create(line, material, null);                
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Ceiling.x, Floor.y, Floor.z), new GPnt(Ceiling.x, Floor.y, Ceiling.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Ceiling.x, Ceiling.y, Floor.z), new GPnt(Ceiling.x, Ceiling.y, Ceiling.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Floor.x, Ceiling.y, Floor.z), new GPnt(Floor.x, Ceiling.y, Ceiling.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
            }
            {
                //Y
                TopoShape line = SketchBuilder.MakeLine(new GPnt(Floor.x, Floor.y, Floor.z), new GPnt(Floor.x, Ceiling.y, Floor.z));
                BrepSceneNode lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Ceiling.x, Floor.y, Floor.z), new GPnt(Ceiling.x, Ceiling.y, Floor.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Ceiling.x, Floor.y, Ceiling.z), new GPnt(Ceiling.x, Ceiling.y, Ceiling.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Floor.x, Floor.y, Ceiling.z), new GPnt(Floor.x, Ceiling.y, Ceiling.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
            }
            {
                //X
                TopoShape line = SketchBuilder.MakeLine(new GPnt(Floor.x, Floor.y, Floor.z), new GPnt(Ceiling.x, Floor.y, Floor.z));
                BrepSceneNode lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Floor.x, Ceiling.y, Floor.z), new GPnt(Ceiling.x, Ceiling.y, Floor.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Floor.x, Floor.y, Ceiling.z), new GPnt(Ceiling.x, Floor.y, Ceiling.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);

                line = SketchBuilder.MakeLine(new GPnt(Floor.x, Ceiling.y, Ceiling.z), new GPnt(Ceiling.x, Ceiling.y, Ceiling.z));
                lineNode = BrepSceneNode.Create(line, material, null);
                lineNode.SetPickable(false);
                plotModel.AddNode(lineNode);
            }
            return plotModel;
        }

    }
}
