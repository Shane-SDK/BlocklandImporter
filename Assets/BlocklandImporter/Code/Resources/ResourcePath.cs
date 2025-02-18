using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blockland.Resources
{
    public struct ResourcePath
    {
        public ResourcePath(string path)
        {
            this.path = path.ToLower();
        }
        public string path;
    }
}
