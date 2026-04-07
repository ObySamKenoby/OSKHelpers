using System.IO;

namespace OSKHelpers.Docker
{
    /// <summary>
    /// Utilities for customising application behaviour when running inside a container.
    /// </summary>
    public class DockerUtils
    {
        #region Properties

        /// <summary>
        /// True if the application is running inside a container.
        /// </summary>
        public static bool IsDockerized {  get; private set; }

        #endregion

        #region Constructor

        static DockerUtils()
        {
            IsDockerized = File.Exists("/.dockerenv");
        }

        #endregion

        #region Methods




        #endregion
    }
}
