using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Linq;
using System;

namespace CloudHelix
{
    /// <summary>
    /// Viewer of scatter points
    /// </summary>
    public class CloudViewer : ModelVisual3D
    {
        private const string AxisName = "Axis";
        private const string PointsName = "Points";
        private const string GridName = "Grids";

        /// <summary>
        /// container for scatter points
        /// </summary>
        //private List<PointsVisual3D> _pointsContainer;
        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register("Visible", typeof(bool), typeof(CloudViewer), new UIPropertyMetadata(true, ModelChanged));
        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register("Points", typeof(Point3D[]), typeof(CloudViewer), new UIPropertyMetadata(null, ModelChanged));
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(CloudViewer), new UIPropertyMetadata(Colors.White, ModelChanged));
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(int), typeof(CloudViewer), new UIPropertyMetadata(4, ModelChanged));
        public static readonly DependencyProperty AutoLabelProperty =
            DependencyProperty.Register("AutoLabel", typeof(bool), typeof(CloudViewer), new UIPropertyMetadata(true, ModelChanged));
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(Vector3D), typeof(CloudViewer), new UIPropertyMetadata(new Vector3D(1, 1, 1), ModelChanged));
        private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CloudViewer)d).UpdateModel();
        }

        private void UpdateModel()
        {
            Content = CreateModel();
        }
        public void Update()
        {
            UpdateModel();
        }
        private Model3D CreateModel()
        {
            if (!Visible)
            {
                return new Model3DGroup();
            }
            Model3DGroup plotModel = new Model3DGroup();
            PointsVisual3D PV = new PointsVisual3D()
            {
                Color = PointColor,
                Size = PointSize
            };
            if ((Points == null) || (Points.Length == 0))
            {
                return plotModel;
            }

            Point3DCollection pcl = new Point3DCollection();
            foreach (Point3D point in Points)
            {
                pcl.Add(new Point3D(point.X * Scale.X, point.Y * Scale.Y, point.Z * Scale.Z));
            }
            PV.Points = pcl;
            PV.Content.SetName(PointsName);
            plotModel.Children.Add(PV.Content);

            if (AutoLabel)
            {
                double minX = Points.Min(p => p.X);
                double maxX = Points.Max(p => p.X);
                double minY = Points.Min(p => p.Y);
                double maxY = Points.Max(p => p.Y);
                double minZ = Points.Min(p => p.Z);
                double maxZ = Points.Max(p => p.Z);
                ceiling = new Vector3D(maxX, maxY, maxZ);
                floor = new Vector3D(minX, minY, minZ);
            }
            if (DrawBox)
            {
                plotModel.Children.Add(DrawBoundingBox());
            }
            if (DrawGrid)
            {
                plotModel.Children.Add(DrawGridLine());
            }            
            return plotModel;
        }

        public Point3D[] Points
        {
            get { return (Point3D[])GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }
        public Color PointColor
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
        public int PointSize
        {
            get { return (int)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }
        public Vector3D Scale
        {
            get { return (Vector3D)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }
        public bool DrawBox { get; set; }
        public bool DrawGrid { get; set; }
        public double IntervalX { get; set; }
        public double IntervalY { get; set; }
        public double IntervalZ { get; set; }
        public double FontSize { get; set; }
        public double LineThickness { get; set; }

        public Vector3D Ceiling
        {
            get => ceiling;
            set
            {
                ceiling = value;
                AutoLabel = false;
            }
        }
        public Vector3D Floor
        {
            get => floor;
            set
            {
                floor = value;
                AutoLabel = false;
            }
        }
        public bool Visible
        {
            get { return (bool)GetValue(VisibleProperty); }
            set { SetValue(VisibleProperty, value); }
        }
        public bool AutoLabel
        {
            get { return (bool)GetValue(AutoLabelProperty); }
            set { SetValue(AutoLabelProperty, value); }
        }
        private readonly ModelVisual3D visualChild;
        private Vector3D ceiling;
        private Vector3D floor;

        public CloudViewer()
        {
            IntervalX = 1;
            IntervalY = 1;
            IntervalZ = 1;
            FontSize = 0.06;
            LineThickness = 0.01;
            Scale = new Vector3D(1, 1, 1);
            DrawBox = true;
            DrawGrid = true;
            visualChild = new ModelVisual3D();
            Children.Add(visualChild);
        }

        private Model3DGroup DrawBoundingBox()
        {
            Model3DGroup plotModel = new Model3DGroup();
            MeshBuilder axesMeshBuilder = new MeshBuilder();
            double minX = Math.Ceiling(Points.Min(p => p.X) / IntervalX) * IntervalX;
            double minY = Math.Ceiling(Points.Min(p => p.Y) / IntervalY) * IntervalY;
            double minZ = Math.Ceiling(Points.Min(p => p.Z) / IntervalZ) * IntervalZ;
            for (double x = minX; x <= Ceiling.X; x += IntervalX)
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D(x.ToString("F0"), Brushes.Black, true, FontSize,
                                                                           new Point3D(x * Scale.X, Floor.Y * Scale.Y - (FontSize * 2.5), Floor.Z * Scale.Z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.Children.Add(label);
            }

            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D("X (mm)", Brushes.Black, true, FontSize,
                                                                           new Point3D((Floor.X + Ceiling.X) * 0.5 * Scale.X, Floor.Y * Scale.Y - (FontSize * 6), Floor.Z * Scale.Z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.Children.Add(label);
            }

            for (double y = minY; y <= Ceiling.Y; y += IntervalY)
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D(y.ToString("F0"), Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X * Scale.X - (FontSize * 3), y * Scale.Y, Floor.Z * Scale.Z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.Children.Add(label);
            }
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D("Y (mm)", Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X * Scale.X - (FontSize * 10), (Floor.Y + Ceiling.Y) * 0.5 * Scale.Y, Floor.Z * Scale.Z),
                                                                           new Vector3D(0, 1, 0), new Vector3D(-1, 0, 0));
                plotModel.Children.Add(label);
            }
            for (double z = minZ; z <= Ceiling.Z + double.Epsilon; z += IntervalZ)
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D(z.ToString("F0"), Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X * Scale.X - (FontSize * 3), Ceiling.Y * Scale.Y, z * Scale.Z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
                plotModel.Children.Add(label);
            }
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D("Z (mm)", Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X * Scale.X - (FontSize * 10), Ceiling.Y * Scale.Y, (Floor.Z + Ceiling.Z) * 0.5 * Scale.Z),
                                                                           new Vector3D(0, 0, 1), new Vector3D(1, 0, 0));
                plotModel.Children.Add(label);
            }

            Rect3D bb = new Rect3D(Floor.X * Scale.X, Floor.Y * Scale.Y, Floor.Z * Scale.Z,
                (Ceiling.X - Floor.X) * Scale.X, (Ceiling.Y - Floor.Y) * Scale.Y, (Ceiling.Z - Floor.Z) * Scale.Z);
            axesMeshBuilder.AddBoundingBox(bb, LineThickness);
            GeometryModel3D axesModel = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Black);
            axesModel.SetName(AxisName);
            plotModel.Children.Add(axesModel);
            return plotModel;
        }

        private Model3DGroup DrawGridLine()
        {
            Model3DGroup plotModel = new Model3DGroup();
            MeshBuilder gridMeshBuilder = new MeshBuilder();
            double minX = Math.Ceiling(Points.Min(p => p.X) / IntervalX) * IntervalX;
            double minY = Math.Ceiling(Points.Min(p => p.Y) / IntervalY) * IntervalY;
            double minZ = Math.Ceiling(Points.Min(p => p.Z) / IntervalZ) * IntervalZ;
            for (double x = minX; x <= Ceiling.X; x += IntervalX)
            {
                gridMeshBuilder.AddQuad(new Point3D(x - (LineThickness * 0.25f), Floor.Y, Floor.Z),
                    new Point3D(x + (LineThickness * 0.25f), Floor.Y, Floor.Z),
                    new Point3D(x + (LineThickness * 0.25f), Ceiling.Y, Floor.Z),
                    new Point3D(x - (LineThickness * 0.25f), Ceiling.Y, Floor.Z));
            }
            for (double y = minY; y <= Ceiling.Y; y += IntervalY)
            {
                gridMeshBuilder.AddQuad(new Point3D(Floor.X, y - (LineThickness * 0.25f), Floor.Z),
                    new Point3D(Floor.X, y + (LineThickness * 0.25f), Floor.Z),
                    new Point3D(Ceiling.X, y + (LineThickness * 0.25f), Floor.Z),
                    new Point3D(Ceiling.X, y - (LineThickness * 0.25f), Floor.Z));
            }
            GeometryModel3D gridModel = new GeometryModel3D(gridMeshBuilder.ToMesh(), Materials.Gray)
            {
                BackMaterial = Materials.Gray,
                Transform = new ScaleTransform3D(Scale)
            };
            gridModel.SetName(GridName);
            plotModel.Children.Add(gridModel);
            return plotModel;
        }
    }
}