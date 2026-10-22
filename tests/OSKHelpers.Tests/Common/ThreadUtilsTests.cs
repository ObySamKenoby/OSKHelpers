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

            var logged = SpinWait.SpinUntil(
                () => File.Exists(SimpleLog.LogFile) && File.ReadAllText(SimpleLog.LogFile).Contains("ThreadUtils test failure"), TIMEOUT);
            Assert.IsTrue(logged);
        }

        #endregion
    }
}
