using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OSKHelpers.ODBC
{
    /// <summary>
    /// Utilities for ODBC data-source enumeration and connection-string building (Windows only).
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Returns the list of system and 32-bit ODBC data-source names read from the Windows Registry.<br/>
        /// Returns an empty list on non-Windows platforms.
        /// </summary>
        /// <returns>List of ODBC data-source names.</returns>
        public static List<string> GetODBCSources()
        {
            var sources = new List<string>();

            // Registry access is only allowed on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    RegistryKey reg = Registry.LocalMachine
                        .OpenSubKey("Software")?
                        .OpenSubKey("ODBC")?
                        .OpenSubKey("ODBC.INI")?
                        .OpenSubKey("ODBC Data Sources");
                    if (reg != null)
                    {
                        sources.AddRange(reg.GetValueNames());
                        reg.Close();
                    }
                    reg = Registry.LocalMachine
                        .OpenSubKey("Software")?
                        .OpenSubKey("WOW6432Node")?
                        .OpenSubKey("ODBC")?
                        .OpenSubKey("ODBC.INI")?
                        .OpenSubKey("ODBC Data Sources");
                    if (reg != null)
                    {
                        sources.AddRange(reg.GetValueNames());
                        reg.Close();
                    }
                }
                catch (Exception ex)
                {
                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine($"Eccezione in GetODBCSources:\n  {ex.GetType()}\n  {ex.Message}\n{FormatInnerException(ex)} Stack: {ex.StackTrace}");
                    }
                }
            }
            return sources;
        }

        /// <summary>
        /// Formats the inner exception of <paramref name="ex"/> into a readable string.
        /// </summary>
        /// <param name="ex">Exception whose inner exception is to be formatted.</param>
        /// <returns>Formatted string, or an empty string if there is no inner exception.</returns>
        private static string FormatInnerException(Exception ex) => ex?.InnerException != null ? "  InnerException: " + ex.InnerException.GetType() + "\n  " + ex.InnerException.Message + "\n  " + ex.InnerException.Message + "\n" : "";

        /// <summary>
        /// Builds a DSN connection string for the given data-source name.
        /// </summary>
        /// <param name="dsnName">Name of the ODBC data source.</param>
        /// <param name="username">Optional username.</param>
        /// <param name="password">Optional password.</param>
        /// <param name="trustedConnection">When true, adds a trusted-connection clause.</param>
        /// <returns>The DSN connection string, or an empty string if <paramref name="dsnName"/> is blank.</returns>
        public static string GetConnectionString(string dsnName, string username = null, string password = null, bool trustedConnection = false)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(dsnName))
            {
                sb.Append("DSN=").Append(dsnName.Trim()).Append(";");
                if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
                {
                    sb.Append("Uid=").Append(password.Trim()).Append(";Pwd=").Append(password.Trim()).Append(';');
                }
                else if (trustedConnection)
                {
                    sb.Append("Trusted_Connection=Yes;");
                }
            }
            return sb.ToString();
        }

    }
}
