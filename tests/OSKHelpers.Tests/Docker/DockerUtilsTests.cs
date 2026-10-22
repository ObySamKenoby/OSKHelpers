using System;
using System.IO;
using OSKHelpers.Docker;

namespace OSKHelpers.Tests.Docker
{
    /// <summary>
    /// Tests for the <see cref="DockerUtils"/> class.<br/>
    /// The tests pass the path of the file to check through the Debug-only parameter of CheckIsDockerized.
    /// </summary>
    [TestClass]
    public class DockerUtilsTests
    {
#if DEBUG

        #region CheckIsDockerized

        /// <summary>
        /// Verifies that an existing container marker file is detected.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-039")]
        public void CheckIsDockerized_MarkerFileExists_ReturnsTrue()
        {
            var markerFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dockerenv");
            File.WriteAllText(markerFile, string.Empty);
            try
            {
                Assert.IsTrue(DockerUtils.CheckIsDockerized(markerFile));
            }
            finally
            {
                File.Delete(markerFile);
            }
        }

        /// <summary>
        /// Verifies that a missing container marker file is not detected.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-039")]
        public void CheckIsDockerized_MarkerFileMissing_ReturnsFalse()
        {
            var markerFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dockerenv");

            Assert.IsFalse(DockerUtils.CheckIsDockerized(markerFile));
        }

        #endregion

#endif

        #region IsDockerized

        /// <summary>
        /// Verifies that IsDockerized matches the check on the default path.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-039")]
        public void IsDockerized_Startup_MatchesCheckOnDefaultPath()
        {
            Assert.AreEqual(DockerUtils.CheckIsDockerized(), DockerUtils.IsDockerized);
        }

        #endregion
    }
}
