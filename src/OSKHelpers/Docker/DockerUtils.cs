using System.IO;

namespace OSKHelpers.Docker
{
    /// <summary>
    /// Utilities for customising application behaviour when running inside a container.
    /// </summary>
    public class DockerUtils
    {
        #region Constants

        private const string DOCKERENVPATH = "/.dockerenv";

        #endregion

        #region Properties

        /// <summary>
        /// True if the application is running inside a container.
        /// </summary>
        public static bool IsDockerized {  get; private set; }

        #endregion

        #region Constructor

        static DockerUtils()
        {
            IsDockerized = CheckIsDockerized();
        }

        #endregion

        #region Methods

#if DEBUG
        /// <summary>
        /// Checks whether the application is running inside a container, by verifying the existence of the
        /// <c>/.dockerenv</c> file.<br/>
        /// Unlike <see cref="IsDockerized"/>, evaluated once at startup, the check is performed on every call.
        /// </summary>
        /// <param name="path">For testing only: path of the file to check, by default <c>/.dockerenv</c>.</param>
        /// <returns>True if the file exists.</returns>
        public static bool CheckIsDockerized(string path = DOCKERENVPATH)
#else
        /// <summary>
        /// Checks whether the application is running inside a container, by verifying the existence of the
        /// <c>/.dockerenv</c> file.<br/>
        /// Unlike <see cref="IsDockerized"/>, evaluated once at startup, the check is performed on every call.
        /// </summary>
        /// <returns>True if the file exists.</returns>
        public static bool CheckIsDockerized()
#endif
        {
#if !DEBUG
            var path = DOCKERENVPATH;
#endif

            return File.Exists(path);
        }

        #endregion
    }
}
