using System;
using System.Collections.Generic;
using System.Text;

namespace OSKHelpers.Types.IsChanged
{
    public static class IIsChangedExtensionMethods
    {
        #region Metodi

        /// <summary>
        /// Imposta il vaore del membro passato come parametro restituendo True se è andato a buon fine.
        /// </summary>
        /// <typeparam name="T">Tipologia della proprietà.</typeparam>
        /// <param name="field">Membro (passato come riferimento).</param>
        /// <param name="newValue">Nuovo valore da attribuire al membro.</param>
        /// <param name="onValueChanging">
        /// Funzione che sarà richiamata prima della modifica del valore nel caso in cui questo sia diverso dal precedente,<br/>
        /// se sarà resituito un valore False la modifica sarà annullata.
        /// <code>
        /// bool OnValueChanging(T newValue)
        /// </code>
        /// <b>newValue</b>: nuovo valore della proprietà;<br/>
        /// <b>Restituisce</b>: False se l'aggiornamento del valore non deve essere portato a termine.
        /// </param>
        /// <param name="onValueChanged">
        /// Azione che sarà eseguita dopo l'aggiornamento del valore e dopo che IsChanged è stato impostato a True.
        /// <code>
        /// void OnValueChanged(T oldValue, T newValue)
        /// </code>
        /// <b>oldValue</b>: vecchio valore della proprietà;<br/>
        /// <b>newValue</b>: nuovo valore della proprietà.
        /// </param>
        /// <returns>True se il valore della proprietà è stato aggiornato.</returns>
        public static bool SetProperty<T>(this IIsChanged obj, ref T field, T newValue, Func<T, bool> onValueChanging = null, Action<T, T> onValueChanged = null)
        {
            bool changed = false;

            if (!EqualityComparer<T>.Default.Equals(field, newValue))
            {
                if (onValueChanging == null || onValueChanging(newValue))
                {
                    var oldValue    = field;
                    field           = newValue;
                    changed         = true;
                    obj.SetIsChanged();
                    if (onValueChanged != null)
                    {
                        onValueChanged(oldValue, newValue);
                    }
                }

            }

            return changed;
        }

        /// <param name="onValueChanged">
        /// Azione che sarà eseguita dopo l'aggiornamento del valore e dopo che IsChanged è stato impostato a True.
        /// </param>
        /// <inheritdoc cref="SetProperty{T}(IIsChanged, ref T, T, Func{T, bool}, Action{T, T})"/>
        public static bool SetProperty<T>(this IIsChanged obj, ref T field, T newValue, Func<T, bool> onValueChanging, Action onValueChanged)
            => obj.SetProperty(ref field, newValue, onValueChanging, new Action<T, T>((v1, v2) => { onValueChanged(); }));

        /// <param name="onValueChanged">
        /// Azione che sarà eseguita dopo l'aggiornamento del valore e dopo che IsChanged è stato impostato a True.
        /// <code>
        /// void OnValueChanged(T newValue)
        /// </code>
        /// <b>newValue</b>: nuovo valore della proprietà.
        /// </param>
        public static bool SetProperty<T>(this IIsChanged obj, ref T field, T newValue, Func<T, bool> onValueChanging, Action<T> onValueChanged)
            => obj.SetProperty(ref field, newValue, onValueChanging, new Action<T, T>((oldVal, newVal) => { onValueChanged(newValue); }));

        /// <inheritdoc cref="SetProperty{T}(IIsChanged, ref T, T, Func{T, bool}, Action{T, T})"/>
        public static bool SetProperty<T>(this IIsChanged obj, ref T field, T newValue, Action<T, T> onValueChanged)
            => obj.SetProperty(ref field, newValue, null, onValueChanged);

        /// <inheritdoc cref="SetProperty{T}(IIsChanged, ref T, T, Action{T})"/>
        public static bool SetProperty<T>(this IIsChanged obj, ref T field, T newValue, Action onValueChanged)
            => obj.SetProperty(ref field, newValue, null, new Action<T, T>((v1, v2) => { onValueChanged(); }));

        /// <inheritdoc cref="SetProperty{T}(IIsChanged, ref T, T, Func{T, bool}, Action{T, T})"/>
        public static bool SetProperty<T>(this IIsChanged obj, ref T field, T newValue, Action<T> onValueChanged)
            => obj.SetProperty(ref field, newValue, null, new Action<T, T>((oldVal, newVal) => { onValueChanged(newValue); }));

        // Metodi per utilizzi particolari

        /// <summary>
        /// Imposta il valore del campo passato come parametro di riferimento convertendo la stringa <seealso cref="PropertyStringUtils.SetProperty(ref decimal, string)"/>
        /// </summary>
        /// <inheritdoc cref="SetProperty{T}(IIsChanged, ref T, T, Func{T, bool}, Action{T, T})"/>
        public static bool SetProperty(this IIsChanged obj, ref decimal field, string newValue)
        {
            bool changed = PropertyStringUtils.SetProperty(ref field, newValue);

            if (changed)
            {
                obj.SetIsChanged();
            }

            return changed;
        }

        /// <summary>
        /// Imposta il valore del campo passato come parametro di riferimento convertendo la stringa <seealso cref="PropertyStringUtils.SetProperty(ref decimal, string)"/>
        /// </summary>
        /// <inheritdoc cref="SetProperty{T}(IIsChanged, ref T, T, Func{T, bool}, Action{T, T})"/>
        public static bool SetProperty(this IIsChanged obj, ref decimal? field, string newValue)
        {
            bool changed = PropertyStringUtils.SetProperty(ref field, newValue);

            if (changed)
            {
                obj.SetIsChanged();
            }

            return changed;
        }



        /// <summary>
        /// Imposta il valore di IsChanged.
        /// </summary>
        public static void SetIsChanged(this IIsChanged obj, bool isChanged = true)
        {
            obj.IsChanged = isChanged;
        }

        /// <summary>
        /// Resetta lo stato di Ischanged.<br/>
        /// Può essere effettuato un override nelle classi derivate per scenari più complessi (collezioni o altro).
        /// </summary>
        public static void ResetIsChanged(this IIsChanged obj)
        {
            obj.ResetIsChangedExecute();
            obj.IsChanged = false;
        }

        #endregion

    }
}
