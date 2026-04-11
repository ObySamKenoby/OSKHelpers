using System;
using System.Linq;
using System.Text.RegularExpressions;
using OSKHelpers.Common;

namespace OSKHelpers.Tests.Common
{
    [TestClass]
    public class StringUtilsTests
    {
        [TestMethod]
        [DataRow(null,  "ABC",  false,  0, true)]
        [DataRow(null,  "ABC",  true,   0, false)]
        [DataRow(null,  "ABC",  false,  1, true)]
        [DataRow("A",   "ABC",  false,  0, true)]
        [DataRow("A",   "ABC",  true,   0, true)]
        [DataRow("A",   "ABC",  true,   1, true)]
        [DataRow("A",   "ABC",  false,  1, true)]
        [DataRow("A",   "ABC",  false,  2, true)]
        [DataRow("AB",  "ABC",  true,   2, true)]
        [DataRow("D",   "ABC",  false,  0, false)]
        [DataRow("D",   "ABC",  true,   0, false)]
        [DataRow("D",   "ABC",  true,   1, false)]
        [DataRow("D",   "ABC",  false,  1, false)]
        [DataRow("A",   null,   false,  0, false)]
        [DataRow("A",   "",     false,  0, false)]
        public void CheckCharsTests(string text, string validChars, bool notEmpty, int maxLength, bool expected)
        {
            Assert.AreEqual(expected, StringUtils.CheckChars(text, validChars, notEmpty, maxLength));
        }
        [TestMethod]
        [DataRow(null,  "ABC",  false,  0, 0, true)]
        [DataRow(null,  "ABC",  true,   0, 0, false)]
        [DataRow(null,  "ABC",  false,  0, 1, true)]
        [DataRow("A",   "ABC",  false,  0, 0, true)]
        [DataRow("A",   "ABC",  true,   0, 0, true)]
        [DataRow("A",   "ABC",  true,   0, 1, true)]
        [DataRow("A",   "ABC",  false,  0, 1, true)]
        [DataRow("A",   "ABC",  false,  0, 2, true)]
        [DataRow("AB",  "ABC",  true,   0, 2, true)]
        [DataRow("A",   "ABC",  false,  2, 0, false)]
        [DataRow("A",   "ABC",  true,   2, 0, false)]
        [DataRow("A",   "ABC",  true,   2, 1, false)]
        [DataRow("A",   "ABC",  false,  2, 1, false)]
        [DataRow("A",   "ABC",  false,  2, 2, false)]
        [DataRow("AB",  "ABC",  true,   2, 2, true)]
        [DataRow("D",   "ABC",  false,  0, 0, false)]
        [DataRow("D",   "ABC",  true,   0, 0, false)]
        [DataRow("D",   "ABC",  true,   0, 1, false)]
        [DataRow("D",   "ABC",  false,  0, 1, false)]
        [DataRow("A",   null,   false,  0, 0, false)]
        [DataRow("A",   "",     false,  0, 0, false)]
        public void CheckCharsTestsMinLength(string text, string validChars, bool notEmpty, int minLength, int maxLength, bool expected)
        {
            Assert.AreEqual(expected, StringUtils.CheckChars(text, validChars, notEmpty, minLength, maxLength));
        }
        [TestMethod]
        [DataRow(null,  @"^[A-C]*$",    false,  0, true)]
        [DataRow(null,  @"^[A-C]*$",    true,   0, false)]
        [DataRow(null,  @"^[A-C]*$",    false,  1, true)]
        [DataRow("A",   @"^[A-C]*$",    false,  0, true)]
        [DataRow("A",   @"^[A-C]*$",    true,   0, true)]
        [DataRow("A",   @"^[A-C]*$",    true,   1, true)]
        [DataRow("A",   @"^[A-C]*$",    false,  1, true)]
        [DataRow("A",   @"^[A-C]*$",    false,  2, true)]
        [DataRow("AB",  @"^[A-C]*$",    true,   2, true)]
        [DataRow("D",   @"^[A-C]*$",    false,  0, false)]
        [DataRow("D",   @"^[A-C]*$",    true,   0, false)]
        [DataRow("D",   @"^[A-C]*$",    true,   1, false)]
        [DataRow("D",   @"^[A-C]*$",    false,  1, false)]
        [DataRow("A",   null,           false,  0, false)]
        [DataRow("A",   "",             false,  0, false)]
        public void CheckRegexTests(string text, string regex, bool notEmpty, int maxLength, bool expected)
        {
            Assert.AreEqual(expected, StringUtils.CheckRegex(text, regex, notEmpty, maxLength));
        }
        [TestMethod]
        [DataRow(null,  @"^[A-C]*$",    false,  0, 0, true)]
        [DataRow(null,  @"^[A-C]*$",    true,   0, 0, false)]
        [DataRow(null,  @"^[A-C]*$",    false,  0, 1, true)]
        [DataRow("A",   @"^[A-C]*$",    false,  0, 0, true)]
        [DataRow("A",   @"^[A-C]*$",    true,   0, 0, true)]
        [DataRow("A",   @"^[A-C]*$",    true,   0, 1, true)]
        [DataRow("A",   @"^[A-C]*$",    false,  0, 1, true)]
        [DataRow("A",   @"^[A-C]*$",    false,  0, 2, true)]
        [DataRow("AB",  @"^[A-C]*$",    true,   0, 2, true)]
        [DataRow("A",   @"^[A-C]*$",    false,  2, 0, false)]
        [DataRow("A",   @"^[A-C]*$",    true,   2, 0, false)]
        [DataRow("A",   @"^[A-C]*$",    true,   2, 1, false)]
        [DataRow("A",   @"^[A-C]*$",    false,  2, 1, false)]
        [DataRow("A",   @"^[A-C]*$",    false,  2, 2, false)]
        [DataRow("AB",  @"^[A-C]*$",    true,   2, 2, true)]
        [DataRow("D",   @"^[A-C]*$",    false,  0, 0, false)]
        [DataRow("D",   @"^[A-C]*$",    true,   0, 0, false)]
        [DataRow("D",   @"^[A-C]*$",    true,   0, 1, false)]
        [DataRow("D",   @"^[A-C]*$",    false,  0, 1, false)]
        [DataRow("A",   null,           false,  0, 0, false)]
        [DataRow("A",   "",             false,  0, 0, false)]
        public void CheckRegexTestsMinLength(string text, string regex, bool notEmpty, int minLength, int maxLength, bool expected)
        {
            Assert.AreEqual(expected, StringUtils.CheckRegex(text, regex, notEmpty, minLength, maxLength));
        }

        /// <summary>
        /// Checks each string through various automated checks.<br/>
        /// Each string must be accompanied by a description of its content.<br/>
        /// Fail can be used to force the returned result; when False all checks will return false.
        /// </summary>
        /// <param name="text">Testo da verificare.</param>
        /// <param name="isLowerCase">La stringa contiene caratteri minuscoli.</param>
        /// <param name="isUpperCase">La stringa contiene caratteri maiuscoli.</param>
        /// <param name="isNumbers">La stringa contiene numeri.</param>
        /// <param name="space">La stringa contiene spazi</param>
        [TestMethod]
        public void StandardCheckTests()
        {
            const string UPPER  = "AGZ";
            const string LOWER  = "agz";
            const string NUMS   = "059";
            const string SPC    = "  ";
            const string EMPTY = "";
            var isLowerCase = false;
            var isUpperCase = false;
            var isNumbers   = false;
            var isSpace     = false;
            var failMaxLen  = false;
            var notEmpty    = false;
            var setLength   = false;
            var maxLength      = 0;

            do
            {
                do
                {
                    do
                    {
                        do
                        {
                            do
                            {
                                do
                                {
                                    var text = $"{(isLowerCase ? LOWER : EMPTY)}{(isUpperCase ? UPPER : EMPTY)}{(isNumbers ? NUMS : EMPTY)}{(isSpace ? SPC : EMPTY)}";

                                    var empty = string.IsNullOrEmpty(text);

                                    if (empty)
                                    {
                                        failMaxLen        = !setLength;
                                        notEmpty    = failMaxLen;
                                        maxLength   = 0;
                                    }
                                    else
                                    {
                                        notEmpty    = true;
                                        maxLength   = failMaxLen ? text.Length - 1 : (!setLength ? 0 : text.Length);
                                    }

                                    bool res = false;
                                    res = !failMaxLen && (empty || (!isUpperCase && isLowerCase && !isNumbers && !isSpace));
                                    Assert.AreEqual(res, StringUtils.IsLowercase(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || (isUpperCase && !isLowerCase && !isNumbers && !isSpace));
                                    Assert.AreEqual(res, StringUtils.IsUppercase(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || (!isUpperCase && !isLowerCase && isNumbers && !isSpace));
                                    Assert.AreEqual(res, StringUtils.IsNumbers(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isUpperCase || isLowerCase) && !isNumbers && !isSpace));
                                    Assert.AreEqual(res, StringUtils.IsAlpha(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isUpperCase || isLowerCase || isNumbers) && !isSpace));
                                    Assert.AreEqual(res, StringUtils.IsAlphaNumbers(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isLowerCase || isNumbers) && !isUpperCase && !isSpace));
                                    Assert.AreEqual(res, StringUtils.IsLowercaseNumbers(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isUpperCase || isNumbers) && !isLowerCase && !isSpace));
                                    Assert.AreEqual(res, StringUtils.IsUppercaseNumbers(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isLowerCase || isSpace) && !isUpperCase && !isNumbers));
                                    Assert.AreEqual(res, StringUtils.IsLowercaseSpc(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isUpperCase || isSpace) && !isLowerCase && !isNumbers));
                                    Assert.AreEqual(res, StringUtils.IsUppercaseSpc(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isNumbers || isSpace) && !isUpperCase && !isLowerCase));
                                    Assert.AreEqual(res, StringUtils.IsNumbersSpc(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isUpperCase || isLowerCase || isSpace) && !isNumbers));
                                    Assert.AreEqual(res, StringUtils.IsAlphaSpc(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || isUpperCase || isLowerCase || isNumbers || isSpace);
                                    Assert.AreEqual(res, StringUtils.IsAlphaNumbersSpc(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isLowerCase || isNumbers || isSpace) && !isUpperCase));
                                    Assert.AreEqual(res, StringUtils.IsLowercaseNumbersSpc(text, notEmpty, maxLength));
                                    res = !failMaxLen && (empty || ((isUpperCase || isNumbers || isSpace) && !isLowerCase));
                                    Assert.AreEqual(res, StringUtils.IsUppercaseNumbersSpc(text, notEmpty, maxLength));
                                    failMaxLen = string.IsNullOrWhiteSpace(text) ? true : !failMaxLen; 
                                } while (!failMaxLen);
                                setLength = !setLength;
                            } while (!setLength);
                            isSpace = !isSpace;
                        } while (!isSpace);
                        isNumbers = !isNumbers;
                    } while (!isNumbers);
                    isUpperCase = !isUpperCase;
                } while (!isUpperCase);
                isLowerCase = !isLowerCase;
            } while (!isLowerCase);

        }

        #region AsASCII

        /// <summary>
        /// Verifies that non-ASCII characters are removed.
        /// </summary>
        [TestMethod]
        [DataRow("hello",       false, false, "hello")]
        [DataRow("héllo",       false, false, "hllo")]
        [DataRow("café",        false, false, "caf")]
        [DataRow(null,          false, false, "")]
        public void AsASCIIVariousInputsReturnsExpected(string text, bool toUpper, bool trim, string expected)
        {
            var result = StringUtils.AsASCII(text, toUpper, trim);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Verifies the toUpper parameter.
        /// </summary>
        [TestMethod]
        public void AsASCIIToUpperReturnsUppercase()
        {
            var result = StringUtils.AsASCII("hello", toUpper: true);

            Assert.AreEqual("HELLO", result);
        }

        /// <summary>
        /// Verifies the trim parameter.
        /// </summary>
        [TestMethod]
        public void AsASCIITrimRemovesWhitespace()
        {
            var result = StringUtils.AsASCII("  hello  ", trim: true);

            Assert.AreEqual("hello", result);
        }

        /// <summary>
        /// Verifies the length parameter for truncation.
        /// </summary>
        [TestMethod]
        public void AsASCIIWithMaxLengthTruncatesString()
        {
            var result = StringUtils.AsASCII("hello world", length: 5);

            Assert.AreEqual("hello", result);
        }

        /// <summary>
        /// Verifies that padRight pads the string with spaces.
        /// </summary>
        [TestMethod]
        public void AsASCIIWithPadRightPadsWithSpaces()
        {
            var result = StringUtils.AsASCII("hi", length: 5, padRight: true);

            Assert.AreEqual("hi   ", result);
        }

        #endregion

        #region GenerateString

        /// <summary>
        /// Verifies that GenerateString generates a string of the requested length.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(100)]
        [DataRow(1000)]
        public void GenerateStringValidLengthReturnsCorrectLength(int length)
        {
            var result = StringUtils.GenerateString(StringUtils.SCHARSNUMS, length);

            Assert.AreEqual(length, result.Length);
        }

        /// <summary>
        /// Verifies that GenerateString contains only the specified characters.
        /// </summary>
        [TestMethod]
        public void GenerateStringSpecificCharsContainsOnlyThoseChars()
        {
            var chars   = "AB";
            var result  = StringUtils.GenerateString(chars, 100);

            Assert.IsTrue(result.All(c => chars.Contains(c)));
        }

        /// <summary>
        /// Verifies that a zero length throws an exception.
        /// </summary>
        [TestMethod]
        public void GenerateStringZeroLengthThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StringUtils.GenerateString("abc", 0));
        }

        /// <summary>
        /// Verifies that a negative length throws an exception.
        /// </summary>
        [TestMethod]
        public void GenerateStringNegativeLengthThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StringUtils.GenerateString("abc", -1));
        }

        /// <summary>
        /// Verifies that a null or empty character set throws an exception.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void GenerateStringInvalidCharsThrowsArgumentException(string chars)
        {
            Assert.Throws<ArgumentException>(() => StringUtils.GenerateString(chars, 10));
        }

        /// <summary>
        /// Verifies GenerateLowerCharsString.
        /// </summary>
        [TestMethod]
        public void GenerateLowerCharsStringValidLengthReturnsOnlyLowercase()
        {
            var result = StringUtils.GenerateLowerCharsString(50);

            Assert.AreEqual(50, result.Length);
            Assert.IsTrue(Regex.IsMatch(result, @"^[a-z]+$"));
        }

        /// <summary>
        /// Verifies GenerateUpperCharsString.
        /// </summary>
        [TestMethod]
        public void GenerateUpperCharsStringValidLengthReturnsOnlyUppercase()
        {
            var result = StringUtils.GenerateUpperCharsString(50);

            Assert.AreEqual(50, result.Length);
            Assert.IsTrue(Regex.IsMatch(result, @"^[A-Z]+$"));
        }

        /// <summary>
        /// Verifies GenerateAlphaString.
        /// </summary>
        [TestMethod]
        public void GenerateAlphaStringValidLengthReturnsOnlyAlpha()
        {
            var result = StringUtils.GenerateAlphaString(50);

            Assert.AreEqual(50, result.Length);
            Assert.IsTrue(Regex.IsMatch(result, @"^[A-Za-z]+$"));
        }

        /// <summary>
        /// Verifies GenerateAlphaNumericString.
        /// </summary>
        [TestMethod]
        public void GenerateAlphaNumericStringValidLengthReturnsAlphaNumeric()
        {
            var result = StringUtils.GenerateAlphaNumericString(50);

            Assert.AreEqual(50, result.Length);
            Assert.IsTrue(Regex.IsMatch(result, @"^[A-Za-z0-9]+$"));
        }

        #endregion

    }
}
