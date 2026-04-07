using System;
using System.Net.Http;
namespace OSKHelpers.Net
{
    /// <summary>
    /// Class that simplifies the use of a single <see cref="HttpClient"/> instance.<br/>
    /// Can be used either to obtain an automatically managed static instance ready to use<br/>
    /// (via <see cref="Instance"/>) or to access general utility methods (e.g. obtaining the handler for insecure<br/>
    /// connections via <see cref="GetInsecureHttpHandler"/> and more).<br/>
    /// <b>Note</b>: <see cref="Instance"/> is instantiated only on first use, so merely referencing<br/>
    /// the package will not create an <see cref="HttpClient"/> instance.
    /// </summary>
    public class OSKHttpClient
    {
        #region Members

        private static HttpClient _instance;

        private static bool _acceptInsecureConnections;

        private static readonly object _lock = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Returns a static <see cref="HttpClient"/> instance.<br/>
        /// The instance is not created until the property is first accessed, so merely referencing<br/>
        /// the package will not create an <see cref="HttpClient"/> instance.<br/>
        /// Calling <see cref="Dispose"/> sets <see cref="Instance"/> to null.
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
        /// Returns True if <see cref="Instance"/> has been initialised.<br/>
        /// Comparing <see cref="Instance"/> against null will always return false because accessing<br/>
        /// <see cref="Instance "/> triggers its initialisation if needed.
        /// </summary>
        public static bool InstanceIsNotNull  => _instance != null;

        /// <summary>
        /// When True, <see cref="Instance"/> will accept insecure connections.<br/>
        /// This property must be set before <see cref="Instance"/> is used and will not trigger its instantiation.<br/>
        /// <b>Note</b>: attempting to change this property after <see cref="Instance"/> has been used<br/>
        /// will throw an exception; call <see cref="Dispose"/> beforehand if needed.
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

        #region Methods

        /// <summary>
        /// Returns the handler that allows insecure connections over SSL.<br/>
        /// <b>Warning</b>: the use of insecure connections is a potential security risk.
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
        /// Allows a custom <see cref="HttpClient"/> instance to be used as Instance.<br/>
        /// Can only be called when Instance is null.
        /// </summary>
        /// <param name="instance">Instance to use as Instance.</param>
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
        /// Disposes <see cref="Instance"/> and sets it to null.<br/>
        /// If <see cref="Instance"/> is already null, no action is taken.
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

        #region Test methods

        public static HttpClient GetHttpClientPrivateInstance() => _instance;

        #endregion

#endif


        #endregion
    }
}
