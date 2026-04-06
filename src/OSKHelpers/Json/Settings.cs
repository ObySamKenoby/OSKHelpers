using OSKHelpers.Json;

namespace OSKHelpers.Templates.Json
{
    /// <summary>
    /// Classe template per l'utilizzo di <see cref="OSKHelpers.Json.JsonSettings{T}"/>
    /// </summary>
    public class Settings : JsonSettings<Settings>
    {
        #region Costanti

        /// <inheritdoc/>
        protected override int LASTVERSION => 1;

        #endregion

        #region Membri

        // Membri privati dell'oggetto.

        #endregion

        #region Proprietà

        // Proprietà private dell'oggetto.

        #endregion

        #region Costruttori

        /// <summary>
        /// Costruttore di base, inizializza le proprietà necessarie.
        /// </summary>
        public Settings() : base()
        { }

        #endregion

        #region Metodi

        /// <inheritdoc/>
        public override void Init()
        {
            // Aggiungere la logica di inizializzazione successiva al popolamento delle proprietà dell'oggetto.
        }

        /// <inheritdoc/>
        public override bool CheckIsValid()
        {
            IsValid = true;
            return IsValid;
        }

        #endregion
    }
}
