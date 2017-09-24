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

        public FryResults Results = new FryResults();
        private double tolerance = 0.0001;
        private List<Mesh> friedMeshes;


        public FlatFries(List<Curve> fryCurves, double thickness)
        {
            // Process:
            // 01 - Init (Clear memory)
            // 02 - Remove duplicate instances of curve
            // 03 - Add all of the curves into a single dictionary curveDict
            // 04 - Intersect all curves against eachother and create FryResults
            
            // Init
            this.Init();

            // Remove duplicate instances
            for (int i = fryCurves.Count - 1; i> 0; i--)
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

            // Add fryCurves to Results
            foreach(var crv in fryCurves)
            {
                // Reparameterize Curves
                crv.Domain = new Interval(0.0, 1.0);
                Results.Curves.Add(Guid.NewGuid(), crv);
            }

            // Procedural intersection
            foreach (var crv in Results.Curves)
            {
                var intersections = new SortedIntersections(crv);

                foreach(var crv2 in Results.Curves)
                {
                    // If equal, continue
                    if (crv.Key == crv2.Key) { continue; }

                    // Perform an intersection
                    var crvIntersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(crv.Value, crv2.Value, tolerance, tolerance);

                    // If there is no intersection, continue
                    if (crvIntersections.Count == 0) { continue; }

                    // If overlap intersection, continue
                    if (crvIntersections[0].IsOverlap) { continue; }

                    // Add Intersections
                    intersections.AddIntersections(crv2, crvIntersections);
                }

                // Add Intersections to Results
                Results.AddSortedIntersections(crv.Key, intersections);
            }

            // Now we have an organized list of intersection events by curve.
            // Before we can construct the lattice, we need to do some work to support 3+ curve intersections.
            
            foreach(var intersection in Results.SortedIntersections)
            {

            }
        }

        private void Init()
        {
            friedMeshes.Clear();
        }

        /// <summary>
        /// The complete mesh lattice
        /// </summary>
        public virtual List<Mesh> FriedMeshes
        {
            get { return this.friedMeshes; }
        }    

    }

    /// <summary>
    /// Stores results of curve intersections. Can contain many intersections, each containing a FryIntersection object.
    /// </summary>
    public class FryResults
    {
        #region Members
        public readonly Dictionary<Guid, Curve> Curves = new Dictionary<Guid, Curve>();
        public readonly Dictionary<Guid, SortedIntersections> SortedIntersections = new Dictionary<Guid, SortedIntersections>();
        public readonly Dictionary<Guid, List<FryIntersection>> IntersectionsByNode = new Dictionary<Guid, List<FryIntersection>>();
        public readonly Dictionary<Guid, Point3d> IntersectionNodes = new Dictionary<Guid, Point3d>();
        public double tolerance = 0.0001;
        #endregion

        #region CTORS
        public FryResults()
        {
        }
        #endregion

        #region Methods


        public void AddCurve(Curve curve)
        {
            this.Curves.Add(Guid.NewGuid(), curve);
        }

        public void AddSortedIntersections(Guid crvGuid, SortedIntersections sortedIntersections)
        {
            sortedIntersections.SortIntersections();
            SortedIntersections.Add(crvGuid, sortedIntersections);

            // Assign Ids to Intersections based on their corresponding node
            foreach(var inter in sortedIntersections.myIntersections)
            {
                if (IntersectionNodes.Count != 0)
                {
                    foreach(var ptPair in IntersectionNodes)
                    {
                        if (inter.ComparePoints(ptPair.Value, tolerance))
                        {
                            inter.Id = ptPair.Key;
                            break;
                        }
                    }
                    if (inter.Id == null)
                    {
                        inter.Id = Guid.NewGuid();
                        IntersectionNodes.Add(inter.Id, inter.pA);
                    }
                }
            }
        }

        public void ProcessIntersections()
        {
            // We can now get all of the intersections by node
        }

        #endregion
    }

    public class SortedIntersections
    {
        #region Members
        public readonly KeyValuePair<Guid,Curve> myCurveData;
        public readonly List<FryIntersection> myIntersections;
        private double tolerance;
        bool hasZero;
        bool hasOne;
        #endregion

        #region CTORS

        public SortedIntersections(KeyValuePair<Guid, Curve> curveData, double tolerance = 0.001)
        {
            this.myCurveData = curveData;
            this.tolerance = 0.001;
        }

        #endregion

        #region Methods

        public void AddIntersections(KeyValuePair<Guid, Curve> curveBData, CurveIntersections intersections)
        {
            foreach(var inter in intersections)
            {
                var fryIntersection = new FryIntersection(inter, myCurveData, curveBData);
                this.myIntersections.Add(fryIntersection);
            }
        }

        public void SortIntersections()
        {
            foreach(var fryInter in myIntersections)
            {
                // Arrange intersection points along curve by param
                myIntersections.Sort((int1, int2) => int1.tA.CompareTo(int2.tA));
                myIntersections.Reverse();

                // Add flags if intersections include the endpoints of the curve, otherwise these will
                // need to be generated in the lattice.
                hasZero = false;
                hasOne = false;

                foreach (var inter in myIntersections)
                {
                    if (inter.tA == 0)
                    {
                        hasZero = true;
                    }
                    if (inter.tA == 1)
                    {
                        hasOne = true;
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Stores data for each intersection and helps with calculating geometry.
    /// </summary>
    public class FryIntersection
    {
        #region Members
        private Guid id;
        public readonly Guid CurveA_Id;
        public readonly Guid CurveB_Id;
        public readonly Vector3d vecA;
        public readonly Vector3d vecB;
        public readonly Point3d pA;
        public readonly Point3d pB;
        public readonly double tA;
        public readonly double tB;

        private readonly List<Guid> curves;
        private readonly List<Vector3d> tangents;
        private readonly List<Point3d> points;
        private readonly List<double> crvParams;
        #endregion

        #region CTORS

        public FryIntersection(IntersectionEvent intEvent, KeyValuePair<Guid, Curve> curveAData, KeyValuePair<Guid, Curve> curveBData)
        {
            this.pA = intEvent.PointA;
            this.pB = intEvent.PointB;
            this.CurveA_Id = curveAData.Key;
            this.CurveB_Id = curveBData.Key;
            this.tA = intEvent.ParameterA;
            this.tB = intEvent.ParameterB;
            // Get tangent vectors from curves
            vecA = curveAData.Value.TangentAt(intEvent.ParameterA);
            vecB = curveBData.Value.TangentAt(intEvent.ParameterB);
        }

        public virtual Guid Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// True if points overlap within tolerance
        /// </summary>
        public bool ComparePoints(Point3d testPt, double tolerance)
        {
            if (pA.X < testPt.X + tolerance && pA.X > testPt.X - tolerance)
            {
                if (pA.Y < testPt.Y + tolerance && pA.Y > testPt.Y - tolerance)
                {
                    if (pA.Z < testPt.Z + tolerance && pA.Z > testPt.Z - tolerance)
                    {
                        return true;
                    }
                }
            }
            if (pB.X < testPt.X + tolerance && pB.X > testPt.X - tolerance)
            {
                if (pB.Y < testPt.Y + tolerance && pB.Y > testPt.Y - tolerance)
                {
                    if (pB.Z < testPt.Z + tolerance && pB.Z > testPt.Z - tolerance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
    }
}
