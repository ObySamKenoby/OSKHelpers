using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilities for the string type.
    /// </summary>
    public class StringUtils
    {

        private const string ALPHALOWER         = @"^[a-z]*$";
        private const string ALPHAUPPER         = @"^[A-Z]*$";
        private const string NUMBERS            = @"^[0-9]*$";
        private const string ALPHA              = @"^[A-Za-z]*$";
        private const string ALPHANUM           = @"^[A-Za-z0-9]*$";
        private const string ALPHALOWERNUM      = @"^[a-z0-9]*$";
        private const string ALPHAUPPERNUM      = @"^[A-Z0-9]*$";
        private const string ALPHALOWERSPC      = @"^[a-z ]*$";
        private const string ALPHAUPPERSPC      = @"^[A-Z ]*$";
        private const string NUMBERSSPC         = @"^[0-9 ]*$";
        private const string ALPHASPC           = @"^[A-Za-z ]*$";
        private const string ALPHANUMSPC        = @"^[A-Za-z0-9 ]*$";
        private const string ALPHALOWERNUMSPC   = @"^[a-z0-9 ]*$";
        private const string ALPHAUPPERNUMSPC   = @"^[A-Z0-9 ]*$";

        /// <summary>
        /// String containing only lowercase characters (typically used for random string generation).
        /// </summary>
        public const string SLOWERCHARS         = "abcdefghijklmnopqrstuvwxyz";
        /// <summary>
        /// String containing only uppercase characters (typically used for random string generation).
        /// </summary>
        public const string SUPPERCHARS         = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        /// <summary>
        /// String containing digits (typically used for random string generation).
        /// </summary>
        public const string SNUMBERS            = "01234567890";
        /// <summary>
        /// String containing special characters (typically used for random string generation).
        /// </summary>
        public const string SSPECIALCHARS       = "@#!£$%&=^_-+<>()[]{}.";


        /// <summary>
        /// String containing lowercase and uppercase characters (typically used for random string generation).
        /// </summary>
        public const string SLOWERUPPERCHARS    = SLOWERCHARS + SUPPERCHARS;
        /// <summary>
        /// String containing lowercase characters and digits (typically used for random string generation).
        /// </summary>
        public const string SLOWERCHARSNUMS     = SLOWERCHARS + SNUMBERS;
        /// <summary>
        /// String containing uppercase characters and digits (typically used for random string generation).
        /// </summary>
        public const string SUPPERCHARSNUMS     = SUPPERCHARS + SNUMBERS;
        /// <summary>
        /// String containing uppercase, lowercase characters and digits (typically used for random string generation).
        /// </summary>
        public const string SCHARSNUMS          = SUPPERCHARS + SLOWERCHARS + SNUMBERS;
        /// <summary>
        /// String containing uppercase, lowercase, digits and special characters (typically used for random string generation).
        /// </summary>
        public const string SCHARSNUMSSPECIALS  = SUPPERCHARS + SLOWERCHARS + SNUMBERS + SSPECIALCHARS;


        /// <summary>
        /// Returns the string converted to ASCII.
        /// </summary>
        /// <param name="text">Text to convert to ASCII.</param>
        /// <param name="toUpper">When true, the string is converted to uppercase.</param>
        /// <param name="trim">Whether to trim the string (default: false).</param>
        /// <param name="length">Maximum string length (default: none).</param>
        /// <param name="padRight">When the string is shorter than the specified length, pads it with trailing spaces.</param>
        /// <returns></returns>
        public static string AsASCII(string text, bool toUpper = false, bool trim = false, int? length = null, bool padRight = false)
        {
            var sb = new StringBuilder();
            if (text != null)
            {
                if (trim)
                {
                    text = text.Trim();
                }
                if (toUpper)
                {
                    text = text.ToUpper();
                }
                foreach (char c in text)
                {
                    int unicode = c;
                    if (unicode < 128)
                    {
                        sb.Append(c);
                    }
                }
            }
            var t = sb.ToString();
            // Check string length
            int l = t.Length; // length of the ASCII-converted string
            if (length != null && length >= 0)
            {
                if (length < l)
                {
                    l = (int)length;
                }
                else if (padRight)
                {
                    l = (int)length;
                    t = t.PadRight(l, ' ');
                }
            }

            t = l > 0 ? t.Substring(0, l) : string.Empty;

            return t;
        }

        /// <summary>
        /// Verifies that all characters of text are contained in validChars.<br/>
        /// If length is greater than zero, also verifies that the length of text does not exceed it.
        /// </summary>
        /// <param name="text">String to validate.</param>
        /// <param name="validChars">Valid characters.</param>
        /// <param name="notEmpty">When false, text is considered valid when null or empty; otherwise it must contain at least one character.</param>
        /// <param name="minLength">Minimum string length.</param>
        /// <param name="maxLength">Maximum string length; 0 means no limit.</param>
        /// <returns>True if text is valid.</returns>
        public static bool CheckChars(string text, string validChars, bool notEmpty, int minLength, int maxLength)
        {
            var textNull = string.IsNullOrEmpty(text);
            bool isValid =
                (!notEmpty && textNull) // string can be and is empty
                || (!textNull && !string.IsNullOrWhiteSpace(validChars)
                    && (minLength <= 0 || text.Length >= minLength)
                    && (maxLength <= 0 || text.Length <= maxLength));

            if (isValid && !string.IsNullOrEmpty(text))
            {
                for (var i = 0; i < text.Length; i++)
                {
                    isValid &= validChars.Contains(text[i]);
                    if (!isValid)
                    {
                        break;
                    }
                }
            }

            return isValid;
        }

        /// <inheritdoc cref="CheckChars(string, string, bool, int, int)"/>
        public static bool CheckChars(string text, string validChars, bool notEmpty = false, int maxLength = 0)
            => CheckChars(text, validChars, notEmpty, 0, maxLength);

        /// <summary>
        /// Verifies that all characters of text are contained in validChars.<br/>
        /// If length is greater than zero, also verifies that the length of text does not exceed it.
        /// </summary>
        /// <param name="text">String to validate.</param>
        /// <param name="regEx">Regular expression to evaluate text against.</param>
        /// <param name="notEmpty">When false, text is considered valid when null or empty; otherwise it must contain at least one character.</param>
        /// <param name="minLength">Minimum string length.</param>
        /// <param name="maxLength">Maximum string length; 0 means no limit.</param>
        /// <returns>True if text is valid.</returns>
        public static bool CheckRegex(string text, string regEx, bool notEmpty, int minLength, int maxLength)
        {
            var textNull = string.IsNullOrEmpty(text);
            bool isValid =
                (!notEmpty && textNull) // string can be and is empty
                || (!textNull && !string.IsNullOrWhiteSpace(regEx)
                    && (minLength <= 0 || text.Length >= minLength)
                    && (maxLength <= 0 || text.Length <= maxLength));

            if (isValid && !string.IsNullOrEmpty(text))
            {
                isValid = Regex.IsMatch(text, regEx);
            }

            return isValid;
        }

        /// <inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool CheckRegex(string text, string regEx, bool notEmpty = false, int maxLength = 0)
            => CheckRegex(text, regEx, notEmpty, 0, maxLength);

        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercase(string text, bool notEmpty = false, int maxLength = 0)           => CheckRegex(text, ALPHALOWER, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercase(string text, bool notEmpty = false, int maxLength = 0)           => CheckRegex(text, ALPHAUPPER, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbers(string text, bool notEmpty = false, int maxLength = 0)             => CheckRegex(text, NUMBERS, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase or lowercase letters.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlpha(string text, bool notEmpty = false, int maxLength = 0)               => CheckRegex(text, ALPHA, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase and lowercase letters or digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbers(string text, bool notEmpty = false, int maxLength = 0)        => CheckRegex(text, ALPHANUM, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters and digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbers(string text, bool notEmpty = false, int maxLength = 0)    => CheckRegex(text, ALPHALOWERNUM, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters and digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbers(string text, bool notEmpty = false, int maxLength = 0)    => CheckRegex(text, ALPHAUPPERNUM, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseSpc(string text, bool notEmpty = false, int maxLength = 0)        => CheckRegex(text, ALPHALOWERSPC, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseSpc(string text, bool notEmpty = false, int maxLength = 0)        => CheckRegex(text, ALPHAUPPERSPC, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of digits and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbersSpc(string text, bool notEmpty = false, int maxLength = 0)          => CheckRegex(text, NUMBERSSPC, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase or lowercase letters and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaSpc(string text, bool notEmpty = false, int maxLength = 0)            => CheckRegex(text, ALPHASPC, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase or lowercase letters, digits, and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbersSpc(string text, bool notEmpty = false, int maxLength = 0)     => CheckRegex(text, ALPHANUMSPC, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters, digits, and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbersSpc(string text, bool notEmpty = false, int maxLength = 0) => CheckRegex(text, ALPHALOWERNUMSPC, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters, digits, and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbersSpc(string text, bool notEmpty = false, int maxLength = 0) => CheckRegex(text, ALPHAUPPERNUMSPC, notEmpty, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercase(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWER, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercase(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPER, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, NUMBERS, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase or lowercase letters.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlpha(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHA, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase and lowercase letters or digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHANUM, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters and digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWERNUM, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters and digits.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPERNUM, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWERSPC, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPERSPC, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of digits and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, NUMBERSSPC, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase or lowercase letters and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHASPC, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase or lowercase letters, digits, and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHANUMSPC, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of lowercase letters, digits, and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWERNUMSPC, notEmpty, minLength, maxLength);
        ///<summary>
        /// Verifies that the entire string content consists of uppercase letters, digits, and spaces.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPERNUMSPC, notEmpty, minLength, maxLength);


        /// <summary>
        /// Generates a new string using the characters in <paramref name="chars"/> with the given <paramref name="length"/>.<br/>
        /// The class provides predefined constants that can be used as the <paramref name="chars"/> value.
        /// </summary>
        /// <param name="chars">Characters to use for generating the new string.</param>
        /// <param name="length">Length of the generated string; must be greater than 0.</param>
        /// <returns>The generated string.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string GenerateString(string chars, int length)
        {
            if (String.IsNullOrWhiteSpace(chars))
            {
                throw new ArgumentException(nameof(chars));
            }
            if (length <= 0)
            { 
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            
            var cstring = new char[length];
            var len = chars.Length;
            for (int i = 0; i < length; i++)
            {
                cstring[i] = chars[OSKRandom.Shared.Next(0, len)];
            }

            return new string(cstring);
        }

        /// <summary>
        /// Generates a string of the given length using only lowercase characters.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateLowerCharsString(int length)          => GenerateString(SLOWERCHARS, length);
        /// <summary>
        /// Generates a string of the given length using only uppercase characters.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateUpperCharsString(int length)          => GenerateString(SUPPERCHARS, length);
        /// <summary>
        /// Generates a string of the given length using lowercase and uppercase characters.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateAlphaString(int length)               => GenerateString(SLOWERUPPERCHARS, length);
        /// <summary>
        /// Generates a string of the given length using lowercase, uppercase characters and digits.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateAlphaNumericString(int length)        => GenerateString(SCHARSNUMS, length);
        /// <summary>
        /// Generates a string of the given length using lowercase, uppercase characters, digits, and special characters.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateAlphaNumSpecialsString(int length)    => GenerateString(SCHARSNUMS, length);


    }
}
