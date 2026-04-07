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
        /// Restituisce un timer da poter utilizzare per forzare l'intervento del Garbage Collector al raggiungimento<br/>
        /// di una certa dimensione dell'heap.<br/>
        /// Il timer avrà già preimpostata la funzione di pulizia e <see cref="Timer.AutoReset"/> a true, sarà responsabilità<br/>
        /// della classe chiamante occuparsi dell'avvio e dell'arresto.
        /// </summary>
        /// <param name="interval">
        /// Intervallo in millisecondi per effettuar e la verifica.<br/>
        /// Il valore minimo è 1000 (un secondo), tuttavia è necessario tenere in considerazione il peso relativo alla verifica<br/>
        /// dell'heap e all'esecuzione della pulizia da parte del Garbage Collector onde evitar eun decadimento delle prestazioni.
        /// </param>
        /// <param name="triggerSize">Dimensioni (espresse in MB) che comporteranno il richiamo a <see cref="GC.Collect()"/>, il valore minimo è 1.</param>
        /// <returns>Il timer da utilizzare per la verifica del Garbage Collector.</returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static Timer GetCheckTimer(double interval, int triggerSize)
        {
            if (interval < 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Il minimo valore consentito è 1000 (un secondo).");
            }
            if (triggerSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(triggerSize), triggerSize, "Il minimo valore consentito è 1.");
            }

            var gcTimer = new Timer();
            long size = triggerSize << 10; // triggerSize viene convertito in bytes.

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
