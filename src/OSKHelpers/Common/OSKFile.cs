using System;
using System.Diagnostics;
using System.IO;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilities for opening and managing external files.
    /// </summary>
    public class OSKFile
    {
        /// <summary>
        /// Opens the specified file using the OS shell.
        /// </summary>
        /// <param name="filename">Name of the file to open; may be relative or absolute.</param>
        public static void Open(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("The file name must be valid and cannot be null or empty.");
            }
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"Cannot find file {filename}.");
            }

            Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
        }
    }
}
