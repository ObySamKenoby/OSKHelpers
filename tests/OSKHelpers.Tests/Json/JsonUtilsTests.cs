using System.Text.Json;
using OSKHelpers.Json;

namespace OSKHelpers.Tests.Json
{
    /// <summary>
    /// Tests for the <see cref="JsonUtils"/> class.
    /// </summary>
    [TestClass]
    public class JsonUtilsTests
    {
        #region Support classes

        private class BaseItem
        {
            public string Name { get; set; }
        }

        private class DerivedItem : BaseItem
        {
            public int Count { get; set; }
            public string Optional { get; set; }
        }

        #endregion

        #region Serialize

        /// <summary>
        /// Verifies that a null object is serialised as an empty object.
        /// </summary>
        [TestMethod]
        public void Serialize_NullObject_ReturnsEmptyObject()
        {
            Assert.AreEqual("{}", JsonUtils.Serialize(null));
        }

        /// <summary>
        /// Verifies that the runtime type is used, so the properties of the derived class are included.
        /// </summary>
        [TestMethod]
        public void Serialize_DerivedObjectAsBase_IncludesDerivedProperties()
        {
            BaseItem item = new DerivedItem { Name = "Test", Count = 3 };

            var json = JsonUtils.Serialize(item);

            using var document = JsonDocument.Parse(json);
            Assert.AreEqual("Test", document.RootElement.GetProperty("Name").GetString());
            Assert.AreEqual(3, document.RootElement.GetProperty("Count").GetInt32());
        }

        /// <summary>
        /// Verifies that the default options include null properties and indent the output.
        /// </summary>
        [TestMethod]
        public void Serialize_DefaultOptions_IncludesNullPropertiesAndIndents()
        {
            var json = JsonUtils.Serialize(new DerivedItem());

            Assert.Contains("\"Optional\": null", json);
            Assert.Contains("\n", json);
        }

        /// <summary>
        /// Verifies that the options passed as parameter are used.
        /// </summary>
        [TestMethod]
        public void Serialize_CustomOptions_UsesOptions()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

            var json = JsonUtils.Serialize(new DerivedItem { Name = "Test", Count = 1 }, options);

            using var document = JsonDocument.Parse(json);
            Assert.AreEqual("Test", document.RootElement.GetProperty("name").GetString());
            Assert.AreEqual(1, document.RootElement.GetProperty("count").GetInt32());
            Assert.DoesNotContain("\n", json);
        }

        #endregion
    }
}
