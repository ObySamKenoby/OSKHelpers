using System;
using System.IO;
using OSKHelpers.Diagnostics;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Diagnostics
{
    /// <summary>
    /// Tests for the <see cref="MemoryDiagnostics"/> class.<br/>
    /// The measured value depends on the garbage collector, so only the behaviour and a lower bound are verified.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MemoryDiagnosticsTests
    {
        #region Members

        private string _tempLogPath;
        private bool _originalActivateDiagnostics;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Redirects the default log to a temporary directory.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempLogPath                    = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _originalActivateDiagnostics    = MemoryDiagnostics.ActivateDiagnostics;
            SimpleLog.LogPath               = _tempLogPath;
        }

        /// <summary>
        /// Restores the default log and the diagnostics state.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            SimpleLog.LogPath                       = null;
            MemoryDiagnostics.ActivateDiagnostics   = _originalActivateDiagnostics;
            try
            {
                if (Directory.Exists(_tempLogPath))
                {
                    Directory.Delete(_tempLogPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors during tests.
            }
        }

        #endregion

        #region ActivateDiagnostics

        /// <summary>
        /// Verifies that diagnostics are disabled by default.
        /// </summary>
        [TestMethod]
        public void ActivateDiagnostics_Default_IsFalse()
        {
            Assert.IsFalse(_originalActivateDiagnostics);
        }

        #endregion

        #region MeasureMemoryUsage

        /// <summary>
        /// Verifies that, with diagnostics disabled, the action is executed, 0 is returned and nothing is logged.
        /// </summary>
        [TestMethod]
        public void MeasureMemoryUsage_DiagnosticsDisabled_ExecutesActionAndReturnsZero()
        {
            MemoryDiagnostics.ActivateDiagnostics = false;
            var executed = false;

            var usage = MemoryDiagnostics.MeasureMemoryUsage(() => executed = true, "Measure");

            Assert.IsTrue(executed);
            Assert.AreEqual(0, usage);
            Assert.IsFalse(File.Exists(SimpleLog.LogFile));
        }

        /// <summary>
        /// Verifies that, with diagnostics enabled, the action is executed and the measure is logged with the message.
        /// </summary>
        [TestMethod]
        public void MeasureMemoryUsage_DiagnosticsEnabled_ExecutesActionAndLogsMeasure()
        {
            MemoryDiagnostics.ActivateDiagnostics = true;
            var executed = false;

            MemoryDiagnostics.MeasureMemoryUsage(() => executed = true, "Test measure");

            Assert.IsTrue(executed);
            var content = File.ReadAllText(SimpleLog.LogFile);
            Assert.Contains("Test measure", content);
            Assert.Contains("bytes", content);
        }

        /// <summary>
        /// Verifies that the measure of an allocation kept alive after the action is at least the allocated size.<br/>
        /// The exact value depends on the garbage collector, so only the lower bound is verified.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-039")]
        public void MeasureMemoryUsage_AllocationKeptAlive_ReturnsAtLeastAllocatedSize()
        {
            const int ALLOCATEDSIZE = 10 << 20;

            MemoryDiagnostics.ActivateDiagnostics = true;
            byte[] buffer = null;

            var usage = MemoryDiagnostics.MeasureMemoryUsage(() => buffer = new byte[ALLOCATEDSIZE]);

            GC.KeepAlive(buffer);
            Assert.IsGreaterThanOrEqualTo(ALLOCATEDSIZE, usage);
        }

        #endregion
    }
}
