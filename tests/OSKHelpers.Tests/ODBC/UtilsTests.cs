using OSKHelpers.ODBC;

namespace OSKHelpers.Tests.ODBC
{
    [TestClass]
    public class UtilsTests
    {
        #region Methods

        [TestMethod]
        [DataRow(null,      null,   null,       false,  "")]
        [DataRow(" ",       "user", "pwd",      false,  "")]
        [DataRow("DSN1",    null,   null,       false,  "DSN=DSN1;")]
        [DataRow(" DSN1 ",  null,   null,       true,   "DSN=DSN1;Trusted_Connection=Yes;")]
        [DataRow("DSN1",    "user", "pwd",      false,  "DSN=DSN1;Uid=user;Pwd=pwd;")]
        [DataRow("DSN1",    " user ", " pwd ",  true,   "DSN=DSN1;Uid=user;Pwd=pwd;")]
        [DataRow("DSN1",    "user", null,       false,  "DSN=DSN1;Uid=user;Pwd=;")]
        [DataRow("DSN1",    null,   "pwd",      false,  "DSN=DSN1;Uid=;Pwd=pwd;")]
        public void GetConnectionString_Parameters_ReturnsExpectedString(string dsnName, string username, string password, bool trustedConnection, string expected)
        {
            Assert.AreEqual(expected, Utils.GetConnectionString(dsnName, username, password, trustedConnection));
        }

        /// <summary>
        /// Integration test: verifies that the data sources are read from the registry without exceptions
        /// (on non-Windows platforms the list is empty).
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public void GetODBCSources_CurrentSystem_ReturnsListWithoutEmptyNames()
        {
            var sources = Utils.GetODBCSources();

            Assert.IsNotNull(sources);
            Assert.IsFalse(sources.Exists(string.IsNullOrWhiteSpace));
            if (!System.OperatingSystem.IsWindows())
            {
                Assert.IsEmpty(sources);
            }
        }

        #endregion
    }
}
