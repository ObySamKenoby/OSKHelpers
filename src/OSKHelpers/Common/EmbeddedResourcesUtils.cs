using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilities to simplify the use of embedded resources.
    /// </summary>
    public class EmbeddedResourcesUtils
    {
        /// <summary>
        /// Extracts the embedded resource named <paramref name="fileName"/> and saves it as <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="fileName">Name of the file to retrieve from the embedded resources.</param>
        /// <param name="outputPath">Full output path (including file name).</param>
        /// <param name="caseSensitive">When true, the resource name search is case-sensitive.</param>
        /// <exception cref="ArgumentNullException">The file name or the output path is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">The output directory does not exist.</exception>
        /// <exception cref="InvalidOperationException">No embedded resource matches <paramref name="fileName"/>.</exception>
        public static void ExtractEmbeddedFile(string fileName, string outputPath, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }
            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            {
                throw new DirectoryNotFoundException(Path.GetDirectoryName(outputPath));
            }

            var asm = Assembly.GetCallingAssembly();
            var resourceName = asm
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new InvalidOperationException($"Resource not found: {fileName}");
            }

            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Resource not found: {resourceName}");
                }

                using (var file = File.Create(outputPath))
                {
                    stream.CopyTo(file);
                }
            }
        }
    }
}
