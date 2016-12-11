using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    public class Payload
    {
        public Payload(string name, object value)
        {
            Name = name;
            Value = value;

        }

        public string Name { get; }
        public object Value { get; }
    }
}
