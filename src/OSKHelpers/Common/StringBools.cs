using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Rappresenta un array di booleani sotto forma di stringa dove ogni valore True è rappresentato con un 1 e ogni False con uno 0
    /// </summary>
    public class StringBools
    {
        #region Costanti

        public const int    TrueInt     = 1;
        public const int    FalseInt    = 0;
        public const string TrueString  = "1";
        public const string FalseString = "0";
        public const char   TrueChar    = '1';
        public const char   FalseChar   = '0';

        #endregion

        #region Membri

        private List<bool> _bools;

        #endregion

        #region Proprietà

        /// <summary>
        /// True if there is at least one True among values
        /// </summary>
        public bool AnyTrue => _bools.Any(b => b);

        /// <summary>
        /// True if there is at least one False among values
        /// </summary>
        public bool AnyFalse => _bools.Any(b => !b);

        /// <summary>
        /// Fired when one of the values changes.
        /// </summary>
        public event EventHandler ValueChanged;

        /// <summary>
        /// Returns the length of bool array
        /// </summary>
        public int Length => _bools.Count;

        #endregion

        #region Costruttori

        public StringBools()
        {
            _bools = new List<bool>();
        }

        public StringBools(IEnumerable<bool> bools) : this()
        {
            if (bools != null)
            {
                _bools.AddRange(bools);
            }
        }

        public StringBools(string bools) : this()
        {
            if (!string.IsNullOrEmpty(bools))
            {
                _bools.AddRange(bools.Select(c => c == '1'));
            }
        }

        public StringBools(string bools, int length) : this()
        {
            UpdateFromString(bools, length);
        }

        public StringBools(int length) : this()
        {
            SetLength(length);
        }

        #endregion

        #region Metodi

        /// <summary>
        /// Imposta la lunghezza dell'array.<br/>
        /// Se length fosse minore o uguale a 0 la lunghezza dell'array  sarà impostata a 0.<br/>
        /// Attenzione: tutto il contenuto dell'array sarà cancellato.
        /// </summary>
        /// <param name="length">Lunghezza desiderata</param>
        public void SetLength(int length)
        {
            _bools.Clear();
            if (length > 0)
            {
                _bools.AddRange(new bool[length]);
                Reset();
            }
        }

        /// <summary>
        /// Imposta il valore nella posizione pos. Se la posizione non è valida non accade niente.
        /// </summary>
        /// <param name="pos">Posizione</param>
        /// <param name="value">Valore da impostare</param>
        public void SetValue(int pos,  bool value)
        {
            if (pos >= 0 && pos < _bools.Count) 
            {
                _bools[pos] = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Imposta il valore nella posizione pos. Se la posizione non è valida non accade niente.
        /// </summary>
        /// <param name="pos">Posizione</param>
        /// <param name="value">Valore da impostare (1 True, qualsiasi altro valore False)</param>
        public void SetValue(int pos, int value)
        {
            if (pos >= 0 && pos < _bools.Count)
            {
                _bools[pos] = value == 1;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Imposta il valore nella posizione pos. Se la posizione non è valida non accade niente.
        /// </summary>
        /// <param name="pos">Posizione</param>
        /// <param name="value">Valore da impostare (1 True, qualsiasi altro valore False)</param>
        public void SetValue(int pos, string value)
        {
            if (pos >= 0 && pos < _bools.Count)
            {
                _bools[pos] = value == "1";
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Imposta il valore nella posizione pos. Se la posizione non è valida non accade niente.
        /// </summary>
        /// <param name="pos">Posizione</param>
        /// <param name="value">Valore da impostare (1 True, qualsiasi altro valore False)</param>
        public void SetValue(int pos, char value)
        {
            if (pos >= 0 && pos < _bools.Count)
            {
                _bools[pos] = value == '1';
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Restituisce il valore alla posizione pos. Se la posizione non è valida restituisce false.
        /// </summary>
        /// <param name="pos">Posizione di cui si desidera recuperare il valore</param>
        public bool Get(int pos)
        {
            return pos >= 0 && pos < _bools.Count ? _bools[pos] : false;
        }

        /// <summary>
        /// Restituisce come intero il valore alla posizione pos. Se la posizione non è valida restituisce 0.
        /// </summary>
        /// <param name="pos">Posizione di cui si desidera recuperare il valore</param>
        public int GetInt(int pos)
        {
            return pos >= 0 && pos < _bools.Count && _bools[pos] ? 1 : 0;
        }

        /// <summary>
        /// Restituisce come stringa il valore alla posizione pos. Se la posizione non è valida restituisce "0".
        /// </summary>
        /// <param name="pos">Posizione di cui si desidera recuperare il valore</param>
        public string GetString(int pos)
        {
            return pos >= 0 && pos < _bools.Count && _bools[pos] ? "1" : "0";
        }

        /// <summary>
        /// Restituisce come char il valore alla posizione pos. Se la posizione non è valida restituisce '0'.
        /// </summary>
        /// <param name="pos">Posizione di cui si desidera recuperare il valore</param>
        public char GetChar(int pos)
        {
            return pos >= 0 && pos < _bools.Count && _bools[pos] ? '1' : '0';
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Join("", _bools.Select(b => b ? "1" : "0"));
        }

        /// <summary>
        /// Resetta tutti gli elementi al valore passato come parametro (default false)
        /// </summary>
        public void Reset(bool value = false)
        {
            for (var i = 0; i < _bools.Count; i++)
            {
                _bools[i] = value;
            }
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Aggiorna i valori dalla stringa passata in <paramref name="bools"/> e setta <see cref="Length"/> a <paramref name="length"/>.
        /// </summary>
        /// <param name="bools">Stringa da cui sarnno recuperati i valori ('1' uguale a True).</param>
        /// <param name="length">Valore da assegnare a <see cref="Length"/>.c</param>
        public void UpdateFromString(string bools, int length)
        {
            SetLength(length);

            if (!string.IsNullOrEmpty(bools))
            {
                var max = bools.Length <= length ? bools.Length : length;
                for (var i = 0; i < max; i++)
                {
                    _bools[i] = bools[i] == '1';
                }
            }
        }

        #endregion
    }
}
