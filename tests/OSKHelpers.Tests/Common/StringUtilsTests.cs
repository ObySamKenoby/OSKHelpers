using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.Common;
using System;

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
        /// Check di ogni stringa attraverso i vari check automatizzati.<br/>
        /// Ogni stringa deve essere accompagnata dalla desccrizione del contenuto.<br/>
        /// Fail può essere utilizzato per forzare il risultato restituito, quando False tutti i check risulteranno false.
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

    }
}
