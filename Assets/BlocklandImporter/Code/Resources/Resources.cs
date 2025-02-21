using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blockland.Resources
{
    public class Resources
    {
        public IReadOnlyList<IResourceProvider> ResourceProviders => resourceProviders;
        Dictionary<ResourcePath, IResource> resourceCache;
        List<IResourceProvider> resourceProviders;
        public readonly string gameDirectory = "D:/SteamLibrary/steamapps/common/Blockland";
        public Resources()
        {
            resourceProviders = new List<IResourceProvider> { new DirectoryProvider(gameDirectory) };
            resourceCache = new();
        }
        public bool GetResource<T>(ResourcePath path, out T resource) where T : IResource
        {
            UnityEngine.Profiling.Profiler.BeginSample("GetResource");

            if (resourceCache.TryGetValue(path, out IResource iresource) && iresource is T)
            {
                resource = (T)iresource;
                UnityEngine.Profiling.Profiler.EndSample();
                return true;
            }

            foreach (IResourceProvider provider in resourceProviders)
            {
                if (provider.TryGetResource<T>(path, out resource))
                {
                    resourceCache[path] = resource;
                    UnityEngine.Profiling.Profiler.EndSample();
                    return true;
                }
            }

            resource = default;
            UnityEngine.Profiling.Profiler.EndSample();
            return false;
        }
        public bool LoadBrickData(ResourcePath path, out Objects.BrickData resource)
        {
#if UNITY_EDITOR
            resource = UnityEditor.AssetDatabase.LoadAssetAtPath<Objects.BrickData>(path.AssetDatabasePath);
            return resource != null;
#endif

            resource = null;
            return false;
        }
    }
}
