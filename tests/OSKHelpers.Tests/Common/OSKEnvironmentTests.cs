using System;
using System.Runtime.InteropServices;
using OSKHelpers.Common;
using OSKHelpers.Docker;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="OSKEnvironment"/> class.
    /// </summary>
    [TestClass]
    public class OSKEnvironmentTests
    {
        /// <summary>
        /// Verifies that at least one of the platform properties is true.
        /// </summary>
        [TestMethod]
        public void PlatformProperties_CurrentPlatform_AtLeastOneIsTrue()
        {
            var result = OSKEnvironment.IsWindows || OSKEnvironment.IsMacOS || OSKEnvironment.IsLinux;

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies consistency with RuntimeInformation for Windows.
        /// </summary>
        [TestMethod]
        public void IsWindows_CurrentPlatform_MatchesRuntimeInformation()
        {
            var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            Assert.AreEqual(expected, OSKEnvironment.IsWindows);
        }

        /// <summary>
        /// Verifies consistency with RuntimeInformation for Linux.
        /// </summary>
        [TestMethod]
        public void IsLinux_CurrentPlatform_MatchesRuntimeInformation()
        {
            var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            Assert.AreEqual(expected, OSKEnvironment.IsLinux);
        }

        /// <summary>
        /// Verifies consistency with RuntimeInformation for macOS.
        /// </summary>
        [TestMethod]
        public void IsMacOS_CurrentPlatform_MatchesRuntimeInformation()
        {
            var expected = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            Assert.AreEqual(expected, OSKEnvironment.IsMacOS);
        }

        /// <summary>
        /// Verifies that IsDockerized is consistent with DockerUtils.
        /// </summary>
        [TestMethod]
        public void IsDockerized_CurrentEnvironment_MatchesDockerUtils()
        {
            Assert.AreEqual(DockerUtils.IsDockerized, OSKEnvironment.IsDockerized);
        }
    }
}
