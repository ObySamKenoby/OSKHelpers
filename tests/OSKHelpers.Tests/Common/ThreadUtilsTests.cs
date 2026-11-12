using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OSKHelpers.Common;
using OSKHelpers.Logging;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="ThreadUtils"/> class.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ThreadUtilsTests
    {
        #region Constants

        /// <summary>
        /// Maximum wait for the operations executed on the separate thread (milliseconds).
        /// </summary>
        private const int TIMEOUT = 5000;

        #endregion

        #region Members

        private string _tempLogPath;

        #endregion

        #region Setup / Cleanup

        /// <summary>
        /// Redirects the default log to a temporary directory.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _tempLogPath        = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            SimpleLog.LogPath   = _tempLogPath;
        }

        /// <summary>
        /// Restores the default log and removes the temporary directory.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            SimpleLog.LogPath = null;
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

        #region ExecuteTaskAsync

        /// <summary>
        /// Verifies that a null task throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void ExecuteTaskAsync_NullTask_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ThreadUtils.ExecuteTaskAsync(null));

            Assert.AreEqual("task", ex.ParamName);
        }

        /// <summary>
        /// Verifies that the task is awaited on the separate thread.
        /// </summary>
        [TestMethod]
        public void ExecuteTaskAsync_ValidTask_ExecutesTask()
        {
            var executed    = new ManualResetEventSlim(false);
            var task        = Task.Run(() => executed.Set());

            ThreadUtils.ExecuteTaskAsync(task);

            Assert.IsTrue(executed.Wait(TIMEOUT));
        }

        /// <summary>
        /// Verifies that an exception of the task is logged and not propagated to the caller.
        /// </summary>
        [TestMethod]
        public void ExecuteTaskAsync_FaultedTask_LogsExceptionWithoutThrowing()
        {
            var task = Task.FromException(new InvalidOperationException("ThreadUtils test failure"));

            ThreadUtils.ExecuteTaskAsync(task);

            var logged = SpinWait.SpinUntil(() => LogFileContains("ThreadUtils test failure"), TIMEOUT);
            Assert.IsTrue(logged);
        }

        #endregion

        #region Support methods

        /// <summary>
        /// Checks whether the log file contains the given text, reading it while the logger may still be writing it.
        /// </summary>
        /// <remarks>
        /// The exception is logged on a separate thread: the file is read with <see cref="FileShare.ReadWrite"/> so that the
        /// writing in progress does not make the reading fail, and a sharing error is treated as "not yet written" so that
        /// the caller retries.
        /// </remarks>
        /// <param name="text">Text to look for.</param>
        /// <returns>True if the log file exists and contains the text.</returns>
        private static bool LogFileContains(string text)
        {
            bool result;

            try
            {
                using (var stream = new FileStream(SimpleLog.LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd().Contains(text);
                }
            }
            catch (IOException)
            {
                // File not yet created (FileNotFoundException, DirectoryNotFoundException) or locked by the writer.
                result = false;
            }

            return result;
        }

        #endregion
    }
}
