using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilità per semplificare l'utilizzo delle Embedded Resources.
    /// </summary>
    public class EmbeddedResourcesUtils
    {
        /// <summary>
        /// Estrae il file <paramref name="fileName"/> dalle risorse incorporate e lo salva come <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="fileName">Nome del file da recuperare dalle risorse incorporate.</param>
        /// <param name="outputPath">Percorso completo (comprensivo del nome) del file da salvare.</param>
        /// <param name="caseSensitive">Se True la ricerca del nome della risorsa sarà case sensitive.</param>
        /// <exception cref="InvalidOperationException" />
        public static void ExtractEmbeddedFile(string fileName, string outputPath, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(fileName);
            }
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentNullException(outputPath);
            }
            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            {
                throw new DirectoryNotFoundException(Path.GetDirectoryName(outputPath));
            }

            var asm = Assembly.GetCallingAssembly();
            var resourceName = asm
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(resourceName))
            {
                using (var stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException($"Risorsa non trovata: {resourceName}");
                    }

                    using (var file = File.Create(outputPath))
                    {
                        stream.CopyTo(file);
                    }
                }
            }
        }
    }
}
