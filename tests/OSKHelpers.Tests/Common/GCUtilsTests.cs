using System;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    [TestClass]
    public class GCUtilsTests
    {
        #region Constants

        private const string GCHEAPHARDLIMIT = "GCHeapHardLimit";

        #endregion

        #region Methods

        [TestMethod]
        public void SetHeapHardLimit_ValidSize_SetsLimitInBytes()
        {
            // A very high limit (1 TB) so the test does not constrain the memory of the test process
            const int MAXHEAPSIZE = 1 << 20;

            GCUtils.SetHeapHardLimit(MAXHEAPSIZE);

            Assert.AreEqual((ulong)MAXHEAPSIZE << 20, AppContext.GetData(GCHEAPHARDLIMIT));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void SetHeapHardLimit_NotPositiveSize_ThrowsArgumentOutOfRangeException(int maxHeapSize)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GCUtils.SetHeapHardLimit(maxHeapSize));
        }

        [TestMethod]
        public void GetCheckTimer_ValidParameters_ReturnsConfiguredTimer()
        {
            // Trigger size above 2 GB: before the fix the conversion to bytes used int arithmetic
            using var timer = GCUtils.GetCheckTimer(1000, 4096);

            Assert.AreEqual(1000, timer.Interval);
            Assert.IsTrue(timer.AutoReset);
            Assert.IsFalse(timer.Enabled);
        }

        [TestMethod]
        [DataRow(999,   1)]
        [DataRow(1000,  0)]
        public void GetCheckTimer_InvalidParameters_ThrowsArgumentOutOfRangeException(double interval, int triggerSize)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GCUtils.GetCheckTimer(interval, triggerSize));
        }

        /// <summary>
        /// Verifies that a normal and a complete collection run a garbage collection without exceptions.
        /// </summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Force_Complete_RunsGarbageCollection(bool complete)
        {
            var collections = GC.CollectionCount(0);

            GCUtils.Force(complete);

            Assert.IsGreaterThan(collections, GC.CollectionCount(0));
        }

        #endregion
    }
}
