using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="KeyValue{KeyType, ValueType}"/> class.
    /// </summary>
    [TestClass]
    public class KeyValueTests
    {
        #region Constructors

        /// <summary>
        /// Verifies that the default constructor leaves key and value at their default values.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_KeyAndValueAreDefault()
        {
            var keyValue = new KeyValue<int, string>();

            Assert.AreEqual(0, keyValue.Key);
            Assert.IsNull(keyValue.Value);
        }

        /// <summary>
        /// Verifies that the constructor with parameters sets key and value.
        /// </summary>
        [TestMethod]
        public void Constructor_KeyAndValue_SetsProperties()
        {
            var keyValue = new KeyValue<int, string>(5, "Five");

            Assert.AreEqual(5, keyValue.Key);
            Assert.AreEqual("Five", keyValue.Value);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Verifies that key and value can be changed after creation.
        /// </summary>
        [TestMethod]
        public void KeyAndValue_SetValues_ReturnsSetValues()
        {
            var keyValue    = new KeyValue<string, decimal>("A", 1m);
            keyValue.Key    = "B";
            keyValue.Value  = 2.5m;

            Assert.AreEqual("B", keyValue.Key);
            Assert.AreEqual(2.5m, keyValue.Value);
        }

        #endregion
    }
}
