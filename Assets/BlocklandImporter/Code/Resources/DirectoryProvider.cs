using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blockland.Resources
{
    public class DirectoryProvider : IResourceProvider
    {
        public string rootPath;
        public DirectoryProvider(string rootPath)
        {
            this.rootPath = rootPath;
        }
        public bool TryGetResource<T>(ResourcePath resourceName, out T resource) where T : IResource
        {
            resource = default;
            string path = System.IO.Directory.EnumerateFiles(rootPath, resourceName.path, System.IO.SearchOption.AllDirectories).FirstOrDefault();

            if (string.IsNullOrEmpty(path))
                return false;

            System.IO.FileStream stream = System.IO.File.OpenRead(path);
            System.IO.StreamReader sr = new System.IO.StreamReader(stream);
            ResourceFactory.CreateResource(sr, ResourceType.Brick, out resource);

            return true;
        }
    }
}
