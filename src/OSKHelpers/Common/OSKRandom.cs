using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Classe di utilità derivata da Random.<br/>
    /// Contiene un generatore statico di numeri casuali (Shared)
    /// </summary>
    public class OSKRandom : Random
    {
        #region Proprietà

        /// <summary>
        /// Fornisce un seed basato sui ticks attuali
        /// </summary>
        public static int Seed => Math.Abs((int)DateTime.Now.Ticks);

        /// <summary>
        /// Generatore random statico
        /// </summary>
        public static new OSKRandom Shared;

        #endregion

        #region Costruttori

        static OSKRandom() 
        { 
            Shared = new OSKRandom(); 
        }

        /// <summary>
        /// Costruttore base
        /// </summary>
        public OSKRandom() : base(Seed) { }

        /// <summary>
        /// Costruttore con seed personalizzato
        /// </summary>
        /// <param name="seed"></param>
        public OSKRandom(int seed) : base(seed) { }

        #endregion

    }
}
