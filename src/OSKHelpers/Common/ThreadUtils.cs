using System;
using System.Threading.Tasks;
using System.Threading;
using OSKHelpers.Logging;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilities for running tasks on dedicated threads with built-in error handling.
    /// </summary>
    public class ThreadUtils
    {
        /// <summary>
        /// Executes the given task on a separate thread, wrapped in a try/catch block.
        /// </summary>
        /// <param name="task">Task to execute.</param>
        public static void ExecuteTaskAsync(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }
            var t = new Thread(async () =>
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    SimpleLog.LogError(ex);
                }
            });
            t.Start();
        }
    }
}
