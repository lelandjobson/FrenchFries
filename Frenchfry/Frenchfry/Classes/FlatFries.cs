using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Frenchfry
{
    public class FlatFries
    {
        // Intersection tolerance
        private double tolerance = 0.0001;
        private List<Mesh> friedMeshes;

        // Storage
        Dictionary<Guid, List<IntersectionEvent>> intersectionDict;
        Dictionary<Guid, Curve> curveDict;
        List<Curve> curveList;
        List<Interval> curveIntervalStorage;
        List<FryResult> fryIntersections;

        public FlatFries(List<Curve> fryCurves, double thickness)
        {
            // Init
            this.Init();

            // Remove duplicate instances
            for(int i = fryCurves.Count - 1; i> 0; i--)
            {
                var crv = fryCurves[i];
                var hash = crv.GetHashCode();
                bool self = false;
                for(int j = fryCurves.Count - 1; j > 0; j--)
                {
                    var crv2 = fryCurves[j];
                    if (hash == crv2.GetHashCode())
                    {
                        if (!self) { self = true; }
                        else { fryCurves.Remove(crv2); }
                    }
                }
            }

            // Add fryCurves to dictionary
            foreach(var crv in fryCurves)
            {
                // Reparameterize Curves
                crv.Domain = new Interval(0.0, 1.0);

                curveDict.Add(new Guid(), crv);
            }

            // Procedural intersection
            foreach (var crv in curveDict)
            {
                foreach(var crv2 in curveDict)
                {
                    // If equal, continue
                    if (crv.Key == crv2.Key) { continue; }

                    // Perform an intersection
                    var iResults = Rhino.Geometry.Intersect.Intersection.CurveCurve(crv.Value, crv2.Value, tolerance, tolerance);

                    // If there is no intersection, continue
                    if (iResults.Count == 0) { continue; }

                    // If overlap intersection, continue
                    if (iResults[0].IsOverlap) { continue; }

                    // Add fryReuslt
                    fryIntersections.Add(new FryResult(iResults,crv, crv2));
                }

                // Init list of intersection events
                var interEventList = new List<IntersectionEvent>();
                interEventList.Clear();

                // Turn curveintersections in intersection events
                foreach (var fryInter in fryIntersections)
                {
                    foreach(var interItem in fryInter.Intersections)
                    {
                        interEventList.Add(interItem);
                    }
                }

                // Add each curve w/ intersection to dictionary
                intersectionDict.Add(crv.Key, interEventList);
            }

            // Organize Intersection Data
            foreach(var interPair in intersectionDict)
            {
                var intList = interPair.Value;
                // Arrange intersection points along curve by param
                intList. Sort((int1, int2) => int1.ParameterA.CompareTo(int2.ParameterA));
                intList.Reverse();

                // Check if there are 0 or 1 items for this curve
                bool hasZero = false;
                bool hasOne = false;

                foreach (var inter in intList)
                {
                    if (inter.ParameterA == 0)
                    {
                        hasZero = true;
                    }
                    if (inter.ParameterA == 1)
                    {
                        hasOne = true;
                    }
                }

                // Get matching curve 
                Curve crv;
                curveDict.TryGetValue(interPair.Key, out crv);
                if (crv == null) { continue; }

                // Add in start and end points if necessary
                if (!hasZero) { intList.Insert(0,Rhino.Geometry.Intersect.Intersection.CurveCurve(crv, new Line(crv.PointAtStart, new Point3d(crv.PointAtStart.X, crv.PointAtStart.Y, crv.PointAtStart.Z + tolerance * 2)).ToNurbsCurve(), tolerance, tolerance)[0]); }
                if(!hasOne) { intList.Insert(intList.Count,Rhino.Geometry.Intersect.Intersection.CurveCurve(crv, new Line(crv.PointAtEnd, new Point3d(crv.PointAtEnd.X, crv.PointAtEnd.Y, crv.PointAtEnd.Z + tolerance * 2)).ToNurbsCurve(), tolerance, tolerance)[0]); }
            }

            // Now we have an organized list of intersection events by curve, including start and end points

        }

        private void Init()
        {
            intersectionDict.Clear();
            curveIntervalStorage.Clear();
            friedMeshes.Clear();
            curveDict.Clear();

            intersectionDict = new Dictionary<Guid, List<IntersectionEvent>>();
            curveIntervalStorage = new List<Interval>();
            friedMeshes = new List<Mesh>();
            curveDict = new Dictionary<Guid, Curve>();
            fryIntersections = new List<FryResult>();
        }

        /// <summary>
        /// The complete mesh lattice
        /// </summary>
        public virtual List<Mesh> FriedMeshes
        {
            get { return this.friedMeshes; }
        }    

    }

    public class FryIntersection
    {
        public readonly int id;
        public readonly Vector3d vecA;
        public readonly Vector3d vecB;
        public readonly Point3d pA;
        public readonly Point3d pB;
        public readonly Guid crvAId;
        public readonly Guid crvBId;

        public FryIntersection(IntersectionEvent intEvent, Curve a, Curve b)
        {
            this.pA = intEvent.PointA;
            this.pB = intEvent.PointB;

            // Get tangent vectors from curves
            vecA = a.TangentAt(intEvent.ParameterA);
            vecB = b.TangentAt(intEvent.ParameterB);
        }

        public void AddIntersection(Point3d iPoint, Vector3d curveTanDir)
        {

        }
    }

    public class FryResult
    {
        public readonly KeyValuePair<Guid, Curve> PairA;
        public readonly KeyValuePair<Guid, Curve> PairB;
        public readonly CurveIntersections Intersections;
        public FryResult(CurveIntersections intersections, KeyValuePair<Guid,Curve> pairA, KeyValuePair<Guid,Curve> pairB)
        {
            PairA = pairA;
            PairB = pairB;
            Intersections = intersections;
        }
    }
}
