using System;
using System.Collections.Generic;
using System.Linq;
using MVUnity;
using MVUnity.Geometry3D;
using MVUnity.PointCloud;

namespace WpfCloud
{
    public class DBScan
    {
        public static List<List<Plane>> ClusteringRects(List<Plane> Input)
        {
            List<Tuple<int, int>> eqvPairs = new List<Tuple<int, int>>();
            List<int> labels = Enumerable.Repeat(-1, Input.Count).ToList();
            List<List<Plane>> output = new List<List<Plane>>();
            int label = 0;
            while (labels.Contains(-1))
            {
                int id = labels.IndexOf(-1);
                labels[id] = label;
                for (int i = 0; i < Input.Count; i++)
                {
                    if (i == id) continue;
                    if (Similar(Input[id], Input[i]))
                    {
                        if (labels[i] != -1) labels[i] = label;
                        else
                        {
                            eqvPairs.Add(new Tuple<int, int>(label, labels[i]));
                        }
                    }
                }
                label++;
            }
            List<List<int>> linkedmap = Operation.Eqvpairs_to_groups(eqvPairs, label);
            for (int i = 0; i < linkedmap.Count; i++)
            {
                List<Plane> _group = new List<Plane>();
                for (int j = 0; j < Input.Count; j++)
                {
                    if (linkedmap[i].Contains(labels[j])) _group.Add(Input[j]);
                }
                output.Add(_group);
            }
            return output;
        }

        public static List<List<CompiledRegion>> ClusteringRegions(List<CompiledRegion> Input)
        {
            List<Tuple<int, int>> eqvPairs = new List<Tuple<int, int>>();
            List<int> labels = Enumerable.Repeat(-1, Input.Count).ToList();
            List<List<CompiledRegion>> output = new List<List<CompiledRegion>>();
            int label = 0;
            while (labels.Contains(-1))
            {
                int id = labels.IndexOf(-1);
                labels[id] = label;
                for (int i = 0; i < Input.Count; i++)
                {
                    if (i == id) continue;
                    if (Similar(Input[id], Input[i]))
                    {
                        if (labels[i] == -1) labels[i] = label;
                        else
                        {
                            eqvPairs.Add(new Tuple<int, int>(label, labels[i]));
                        }
                    }
                }
                label++;
            }
            List<List<int>> linkedmap = Operation.Eqvpairs_to_groups(eqvPairs, label);
            for (int i = 0; i < linkedmap.Count; i++)
            {
                List<CompiledRegion> _group = new List<CompiledRegion>();
                for (int j = 0; j < Input.Count; j++)
                {
                    if (linkedmap[i].Contains(labels[j])) _group.Add(Input[j]);
                }
                output.Add(_group);
            }
            return output;
        }

        private static bool Similar(Plane Plane1, Plane Plane2)
        {
            bool similar1 = Plane1.Norm.Dot(Plane2.Norm) > Tolerance.CollinearDotPdt;
            bool similar2 = Math.Abs(Plane1.Rho - Plane2.Rho) < Tolerance.DistThresoldOnPlane;
            return similar1 & similar2;
        }
        private static bool Similar(CompiledRegion reg1, CompiledRegion reg2)
        {
            Polygon3D rect1 = reg1.Boundary;
            Polygon3D rect2 = reg2.Boundary;
            bool similar1 = rect1.Norm.Dot(rect2.Norm) > Tolerance.CollinearDotPdt;
            bool similar2 = Math.Abs(rect1.Center.Dot(rect1.Norm) - rect2.Center.Dot(rect2.Norm)) < Tolerance.DistThresoldOnPlane;
            return similar1 & similar2;
        }
    }
}
