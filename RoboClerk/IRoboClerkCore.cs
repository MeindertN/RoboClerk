using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk
{
    internal interface IRoboClerkCore
    {
        void GenerateDocs();
        void SaveDocumentsToDisk();
    }
}
