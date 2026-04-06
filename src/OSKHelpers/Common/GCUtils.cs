using System;
using System.Diagnostics;
using System.Timers;

namespace OSKHelpers.Common
{
    
    /// <summary>
    /// Utilità relative alla configurazione e l'utilizzo del Garbage Collector.
    /// </summary>
    public class GCUtils
    {
        #region Metodi

    #if NET8_0_OR_GREATER

        /// <summary>
        /// Imposta il valore massimo heap del Garbage Collector.<br/>
        /// https://learn.microsoft.com/it-it/dotnet/api/system.gc.refreshmemorylimit?view=net-8.0#system-gc-refreshmemorylimit
        /// </summary>
        /// <param name="maxHeapSize">Dimensione massima dellheap espressa in MB.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void SetHeapHardLimit(int maxHeapSize)
        {
            if (maxHeapSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHeapSize), $"Il valore di {nameof(maxHeapSize)} deve essere maggiore di zero.");
            }
            AppContext.SetData("GCHeapHardLimit", (ulong)500 << 20);
            GC.RefreshMemoryLimit();
        }

        /// <summary>
        /// Forza l'esecuzione della pulizia da parte del Garbage collector.<br/>
        /// Se <paramref name="complete"/> è false si limita a richiamare <see cref="GC.Collect()"/>,<br/>
        /// altrimenti esegue una pulizia completa.<br/>
        /// In linea di massima è preferibile non intervenire sul funzionaamnento del Garbage Collector,<br/>
        /// se si rendesse necessario prestare la massima attenzione prima di utilizzare un valore<br/>
        /// true per <paramref name="complete"/>.
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
        /// Forza l'esecuzione della pulizia da parte del Garbage Collector.<br/>
        /// E' a tutti gli effetti un alias per <see cref="GC.Collect()"/>.
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
