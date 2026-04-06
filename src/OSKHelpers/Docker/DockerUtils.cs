using System.IO;

namespace OSKHelpers.Docker
{
    /// <summary>
    /// Utilità per la personalizzazione delle funzionalità in caso di esecuzione all'interno di un container.
    /// </summary>
    public class DockerUtils
    {
        #region Proprietà

        /// <summary>
        /// Ha valore true se l'applicativo viene eseguito all'interno di un container.
        /// </summary>
        public static bool IsDockerized {  get; private set; }

        #endregion

        #region Costruttore

        static DockerUtils()
        {
            IsDockerized = File.Exists("/.dockerenv");
        }

        #endregion

        #region Metodi




        #endregion
    }
}
