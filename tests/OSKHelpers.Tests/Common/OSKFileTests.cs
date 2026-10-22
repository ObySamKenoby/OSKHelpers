using System;
using System.IO;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="OSKFile"/> class.<br/>
    /// Opening an existing file starts a process through the operating system shell and is not tested.
    /// </summary>
    [TestClass]
    public class OSKFileTests
    {
        #region Open

        /// <summary>
        /// Verifies that an empty file name throws ArgumentException.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Open_EmptyFileName_ThrowsArgumentException(string filename)
        {
            Assert.Throws<ArgumentException>(() => OSKFile.Open(filename));
        }

        /// <summary>
        /// Verifies that a missing file throws FileNotFoundException.
        /// </summary>
        [TestMethod]
        public void Open_MissingFile_ThrowsFileNotFoundException()
        {
            var filename = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");

            Assert.Throws<FileNotFoundException>(() => OSKFile.Open(filename));
        }

        #endregion
    }
}
