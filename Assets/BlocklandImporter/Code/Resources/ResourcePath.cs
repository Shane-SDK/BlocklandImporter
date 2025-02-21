using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blockland.Resources
{
    public struct ResourcePath
    {
        public string AssetDatabasePath
        {
            get
            {
                return $"Assets/BlocklandImporter/Assets/{path}";
            }
        }
        public ResourcePath(string blocklandPath)
        {
            this.path = blocklandPath.ToLower();
        }
        public string path;
    }
}
