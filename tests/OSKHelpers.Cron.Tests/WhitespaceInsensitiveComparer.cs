using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OSKHelpers.Cron.Tests
{
    /// <summary>
    /// Comparer of the actions for the tests of <see cref="Crontab.ActionComparer"/>: without distinction between upper and
    /// lower case and between one or more consecutive spaces.
    /// </summary>
    public class WhitespaceInsensitiveComparer : IEqualityComparer<string>
    {
        #region Methods

        /// <inheritdoc/>
        public bool Equals(string x, string y)
            => StringComparer.OrdinalIgnoreCase.Equals(Normalize(x), Normalize(y));

        /// <inheritdoc/>
        public int GetHashCode(string obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(Normalize(obj) ?? string.Empty);

        /// <summary>
        /// Replaces every sequence of spaces with a single space.
        /// </summary>
        /// <param name="value">Action to normalize.</param>
        /// <returns>The normalized action; null if <paramref name="value"/> is null.</returns>
        private static string Normalize(string value)
            => value == null ? null : Regex.Replace(value.Trim(), @"\s+", " ");

        #endregion
    }
}
