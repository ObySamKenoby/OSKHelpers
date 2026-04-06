using System;
using System.Collections.Generic;
using System.Text;

namespace OSKHelpers.Types.IsChanged
{
    /// <summary>
    /// Interfaccia base per l'implemntazione di classi che vogliano sfruttare il campo IsChanged.<br/>
    /// Tali classi per funzionare correttamente devono implementare questa interfaccia e referenziare il<br/>
    /// namespace <see cref="OSKHelpers.Types.IsChanged"/> in modo da poter accedere ai metodi d'estensione.
    /// </summary>
    public interface IIsChanged
    {
        #region Proprietà

        bool IsChanged { get; set; }

        #endregion

        #region Metodi

        /// <summary>
        /// Implementazioni necessarie per resettare lo stato sdi IsChanged (reset dello stato di elementi di collezioni, reset di particolari oggetti eccetera).
        /// </summary>
        void ResetIsChangedExecute();

        #endregion
    }
}
