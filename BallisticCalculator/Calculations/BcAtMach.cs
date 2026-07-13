namespace BallisticCalculator
{
    /// <summary>
    /// <para>A single knot (a Mach number and the effective ballistic coefficient at it) of a multi-BC profile.</para>
    /// <para>Consumed by [clink=BallisticCalculator.DrgDragTableFactory]DrgDragTableFactory[/clink] to synthesize a custom drag curve.</para>
    /// </summary>
    public class BcAtMach
    {
        /// <summary>
        /// The Mach number (velocity relative to the speed of sound) at which the ballistic coefficient is specified.
        /// </summary>
        public double Mach { get; }

        /// <summary>
        /// The effective ballistic coefficient at the specified Mach number, relative to the base drag curve.
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
