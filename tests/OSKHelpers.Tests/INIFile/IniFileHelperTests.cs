using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.INIFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.Tests.INIFile
{
    [TestClass]
    public class IniFileHelperTests
    {
        #region Costanti

        private const string KEY1 = "Key1";
        private const string KEY2 = "Key2";
        private const string KEY3 = "Key3";
        private const string KEY4 = "Key4";
        private const string KEY5 = "Key5";
        private const string KEY6 = "Key6";
        private const string KEY7 = "Key7";
        private const string KEY8 = "Key8";
        private const string KEY9 = "Key9";
        private const string ARR1 = "Arr1";
        private const string ARR2 = "Arr2";
        private const string ARR3 = "Arr3";
        private const string VALUE1 = "Value1";
        private const string VALUE2 = "Value2";
        private const string VALUE3 = "Value3";
        private const string VALUE4 = "4";
        private const string VALUE5 = "5";
        private const string VALUE6 = "True";
        private const string VALUE7 = "False";
        private const string VALUE8 = "1";
        private const string VALUE9 = "0";
        private const string ARR1ELEM1 = "Arr1Elem1";
        private const string ARR1ELEM2 = "Arr1Elem2";
        private const string ARR1ELEM3 = "Arr1Elem3";
        private const string ARR2ELEM1 = "Arr2Elem1";
        private const string ARR2ELEM2 = "Arr2Elem2";
        private const string ARR3ELEM1 = "Arr3Elem1";
        private readonly string KEYVALUE1 = $"{KEY1} = {VALUE1}";
        private readonly string KEYVALUE2 = $"{KEY2} = {VALUE2}";
        private readonly string KEYVALUE3 = $"{KEY3} = {VALUE3}";
        private readonly string KEYVALUE4 = $"{KEY4} = {VALUE4}";
        private readonly string KEYVALUE5 = $"{KEY5} = {VALUE5}";
        private readonly string KEYVALUE6 = $"{KEY6} = {VALUE6}";
        private readonly string KEYVALUE7 = $"{KEY7} = {VALUE7}";
        private readonly string KEYVALUE8 = $"{KEY8} = {VALUE8}";
        private readonly string KEYVALUE9 = $"{KEY9} = {VALUE9}";

        #endregion

        #region Metodi

        #region Metodi di supporto

        private string[] GetLinesCommenti()
        {
            return new string[]
            {
                "# Commento 1",
                "# Commento 2",
                "# Commento 3"
            };
        }

        private string[] GetLinesValide()
        {
            return new string[]
            {
                "Key1 = Value1",
                "# Commento",
                "   keY2 = 2",
                "# Commento",
                "KEY3 = Value3 = Value3    ",
                "KEY4 = Value4 # Value3 = Value4 "
            };
        }

        private string[] GetLinesArrValide()
        {
            return new string[]
            {
                "*Arr1=Arr1Elem1",
                "*Arr2 = Arr2Elem1",
                "*Arr1 = Arr1Elem2",
                "  *Arr1   = Arr1Elem3",
                "*Arr2 = Arr2Elem2"
            };
        }

        private string GetLineErr1() => "= Key5 = Value6";
        private string GetLineErr2() => "Key6Value6=";
        private string GetLineErr3() => "Key7 Value7";
        private string GetArrayErr1() => "* = Arr1Elem1";
        private string GetArrayErr2() => "*Arr1Elem1=";
        private string GetArrayErr3() => "*Arr1 Arr1Elem1";


        private void PopulateHelper3(IniFileHelper helper)
        {
            if (helper != null)
            {
                helper.Parse(new List<string>() { KEYVALUE1, KEYVALUE2, KEYVALUE3 });
            }
        }

        private void PopulateHelper3Alternate(IniFileHelper helper)
        {
            if (helper != null)
            {
                helper.Parse(new List<string>() { KEYVALUE1, KEYVALUE2, KEYVALUE5 });
            }
        }

        private void PopulateHelper5(IniFileHelper helper)
        {
            if (helper != null)
            {
                helper.Parse(new List<string>() { KEYVALUE1, KEYVALUE2, KEYVALUE3, KEYVALUE4, KEYVALUE5 });
            }
        }

        private void PopulateHelper9(IniFileHelper helper)
        {
            if (helper != null)
            {
                helper.Parse(new List<string>() { KEYVALUE1, KEYVALUE2, KEYVALUE3, KEYVALUE4, KEYVALUE5, KEYVALUE6, KEYVALUE7, KEYVALUE8, KEYVALUE9 });
            }
        }

        private string[] GetKeys3() => new string[] { KEY1, KEY2, KEY3 };
        private string[] GetKeys4() => new string[] { KEY1, KEY2, KEY3, KEY4 };
        private string[] GetKeys5() => new string[] { KEY1, KEY2, KEY3, KEY4, KEY5 };
        private string[] GetKeys7() => new string[] { KEY1, KEY2, KEY3, KEY4, KEY5, KEY6, KEY7 };
        private string[] GetKeys9() => new string[] { KEY1, KEY2, KEY3, KEY4, KEY5, KEY6, KEY7, KEY8, KEY9 };

        #endregion

        [TestMethod]
        public void ParseLinesTestLinesNullReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.Parse(null));
        }

        [TestMethod]
        public void ParseLinesTestNoLinesReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.Parse(new List<string>()));
        }

        [TestMethod]
        public void ParseLinesCommentsGoodReturnsTrue()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.AddRange(GetLinesCommenti());
            Assert.IsTrue(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesCommentsGoodDuplicatedKeyReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesCommentsGoodErr1ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.AddRange(GetLinesCommenti());
            lines.Add(GetLineErr1());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesCommentsGoodErr2ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.AddRange(GetLinesCommenti());
            lines.Add(GetLineErr2());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesCommentsGoodErr3ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.AddRange(GetLinesCommenti());
            lines.Add(GetLineErr3());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesIsFileReadTests()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.IniFileRead);
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            Assert.IsTrue(helper.Parse(lines) && helper.IniFileRead);
            lines.Add(GetLineErr1());
            Assert.IsTrue(!helper.Parse(lines) && !helper.IniFileRead);
        }

        [TestMethod]
        public void ParseLinesArrCommentsGoodErr1ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.Add(GetArrayErr1());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesArrCommentsGoodErr2ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.Add(GetArrayErr2());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesArrCommentsGoodErr3ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.Add(GetArrayErr3());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void ParseLinesArrCommentsGoodValidReturnsTrue()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.AddRange(GetLinesArrValide());
            Assert.IsTrue(helper.Parse(lines));
        }

        [TestMethod]
        public void LinesArrCommentsGoodValidTests()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetLinesCommenti());
            lines.AddRange(GetLinesValide());
            lines.AddRange(GetLinesArrValide());
            Assert.IsTrue(helper.Parse(lines));
            Assert.IsFalse(helper.HasKey("Arr1"));
            Assert.AreEqual(helper.Arrays.Count, 2);
            Assert.IsTrue(helper.HasArray(ARR1));
            Assert.AreEqual(helper.Array(ARR1).Count, 3);
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM1));
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM2));
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM3));
            Assert.IsTrue(helper.HasArray(ARR2));
            Assert.AreEqual(helper.Array(ARR2).Count, 2);
            Assert.IsTrue(helper.Array(ARR2).Contains(ARR2ELEM1));
            Assert.IsTrue(helper.Array(ARR2).Contains(ARR2ELEM2));
            Assert.AreEqual(helper.Array(ARR1)[0], ARR1ELEM1);
            Assert.AreEqual(helper.Array(ARR2)[1], ARR2ELEM2);
        }

        /* Test rimossi perché non verificabili
        
        [TestMethod]
        public void LoadFileContainsErrorFails()
        {
            var helper = new IniFileHelper(Path.Combine(Paths.AssemblyPath, "INIFile", "SettingsTestsFail.ini"), false);
            Assert.IsFalse(helper.IniFileRead);
        }

        [TestMethod]
        public void LoadFileGood()
        {
            var helper = new IniFileHelper(Path.Combine(Paths.AssemblyPath, "INIFile", "SettingsTestsOk.ini"), false);
            Assert.IsTrue(helper.IniFileRead);
            Assert.IsTrue(helper.HasKeys(new string[] { KEY1, KEY2, KEY3 }));
            Assert.IsTrue(helper.HasArray(ARR1));
            Assert.AreEqual(helper.Array(ARR1).Count, 3);
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM1));
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM2));
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM3));
            Assert.IsTrue(helper.HasArray(ARR2));
            Assert.AreEqual(helper.Array(ARR2).Count, 1);
            Assert.IsTrue(helper.Array(ARR2).Contains(ARR2ELEM1));
        }

        [TestMethod]
        public void SaveFileTests()
        {
            string outputFile = Path.Combine(Paths.AssemblyPath, "INIFile", "SettingsTestsOutput.ini");
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            var helper = new IniFileHelper(Path.Combine(Paths.AssemblyPath, "INIFile", "SettingsTestsOk.ini"), false);
            Assert.IsTrue(helper.IniFileRead);
            helper.AddKey(KEY9, VALUE9);
            helper.AddArrayElement(ARR2, ARR2ELEM2);
            helper.AddArrayElement(ARR3, ARR3ELEM1);
            Assert.IsTrue(helper.Save(outputFile, false));
            helper = new IniFileHelper(outputFile, false);
            Assert.IsTrue(helper.IniFileRead);
            Assert.IsTrue(helper.HasKey(KEY9));
            Assert.IsTrue(helper.HasArray(ARR2));
            Assert.IsTrue(helper.Array(ARR2).Contains(ARR2ELEM2));
            Assert.IsTrue(helper.HasArray(ARR3));
            Assert.IsTrue(helper.Array(ARR3).Contains(ARR3ELEM1));
        } */

        [TestMethod]
        public void HasKeyNoKeysReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.HasKey(KEY1));
        }

        [TestMethod]
        public void HasKeyValueNullReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKey(null));
        }

        [TestMethod]
        public void HasKeyValueSpacesReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKey(string.Empty));
        }

        [TestMethod]
        public void HasKeyNotExistsReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKey(KEY5));
        }

        [TestMethod]
        public void HasKeyExistsReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsTrue(helper.HasKey(KEY1));
        }

        [TestMethod]
        public void HasKeysNoKeysMemorizedReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.HasKeys(GetKeys3()));
        }

        [TestMethod]
        public void HasKeysNullParameterReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKeys(null));
        }

        [TestMethod]
        public void HasKeysParameterTooLongReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKeys(GetKeys4()));
        }

        [TestMethod]
        public void HasKeysOneKeyNotPresentReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3Alternate(helper);
            Assert.IsFalse(helper.HasKeys(GetKeys3()));
        }

        [TestMethod]
        public void HasKeysParameterSubsetOfKeysReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.IsTrue(helper.HasKeys(GetKeys4()));
        }

        [TestMethod]
        public void HasKeysParameterEqualsKeysReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.IsTrue(helper.HasKeys(GetKeys5()));
        }

        [TestMethod]
        public void GetKeysReturnsKeys()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.IsTrue(helper.GetKeys().SequenceEqual(GetKeys5().Select(k => k.ToUpper())));
        }

        [TestMethod]
        public void GetStringKeyDoesntExistsReturnsEmptyString()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(helper.GetString(KEY5), string.Empty);
        }

        [TestMethod]
        public void GetStringKeyExistsReturnsString()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(helper.GetString(KEY1), VALUE1);
        }

        [TestMethod]
        public void GetIntKeyDoesntExistsReturns0()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(helper.GetInt(KEY5), 0);
        }

        [TestMethod]
        public void GetIntKeyValueNotIntReturns0()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.AreEqual(helper.GetInt(KEY3), 0);
        }

        [TestMethod]
        public void GetIntKeyExistsReturnsValue()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.AreEqual(helper.GetInt(KEY5), 5);
        }

        [TestMethod]
        public void GetBoolKeyDoesntExistsReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(helper.GetBool(KEY5), false);
        }

        [TestMethod]
        public void GetBoolKeyValueNotBoolReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(helper.GetBool(KEY5), false);
        }

        [TestMethod]
        public void GetBoolKeyTrueReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(helper.GetBool(KEY6), true);
        }

        [TestMethod]
        public void GetBoolKeyFalseReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(helper.GetBool(KEY7), false);
        }

        [TestMethod]
        public void GetBoolKey1ReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(helper.GetBool(KEY8), true);
        }

        [TestMethod]
        public void GetBoolKey0ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(helper.GetBool(KEY9), false);
        }

        #endregion

    }
}