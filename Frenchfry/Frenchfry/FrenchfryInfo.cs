using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Frenchfry
{
    public class FrenchfryInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Frenchfry";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("a9ef74f9-b57e-4592-be27-4a3c107e3e0e");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Microsoft";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
