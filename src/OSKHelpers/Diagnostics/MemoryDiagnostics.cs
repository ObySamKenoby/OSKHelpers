using OSKHelpers.Logging;
using System;

namespace OSKHelpers.Diagnostics
{
    /// <summary>
    /// Class containing utility methods for memory usage diagnostics during program execution.
    /// </summary>
    public class MemoryDiagnostics
    {

        #region Properties

        /// <summary>
        /// If True, the methods will actually perform diagnostics; otherwise, they will be inert.
        /// </summary>
        public static bool ActivateDiagnostics {  get; set; }

        #endregion

        #region Constructor

        static MemoryDiagnostics()
        {
            ActivateDiagnostics = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Measures the memory usage for the <paramref name="action"/> method, writing the result to the standard output of <see cref="SimpleLog"/>.<br/>
        /// The method uses the Garbage Collector to check memory usage, introducing a performance decay; its use<br/>
        /// in production is not recommended except for diagnostic purposes.<br/>
        /// <b>The measurement is not precise (using the GC means it may also be affected by noise), but it can be considered<br/>
        /// a reliable estimate for focused operations</b>.<br/>
        /// If <see cref="ActivateDiagnostics"/> is False, it simply executes <paramref name="action"/> and returns 0.<br/>
        /// <br/>
        /// <example>
        /// <b>Example</b>:<br/>
        /// <i>Note</i>: <i>list</i> must be initialized outside the called method to prevent the GC from destroying it before returning<br/>
        /// control to the calling method.<br/>
        /// <b>This method is mainly intended to check memory usage for an object, so it is useful to use it during the object's initialization phase.</b><br/>
        /// <br/>
        /// <code>
        /// 
        /// var list = new List&lt;int&gt;();
        /// var b = MemoryDiagnostics.MeasureMemoryUsage(Test);
        /// Console.WriteLine($"Estimated usage: {b} bytes ({b/1024} kb).");
        /// 
        /// void Test(List&lt;int&gt; list)
        /// {
        ///     for (int i = 0; i &lt; 1_000_000; i++)
        ///         list.Add(i);
        /// }
        /// 
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="action">Method to execute.</param>
        /// <param name="message">Message to display, will always be followed by ' {dim} bytes.</param>
        /// <returns>
        /// The amount of memory (in bytes) used by the method.
        /// </returns>
        public static long MeasureMemoryUsage(Action action, string message = null)
        {
            long usage = 0;

            if (ActivateDiagnostics)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long before = GC.GetTotalMemory(true);
                action();
                long after = GC.GetTotalMemory(true);
                usage = after - before;
                SimpleLog.Write($"{(!string.IsNullOrWhiteSpace(message) ? message : "Utilizzo memoria")} {usage:#,##0} bytes ({usage/1024:#,##0} kb).");
            }
            else
            {
                action();
            }

            return usage;
        }

        #endregion


    }
}
