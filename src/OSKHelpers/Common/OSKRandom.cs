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
        /// Static random generator instance, safe to use from multiple threads.
        /// </summary>
#if NETSTANDARD2_0
        public static OSKRandom Shared { get; private set; }
#endif
        #endregion

        #region Constructors

        static OSKRandom()
        {
#if NETSTANDARD2_0
            Shared = new ThreadSafeRandom();
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

#if NETSTANDARD2_0

        #region Classes

        /// <summary>
        /// <see cref="OSKRandom"/> whose methods are serialised with a lock, since <see cref="Random"/> is not thread-safe<br/>
        /// (on .NET 6 or later <see cref="Shared"/> resolves to the thread-safe Random.Shared).
        /// </summary>
        private sealed class ThreadSafeRandom : OSKRandom
        {
            #region Members

            private readonly object _lock = new object();

            #endregion

            #region Methods

            /// <inheritdoc/>
            public override int Next()
            {
                lock (_lock)
                {
                    return base.Next();
                }
            }

            /// <inheritdoc/>
            public override int Next(int maxValue)
            {
                lock (_lock)
                {
                    return base.Next(maxValue);
                }
            }

            /// <inheritdoc/>
            public override int Next(int minValue, int maxValue)
            {
                lock (_lock)
                {
                    return base.Next(minValue, maxValue);
                }
            }

            /// <inheritdoc/>
            public override double NextDouble()
            {
                lock (_lock)
                {
                    return base.NextDouble();
                }
            }

            /// <inheritdoc/>
            public override void NextBytes(byte[] buffer)
            {
                lock (_lock)
                {
                    base.NextBytes(buffer);
                }
            }

            /// <inheritdoc/>
            protected override double Sample()
            {
                lock (_lock)
                {
                    return base.Sample();
                }
            }

            #endregion
        }

        #endregion

#endif

    }
}
