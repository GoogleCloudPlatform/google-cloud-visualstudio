using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AspnetTool
{
    public static class Utils
    {
        private static readonly Lazy<Dictionary<string, Func<ICommand>>> s_commands = new Lazy<Dictionary<string, Func<ICommand>>>(ScanCommands);

        public static Func<ICommand> GetCommandFactory(string name)
        {
            Func<ICommand> result = null;
            s_commands.Value.TryGetValue(name, out result);
            return result;
        }

        public static string GetCommandName(Type cmd)
        {
            var attribute = cmd.GetCustomAttribute<CommandNameAttribute>(true);
            if (attribute != null)
            {
                return attribute.Name;
            }
            return cmd.Name.ToLower();
        }

        private static Dictionary<string, Func<ICommand>> ScanCommands()
        {
            var types = Assembly.GetCallingAssembly()
                .GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ICommand)));

            var result = new Dictionary<string, Func<ICommand>>();
            foreach (var t in types)
            {
                result.Add(GetCommandName(t), () => (ICommand)Activator.CreateInstance(t));
            }
            return result;
        }
    }
}
