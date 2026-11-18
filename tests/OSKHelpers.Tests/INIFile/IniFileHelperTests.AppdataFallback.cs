#if DEBUG

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSKHelpers.Common;
using OSKHelpers.INIFile;
using System;
using System.IO;

namespace OSKHelpers.Tests.INIFile
{
    /// <summary>
    /// Tests of the lookup of the INI file in <see cref="Paths.AppdataDirectory"/> when it is missing in the assembly
    /// directory (intervention 048): they change the shared state of <see cref="Paths"/>, so they never run in parallel.
    /// </summary>
    public partial class IniFileHelperTests
    {
        #region Methods

        #region Appdata fallback helper methods

        /// <summary>
        /// Runs <paramref name="test"/> with a temporary data directory and an INI file name that is unique to the test,
        /// restoring <see cref="Paths.AppdataDirectory"/> and removing the files at the end.
        /// </summary>
        private static void RunWithTemporaryAppdata(Action<string, string> test)
        {
            var originalAppdata = Paths.AppdataDirectory;
            var appdata         = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var iniFileName     = $"{Guid.NewGuid()}.ini";
            try
            {
                Directory.CreateDirectory(appdata);
                Paths.OverrideAppdataDirectory(appdata);

                test(appdata, iniFileName);
            }
            finally
            {
                Paths.OverrideAppdataDirectory(originalAppdata);
                File.Delete(Path.Combine(Paths.AssemblyPath, iniFileName));
                if (Directory.Exists(appdata))
                {
                    Directory.Delete(appdata, true);
                }
            }
        }

        #endregion

        #region Appdata fallback

        /// <summary>
        /// Verifies that Load reads the file from the data directory when it is missing in the assembly directory.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void Load_DefaultPathFileOnlyInAppdataDirectory_ReadsFileFromAppdata()
        {
            RunWithTemporaryAppdata((appdata, iniFileName) =>
            {
                File.WriteAllLines(Path.Combine(appdata, iniFileName), new[] { KEYVALUE1 });
                var helper = new IniFileHelper();

                var read = helper.Load(iniFileName);

                Assert.IsTrue(read);
                Assert.AreEqual(VALUE1, helper.GetString(KEY1));
            });
        }

        /// <summary>
        /// Verifies that the file in the assembly directory wins over the one in the data directory.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void Load_DefaultPathFileInBothDirectories_PrefersAssemblyPath()
        {
            RunWithTemporaryAppdata((appdata, iniFileName) =>
            {
                File.WriteAllLines(Path.Combine(appdata, iniFileName), new[] { KEYVALUE1 });
                File.WriteAllLines(Path.Combine(Paths.AssemblyPath, iniFileName), new[] { KEYVALUE2 });
                var helper = new IniFileHelper();

                var read = helper.Load(iniFileName);

                Assert.IsTrue(read);
                Assert.AreEqual(VALUE2, helper.GetString(KEY2));
                Assert.AreEqual(string.Empty, helper.GetString(KEY1));
            });
        }

        /// <summary>
        /// Verifies that Save writes the file it has read from the data directory, without creating one in the assembly
        /// directory.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void Save_DefaultPathFileOnlyInAppdataDirectory_WritesFileInAppdata()
        {
            RunWithTemporaryAppdata((appdata, iniFileName) =>
            {
                var appdataFile = Path.Combine(appdata, iniFileName);
                File.WriteAllLines(appdataFile, new[] { KEYVALUE1 });
                var helper = new IniFileHelper();
                helper.Load(iniFileName);
                helper.Set(KEY1, VALUE3);

                var saved = helper.Save(iniFileName);

                Assert.IsTrue(saved);
                Assert.IsFalse(File.Exists(Path.Combine(Paths.AssemblyPath, iniFileName)));
                Assert.IsTrue(new IniFileHelper(appdataFile, false).IniFileRead);
                Assert.AreEqual(VALUE3, new IniFileHelper(appdataFile, false).GetString(KEY1));
            });
        }

        /// <summary>
        /// Verifies that Save creates a new file in the assembly directory when it exists in neither directory.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void Save_DefaultPathFileInNeitherDirectory_WritesFileInAssemblyPath()
        {
            RunWithTemporaryAppdata((appdata, iniFileName) =>
            {
                var helper = new IniFileHelper();
                helper.Set(KEY1, VALUE1);

                var saved = helper.Save(iniFileName);

                Assert.IsTrue(saved);
                Assert.IsTrue(File.Exists(Path.Combine(Paths.AssemblyPath, iniFileName)));
                Assert.IsFalse(File.Exists(Path.Combine(appdata, iniFileName)));
            });
        }

        /// <summary>
        /// Verifies that CheckIniFileExists finds the file in the data directory and does not copy the template.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [TestCategory("Intervento-048")]
        public void CheckIniFileExists_FileOnlyInAppdataDirectory_ReturnsTrueWithoutCopyingTemplate()
        {
            RunWithTemporaryAppdata((appdata, iniFileName) =>
            {
                File.WriteAllLines(Path.Combine(appdata, iniFileName), new[] { KEYVALUE1 });

                var (exists, message) = IniFileHelper.CheckIniFileExists(iniFileName);

                Assert.IsTrue(exists);
                Assert.AreEqual(string.Empty, message);
                Assert.IsFalse(File.Exists(Path.Combine(Paths.AssemblyPath, iniFileName)));
            });
        }

        #endregion

        #endregion
    }
}

#endif
