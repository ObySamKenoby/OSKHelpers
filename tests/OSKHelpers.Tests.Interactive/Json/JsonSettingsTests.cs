using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSKHelpers.Common;
using OSKHelpers.Json;

namespace OSKHelpers.Tests.Interactive.Json
{
    public class JsonSettingsTests
    {
        class FakeJSonSettings : JsonSettings<FakeJSonSettings>
        {
            public string String1 { get; set; }
            public string String2 { get; set; }
            public string String3 { get; set; }
            public int Int1 { get; set; }
            public int Int2 { get; set; }
            public int Int3 { get; set; }

            public override bool CheckIsValid()
            {
                IsValid = true;
                return true;
            }

            public override void Init()
            {
                
            }
        }

        public static void TestJsonSettings()
        {
            File.Delete(Path.Combine(Paths.DefaultConfigsDirectory, FakeJSonSettings.DEFAULTFILENAME));
            var res = JsonSettings<FakeJSonSettings>.Load();
            var settings = res.Object;
            Console.WriteLine($"IsNew => {res.IsNew}");
            Console.WriteLine($"Settings:\n{JsonUtils.Serialize(settings)}");
            Console.WriteLine("Premere un taso per continuare il test...");
            Console.ReadKey();
            Console.WriteLine("Aggiornamento valori di settings...");
            settings.String1 = nameof(FakeJSonSettings.String1);
            settings.String2 = nameof(FakeJSonSettings.String2);
            settings.String3 = nameof(FakeJSonSettings.String3);
            settings.Int1 = 1;
            settings.Int2 = 2;
            settings.Int3 = 3;
            Console.WriteLine("Salvataggio file...");
            settings.Save();
            Console.WriteLine($"IsNew => {res.IsNew}");
            Console.WriteLine($"Settings:\n{JsonUtils.Serialize(settings)}");
            Console.WriteLine("Premere un taso per uscire dal test...");


        }
    }
}
