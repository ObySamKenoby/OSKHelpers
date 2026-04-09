using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Represents an array of booleans as a string where each true value is represented as '1' and each false as '0'.
    /// </summary>
    public class StringBools
    {
        #region Constants

        /// <summary>Integer representation of true.</summary>
        public const int    TrueInt     = 1;
        /// <summary>Integer representation of false.</summary>
        public const int    FalseInt    = 0;
        /// <summary>String representation of true.</summary>
        public const string TrueString  = "1";
        /// <summary>String representation of false.</summary>
        public const string FalseString = "0";
        /// <summary>Char representation of true.</summary>
        public const char   TrueChar    = '1';
        /// <summary>Char representation of false.</summary>
        public const char   FalseChar   = '0';

        #endregion

        #region Members

        /// <summary>
        /// Internal list of boolean values.
        /// </summary>
        private List<bool> _bools;

        #endregion

        #region Properties

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

        #region Constructors

        /// <summary>
        /// Initializes a new empty instance of <see cref="StringBools"/>.
        /// </summary>
        public StringBools()
        {
            _bools = new List<bool>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StringBools"/> from an enumerable of booleans.
        /// </summary>
        /// <param name="bools">Boolean values to initialise the collection with.</param>
        public StringBools(IEnumerable<bool> bools) : this()
        {
            if (bools != null)
            {
                _bools.AddRange(bools);
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StringBools"/> from a string of '1'/'0' characters.
        /// </summary>
        /// <param name="bools">String whose characters represent boolean values ('1' = true, '0' = false).</param>
        public StringBools(string bools) : this()
        {
            if (!string.IsNullOrEmpty(bools))
            {
                _bools.AddRange(bools.Select(c => c == '1'));
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StringBools"/> from a string of '1'/'0' characters with a fixed length.
        /// </summary>
        /// <param name="bools">String whose characters represent boolean values.</param>
        /// <param name="length">Fixed length of the collection.</param>
        public StringBools(string bools, int length) : this()
        {
            UpdateFromString(bools, length);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StringBools"/> with all values set to false.
        /// </summary>
        /// <param name="length">Number of boolean values in the collection.</param>
        public StringBools(int length) : this()
        {
            SetLength(length);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the length of the array.<br/>
        /// If <paramref name="length"/> is less than or equal to 0, the array length is set to 0.<br/>
        /// Warning: all existing content is cleared.
        /// </summary>
        /// <param name="length">Desired length.</param>
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
        /// Sets the value at position <paramref name="pos"/>. Does nothing if the position is invalid.
        /// </summary>
        /// <param name="pos">Position.</param>
        /// <param name="value">Value to set.</param>
        public void SetValue(int pos,  bool value)
        {
            if (pos >= 0 && pos < _bools.Count) 
            {
                _bools[pos] = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sets the value at position <paramref name="pos"/>. Does nothing if the position is invalid.
        /// </summary>
        /// <param name="pos">Position.</param>
        /// <param name="value">Value to set (1 = true, any other value = false).</param>
        public void SetValue(int pos, int value)
        {
            if (pos >= 0 && pos < _bools.Count)
            {
                _bools[pos] = value == 1;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sets the value at position <paramref name="pos"/>. Does nothing if the position is invalid.
        /// </summary>
        /// <param name="pos">Position.</param>
        /// <param name="value">Value to set ("1" = true, any other value = false).</param>
        public void SetValue(int pos, string value)
        {
            if (pos >= 0 && pos < _bools.Count)
            {
                _bools[pos] = value == "1";
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sets the value at position <paramref name="pos"/>. Does nothing if the position is invalid.
        /// </summary>
        /// <param name="pos">Position.</param>
        /// <param name="value">Value to set ('1' = true, any other value = false).</param>
        public void SetValue(int pos, char value)
        {
            if (pos >= 0 && pos < _bools.Count)
            {
                _bools[pos] = value == '1';
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Returns the value at position <paramref name="pos"/>. Returns false if the position is invalid.
        /// </summary>
        /// <param name="pos">Position whose value is to be retrieved.</param>
        public bool Get(int pos)
        {
            return pos >= 0 && pos < _bools.Count ? _bools[pos] : false;
        }

        /// <summary>
        /// Returns the integer representation of the value at position <paramref name="pos"/>. Returns 0 if the position is invalid.
        /// </summary>
        /// <param name="pos">Position whose value is to be retrieved.</param>
        public int GetInt(int pos)
        {
            return pos >= 0 && pos < _bools.Count && _bools[pos] ? 1 : 0;
        }

        /// <summary>
        /// Returns the string representation of the value at position <paramref name="pos"/>. Returns "0" if the position is invalid.
        /// </summary>
        /// <param name="pos">Position whose value is to be retrieved.</param>
        public string GetString(int pos)
        {
            return pos >= 0 && pos < _bools.Count && _bools[pos] ? "1" : "0";
        }

        /// <summary>
        /// Returns the char representation of the value at position <paramref name="pos"/>. Returns '0' if the position is invalid.
        /// </summary>
        /// <param name="pos">Position whose value is to be retrieved.</param>
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
        /// Resets all elements to the value passed as parameter (default false).
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
        /// Updates the values from the string passed in <paramref name="bools"/> and sets <see cref="Length"/> to <paramref name="length"/>.
        /// </summary>
        /// <param name="bools">String from which values will be retrieved ('1' equals true).</param>
        /// <param name="length">Value to assign to <see cref="Length"/>.</param>
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
