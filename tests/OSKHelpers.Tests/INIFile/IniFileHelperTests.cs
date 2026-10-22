using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.Common;
using OSKHelpers.INIFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OSKHelpers.Tests.INIFile
{
    [TestClass]
    public class IniFileHelperTests
    {
        #region Constants

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
        private const string KEYVALUE1 = $"{KEY1} = {VALUE1}";
        private const string KEYVALUE2 = $"{KEY2} = {VALUE2}";
        private const string KEYVALUE3 = $"{KEY3} = {VALUE3}";
        private const string KEYVALUE4 = $"{KEY4} = {VALUE4}";
        private const string KEYVALUE5 = $"{KEY5} = {VALUE5}";
        private const string KEYVALUE6 = $"{KEY6} = {VALUE6}";
        private const string KEYVALUE7 = $"{KEY7} = {VALUE7}";
        private const string KEYVALUE8 = $"{KEY8} = {VALUE8}";
        private const string KEYVALUE9 = $"{KEY9} = {VALUE9}";
        private const string SHOWKEYVALUE = "!!SHOWKEYVALUE!!";

        #endregion

        #region Methods

        #region Helper methods

        private string[] GetCommentLines()
        {
            return new string[]
            {
                "# Comment 1",
                "# Comment 2",
                "# Comment 3"
            };
        }

        private string[] GetValidLines()
        {
            return new string[]
            {
                "Key1 = Value1",
                "# Comment",
                "   keY2 = 2",
                "# Comment",
                "KEY3 = Value3 = Value3    ",
                "KEY4 = Value4 # Value3 = Value4 "
            };
        }

        private string[] GetValidArrayLines()
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
        public void Parse_NullLines_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.Parse(null));
        }

        [TestMethod]
        public void Parse_NoLines_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.Parse(new List<string>()));
        }

        [TestMethod]
        public void Parse_CommentsAndValidLines_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.AddRange(GetCommentLines());
            Assert.IsTrue(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_DuplicatedKey_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_LineWithoutKey_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.AddRange(GetCommentLines());
            lines.Add(GetLineErr1());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_LineWithoutValue_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.AddRange(GetCommentLines());
            lines.Add(GetLineErr2());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_LineWithoutEqualSign_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.AddRange(GetCommentLines());
            lines.Add(GetLineErr3());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_ValidAndInvalidLines_UpdatesIniFileRead()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.IniFileRead);
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            Assert.IsTrue(helper.Parse(lines) && helper.IniFileRead);
            lines.Add(GetLineErr1());
            Assert.IsTrue(!helper.Parse(lines) && !helper.IniFileRead);
        }

        [TestMethod]
        public void Parse_ArrayLineWithoutName_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.Add(GetArrayErr1());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_ArrayLineWithoutValue_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.Add(GetArrayErr2());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_ArrayLineWithoutEqualSign_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.Add(GetArrayErr3());
            Assert.IsFalse(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_ValidArrayLines_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.AddRange(GetValidArrayLines());
            Assert.IsTrue(helper.Parse(lines));
        }

        [TestMethod]
        public void Parse_ValidArrayLines_LoadsArrays()
        {
            var helper = new IniFileHelper();
            List<string> lines = new List<string>();
            lines.AddRange(GetCommentLines());
            lines.AddRange(GetValidLines());
            lines.AddRange(GetValidArrayLines());
            Assert.IsTrue(helper.Parse(lines));
            Assert.IsFalse(helper.HasKey("Arr1"));
            Assert.AreEqual(2, helper.Arrays.Count);
            Assert.IsTrue(helper.HasArray(ARR1));
            Assert.AreEqual(3, helper.Array(ARR1).Count);
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM1));
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM2));
            Assert.IsTrue(helper.Array(ARR1).Contains(ARR1ELEM3));
            Assert.IsTrue(helper.HasArray(ARR2));
            Assert.AreEqual(2, helper.Array(ARR2).Count);
            Assert.IsTrue(helper.Array(ARR2).Contains(ARR2ELEM1));
            Assert.IsTrue(helper.Array(ARR2).Contains(ARR2ELEM2));
            Assert.AreEqual(ARR1ELEM1, helper.Array(ARR1)[0]);
            Assert.AreEqual(ARR2ELEM2, helper.Array(ARR2)[1]);
        }

        /* Tests removed because not verifiable
        
        [TestMethod]
        public void Constructor_FileWithErrors_DoesNotReadFile()
        {
            var helper = new IniFileHelper(Path.Combine(Paths.AssemblyPath, "INIFile", "SettingsTestsFail.ini"), false);
            Assert.IsFalse(helper.IniFileRead);
        }

        [TestMethod]
        public void Constructor_ValidFile_LoadsKeysAndArrays()
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
        public void Save_ModifiedSettings_WritesReadableFile()
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
        public void Parse_OnlyPragmas_ReturnsFalseWithoutException()
        {
            var helper = new IniFileHelper();

            Assert.IsFalse(helper.Parse(new List<string>() { SHOWKEYVALUE, SHOWKEYVALUE }));
            Assert.IsFalse(helper.IniFileRead);
        }

        [TestMethod]
        public void Parse_PragmaLines_DoesNotModifyCallerList()
        {
            var helper  = new IniFileHelper();
            var lines   = new List<string>() { SHOWKEYVALUE, KEYVALUE1, KEYVALUE2 };

            Assert.IsTrue(helper.Parse(lines));
            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual(SHOWKEYVALUE, lines[0]);
            Assert.IsTrue(helper.HasKeys(new string[] { KEY1, KEY2 }));
        }

        [TestMethod]
        public void Parse_PragmaAfterComment_IsParsedAsInvalidLine()
        {
            // Pragmas are recognised only on the first physical lines, as documented in SettingsTemplate.ini
            var helper = new IniFileHelper();

            Assert.IsFalse(helper.Parse(new List<string>() { "# Comment", SHOWKEYVALUE, KEYVALUE1 }));
        }

        [TestMethod]
        public void Parse_InlineComment_IsPartOfValue()
        {
            var helper = new IniFileHelper();

            Assert.IsTrue(helper.Parse(new List<string>() { $"{KEYVALUE1} # Comment" }));
            Assert.AreEqual($"{VALUE1} # Comment", helper.GetString(KEY1));
        }

        [TestMethod]
        [DoNotParallelize]
        public void CheckIniFileExists_DefaultPath_CopiesTemplateIntoAssemblyPath()
        {
            // Unique names, so the files cannot collide with those of other tests
            var iniFile             = $"{Guid.NewGuid()}.ini";
            var templateFile        = $"{Guid.NewGuid()}.ini";
            var iniFilePath         = Path.Combine(Paths.AssemblyPath, iniFile);
            var templateFilePath    = Path.Combine(Paths.AssemblyPath, templateFile);
            try
            {
                File.WriteAllText(templateFilePath, KEYVALUE1);

                var firstCheck  = IniFileHelper.CheckIniFileExists(iniFile, templateFile);
                var secondCheck = IniFileHelper.CheckIniFileExists(iniFile, templateFile);

                Assert.IsFalse(firstCheck.Exists);
                Assert.IsTrue(File.Exists(iniFilePath));
                Assert.IsTrue(secondCheck.Exists);
            }
            finally
            {
                File.Delete(iniFilePath);
                File.Delete(templateFilePath);
            }
        }

        [TestMethod]
        public void CheckIniFileExists_FullPath_CopiesTemplateIntoGivenPath()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(directory);
                var iniFilePath         = Path.Combine(directory, IniFileHelper.DEFAULTINIFILE);
                var templateFilePath    = Path.Combine(directory, IniFileHelper.INIFILETEMPLATE);
                File.WriteAllText(templateFilePath, KEYVALUE1);

                var check = IniFileHelper.CheckIniFileExists(iniFilePath, templateFilePath, false);

                Assert.IsFalse(check.Exists);
                Assert.IsTrue(File.Exists(iniFilePath));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [TestMethod]
        public void HasKey_NoKeys_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.HasKey(KEY1));
        }

        [TestMethod]
        public void HasKey_NullKey_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKey(null));
        }

        [TestMethod]
        public void HasKey_WhitespaceKey_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKey(string.Empty));
        }

        [TestMethod]
        public void HasKey_MissingKey_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKey(KEY5));
        }

        [TestMethod]
        public void HasKey_ExistingKey_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsTrue(helper.HasKey(KEY1));
        }

        [TestMethod]
        public void HasKeys_NoKeysStored_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            Assert.IsFalse(helper.HasKeys(GetKeys3()));
        }

        [TestMethod]
        public void HasKeys_NullParameter_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKeys(null));
        }

        [TestMethod]
        public void HasKeys_MoreKeysThanStored_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.IsFalse(helper.HasKeys(GetKeys4()));
        }

        [TestMethod]
        public void HasKeys_OneKeyMissing_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3Alternate(helper);
            Assert.IsFalse(helper.HasKeys(GetKeys3()));
        }

        [TestMethod]
        public void HasKeys_SubsetOfKeys_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.IsTrue(helper.HasKeys(GetKeys4()));
        }

        [TestMethod]
        public void HasKeys_AllKeys_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.IsTrue(helper.HasKeys(GetKeys5()));
        }

        [TestMethod]
        public void GetKeys_StoredKeys_ReturnsKeys()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.IsTrue(helper.GetKeys().SequenceEqual(GetKeys5().Select(k => k.ToUpper())));
        }

        [TestMethod]
        public void GetString_MissingKey_ReturnsEmptyString()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(helper.GetString(KEY5), string.Empty);
        }

        [TestMethod]
        public void GetString_ExistingKey_ReturnsValue()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(VALUE1, helper.GetString(KEY1));
        }

        [TestMethod]
        public void GetInt_MissingKey_ReturnsZero()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(0, helper.GetInt(KEY5));
        }

        [TestMethod]
        public void GetInt_ValueNotInt_ReturnsZero()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.AreEqual(0, helper.GetInt(KEY3));
        }

        [TestMethod]
        public void GetInt_ExistingKey_ReturnsValue()
        {
            var helper = new IniFileHelper();
            PopulateHelper5(helper);
            Assert.AreEqual(5, helper.GetInt(KEY5));
        }

        [TestMethod]
        public void GetBool_MissingKey_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            Assert.AreEqual(false, helper.GetBool(KEY5));
        }

        [TestMethod]
        public void GetBool_ValueNotBool_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(false, helper.GetBool(KEY5));
        }

        [TestMethod]
        public void GetBool_ValueTrue_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(true, helper.GetBool(KEY6));
        }

        [TestMethod]
        public void GetBool_ValueFalse_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(false, helper.GetBool(KEY7));
        }

        [TestMethod]
        public void GetBool_Value1_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(true, helper.GetBool(KEY8));
        }

        [TestMethod]
        public void GetBool_Value0_ReturnsFalse()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            Assert.AreEqual(false, helper.GetBool(KEY9));
        }

        #region AddKey

        /// <summary>
        /// Verifies that AddKey adds a new key with a string value.
        /// </summary>
        [TestMethod]
        public void AddKey_NewStringKey_AddsKey()
        {
            var helper  = new IniFileHelper();
            var result  = helper.AddKey("NewKey", "NewValue");

            Assert.IsTrue(result);
            Assert.IsTrue(helper.HasKey("NewKey"));
            Assert.AreEqual("NewValue", helper.GetString("NewKey"));
        }

        /// <summary>
        /// Verifies that AddKey updates an existing key.
        /// </summary>
        [TestMethod]
        public void AddKey_ExistingKey_UpdatesValue()
        {
            var helper = new IniFileHelper();
            helper.AddKey("Key", "Old");
            helper.AddKey("Key", "New");

            Assert.AreEqual("New", helper.GetString("Key"));
        }

        /// <summary>
        /// Verifies that AddKey with invalid parameters returns false.
        /// </summary>
        [TestMethod]
        [DataRow(null,  "value")]
        [DataRow("",    "value")]
        [DataRow("key", null)]
        [DataRow("key", "")]
        public void AddKey_InvalidParameters_ReturnsFalse(string key, string value)
        {
            var helper  = new IniFileHelper();
            var result  = helper.AddKey(key, value);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Verifies the AddKey(string, int) overload.
        /// </summary>
        [TestMethod]
        public void AddKey_IntValue_StoresAsString()
        {
            var helper = new IniFileHelper();
            helper.AddKey("Port", 8080);

            Assert.AreEqual(8080, helper.GetInt("Port"));
        }

        /// <summary>
        /// Verifies the AddKey(string, bool) overload with true.
        /// </summary>
        [TestMethod]
        public void AddKey_BoolTrue_StoresAsUppercaseString()
        {
            var helper = new IniFileHelper();
            helper.AddKey("Enabled", true);

            Assert.AreEqual("TRUE", helper.GetString("Enabled"));
            Assert.IsTrue(helper.GetBool("Enabled"));
        }

        /// <summary>
        /// Verifies the AddKey(string, bool) overload with false.
        /// </summary>
        [TestMethod]
        public void AddKey_BoolFalse_StoresAsUppercaseString()
        {
            var helper = new IniFileHelper();
            helper.AddKey("Enabled", false);

            Assert.AreEqual("FALSE", helper.GetString("Enabled"));
            Assert.IsFalse(helper.GetBool("Enabled"));
        }

        #endregion

        #region Set

        /// <summary>
        /// Verifies that Set updates an existing key.
        /// </summary>
        [TestMethod]
        public void Set_ExistingKey_UpdatesValue()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            helper.Set(KEY1, "Updated");

            Assert.AreEqual("Updated", helper.GetString(KEY1));
        }

        /// <summary>
        /// Verifies that Set does nothing for a non-existing key.
        /// </summary>
        [TestMethod]
        public void Set_MissingKey_DoesNothing()
        {
            var helper = new IniFileHelper();
            PopulateHelper3(helper);
            helper.Set("Missing", "Value");

            Assert.IsFalse(helper.HasKey("Missing"));
        }

        /// <summary>
        /// Verifies Set with the integer overload.
        /// </summary>
        [TestMethod]
        public void Set_IntValue_UpdatesValue()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            helper.Set(KEY4, 9999);

            Assert.AreEqual(9999, helper.GetInt(KEY4));
        }

        /// <summary>
        /// Verifies Set with the boolean overload.
        /// </summary>
        [TestMethod]
        public void Set_BoolValue_UpdatesValue()
        {
            var helper = new IniFileHelper();
            PopulateHelper9(helper);
            helper.Set(KEY6, false);

            Assert.IsFalse(helper.GetBool(KEY6));
        }

        #endregion

        #region Array operations

        /// <summary>
        /// Verifies AddArray creates an empty array.
        /// </summary>
        [TestMethod]
        public void AddArray_NewArray_CreatesEmptyArray()
        {
            var helper = new IniFileHelper();
            helper.AddArray("Colors");

            Assert.IsTrue(helper.HasArray("Colors"));
            Assert.AreEqual(0, helper.Array("Colors").Count);
        }

        /// <summary>
        /// Verifies that AddArray on an already existing array does not overwrite it.
        /// </summary>
        [TestMethod]
        public void AddArray_ExistingArray_DoesNotOverwrite()
        {
            var helper = new IniFileHelper();
            helper.AddArrayElement("Colors", "Red");
            helper.AddArray("Colors");

            Assert.AreEqual(1, helper.Array("Colors").Count);
        }

        /// <summary>
        /// Verifies AddArrayElement.
        /// </summary>
        [TestMethod]
        public void AddArrayElement_ValidElement_AddsToArray()
        {
            var helper = new IniFileHelper();
            helper.AddArrayElement("Colors", "Red");
            helper.AddArrayElement("Colors", "Blue");

            Assert.IsTrue(helper.HasArray("Colors"));
            Assert.AreEqual(2, helper.Array("Colors").Count);
        }

        /// <summary>
        /// Verifies ClearArray on an existing array.
        /// </summary>
        [TestMethod]
        public void ClearArray_ExistingArray_RemovesAllElements()
        {
            var helper = new IniFileHelper();
            helper.AddArrayElement("Colors", "Red");
            helper.AddArrayElement("Colors", "Blue");
            var result = helper.ClearArray("Colors");

            Assert.IsTrue(result);
            Assert.IsTrue(helper.HasArray("Colors"));
            Assert.AreEqual(0, helper.Array("Colors").Count);
        }

        /// <summary>
        /// Verifies ClearArray on a non-existing array.
        /// </summary>
        [TestMethod]
        public void ClearArray_MissingArray_ReturnsFalse()
        {
            var helper  = new IniFileHelper();
            var result  = helper.ClearArray("Missing");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Verifies RemoveArray.
        /// </summary>
        [TestMethod]
        public void RemoveArray_ExistingArray_RemovesArray()
        {
            var helper = new IniFileHelper();
            helper.AddArray("Colors");
            helper.RemoveArray("Colors");

            Assert.IsFalse(helper.HasArray("Colors"));
        }

        /// <summary>
        /// Verifies AddArray with a typed list.
        /// </summary>
        [TestMethod]
        public void AddArray_TypedList_AddsAllElements()
        {
            var helper = new IniFileHelper();
            helper.AddArray("Numbers", new List<int> { 1, 2, 3 });

            Assert.AreEqual(3, helper.Array("Numbers").Count);
        }

        /// <summary>
        /// Verifies that Array returns an empty list for a non-existing name.
        /// </summary>
        [TestMethod]
        public void Array_MissingName_ReturnsEmptyList()
        {
            var helper  = new IniFileHelper();
            var result  = helper.Array("Missing");

            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Verifies that HasArray is case-insensitive.
        /// </summary>
        [TestMethod]
        public void HasArray_DifferentCase_ReturnsTrue()
        {
            var helper = new IniFileHelper();
            helper.AddArray("Colors");

            Assert.IsTrue(helper.HasArray("colors"));
            Assert.IsTrue(helper.HasArray("COLORS"));
        }

        #endregion

        #region GetDaysOfWeek

        /// <summary>
        /// Verifies GetDaysOfWeek with a valid value.
        /// </summary>
        [TestMethod]
        public void GetDaysOfWeek_ValidValue_ReturnsParsedDays()
        {
            var helper = new IniFileHelper();
            helper.Parse(new List<string> { "Days=1 3 5" });
            var result = helper.GetDaysOfWeek("Days");

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.DaysOfWeek.Count);
            Assert.IsTrue(result.Contains(DayOfWeek.Monday));
            Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
            Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        }

        /// <summary>
        /// Verifies GetDaysOfWeek with a missing key.
        /// </summary>
        [TestMethod]
        public void GetDaysOfWeek_MissingKey_ReturnsEmptyDays()
        {
            var helper  = new IniFileHelper();
            var result  = helper.GetDaysOfWeek("Missing");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.DaysOfWeek.Count);
        }

        /// <summary>
        /// Verifies GetDaysOfWeek with an invalid value: an empty instance is returned.
        /// </summary>
        [TestMethod]
        public void GetDaysOfWeek_InvalidValue_ReturnsEmptyDays()
        {
            var helper = new IniFileHelper();
            helper.Parse(new List<string> { "Days=1 9" });

            var result = helper.GetDaysOfWeek("Days");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.DaysOfWeek.Count);
        }

        #endregion

        #region Default values

        /// <summary>
        /// Verifies that the Get methods return the default value passed for a missing key.
        /// </summary>
        [TestMethod]
        public void GetMethods_MissingKeyWithDefault_ReturnDefaultValue()
        {
            var helper = new IniFileHelper();

            Assert.AreEqual("Default", helper.GetString("Missing", "Default"));
            Assert.AreEqual(42, helper.GetInt("Missing", 42));
            Assert.IsTrue(helper.GetBool("Missing", true));
        }

        /// <summary>
        /// Verifies that GetInt and GetBool return the default value passed for an invalid value.
        /// </summary>
        [TestMethod]
        public void GetMethods_InvalidValueWithDefault_ReturnDefaultValue()
        {
            var helper = new IniFileHelper();
            helper.Parse(new List<string> { "Number = abc", "Flag = maybe" });

            Assert.AreEqual(42, helper.GetInt("Number", 42));
            Assert.IsTrue(helper.GetBool("Flag", true));
        }

        #endregion

        #region Keys / Arrays / Set

        /// <summary>
        /// Verifies that Keys and Arrays expose the parsed keys (uppercase) and arrays.
        /// </summary>
        [TestMethod]
        public void KeysAndArrays_ParsedLines_ExposeKeysAndArrays()
        {
            var helper = new IniFileHelper();
            helper.Parse(new List<string>(GetValidLines().Concat(GetValidArrayLines())));

            Assert.HasCount(4, helper.Keys);
            Assert.AreEqual(VALUE1, helper.Keys[KEY1.ToUpper()]);
            Assert.HasCount(2, helper.Arrays);
            Assert.HasCount(3, helper.Arrays[ARR1.ToUpper()]);
        }

        /// <summary>
        /// Verifies that Set(DaysOfWeekParameter) updates an existing key with the string representation of the days.
        /// </summary>
        [TestMethod]
        public void Set_DaysOfWeekExistingKey_UpdatesValue()
        {
            var helper = new IniFileHelper();
            helper.AddKey("Days", "1");

            helper.Set("Days", new DaysOfWeekParameter("5 1"));

            Assert.AreEqual("1 5", helper.GetString("Days"));
        }

        /// <summary>
        /// Verifies that Set(DaysOfWeekParameter) does nothing for a missing key.
        /// </summary>
        [TestMethod]
        public void Set_DaysOfWeekMissingKey_DoesNothing()
        {
            var helper = new IniFileHelper();

            helper.Set("Days", new DaysOfWeekParameter("1"));

            Assert.IsFalse(helper.HasKey("Days"));
        }

        /// <summary>
        /// Verifies that AddArray&lt;T&gt; does not add values already present, also among the values passed.
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-032")]
        public void AddArray_TypedListWithDuplicates_AddsEachValueOnce()
        {
            var helper = new IniFileHelper();
            helper.AddArray(ARR1, new List<int> { 1 });

            helper.AddArray(ARR1, new List<int> { 1, 2, 2, 3 });

            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, helper.Array(ARR1).ToArray());
        }

        /// <summary>
        /// Verifies that AddArrayElement creates the missing array and trims the element (null becomes empty).
        /// </summary>
        [TestMethod]
        public void AddArrayElement_MissingArray_CreatesArrayWithElement()
        {
            var helper = new IniFileHelper();

            helper.AddArrayElement(ARR3, "  Elem  ");
            helper.AddArrayElement(ARR3, null);

            Assert.IsTrue(helper.HasArray(ARR3));
            CollectionAssert.AreEqual(new[] { "Elem", string.Empty }, helper.Array(ARR3).ToArray());
        }

        /// <summary>
        /// Verifies that the array methods ignore an empty array name.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ArrayMethods_EmptyName_DoNothing(string arrayName)
        {
            var helper = new IniFileHelper();

            helper.AddArray(arrayName);
            helper.AddArrayElement(arrayName, "Elem");
            helper.RemoveArray(arrayName);

            Assert.IsFalse(helper.ClearArray(arrayName));
            Assert.HasCount(0, helper.Array(arrayName));
            Assert.HasCount(0, helper.Arrays);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Verifies that the default constructor creates an empty helper not read from file.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_CreatesEmptyHelper()
        {
            var helper = new IniFileHelper(false);

            Assert.IsFalse(helper.IniFileRead);
            Assert.HasCount(0, helper.Keys);
            Assert.HasCount(0, helper.Arrays);
        }

        /// <summary>
        /// Verifies that the constructor with a missing file does not read anything.
        /// </summary>
        [TestMethod]
        public void Constructor_MissingFile_DoesNotReadFile()
        {
            var helper = new IniFileHelper(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ini"), false);

            Assert.IsFalse(helper.IniFileRead);
            Assert.HasCount(0, helper.Keys);
        }

        #endregion

        #endregion

    }
}