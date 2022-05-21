using System.ComponentModel;
using System.Configuration;
using System.Windows.Media;
using MVUnity;

namespace MatDesign
{
    public class Parameters //: INotifyPropertyChanged
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
        private SolidColorBrush pointColor;
        private int pointSize;
        private int minRegionCellCount;
        private double minRegionArea;
        private double mergeRation;
        private double searchDistance;
        //public event PropertyChangedEventHandler PropertyChanged;
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
        public SolidColorBrush PointBrush
        {
            get => pointColor;
            set
            {
                if (pointColor != value)
                {
                    pointColor = value;
                    //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PointBrush)));
                }
            }
        }
        public int PointSize { get => pointSize; set => pointSize = value; }
        public int MinRegionCellCount { get => minRegionCellCount; set => minRegionCellCount = value; }
        public double MinRegionArea { get => minRegionArea; set => minRegionArea = value; }
        public double MergeRation { get => mergeRation; set => mergeRation = value; }
        public double BoundarySearchWidth { get => searchDistance; set => searchDistance = value; }

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
            Color pointcolor = (Color)ColorConverter.ConvertFromString(burshString);
            PointBrush = new SolidColorBrush(pointcolor);
            MinRegionArea = double.Parse(ConfigurationManager.AppSettings["MinRegionArea"]);
            MinRegionCellCount = int.Parse(ConfigurationManager.AppSettings["MinRegionCellCount"]);
            MergeRation = double.Parse(ConfigurationManager.AppSettings["MergeRation"]);
            BoundarySearchWidth = double.Parse(ConfigurationManager.AppSettings["BoundarySearchDistance"]);
        }
    }
}
