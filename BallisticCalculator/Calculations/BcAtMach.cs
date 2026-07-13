namespace BallisticCalculator
{
    /// <summary>
    /// A single knot of an effective ballistic-coefficient-vs-Mach profile,
    /// used by <see cref="DrgDragTableFactory"/> to synthesize a custom drag curve.
    /// </summary>
    public class BcAtMach
    {
        /// <summary>
        /// The Mach number (velocity relative to the speed of sound) at which the
        /// ballistic coefficient is specified.
        /// </summary>
        public double Mach { get; }

        /// <summary>
        /// The effective ballistic coefficient at <see cref="Mach"/>, relative to the
        /// base drag curve passed to the factory.
        /// </summary>
        public double Bc { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mach">Mach number (must be non-negative).</param>
        /// <param name="bc">Effective ballistic coefficient (must be positive).</param>
        public BcAtMach(double mach, double bc)
        {
            Mach = mach;
            Bc = bc;
        }
    }
}
