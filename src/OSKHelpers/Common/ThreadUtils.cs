using System;
using System.Threading.Tasks;
using System.Threading;
using OSKHelpers.Logging;

namespace OSKHelpers.Common
{
    public class ThreadUtils
    {
        /// <summary>
        /// Esegue il Task passato come parametro all'interno di un Thread separato incapsulandolo all'interno di un blocco try...catch.
        /// </summary>
        /// <param name="task">Task da eseguire.</param>
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
