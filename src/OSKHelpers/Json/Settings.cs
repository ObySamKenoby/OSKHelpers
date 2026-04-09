using OSKHelpers.Json;

namespace OSKHelpers.Templates.Json
{
    /// <summary>
    /// Template class for using <see cref="OSKHelpers.Json.JsonSettings{T}"/>.
    /// </summary>
    public class Settings : JsonSettings<Settings>
    {
        #region Constants

        /// <inheritdoc/>
        protected override int LASTVERSION => 1;

        #endregion

        #region Members

        // Private members of the object.

        #endregion

        #region Properties

        // Private properties of the object.

        #endregion

        #region Constructors

        /// <summary>
        /// Base constructor; initialises the required properties.
        /// </summary>
        public Settings() : base()
        { }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void Init()
        {
            // Add initialisation logic to be executed after the object properties have been populated.
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
