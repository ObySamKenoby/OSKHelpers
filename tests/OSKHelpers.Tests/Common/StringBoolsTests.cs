using System.Linq;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    /// <summary>
    /// Tests for the <see cref="StringBools"/> class.
    /// </summary>
    [TestClass]
    public class StringBoolsTests
    {
        #region Constructors

        /// <summary>
        /// Verifies that the default constructor creates an empty collection.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_CreatesEmptyCollection()
        {
            var bools = new StringBools();

            Assert.AreEqual(0, bools.Length);
            Assert.AreEqual(string.Empty, bools.ToString());
            Assert.IsFalse(bools.AnyTrue);
            Assert.IsFalse(bools.AnyFalse);
        }

        /// <summary>
        /// Verifies the constructor from an enumerable of booleans, including null.
        /// </summary>
        [TestMethod]
        public void Constructor_Enumerable_CopiesValues()
        {
            Assert.AreEqual("101", new StringBools(new[] { true, false, true }).ToString());
            Assert.AreEqual(0, new StringBools((bool[])null).Length);
        }

        /// <summary>
        /// Verifies the constructor from a string: '1' is true, any other character is false.
        /// </summary>
        [TestMethod]
        [DataRow("1010", "1010")]
        [DataRow("1x0T", "1000")]
        [DataRow("", "")]
        [DataRow(null, "")]
        public void Constructor_String_ParsesValues(string text, string expected)
        {
            Assert.AreEqual(expected, new StringBools(text).ToString());
        }

        /// <summary>
        /// Verifies the constructor from a string with a fixed length: the string is truncated or padded with false.
        /// </summary>
        [TestMethod]
        [DataRow("1111", 2, "11")]
        [DataRow("11", 4, "1100")]
        [DataRow(null, 3, "000")]
        [DataRow("11", 0, "")]
        public void Constructor_StringAndLength_SetsFixedLength(string text, int length, string expected)
        {
            Assert.AreEqual(expected, new StringBools(text, length).ToString());
        }

        /// <summary>
        /// Verifies the constructor with a length: all values are false; a non positive length creates an empty collection.
        /// </summary>
        [TestMethod]
        [DataRow(3, "000")]
        [DataRow(0, "")]
        [DataRow(-2, "")]
        public void Constructor_Length_CreatesFalseValues(int length, string expected)
        {
            Assert.AreEqual(expected, new StringBools(length).ToString());
        }

        #endregion

        #region SetLength

        /// <summary>
        /// Verifies that SetLength clears the existing content.
        /// </summary>
        [TestMethod]
        public void SetLength_ExistingValues_ClearsContent()
        {
            var bools = new StringBools("111");

            bools.SetLength(4);

            Assert.AreEqual("0000", bools.ToString());
        }

        #endregion

        #region SetValue

        /// <summary>
        /// Verifies the four SetValue overloads.
        /// </summary>
        [TestMethod]
        public void SetValue_AllOverloads_SetValues()
        {
            var bools = new StringBools(8);

            bools.SetValue(0, true);
            bools.SetValue(1, 1);
            bools.SetValue(2, 2);
            bools.SetValue(3, "1");
            bools.SetValue(4, "true");
            bools.SetValue(5, '1');
            bools.SetValue(6, 'x');

            Assert.AreEqual("11010100", bools.ToString());
        }

        /// <summary>
        /// Verifies that an invalid position is ignored and does not raise ValueChanged.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(3)]
        public void SetValue_InvalidPosition_DoesNothing(int position)
        {
            var bools   = new StringBools(3);
            var raised  = 0;
            bools.ValueChanged += (s, e) => raised++;

            bools.SetValue(position, true);
            bools.SetValue(position, 1);
            bools.SetValue(position, "1");
            bools.SetValue(position, '1');

            Assert.AreEqual("000", bools.ToString());
            Assert.AreEqual(0, raised);
        }

        /// <summary>
        /// Verifies that each SetValue on a valid position raises ValueChanged.
        /// </summary>
        [TestMethod]
        public void SetValue_ValidPosition_RaisesValueChanged()
        {
            var bools   = new StringBools(2);
            object sender = null;
            var raised  = 0;
            bools.ValueChanged += (s, e) => { raised++; sender = s; };

            bools.SetValue(0, true);
            bools.SetValue(1, 1);
            bools.SetValue(0, "0");
            bools.SetValue(1, '0');

            Assert.AreEqual(4, raised);
            Assert.AreSame(bools, sender);
        }

        #endregion

        #region Get

        /// <summary>
        /// Verifies the Get methods for valid positions.
        /// </summary>
        [TestMethod]
        public void Get_ValidPosition_ReturnsValue()
        {
            var bools = new StringBools("10");

            Assert.IsTrue(bools.Get(0));
            Assert.IsFalse(bools.Get(1));
            Assert.AreEqual(1, bools.GetInt(0));
            Assert.AreEqual(0, bools.GetInt(1));
            Assert.AreEqual("1", bools.GetString(0));
            Assert.AreEqual("0", bools.GetString(1));
            Assert.AreEqual('1', bools.GetChar(0));
            Assert.AreEqual('0', bools.GetChar(1));
        }

        /// <summary>
        /// Verifies that the Get methods return the false representation for invalid positions.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(2)]
        public void Get_InvalidPosition_ReturnsFalseRepresentation(int position)
        {
            var bools = new StringBools("11");

            Assert.IsFalse(bools.Get(position));
            Assert.AreEqual(StringBools.FalseInt, bools.GetInt(position));
            Assert.AreEqual(StringBools.FalseString, bools.GetString(position));
            Assert.AreEqual(StringBools.FalseChar, bools.GetChar(position));
        }

        #endregion

        #region AnyTrue / AnyFalse

        /// <summary>
        /// Verifies AnyTrue and AnyFalse for various contents.
        /// </summary>
        [TestMethod]
        [DataRow("111", true, false)]
        [DataRow("000", false, true)]
        [DataRow("101", true, true)]
        public void AnyTrueAndAnyFalse_VariousValues_ReturnsExpected(string text, bool anyTrue, bool anyFalse)
        {
            var bools = new StringBools(text);

            Assert.AreEqual(anyTrue, bools.AnyTrue);
            Assert.AreEqual(anyFalse, bools.AnyFalse);
        }

        #endregion

        #region Reset

        /// <summary>
        /// Verifies that Reset sets all values and raises ValueChanged.
        /// </summary>
        [TestMethod]
        [DataRow(false, "000")]
        [DataRow(true, "111")]
        public void Reset_Value_SetsAllValues(bool value, string expected)
        {
            var bools   = new StringBools("101");
            var raised  = 0;
            bools.ValueChanged += (s, e) => raised++;

            bools.Reset(value);

            Assert.AreEqual(expected, bools.ToString());
            Assert.AreEqual(1, raised);
        }

        #endregion

        #region UpdateFromString

        /// <summary>
        /// Verifies that UpdateFromString replaces content and length.
        /// </summary>
        [TestMethod]
        public void UpdateFromString_NewString_ReplacesContentAndLength()
        {
            var bools = new StringBools("111111");

            bools.UpdateFromString("01", 3);

            Assert.AreEqual(3, bools.Length);
            Assert.AreEqual("010", bools.ToString());
        }

        #endregion

        #region ToString

        /// <summary>
        /// Verifies that ToString uses the true and false string representations.
        /// </summary>
        [TestMethod]
        public void ToString_Values_UsesTrueAndFalseStrings()
        {
            var bools = new StringBools(new[] { true, false });

            Assert.AreEqual(StringBools.TrueString + StringBools.FalseString, bools.ToString());
            Assert.IsTrue(bools.ToString().All(c => c == StringBools.TrueChar || c == StringBools.FalseChar));
        }

        #endregion
    }
}
