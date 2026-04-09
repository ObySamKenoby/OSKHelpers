using System;
using System.Diagnostics;
using System.Timers;

namespace OSKHelpers.Common
{
    
    /// <summary>
    /// Utilities related to the configuration and use of the Garbage Collector.
    /// </summary>
    public class GCUtils
    {
        #region Methods

    #if NET8_0_OR_GREATER

        /// <summary>
        /// Sets the maximum GC heap size.<br/>
        /// https://learn.microsoft.com/en-us/dotnet/api/system.gc.refreshmemorylimit?view=net-8.0
        /// </summary>
        /// <param name="maxHeapSize">Maximum heap size in MB.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void SetHeapHardLimit(int maxHeapSize)
        {
            if (maxHeapSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHeapSize), $"The value of {nameof(maxHeapSize)} must be greater than zero.");
            }
            AppContext.SetData("GCHeapHardLimit", (ulong)500 << 20);
            GC.RefreshMemoryLimit();
        }

        /// <summary>
        /// Forces the Garbage Collector to run.<br/>
        /// When <paramref name="complete"/> is false, simply calls <see cref="GC.Collect()"/>;<br/>
        /// otherwise performs an aggressive full collection.<br/>
        /// In general it is preferable not to interfere with GC behaviour; if you must,
        /// pay careful attention before passing true for <paramref name="complete"/>.
        /// </summary>
        /// <param name="complete"></param>
        public static void Force(bool complete = false)
        {
            if (complete)
            {
                GC.Collect(2, GCCollectionMode.Aggressive);
                GC.WaitForFullGCComplete();
            }
            else
            {
                GC.Collect();
            }
        }

#else

        /// <summary>
        /// Forces the Garbage Collector to run.<br/>
        /// This is effectively an alias for <see cref="GC.Collect()"/>.
        /// </summary>
        public static void Force()
        {
            GC.Collect();
        }

#endif


        /// <summary>
        /// Returns a timer that can be used to force the Garbage Collector when the heap reaches a given size.<br/>
        /// The timer already has the cleanup function preset and <see cref="Timer.AutoReset"/> set to true; it is the
        /// responsibility of the calling class to start and stop it.
        /// </summary>
        /// <param name="interval">
        /// Interval in milliseconds for the check.<br/>
        /// The minimum value is 1000 (one second); however the overhead of checking the heap and running the GC
        /// must be factored in to avoid a performance penalty.
        /// </param>
        /// <param name="triggerSize">Heap size in MB at which <see cref="GC.Collect()"/> will be called; minimum value is 1.</param>
        /// <returns>The timer to use for the Garbage Collector check.</returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static Timer GetCheckTimer(double interval, int triggerSize)
        {
            if (interval < 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "The minimum allowed value is 1000 (one second).");
            }
            if (triggerSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(triggerSize), triggerSize, "The minimum allowed value is 1.");
            }

            var gcTimer = new Timer();
            long size = triggerSize << 10; // triggerSize is converted to bytes.

            gcTimer.Interval = interval;
            gcTimer.AutoReset = true;
            gcTimer.Elapsed += (s, e) =>
            {
                var usedMemory = Process.GetCurrentProcess().PagedMemorySize64;
                if (usedMemory > size)
                {
                    GC.Collect();
                }
            };

            return gcTimer;
        }


#endregion
    }
    
}
