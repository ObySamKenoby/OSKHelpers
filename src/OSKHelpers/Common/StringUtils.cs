using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utilità per il tipo stringa
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
        /// Stringa contenente i soli caratteri minuscoli (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SLOWERCHARS         = "abcdefghijklmnopqrstuvwxyz";
        /// <summary>
        /// Stringa contenente i soli caratteri maiuscoli (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SUPPERCHARS         = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        /// <summary>
        /// Stringa contenente i numeri (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SNUMBERS            = "01234567890";
        /// <summary>
        /// Stringa contenente caratteri speciali (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SSPECIALCHARS       = "@#!£$%&=^_-+<>()[]{}.";

        /// <summary>
        /// Stringa contenente caratteri minuscoli e  maiuscoli (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SLOWERUPPERCHARS    = SLOWERCHARS + SUPPERCHARS;
        /// <summary>
        /// Stringa contenente caratteri minuscoli e numeri  (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SLOWERCHARSNUMS     = SLOWERCHARS + SNUMBERS;
        /// <summary>
        /// Stringa contenente caratteri maiuscoli e numeri  (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SUPPERCHARSNUMS     = SUPPERCHARS + SNUMBERS;
        /// <summary>
        /// Stringa contenente caratteri maiuscoli, minuscoli e numeri  (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SCHARSNUMS          = SUPPERCHARS + SLOWERCHARS + SNUMBERS;
        /// <summary>
        /// Stringa contenente caratteri maiuscoli, minuscoli, numeri e caratteri speciali (normalmente utilizzata per la generazione di nuove stringhe)
        /// </summary>
        public const string SCHARSNUMSSPECIALS  = SUPPERCHARS + SLOWERCHARS + SNUMBERS + SSPECIALCHARS;


        /// <summary>
        /// Restituisce la stringa convertita in ASCII.
        /// </summary>
        /// <param name="text">Testo da convertire in ASCII</param>
        /// <param name="toUpper">Se True la stringa viene converita in maiuscolo.</param>
        /// <param name="trim">Indica se si desidera il Trim della stringa (default: false)</param>
        /// <param name="length">Lunghezza massima della stringa (default: nessuna)</param>
        /// <param name="padRight">Se la stringa è più breve della lunghezza indicata aggiunge spazi in coda fino alla corretta lunghezza</param>
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
            // Verifica della lunghezza della stringa
            int l = t.Length; // lunghezza della stringa convertita ASCII
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
        /// Verifica che tutti i caratteri di text siano contenuti all'interno di validChars.<br/>
        /// Se length è maggiore  di zero verifica che la lunghezza di text sia minore o uguale.
        /// </summary>
        /// <param name="text">Stringa da verificare.</param>
        /// <param name="validChars">Caratteri validi.</param>
        /// <param name="notEmpty">Se False Text è considerato valido quando nullo o vuoto, altrimenti deve contenere almeno un carattere.</param>
        /// <param name="minLength">Lunghezza minima della stringa.</param>
        /// <param name="maxLength">Lunghezza massima della stringa, se 0 non ci sono limiti.</param>
        /// <returns>True se text è valido.</returns>
        public static bool CheckChars(string text, string validChars, bool notEmpty, int minLength, int maxLength)
        {
            var textNull = string.IsNullOrEmpty(text);
            bool isValid =
                (!notEmpty && textNull) // La stringa può essere ed è vuota
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
        /// Verifica che tutti i caratteri di text siano contenuti all'interno di validChars.<br/>
        /// Se length è maggiore  di zero verifica che la lunghezza di text sia minore o uguale.
        /// </summary>
        /// <param name="text">Stringa da verificare.</param>
        /// <param name="regEx">Espressione regolaare con cui valutare text.</param>
        /// <param name="notEmpty">Se False Text è considerato valido quando nullo o vuoto, altrimenti deve contenere almeno un carattere.</param>
        /// <param name="minLength">Lunghezza minima della stringa.</param>
        /// <param name="maxLength">Lunghezza massima della stringa, se 0 non ci sono limiti.</param>
        /// <returns>True se text è valido.</returns>
        public static bool CheckRegex(string text, string regEx, bool notEmpty, int minLength, int maxLength)
        {
            var textNull = string.IsNullOrEmpty(text);
            bool isValid =
                (!notEmpty && textNull) // La stringa può essere ed è vuota
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
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercase(string text, bool notEmpty = false, int maxLength = 0)           => CheckRegex(text, ALPHALOWER, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercase(string text, bool notEmpty = false, int maxLength = 0)           => CheckRegex(text, ALPHAUPPER, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbers(string text, bool notEmpty = false, int maxLength = 0)             => CheckRegex(text, NUMBERS, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole o minuscole.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlpha(string text, bool notEmpty = false, int maxLength = 0)               => CheckRegex(text, ALPHA, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole e minuscole o numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbers(string text, bool notEmpty = false, int maxLength = 0)        => CheckRegex(text, ALPHANUM, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole e numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbers(string text, bool notEmpty = false, int maxLength = 0)    => CheckRegex(text, ALPHALOWERNUM, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole e numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbers(string text, bool notEmpty = false, int maxLength = 0)    => CheckRegex(text, ALPHAUPPERNUM, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseSpc(string text, bool notEmpty = false, int maxLength = 0)        => CheckRegex(text, ALPHALOWERSPC, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscle e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseSpc(string text, bool notEmpty = false, int maxLength = 0)        => CheckRegex(text, ALPHAUPPERSPC, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbersSpc(string text, bool notEmpty = false, int maxLength = 0)          => CheckRegex(text, NUMBERSSPC, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole o minuscole e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaSpc(string text, bool notEmpty = false, int maxLength = 0)            => CheckRegex(text, ALPHASPC, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole o minuscole, numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbersSpc(string text, bool notEmpty = false, int maxLength = 0)     => CheckRegex(text, ALPHANUMSPC, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole, numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbersSpc(string text, bool notEmpty = false, int maxLength = 0) => CheckRegex(text, ALPHALOWERNUMSPC, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole, numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbersSpc(string text, bool notEmpty = false, int maxLength = 0) => CheckRegex(text, ALPHAUPPERNUMSPC, notEmpty, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercase(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWER, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercase(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPER, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, NUMBERS, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole o minuscole.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlpha(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHA, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole e minuscole o numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHANUM, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole e numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWERNUM, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole e numeri.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbers(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPERNUM, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWERSPC, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscle e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPERSPC, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, NUMBERSSPC, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole o minuscole e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHASPC, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole o minuscole, numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsAlphaNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHANUMSPC, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere minuscole, numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsLowercaseNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHALOWERNUMSPC, notEmpty, minLength, maxLength);
        ///<summary>
        ///Verifica che tutti il contenuto della stringa sia composto da lettere maiuscole, numeri e spazi.
        /// </summary>
        ///<inheritdoc cref="CheckRegex(string, string, bool, int, int)"/>
        public static bool IsUppercaseNumbersSpc(string text, bool notEmpty, int minLength, int maxLength) => CheckRegex(text, ALPHAUPPERNUMSPC, notEmpty, minLength, maxLength);


        /// <summary>
        /// Genera UnauthorizedAccessException nuova stringa utilizzando i caratteri presenti in char di lunghezza length.<br/>
        /// La classe definisce delle costanti preimpostate che possibile utilizzare come valore di chars.
        /// </summary>
        /// <param name="chars">Caratteri da utilizzare per la generazione della nuova stringa.</param>
        /// <param name="length">Lunghezza della stringa generata, deve essere maggiore di 0.</param>
        /// <returns>La stringa generata.</returns>
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
        /// Genera una stringa di lunghezza length con soli caratteri minuscoli.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateLowerCharsString(int length)          => GenerateString(SLOWERCHARS, length);
        /// <summary>
        /// Genera una stringa di lunghezza length con soli caratteri maiuscoli.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateUpperCharsString(int length)          => GenerateString(SUPPERCHARS, length);
        /// <summary>
        /// Genera una stringa di lunghezza length con caratteri minuscoli e maiuscoli.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateAlphaString(int length)               => GenerateString(SLOWERUPPERCHARS, length);
        /// <summary>
        /// Genera una stringa di lunghezza length con caratteri minuscoli, maiuscoli e numeri.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateAlphaNumericString(int length)        => GenerateString(SCHARSNUMS, length);
        /// <summary>
        /// Genera una stringa di lunghezza length con caratteri minuscoli, maiuscoli, numeri e caratteri speciali.
        /// </summary>
        /// <inheritdoc cref="GenerateString(string, int)"/>
        public static string GenerateAlphaNumSpecialsString(int length)    => GenerateString(SCHARSNUMS, length);


    }
}
