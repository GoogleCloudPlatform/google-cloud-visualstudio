using System;
using System.Linq;

namespace EchoApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                string flag = args[0];
                string message = string.Join(" ", args.Skip(1));
                if (flag.Equals("-null", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
                else if (flag.Equals("-out", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write(message);
                    return 0;
                }
                else if (flag.Equals("-err", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.Write(message);
                    return 0;
                }
                else if (flag.Equals("-exp", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write(message);
                    return 1;
                }
            }
            return -1;
        }
    }
}
