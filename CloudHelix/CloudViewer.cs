using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Linq;

namespace CloudHelix
{
    /// <summary>
    /// Viewer of scatter points
    /// </summary>
    public class CloudViewer : ModelVisual3D
    {
        private const string AxisName = "Axis";
        private const string PointsName = "Points";

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
                Points = new Point3DCollection(Points),
                Color = PointColor,
                Size = PointSize
            };
            PV.Content.SetName(PointsName);
            plotModel.Children.Add(PV.Content);
            if ((Points == null) || (Points.Length == 0)) return plotModel;
            if (AutoLabel)
            {
                double minX = Points.Min(p => p.X); ;
                double maxX = Points.Max(p => p.X);
                double minY = Points.Min(p => p.Y);
                double maxY = Points.Max(p => p.Y);
                double minZ = Points.Min(p => p.Z);
                double maxZ = Points.Max(p => p.Z);
                ceiling = new Vector3D(maxX, maxY, maxZ);
                floor = new Vector3D(minX, minY, minZ);
            }

            // create bounding box with axes indications
            var axesMeshBuilder = new MeshBuilder();
            for (double x = Floor.X; x <= Ceiling.X; x += IntervalX)
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D(x.ToString("F0"), Brushes.Black, true, FontSize,
                                                                           new Point3D(x, Floor.Y - (FontSize * 2.5), Floor.Z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.Children.Add(label);
            }

            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D("X-axis", Brushes.Black, true, FontSize,
                                                                           new Point3D((Floor.X + Ceiling.X) * 0.5, Floor.Y - (FontSize * 6), Floor.Z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.Children.Add(label);
            }

            for (double y = Floor.Y; y <= Ceiling.Y; y += IntervalY)
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D(y.ToString("F0"), Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X - (FontSize * 3), y, Floor.Z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
                plotModel.Children.Add(label);
            }
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D("Y-axis", Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X - (FontSize * 10), (Floor.Y + Ceiling.Y) * 0.5, Floor.Z),
                                                                           new Vector3D(0, 1, 0), new Vector3D(-1, 0, 0));
                plotModel.Children.Add(label);
            }
            double z0 = (int)(Floor.Z / IntervalZ) * IntervalZ;
            for (double z = z0; z <= Ceiling.Z + double.Epsilon; z += IntervalZ)
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D(z.ToString("F0"), Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X - (FontSize * 3), Ceiling.Y, z),
                                                                           new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
                plotModel.Children.Add(label);
            }
            {
                GeometryModel3D label = TextCreator.CreateTextLabelModel3D("Z-axis", Brushes.Black, true, FontSize,
                                                                           new Point3D(Floor.X - (FontSize * 10), Ceiling.Y, (Floor.Z + Ceiling.Z) * 0.5),
                                                                           new Vector3D(0, 0, 1), new Vector3D(1, 0, 0));
                plotModel.Children.Add(label);
            }

            var bb = new Rect3D(Floor.X, Floor.Y, Floor.Z, Ceiling.X - Floor.X, Ceiling.Y - Floor.Y, Ceiling.Z - Floor.Z);
            axesMeshBuilder.AddBoundingBox(bb, LineThickness);
            var axesModel = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Black);
            axesModel.SetName(AxisName);
            plotModel.Children.Add(axesModel);
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
            //Visible = true;
            visualChild = new ModelVisual3D();
            Children.Add(visualChild);
        }
    }
}
