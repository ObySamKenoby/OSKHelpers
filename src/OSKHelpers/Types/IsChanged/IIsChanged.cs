using System;
using System.Collections.Generic;
using System.Text;

namespace OSKHelpers.Types.IsChanged
{
    /// <summary>
    /// Base interface for implementing classes that want to use the IsChanged field.<br/>
    /// Such classes must implement this interface and reference the<br/>
    /// <see cref="OSKHelpers.Types.IsChanged"/> namespace to access the extension methods.
    /// </summary>
    public interface IIsChanged
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the object has unsaved changes.
        /// </summary>
        bool IsChanged { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Implementations required to reset the IsChanged state (resetting collection element states, specific objects, etc.).
        /// </summary>
        void ResetIsChangedExecute();

        #endregion
    }
}
