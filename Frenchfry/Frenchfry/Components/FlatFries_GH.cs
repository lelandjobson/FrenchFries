using System;
using System.Collections.Generic;
using Frenchfry;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Frenchfry.Properties;

namespace Frenchfry.Components
{
    public class FlatFries_GH : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public FlatFries_GH()
          : base("Flat Fries", "FlatFries",
              "Create a mesh lattice from an arbitrarily organized set of a curves on the plane",
              "FrenchFries", "Curve Toolbox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Unfried Curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Thickness", "t", "Fry Thickness", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh ", "M", "Fried Mesh", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_Curve> fryCurvesGH = new List<GH_Curve>();
            List<GH_Number> thicknessGH = new List<GH_Number>();

            if (!DA.GetDataList(0, fryCurvesGH)) { return; }
            if (!DA.GetDataList(1, thicknessGH)) { return; }

            // Get Rhino Readible Values Out
            var fryCurves = new List<Curve>();
            foreach(var fry in fryCurvesGH)
            {
                fryCurves.Add(fry.Value);
            }
            double thickness = thicknessGH[0].Value;

            // Spin up an instance of FryCuves
            var ff = new FlatFries(fryCurves, thickness);

            // Get Fried Mesh
            DA.SetData(0, ff.FriedMeshes);

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Resources.FirstIcon;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5F60602A-3F11-4D05-B02A-51E08F22873F"); }
        }
    }
}
