using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspnetTool
{
    public interface ICommand
    {
        int Execute();
    }

    public interface ICommandOptions
    {
        ICommand CreateCommand();
    }
}
