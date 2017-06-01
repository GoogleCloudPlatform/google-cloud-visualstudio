using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    public class GcsMoveOperation : Model, IGcsFileOperationCallback
    {
        private readonly GcsItemRef _from;
        private readonly GcsItemRef _to;

        public GcsMoveOperation(GcsItemRef from, GcsItemRef to)
        {
            _from = from;
            _to = to;
        }

        #region IGcsFileOperationCallback implementation.

        void IGcsFileOperationCallback.Cancelled()
        {
            throw new NotImplementedException();
        }

        void IGcsFileOperationCallback.Completed()
        {
            throw new NotImplementedException();
        }

        void IGcsFileOperationCallback.Error(DataSourceException ex)
        {
            throw new NotImplementedException();
        }

        void IGcsFileOperationCallback.Progress(double value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
