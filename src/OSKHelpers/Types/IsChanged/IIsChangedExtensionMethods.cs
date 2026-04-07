using System;
using System.Collections.Generic;
using System.Text;

namespace OSKHelpers.Types.IsChanged
{
    public static class IIsChangedExtensionMethods
    {
        #region Methods

        /// <summary>
        /// Sets the value of the member passed as parameter, returning True if the update succeeded.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="field">Member (passed by reference).</param>
        /// <param name="newValue">New value to assign to the member.</param>
        /// <param name="onValueChanging">
        /// Function called before the value is changed, only when the new value differs from the current one;<br/>
        /// if it returns False the change is cancelled.
        /// <code>
        /// bool OnValueChanging(T newValue)
        /// </code>
        /// <b>newValue</b>: new value of the property;<br/>
        /// <b>Returns</b>: False if the value update must not be completed.
        /// </param>
        /// <param name="onValueChanged">
        /// Action executed after the value has been updated and after IsChanged has been set to True.
        /// <code>
        /// void OnValueChanged(T oldValue, T newValue)
        /// </code>
        /// <b>oldValue</b>: previous value of the property;<br/>
        /// <b>newValue</b>: new value of the property.
        /// </param>
        /// <returns>True if the property value was updated.</returns>
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
        /// Action executed after the value has been updated and after IsChanged has been set to True.
        /// </param>
        /// <inheritdoc cref="SetProperty{T}(IIsChanged, ref T, T, Func{T, bool}, Action{T, T})"/>
        public static bool SetProperty<T>(this IIsChanged obj, ref T field, T newValue, Func<T, bool> onValueChanging, Action onValueChanged)
            => obj.SetProperty(ref field, newValue, onValueChanging, new Action<T, T>((v1, v2) => { onValueChanged(); }));

        /// <param name="onValueChanged">
        /// Action executed after the value has been updated and after IsChanged has been set to True.
        /// <code>
        /// void OnValueChanged(T newValue)
        /// </code>
        /// <b>newValue</b>: new value of the property.
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

        // Methods for special use cases

        /// <summary>
        /// Sets the value of the field passed by reference by converting the string <seealso cref="PropertyStringUtils.SetProperty(ref decimal, string)"/>
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
        /// Sets the value of the field passed by reference by converting the string <seealso cref="PropertyStringUtils.SetProperty(ref decimal, string)"/>
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
        /// Sets the value of IsChanged.
        /// </summary>
        public static void SetIsChanged(this IIsChanged obj, bool isChanged = true)
        {
            obj.IsChanged = isChanged;
        }

        /// <summary>
        /// Resets the IsChanged state.<br/>
        /// Can be overridden in derived classes for more complex scenarios (collections or other).
        /// </summary>
        public static void ResetIsChanged(this IIsChanged obj)
        {
            obj.ResetIsChangedExecute();
            obj.IsChanged = false;
        }

        #endregion

    }
}
