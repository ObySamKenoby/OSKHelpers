using OSKHelpers.Common;
using System;
using System.Collections.Generic;

namespace OSKHelpers.Tests.Interactive.Common
{
    public class ObjectUtilsTests
    {
        #region Classi di supporto

        private class Persona
        {
            public string Nome { get; set; }
            public int Età { get; set; }
            public DateTime DataNascita { get; set; }
        }

        private class Ordine
        {
            public int Id { get; set; }
            public decimal Importo { get; set; }
            public DateTime DataOrdine { get; set; }
        }

        private class Prodotto
        {
            public string Nome { get; set; }
            public double Prezzo { get; set; }
            public bool Disponibile { get; set; }
        }

        #endregion

        #region Metodi

        public static void TestDump()
        {
            // Creazione di una lista di oggetti di test
            List<object> oggettiDiTest = new List<object>
            {
                new Persona { Nome = "Samuele", Età = 30, DataNascita = new DateTime(1995, 5, 27) },
                new Ordine { Id = 1, Importo = 150.75m, DataOrdine = DateTime.UtcNow },
                new Prodotto { Nome = "Laptop", Prezzo = 999.99, Disponibile = true },
                "Test stringa",
                42,
                3.14,
                DateTime.UtcNow,
                new DateTimeOffset(DateTime.UtcNow),
                Guid.NewGuid(),
                new List<int> { 1, 2, 3, 4, 5 } // Test su una collezione
            };

            string separatore = ";";

            // Esecuzione del test
            Console.WriteLine("Test CSVLogItem:");
            foreach (var obj in oggettiDiTest)
            {
                Console.WriteLine();
                ObjectUtils.Dump(obj);
            }
        }

        #endregion
    }
}
