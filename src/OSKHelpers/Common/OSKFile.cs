using System;
using System.Diagnostics;
using System.IO;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilità varie per lapertura e la gestione di file esterni.
    /// </summary>
    public class OSKFile
    {
        /// <summary>
        /// Apre il file passato come parametro.
        /// </summary>
        /// <param name="filename">Nome del file da aprire, può essere relativo od assoluto</param>
        public static void Open(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Il nome del file deve essere valido non può essere nullo o vuoto.");
            }
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"Impossibile trovare il file {filename}.");
            }

            Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
        }
    }
}
