using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsBrowserState
    {
        public IList<GcsItem> Items { get; }

        public IEnumerable<string> PathSteps { get; }

        public string Name => PathSteps.LastOrDefault() ?? "";

        public string CurrentPath
        {
            get
            {
                var path = String.Join("/", PathSteps);
                if (String.IsNullOrEmpty(path))
                {
                    return path;
                }
                return path + "/";
            }
        }

        public GcsBrowserState(IEnumerable<GcsItem> items, string name)
        {
            Items = items.ToList();

            if (String.IsNullOrEmpty(name))
            {
                PathSteps = Enumerable.Empty<string>();
            }
            else
            {
                Debug.Assert(name.Last() == '/');
                PathSteps = name.Substring(0, name.Length - 1).Split('/');
            }
        }
    }
}
