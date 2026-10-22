using System;
using System.IO;
using OSKHelpers.ExtensionMethods;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.ExtensionMethods
{
    [TestClass]
    public class ObjectExtensionMethodsTests
    {
        #region Methods

        [TestMethod]
        [DataRow(1,         true)]
        [DataRow(1.5,       true)]
        [DataRow((byte)1,   true)]
        [DataRow("1",       false)]
        [DataRow(true,      false)]
        [DataRow(null,      false)]
        public void OSKIsNumeric_Value_ReturnsWhetherNumeric(object value, bool expected)
        {
            Assert.AreEqual(expected, value.OSKIsNumeric());
        }

        /// <summary>
        /// Verifies that OSKDump delegates to ObjectUtils.Dump, writing the object to the log when requested.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void OSKDump_LogEnabled_WritesObjectToLog()
        {
            var tempLogPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                SimpleLog.LogPath = tempLogPath;

                new Version(1, 2).OSKDump("Sample", true);

                var content = File.ReadAllText(SimpleLog.LogFile);
                Assert.Contains("Sample: Object", content);
                Assert.Contains("Major (Int32) = 1", content);
            }
            finally
            {
                SimpleLog.LogPath = null;
                if (Directory.Exists(tempLogPath))
                {
                    Directory.Delete(tempLogPath, true);
                }
            }
        }

        #endregion
    }
}
