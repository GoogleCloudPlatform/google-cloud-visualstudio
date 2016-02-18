using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspnetTool.Commands
{
    public class ListCmd : ICommand
    {
        public class Options : ICommandOptions
        {
            public ICommand CreateCommand()
            {
                return new ListCmd(this);
            }
        }

        private readonly Options _options;

        public ListCmd(Options options)
        {
            _options = options;
        }

        public int Execute()
        {
            throw new NotImplementedException();
        }
    }
}
