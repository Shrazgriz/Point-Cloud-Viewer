using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVUnity;
using MVUnity.Geometry3D;
using MVUnity.PointCloud;
using System.Drawing;

namespace WpfCloud
{
    public class Cloud3D
    {
        private Dictionary<int, V3> dict_id_coord;
        public Dictionary<int, V3> Points { get { return dict_id_coord; } }
        private Dictionary<int, Color> dict_id_color;
        public Dictionary<int, Color> Colors { get { return dict_id_color; } }
        public int Count { get { return dict_id_coord.Count; } }
        
        /// Create an empty cloud
        /// </summary>
        public Cloud3D()
        {
            dict_id_coord = new Dictionary<int, V3>();
            dict_id_color = new Dictionary<int, Color>();
        }

        /// <summary>
        /// Duplicate a cloud
        /// </summary>
        /// <param name="duplicate"></param>
        public Cloud3D(Cloud3D duplicate)
        {
            dict_id_color = new Dictionary<int, Color>(duplicate.dict_id_color);
            dict_id_coord = new Dictionary<int, V3>(duplicate.dict_id_coord);
        }

        /// <summary>
        /// Create a new cloud, specifying points' ID and coordinate.
        /// </summary>
        /// <param name="Dic_ID_Coord"></param>
        public static Cloud3D CreateCloud(Dictionary<int, V3> Dic_ID_Coord, Dictionary<int,Color>Dic_ID_Color)
        {
            if (Dic_ID_Color.Count != Dic_ID_Coord.Count) throw new Exception("Entries number dis-match!");
            Cloud3D cld = new Cloud3D();
            foreach (var item in Dic_ID_Coord)
            {
                cld.AddPoint(new Point3D(item.Value, item.Key), Dic_ID_Color[item.Key]);
            }
            return cld;
        }
        /// <summary>
        /// Create a new cloud, specifying points coordinates and colors.
        /// </summary>
        /// <param name="Coords"></param>
        /// <param name="Colors"></param>
        /// <returns></returns>
        public static Cloud3D CreateCloud(List<V3>Coords, List<Color> Colors)
        {            
            Cloud3D cld = new Cloud3D();
            cld.AddPoints(Coords, Colors);
            return cld;
        }
        /// <summary>
        /// Create cloud with points coordinates
        /// </summary>
        /// <param name="coordbuffer">points coordinates : { p1.x, p1.y, p1.z, p2.x, p2.y, p2.z, ... }</param>
        public static Cloud3D CreateCloud(List<double> coordbuffer)
        {
            Cloud3D cld = new Cloud3D();

            for (int i = 0; i < coordbuffer.Count / 3; i++)
            {
                cld.dict_id_coord.Add(i, new V3(coordbuffer[i * 3], coordbuffer[i * 3 + 1], coordbuffer[i * 3 + 2]));
            }
            return cld;
        }

        public static Cloud3D CreateCloud(List<float> coordbuffer)
        {
            Cloud3D cld = new Cloud3D();

            for (int i = 0; i < coordbuffer.Count / 3; i++)
            {
                cld.dict_id_coord.Add(i, new V3(coordbuffer[i * 3], coordbuffer[i * 3 + 1], coordbuffer[i * 3 + 2]));
            }
            return cld;
        }
        /// <summary>
        /// Create cloud with list of points
        /// </summary>
        /// <param name="pointsbuffer"></param>
        /// <returns></returns>
        public static Cloud3D CreateCloud(List<V3> pointsbuffer)
        {
            Cloud3D cld = new Cloud3D();
            cld.AddPoints(pointsbuffer);
            return cld;
        }
        /// <summary>
        /// Create cloud with list of scanrows
        /// </summary>
        /// <param name="rowsbuffer"></param>
        /// <returns></returns>
        public static Cloud3D CreateCloud(List<ScanRow> rowsbuffer)
        {
            Cloud3D cld = new Cloud3D();
            foreach (ScanRow item in rowsbuffer)
            {
                foreach (Vertex vertex in item.Vertices)
                {
                    cld.AddPoint(vertex);
                }
            }
            return cld;
        }

        /// <summary>
        /// Add a point
        /// </summary>
        /// <param name="Point"></param>
        public void AddPoint(Point3D Point)
        {
            dict_id_coord.Add(Point.ID, Point as V3);
        }

        public void AddPoint(Point3D Point, Color Color)
        {
            dict_id_coord.Add(Point.ID, Point as V3);
            dict_id_color.Add(Point.ID, Color);
        }

        /// <summary>
        /// Add a list of points
        /// </summary>
        /// <param name="Coords"></param>
        public void AddPoints(List<V3> Coords)
        {
            for (int i = 0; i < Coords.Count; i++)
            {
                dict_id_coord.Add(i, Coords[i]);
            }
        }

        public void AddPoints(List<V3>Coords, List<Color> Colors)
        {
            if (Coords.Count != Colors.Count) throw new Exception("Entries number dis-match!");
            for (int i = 0; i < Coords.Count; i++)
            {
                dict_id_color.Add(i, Colors[i]);
                dict_id_coord.Add(i, Coords[i]);
            }
        }
        /// <summary>
        /// Create a sub cloud with given ID list
        /// </summary>
        /// <param name="IDList"></param>
        /// <returns>Cloud of points which is contained in ID list</returns>
        public Cloud3D SubCloud(List<int> IDList)
        {
            Dictionary<int, V3> subdic = dict_id_coord.Where(e => IDList.Contains(e.Key)).ToList().ToDictionary(e => e.Key, e => e.Value);
            Dictionary<int, Color>subcol= dict_id_color.Where(e => IDList.Contains(e.Key)).ToList().ToDictionary(e => e.Key, e => e.Value);
            return CreateCloud(subdic, subcol);
        }
        /// <summary>
        /// Remove certain points
        /// </summary>
        /// <param name="IDList"></param>
        public void RemovePoints(List<int> IDList)
        {
            foreach (int item in IDList)
            {
                dict_id_coord.Remove(item);
                dict_id_color.Remove(item);
            }
        }

        public Cloud3D Split(List<int> IDList)
        {
            Cloud3D splited = SubCloud(IDList);
            RemovePoints(IDList);
            return splited;
        }
        /// <summary>
        /// Attempt to find a plane in point cloud, using RANSAC
        /// </summary>
        /// <param name="Distance">Distance criteria. Lesser value gets more accurate result but more likely to fail.</param>
        /// <param name="Fraction">Points count fraction criteria. Greater value gets more accurate result but more likely to fail</param>
        /// <param name="AttemptionLimit">Default value:50</param>
        /// <returns></returns>
        public Plane FindPlane_RANSAC(double Distance, double Fraction, int AttemptionLimit = 50)
        {
            while (dict_id_coord.Count < 4) throw new Exception("Points count <4!");
            int _ptCta = (int)Math.Floor(dict_id_coord.Count * Fraction);
            int _checkpts = 0;
            for (int m = 0; m < AttemptionLimit; m++)
            {
                _checkpts = 0;
                int id1 = RandomPointID();
                int id2 = RandomPointID();
                while (id2 == id1) { id2 = RandomPointID(); }
                int id3 = RandomPointID();
                while ((id3 == id1) | (id3 == id2)) { id3 = RandomPointID(); }
                V3 c1 = dict_id_coord[id1];
                V3 c2 = dict_id_coord[id2];
                V3 c3 = dict_id_coord[id3];
                V3 norm = (c2 - c1).Cross(c3 - c1).Normalized;
                double rho = c1.Dot(norm);
                foreach (KeyValuePair<int, V3> pair in dict_id_coord)
                {
                    if (Math.Abs(GeometryTool3D.AlgDist_PointtoPlane(pair.Value, c1, norm)) < Distance)
                    {
                        _checkpts++;
                        if (_checkpts > _ptCta)
                        {
                            return new Plane(norm, rho);
                        }
                    }
                }
            }
            throw new Exception("Fail to Find a plane");
        }
        private int RandomPointID()
        {
            Random rnd = new Random();
            int s = rnd.Next(dict_id_coord.Count);
            return dict_id_coord.Keys.ToList()[s];
        }
        static int CompareKey(KeyValuePair<int, int> a, KeyValuePair<int, int> b) { return a.Key.CompareTo(b.Key); }

        /// <summary>
        /// Compute covariance matrix of points
        /// </summary>
        /// <returns>covariance matrix</returns>
        private SquareMatrix ComputeCovarianceMatrix()
        {
            SquareMatrix covariance = new SquareMatrix(3);
            // duplicate the vector array
            List<V3> pVectors = dict_id_coord.Values.ToList();

            // compute the average x, y, z values
            V3 avg = V3.Zero;
            foreach (var item in pVectors) avg += item;
            avg *= 1 / Count;
            for (int i = 0; i < Count; ++i) pVectors[i] -= avg;

            // compute the covariance (we are computing the lower-triangular entries then using
            // the symmetric property):
            double cxx = 0;double cxy = 0;double cxz = 0;double cyy = 0;double cyz = 0;double czz = 0;

            // cov(X, Y) = E[(X - x)(Y - y)]
            foreach (var item in pVectors)
            {
                cxx += item.X * item.X;
                cxy += item.X * item.Y;
                cxz += item.X * item.Z;
                cyy += item.Y * item.Y;
                cyz += item.Y * item.Z;
                czz += item.Z * item.Z;
            }

            covariance[0, 0] = cxx / Count;  // covariance matrix is symmetric
            covariance[0, 1] = cxy / Count;
            covariance[1, 0] = covariance[0, 1];
            covariance[1, 1] = cyy / Count;
            covariance[0, 2] = cxz / Count;
            covariance[2, 0] = covariance[0, 2];
            covariance[1, 2] = cyz / Count;
            covariance[2, 1] = covariance[1, 2];
            covariance[2, 2] = czz / Count;

            return covariance;
        }


        /// <summary>
        /// Compute oriented bounding box of point cloud
        /// </summary>
        /// <returns></returns>
        public Box ComputeOBB()
        {
            Box result = new Box();
            V3 sum = V3.Zero;
            List<double> extL = new List<double>();
            List<double> extH = new List<double>();
            List<double> extW = new List<double>();

            foreach (var item in dict_id_coord)
            {
                sum += item.Value;
            }
            V3 MassCenter = sum * (1f / Count);
            SquareMatrix covariance = ComputeCovarianceMatrix();
            covariance.ComputeEigenVectors();
            List<V3> EigenVectors = covariance.GetEigenVectors();
            List<double> EigenValues = covariance.GetEigenValues();
            

            result.MajorOrientation = EigenVectors[0].Normalized;
            result.SecondaryOrientation =EigenVectors[1]- EigenVectors[1].Dot(result.MajorOrientation) * result.MajorOrientation;
            result.MinorOrientation = result.MajorOrientation.Cross(result.SecondaryOrientation);

            foreach (var item in dict_id_coord)
            {
                //V3 displacement = item.Value - MassCenter;
                extL.Add(item.Value.Dot(result.MajorOrientation));
                extW.Add(item.Value.Dot(result.SecondaryOrientation));
                extH.Add(item.Value.Dot(result.MinorOrientation));
            }
            V3 maxExtents = new V3(extL.Max(), extW.Max(), extH.Max());
            V3 minExtents = new V3(extL.Min(), extW.Min(), extH.Min());

            V3 boxCenter = (maxExtents + minExtents) *0.5;
            result.Center = (boxCenter.X * result.MajorOrientation) + (boxCenter.Y * result.SecondaryOrientation) + (boxCenter.Z * result.MinorOrientation);
            result.Size = maxExtents - minExtents;

            return result;
        }

        public Rectangle3D FitRec(double Tolerance)
        {
            Plane _firstFit = FindPlane_RANSAC(5, 0.3);
            Dictionary<int, double> dict_id_delta= new Dictionary<int, double>();
            double std = 0;
            foreach (KeyValuePair<int, V3> pt in dict_id_coord)
            {
                double _d = _firstFit.AbsDistance(pt.Value);
                dict_id_delta.Add(pt.Key, _d);
                std += _d * _d;
            }
            std /= dict_id_coord.Count;

            List<V3> points = new List<V3>();
            foreach (int id in dict_id_delta.Where(e=>e.Value<std*Tolerance).Select(e=>e.Key))
            {
                points.Add(dict_id_coord[id]);
            }
            Plane _secFit = Plane.CreatePlane(points);
            Box _obb = Box.ComputeOBB(points);
            V3 _center = _secFit.Projection(_obb.Center);
            Rectangle3D result = Rectangle3D.CreateRec(_center, _obb.MajorOrientation, _obb.SecondaryOrientation, _obb.Size.X, _obb.Size.Y);
            return result;
        }
    }
}
