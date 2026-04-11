using System;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="OSKRandom"/> class.
    /// </summary>
    [TestClass]
    public class OSKRandomTests
    {
        /// <summary>
        /// Verifies that the seed is always a positive value.
        /// </summary>
        [TestMethod]
        public void SeedAlwaysReturnsPositiveValue()
        {
            var seed = OSKRandom.Seed;

            Assert.IsTrue(seed >= 0);
        }

        /// <summary>
        /// Verifies that the default constructor creates a working instance.
        /// </summary>
        [TestMethod]
        public void ConstructorDefaultCreatesWorkingInstance()
        {
            var rnd     = new OSKRandom();
            var value   = rnd.Next(0, 100);

            Assert.IsTrue(value >= 0 && value < 100);
        }

        /// <summary>
        /// Verifies that the same seed produces reproducible sequences.
        /// </summary>
        [TestMethod]
        public void ConstructorSameSeedProducesSameSequence()
        {
            const int seed = 42;
            var rnd1 = new OSKRandom(seed);
            var rnd2 = new OSKRandom(seed);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(rnd1.Next(), rnd2.Next());
            }
        }

        /// <summary>
        /// Verifies that Shared returns a non-null instance.
        /// </summary>
        [TestMethod]
        public void SharedAlwaysReturnsNonNullInstance()
        {
            Assert.IsNotNull(OSKRandom.Shared);
        }

        /// <summary>
        /// Verifies that Shared generates values in the expected range.
        /// </summary>
        [TestMethod]
        public void SharedNextReturnsValueInRange()
        {
            for (int i = 0; i < 100; i++)
            {
                var value = OSKRandom.Shared.Next(10, 20);
                Assert.IsTrue(value >= 10 && value < 20);
            }
        }
    }
}
