using OSKHelpers.Common;
using OSKHelpers.Tests.Interactive.Common;
using OSKHelpers.Tests.Interactive.Json;
using OSKHelpers.Tests.Interactive.Logging;
using System;

namespace OSKHelpers.Tests.Interactive
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Paths.UseLocalAppdataDirectory();

            var key = string.Empty;

            while (key != "X")
            {
                var pause = true;
                Console.Clear();
                Console.WriteLine("1 - Test CSVLogItem");
                Console.WriteLine("2 - Test ObjectUtils");
                Console.WriteLine("3 - Test JsonSettings");
                Console.WriteLine("X - Uscita");
                Console.WriteLine();
                key = Console.ReadKey().KeyChar.ToString().ToUpper();
                switch(key)
                {
                    case "1":
                        CSVLogItemTests.TestCsvLogItem();
                        break;
                    case "2":
                        ObjectUtilsTests.TestDump();
                        break;
                    case "3":
                        JsonSettingsTests.TestJsonSettings();
                        break;
                    default:
                        pause = false;
                        break;
                }
                if (pause)
                {
                    Console.WriteLine();
                    Console.WriteLine("Premere un taso per continuare");
                    Console.ReadKey();
                }

            }
            
        }
    }
}
