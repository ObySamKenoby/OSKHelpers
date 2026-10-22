using System;
using System.IO;
using OSKHelpers.Common;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="ObjectUtils"/> class.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ObjectUtilsTests
    {
        #region Support classes

        /// <summary>
        /// Object with public properties used by the Dump tests.
        /// </summary>
        private class DumpSample
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public string Failing => throw new InvalidOperationException("Property failure");
        }

        #endregion

        #region Members

        private string _tempLogPath;
        private LogLevel _originalLogLevel;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Redirects the default log to a temporary directory.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempLogPath        = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _originalLogLevel   = SimpleLog.LogLevel;
            SimpleLog.LogPath   = _tempLogPath;
            SimpleLog.LogLevel  = LogLevel.Debug;
        }

        /// <summary>
        /// Restores the default log and removes the temporary directory.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            SimpleLog.LogPath   = null;
            SimpleLog.LogLevel  = _originalLogLevel;
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

        #region IsNumeric

        /// <summary>
        /// Verifies that all numeric types are recognised.
        /// </summary>
        [TestMethod]
        public void IsNumeric_NumericTypes_ReturnsTrue()
        {
            var values = new object[] { (byte)1, (sbyte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL, 1.5f, 1.5d, 1.5m };

            foreach (var value in values)
            {
                Assert.IsTrue(ObjectUtils.IsNumeric(value), value.GetType().Name);
            }
        }

        /// <summary>
        /// Verifies that null and non-numeric types are not recognised.
        /// </summary>
        [TestMethod]
        public void IsNumeric_NullOrNonNumericTypes_ReturnsFalse()
        {
            var values = new object[] { null, "1", '1', true, DateTime.Now, new object() };

            foreach (var value in values)
            {
                Assert.IsFalse(ObjectUtils.IsNumeric(value), value?.GetType().Name ?? "null");
            }
        }

        #endregion

        #region Dump

        /// <summary>
        /// Verifies that, with log enabled, names, types and values of the public properties are written to the log.
        /// </summary>
        [TestMethod]
        public void Dump_LogEnabled_WritesPropertiesToLog()
        {
            var sample = new DumpSample { Name = "Test", Count = 3 };

            ObjectUtils.Dump(sample, "Sample", true);

            var content = File.ReadAllText(SimpleLog.LogFile);
            Assert.Contains("Sample: DumpSample", content);
            Assert.Contains("Name (String) = Test", content);
            Assert.Contains("Count (Int32) = 3", content);
        }

        /// <summary>
        /// Verifies that a property that throws an exception is reported as an error without stopping the dump.
        /// </summary>
        [TestMethod]
        public void Dump_PropertyThrows_WritesErrorValue()
        {
            ObjectUtils.Dump(new DumpSample(), "Sample", true);

            var content = File.ReadAllText(SimpleLog.LogFile);
            Assert.Contains("Failing (String) = #ERROR#", content);
            Assert.Contains("Count (Int32) = 0", content);
        }

        /// <summary>
        /// Verifies that a null object is written as "Type: null".
        /// </summary>
        [TestMethod]
        public void Dump_NullObject_WritesNull()
        {
            ObjectUtils.Dump<DumpSample>(null, null, true);

            Assert.Contains("DumpSample: null", File.ReadAllText(SimpleLog.LogFile));
        }

        /// <summary>
        /// Verifies that nothing is written when the log level is lower than the requested one.
        /// </summary>
        [TestMethod]
        public void Dump_LogLevelNotSufficient_WritesNothing()
        {
            SimpleLog.LogLevel = LogLevel.Error;

            ObjectUtils.Dump(new DumpSample { Name = "Test" }, "Sample", true, LogLevel.Debug);

            Assert.IsFalse(File.Exists(SimpleLog.LogFile));
        }

        #endregion
    }
}
