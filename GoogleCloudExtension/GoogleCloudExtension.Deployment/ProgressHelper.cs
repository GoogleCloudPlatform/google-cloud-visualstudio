using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    internal static class ProgressHelper
    {
        internal static async Task<T> UpdateProgress<T>(
            Task<T> deployTask,
            IProgress<double> progress,
            double from, double to)
        {
            double current = from;
            while (true)
            {
                progress.Report(current);

                var resultTask = await Task.WhenAny(deployTask, Task.Delay(5000));
                if (resultTask == deployTask)
                {
                    return await deployTask;
                }

                current += 0.025;
                current = Math.Min(current, to);
            }
        }
    }
}
