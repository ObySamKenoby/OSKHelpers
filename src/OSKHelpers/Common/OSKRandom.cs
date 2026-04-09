using System;

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
#if NETSTANDARD2_0
        public static OSKRandom Shared { get; private set; }
#endif
        #endregion

        #region Constructors

        static OSKRandom() 
        {
#if NETSTANDARD2_0
            Shared = new OSKRandom();
#endif
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
