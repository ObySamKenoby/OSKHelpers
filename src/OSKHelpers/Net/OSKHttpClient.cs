using System;
using System.Net.Http;
namespace OSKHelpers.Net
{
    /// <summary>
    /// Classe che semplifica l'utilizzo di una singola istanza di <see cref="HttpClient"/>.<br/>
    /// Può essere utilizzata sia per avere automaticamente una singola istanza immediatamente sfruttabile in modo statico<br/>
    /// (attraverso <see cref="Instance"/> che per accedere ai metodi di utilità generale (recupero dell'handler per accettare<br/>
    /// connessioni non sicure con <see cref="GetInsecureHttpHandler"/> ed altro).<br/>
    /// <b>Nota</b>: <see cref="Instance"/> sarà istanziato solamente al momento del primo utilizzo, quindi la sola referenziazione<br/>
    /// del pacchetto non creerà una istanza di <see cref="HttpClient"/>.
    /// </summary>
    public class OSKHttpClient
    {
        #region Membri

        private static HttpClient _instance;

        private static bool _acceptInsecureConnections;

        private static readonly object _lock = new object();

        #endregion

        #region Proprietà

        /// <summary>
        /// Restituisce una istanza statica di HttpHandler.<br/>
        /// l'istanza non viene creata fino al primo utilizzo della proprietà, quindi la sola referenziazione<br/>
        /// del pacchetto non creerà una istanza di <see cref="HttpClient"/>.<br/>
        /// L'eventuale chiamata di <see cref="Dispose"/> pone <see cref="Instance"/> a null.
        /// </summary>
        public static HttpClient Instance 
        { 
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var handler = AcceptInsecureConnections ? GetInsecureHttpHandler() : new HttpClientHandler();
                            _instance = new HttpClient(handler);
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Restituisce True se <see cref="Instance"/> è stato inizializzato.<br/>
        /// Effettuare un confronto tra <see cref="Instance"/> e null restituirà sempre false in quanto richiamare<br/>
        /// <see cref="Instance "/> porta, se necessario, alla sua inizializzazione.
        /// </summary>
        public static bool InstanceIsNotNull  => _instance != null;

        /// <summary>
        /// Se True <see cref="Instance"/> accetterà l'utilizzo di connessioni non sicure.<br/>
        /// La modifica del valore di questa proprietà deve avvenire prima dell'utilizzo di <see cref="Instance"/> e non ne comporterà l'istanziazione.<br/>
        /// <b>Nota</b>: il tentativo di modificare il valore della proprietà successivamente l'utilizzo di <see cref="Instance"/><br/>
        /// comporterà una eccezione, è necessario provvedere in anticipo a richiamare il metodo <see cref="Dispose"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public static bool AcceptInsecureConnections
        {
            get => _acceptInsecureConnections;
            set
            {
                if (value != _acceptInsecureConnections)
                {
                    lock (_lock)
                    {
                        if (value != _acceptInsecureConnections)
                        {
                            if (_instance != null)
                            {
                                throw new InvalidOperationException(NetMessages.AcceptInsecureConnectionsException);
                            }
                            _acceptInsecureConnections = value;
                        }
                    }
                }
            }
        }

        #endregion

        #region Metodi

        /// <summary>
        /// Restituisce l'handler per permettere connessioni non sicure attraverso SSL.<br/>
        /// <b>Attenzione</b>: l'utilizzo di cnonnessioni non sicure è possibile fonte di problemi di sicurezza.
        /// </summary>
        public static HttpClientHandler GetInsecureHttpHandler()
        {
            var insecureHttpHandler = new HttpClientHandler();
            insecureHttpHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            insecureHttpHandler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };
            return insecureHttpHandler;
        }

        /// <summary>
        /// Permette di utilizzare una istanza custom di <see cref="HttpClient"/> come Instance.<br/>
        /// Può essere richiamato esclusivamente quando Instance è null.
        /// </summary>
        /// <param name="instance">Istanza da utilizzare come Instance.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static void SetInstance(HttpClient instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (_instance !=  null)
            {
                throw new InvalidOperationException(NetMessages.SetInstanceException);
            }

            _instance = instance;
        }

        /// <summary>
        /// Effettua il Dispose di <see cref="Instance"/> e lo pone a null.<br/>
        /// Se <see cref="Instance"/> è già null non effettua alcuna operazione.
        /// </summary>
        public static void Dispose()
        {
            lock (_lock)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }

#if DEBUG

        #region Metodi per test

        public static HttpClient GetHttpClientPrivateInstance() => _instance;

        #endregion

#endif


        #endregion
    }
}
