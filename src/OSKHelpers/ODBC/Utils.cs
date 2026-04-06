using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OSKHelpers.ODBC
{
    public class Utils
    {
        public static List<string> GetODBCSources()
        {
            var sources = new List<string>();

            // L'utilizzo del registro è consentito esclusivamente in ambiente Windows
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

        private static string FormatInnerException(Exception ex) => ex?.InnerException != null ? "  InnerException: " + ex.InnerException.GetType() + "\n  " + ex.InnerException.Message + "\n  " + ex.InnerException.Message + "\n" : "";

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
