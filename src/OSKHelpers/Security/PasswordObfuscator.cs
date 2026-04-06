using OSKHelpers.Common;
using System;
using System.Linq;
using System.Text;

namespace OSKHelpers.Security
{ 
/// <summary>
/// Classe contenente metodi per mantenere una password offuscata in memoria.
/// </summary>

    public class PasswordObfuscator
    {
        #region Costanti

        /// <summary>
        /// Lunghezza del seed utilizzato per l'offuscamento.
        /// </summary>
        private const int SEEDLENGTH = 1024;

        #endregion

        #region Membri

        /// <summary>
        /// Seed utilizzato per l'offuscamento delle password.
        /// </summary>
        private static byte[] _seed;
        /// <summary>
        /// Valore che sarà aggiunto all'effettivo punto di inizio della password<br/>
        /// all'interno di <see cref="_seed"/> per la memorizzazione nell'hash.
        /// </summary>
        private static int _startIndexSeed;

        private static Random _rnd;

        #endregion

        #region Costruttore

        static PasswordObfuscator()
        {
            _rnd = new Random();
            ReSeed();
        }

        #endregion

        #region Metodi

        /// <summary>
        /// Restituisce la password offuscata.
        /// </summary>
        /// <param name="password">Password per cui è richiesto l'offuscamento.</param>
        /// <returns>La stringa offuscata contenente la password.</returns>
        public static string EncodePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException($"{nameof(password)} deve essere una stringa valida.");
            }

            // sceglie un punto di inizio casuale
            int startIndex = _rnd.Next(int.MaxValue);

            byte[] pwdBytes = Encoding.UTF8.GetBytes(password);

            byte[] encoded = new byte[pwdBytes.Length];

            for (int i = 0; i < pwdBytes.Length; i++)
            {
                // XOR con il seed, partendo da startIndex
                encoded[i] = (byte)(pwdBytes[i] ^ _seed[(startIndex + i) % SEEDLENGTH]);
            }

            // Converti in Base64 per rappresentazione leggibile
            string encodedBase64 = Convert.ToBase64String(encoded);
            encodedBase64 += EncodeIndex(startIndex);

            return encodedBase64;
        }

        /// <summary>
        /// Restituisce la password in chiaro per la stringa offuscata passata come parametro.
        /// </summary>
        /// <param name="encoded">La stringa offuscata generrata da <see cref="EncodePassword(string)"/></param>
        /// <returns>La password in chiaro </returns>
        public static string DecodePassword(string encoded)
        {
            // La riga da deoffuscare deve essere lunga almeno 8 caratteri (4 sono utilizzati per
            // l'encoding del valore di inizio password all'interno dell'hash, la conversione Base64
            // di un carattere occupa 4 byte e quindi 4 caratteri).
            if (string.IsNullOrWhiteSpace(encoded) || encoded.Length < 8)
            {
                throw new ArgumentException($"{nameof(encoded)} non è una stringa offuscata potenzialmente valida.");
            }

            // Estrae gli ultimi quattro caratteri come indice di partenza per la decodifica della password.
            // Il valore è l'indice reale sommato a _startIndexSeed, quindi è necessario effettuare la
            // relativa sottrazione per ottenere l'indice di inizio corretto.
            string startChars = encoded.Substring(encoded.Length - 4);
            int startIndex = DecodeIndex(startChars);

            string base64Part = encoded.Substring(0, encoded.Length - 4);
            byte[] encodedBytes = Convert.FromBase64String(base64Part);

            byte[] decoded = new byte[encodedBytes.Length];

            for (int i = 0; i < encodedBytes.Length; i++)
            {
                decoded[i] = (byte)(encodedBytes[i] ^ (_seed[(startIndex + i) % SEEDLENGTH]));
            }

            return Encoding.UTF8.GetString(decoded);
        }

        /// <summary>
        /// Rigenera il seed utilizzato per l'offuscamento delle password,<br/>
        /// richiamare questo metodo renderà inutilizzabili tutte le password<br/>
        /// correntemente memorizzate.
        /// </summary>
        public static void ReSeed()
        {
            _seed = StringUtils.GenerateAlphaNumericString(SEEDLENGTH).Select(c => (byte)c).ToArray();

            var oldIndexSeed = _startIndexSeed;
            while (oldIndexSeed == _startIndexSeed)
            {
                _startIndexSeed = _rnd.Next(int.MaxValue);
            }
        }

        /// <summary>
        /// Effettua la codifica dell'indice passato come parametro sotto forma di stringa di quattro caratteri rappresentanti i byte del numero.
        /// </summary>
        private static string EncodeIndex(int startIndex)
        {
            // Aggiunge il punto di inizio come ultima parte della stringa dopo averlo incrociato con _startIndexSeed.
            var strIndex = new string(Array.ConvertAll(BitConverter.GetBytes(startIndex + _startIndexSeed), b => (char)b));
            return strIndex;
        }

        /// <summary>
        /// Restituisce il valore int relativo alla stringa passata come parametro (generata da <see cref="EncodeIndex(int)"/>.
        /// </summary>
        /// <param name="strIndex">Rappresentazione stringa dell'array di byte compoinenti il numero, deve essere lunga esattamente 4 caratteri.</param>
        private static int DecodeIndex(string strIndex)
        {
            if (string.IsNullOrWhiteSpace(strIndex) || strIndex.Length != 4)
            {
                throw new ArgumentException($"{nameof(strIndex)} deve essere lungo 4 caratteri");
            }
            var index =  BitConverter.ToInt32(Array.ConvertAll(strIndex.ToCharArray(), c => (byte)c), 0) - _startIndexSeed;
            return index;
        }

#if DEBUG

        /// <inheritdoc cref="EncodeIndex(int)"/>
        /// <remarks>Da utilizzare esclusivamente per i test.</remarks>
        public static string TestEncodeIndex(int startIndex) => EncodeIndex(startIndex);

        /// <inheritdoc cref="DecodeIndex(string)"/>
        /// <remarks>Da utilizzare esclusivamente per i test.</remarks>
        public static int TestDecodeIndex(string strIndex) => DecodeIndex(strIndex);


#endif

    #endregion
    }
}
