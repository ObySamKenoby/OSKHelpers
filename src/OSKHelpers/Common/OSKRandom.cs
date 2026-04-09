using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSKHelpers.Common
{
    /// <summary>
    /// Utility class derived from <see cref="Random"/>.<br/>
    /// Contains a static random-number generator (Shared).
    /// </summary>
    public class OSKRandom : Random
    {
        #region Properties

        /// <summary>
        /// Provides a seed based on the current tick count.
        /// </summary>
        public static int Seed => Math.Abs((int)DateTime.Now.Ticks);

        /// <summary>
        /// Static random generator instance.
        /// </summary>
        public static new OSKRandom Shared;

        #endregion

        #region Constructors

        static OSKRandom() 
        { 
            Shared = new OSKRandom(); 
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OSKRandom() : base(Seed) { }

        /// <summary>
        /// Constructor with a custom seed.
        /// </summary>
        /// <param name="seed">Custom seed value.</param>
        public OSKRandom(int seed) : base(seed) { }

        #endregion

    }
}
