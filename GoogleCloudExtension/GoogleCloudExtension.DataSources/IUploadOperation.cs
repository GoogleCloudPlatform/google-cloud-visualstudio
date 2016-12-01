using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public interface IUploadOperation
    {
        void Progress(double value);

        void Completed();

        void Cancelled();

        void Error(DataSourceException ex);
    }
}
