using System.ComponentModel;
using System.Configuration;
using System.Windows.Media;
using MVUnity;

namespace RapidWPF
{
    public class Parameters : INotifyPropertyChanged
    {
        private int stiffResX;
        private int stiffResY;
        private double cellNormTol;
        private double cellRhoTol;
        private double planeNormTol;
        private double planeRhoTol;
        private int rowSkip;
        private int columnSkip;
        private V3 cloudscale;
        private string cloudformat;
        private double maxGap;
        private double rhoTolerance;
        private string cloudFilePath;
        private Color pointColor;
        private int pointSize;
        private int minRegionCellCount;
        private double minRegionArea;
        private double mergeRation;
        private double searchDistance;
        private int intervalX;
        private int intervalY;
        private int intervalZ;
        private float fontSize;
        public int StiffResX { get => stiffResX; set => stiffResX = value; }
        public int StiffResY { get => stiffResY; set => stiffResY = value; }
        public double CellNormTol { get => cellNormTol; set => cellNormTol = value; }
        public double CellRhoTol { get => cellRhoTol; set => cellRhoTol = value; }
        public double PlaneNormTol { get => planeNormTol; set => planeNormTol = value; }
        public double PlaneRhoTol { get => planeRhoTol; set => planeRhoTol = value; }
        public int RowSkip { get => rowSkip; set => rowSkip = value; }
        public int VertexSkip { get => columnSkip; set => columnSkip = value; }
        public V3 Cloudscale { get => cloudscale; set => cloudscale = value; }
        public string Cloudformat { get => cloudformat; set => cloudformat = value; }
        public double MaxGap { get => maxGap; set => maxGap = value; }
        public double RhoTolerance { get => rhoTolerance; set => rhoTolerance = value; }
        public string CloudFilePath { get => cloudFilePath; set => cloudFilePath = value; }
        public event PropertyChangedEventHandler PropertyChanged;

        public SolidColorBrush PointBrush
        {
            get
            {
                return new SolidColorBrush(pointColor);
            }
        }
        public int PointSize { get => pointSize; set => pointSize = value; }
        public int MinRegionCellCount { get => minRegionCellCount; set => minRegionCellCount = value; }
        public double MinRegionArea { get => minRegionArea; set => minRegionArea = value; }
        public double MergeRation { get => mergeRation; set => mergeRation = value; }
        public double BoundarySearchWidth { get => searchDistance; set => searchDistance = value; }
        public int Tesslation { get; set; }
        public Color PointColor
        {
            get
            {
                return pointColor;
            }
            set
            {
                pointColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PointColor"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PointBrush"));
            }
        }

        public int IntervalZ { get => intervalZ; set => intervalZ = value; }
        public int IntervalY { get => intervalY; set => intervalY = value; }
        public int IntervalX { get => intervalX; set => intervalX = value; }
        public float FontSize { get => fontSize; set => fontSize = value; }

        public Parameters()
        {
            RhoTolerance = double.Parse(ConfigurationManager.AppSettings["RhoTolerance"]);
            MaxGap = double.Parse(ConfigurationManager.AppSettings["MaxSewGap"]);
            Cloudformat = ConfigurationManager.AppSettings["CloudFormat"];
            Cloudscale = new V3(ConfigurationManager.AppSettings["CloudScale"]);
            PlaneNormTol = double.Parse(ConfigurationManager.AppSettings["PlaneNormTolerance"]);
            PlaneRhoTol = double.Parse(ConfigurationManager.AppSettings["PlaneRhoTolerance"]);
            CellNormTol = double.Parse(ConfigurationManager.AppSettings["CellNormTolerance"]);
            CellRhoTol = double.Parse(ConfigurationManager.AppSettings["CellRhoTolerance"]);
            StiffResX = int.Parse(ConfigurationManager.AppSettings["StiffResolutionX"]);
            StiffResY = int.Parse(ConfigurationManager.AppSettings["StiffResolutionY"]);
            RowSkip = int.Parse(ConfigurationManager.AppSettings["RowSkip"]);
            VertexSkip = int.Parse(ConfigurationManager.AppSettings["VertexSkip"]);
            PointSize = int.Parse(ConfigurationManager.AppSettings["PointSize"]);
            string burshString = ConfigurationManager.AppSettings["PointBrush"];
            PointColor = (Color)ColorConverter.ConvertFromString(burshString);
            MinRegionArea = double.Parse(ConfigurationManager.AppSettings["MinRegionArea"]);
            MinRegionCellCount = int.Parse(ConfigurationManager.AppSettings["MinRegionCellCount"]);
            MergeRation = double.Parse(ConfigurationManager.AppSettings["MergeRation"]);
            BoundarySearchWidth = double.Parse(ConfigurationManager.AppSettings["BoundarySearchDistance"]);
            IntervalX = int.Parse(ConfigurationManager.AppSettings["IntervalX"]);
            IntervalY = int.Parse(ConfigurationManager.AppSettings["IntervalY"]);
            IntervalZ = int.Parse(ConfigurationManager.AppSettings["IntervalZ"]);
            Tesslation = 1;
            FontSize = float.Parse(ConfigurationManager.AppSettings["FontSize"]);
        }
    }
}
