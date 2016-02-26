using System;

namespace AspnetTool
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CommandNameAttribute : Attribute
    {
        public string Name { get; }

        public CommandNameAttribute(string name)
        {
            Name = name;
        }
    }
}
