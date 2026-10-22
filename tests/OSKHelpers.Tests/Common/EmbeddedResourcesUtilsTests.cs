using System;
using System.IO;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="EmbeddedResourcesUtils"/> class.<br/>
    /// The resource is searched in the calling assembly, so the tests use Common\EmbeddedResource.txt,
    /// embedded in this test project.
    /// </summary>
    [TestClass]
    public class EmbeddedResourcesUtilsTests
    {
        #region Constants

        private const string RESOURCENAME       = "EmbeddedResource.txt";
        private const string RESOURCECONTENT    = "Embedded resource content used by the EmbeddedResourcesUtils tests.";

        #endregion

        #region Members

        private string _tempDirectory;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Creates a temporary output directory.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        /// <summary>
        /// Removes the temporary output directory.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors during tests.
            }
        }

        #endregion

        #region ExtractEmbeddedFile

        /// <summary>
        /// Verifies that the resource is extracted with its content.
        /// </summary>
        [TestMethod]
        public void ExtractEmbeddedFile_ExistingResource_WritesResourceContent()
        {
            var outputPath = Path.Combine(_tempDirectory, "output.txt");

            EmbeddedResourcesUtils.ExtractEmbeddedFile(RESOURCENAME, outputPath);

            Assert.AreEqual(RESOURCECONTENT, File.ReadAllText(outputPath));
        }

        /// <summary>
        /// Verifies that, by default, the resource name search is case-insensitive.
        /// </summary>
        [TestMethod]
        public void ExtractEmbeddedFile_DifferentCaseNotCaseSensitive_WritesResourceContent()
        {
            var outputPath = Path.Combine(_tempDirectory, "output.txt");

            EmbeddedResourcesUtils.ExtractEmbeddedFile("embeddedresource.TXT", outputPath);

            Assert.AreEqual(RESOURCECONTENT, File.ReadAllText(outputPath));
        }

        /// <summary>
        /// Verifies that, with a case-sensitive search, a name with different case is not found.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-027")]
        public void ExtractEmbeddedFile_DifferentCaseCaseSensitive_ThrowsInvalidOperationException()
        {
            var outputPath = Path.Combine(_tempDirectory, "output.txt");

            Assert.Throws<InvalidOperationException>(() => EmbeddedResourcesUtils.ExtractEmbeddedFile("embeddedresource.TXT", outputPath, true));
            Assert.IsFalse(File.Exists(outputPath));
        }

        /// <summary>
        /// Verifies that a missing resource throws InvalidOperationException (decision 5 of the test plan).
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-027")]
        public void ExtractEmbeddedFile_MissingResource_ThrowsInvalidOperationException()
        {
            var outputPath = Path.Combine(_tempDirectory, "output.txt");

            Assert.Throws<InvalidOperationException>(() => EmbeddedResourcesUtils.ExtractEmbeddedFile("Missing.txt", outputPath));
            Assert.IsFalse(File.Exists(outputPath));
        }

        /// <summary>
        /// Verifies that an empty file name throws ArgumentNullException for the fileName parameter.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        [TestCategory("Intervento-026")]
        public void ExtractEmbeddedFile_EmptyFileName_ThrowsArgumentNullException(string fileName)
        {
            var ex = Assert.Throws<ArgumentNullException>(() => EmbeddedResourcesUtils.ExtractEmbeddedFile(fileName, Path.Combine(_tempDirectory, "output.txt")));

            Assert.AreEqual("fileName", ex.ParamName);
        }

        /// <summary>
        /// Verifies that an empty output path throws ArgumentNullException for the outputPath parameter.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [TestCategory("Intervento-026")]
        public void ExtractEmbeddedFile_EmptyOutputPath_ThrowsArgumentNullException(string outputPath)
        {
            var ex = Assert.Throws<ArgumentNullException>(() => EmbeddedResourcesUtils.ExtractEmbeddedFile(RESOURCENAME, outputPath));

            Assert.AreEqual("outputPath", ex.ParamName);
        }

        /// <summary>
        /// Verifies that a missing output directory throws DirectoryNotFoundException.
        /// </summary>
        [TestMethod]
        public void ExtractEmbeddedFile_MissingDirectory_ThrowsDirectoryNotFoundException()
        {
            var outputPath = Path.Combine(_tempDirectory, "Missing", "output.txt");

            Assert.Throws<DirectoryNotFoundException>(() => EmbeddedResourcesUtils.ExtractEmbeddedFile(RESOURCENAME, outputPath));
        }

        #endregion
    }
}
