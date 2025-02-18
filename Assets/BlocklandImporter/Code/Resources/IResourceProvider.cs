using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blockland.Resources
{
    public interface IResourceProvider
    {
        public bool TryGetResource<T>(ResourcePath resourceName, out T resource) where T : IResource;
        public void Dispose() { }
    }
}
