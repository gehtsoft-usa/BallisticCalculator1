namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>The style of a line stroke used to draw a reticle element.</para>
    /// <para>Applies to <see cref="ReticleLine"/>, <see cref="ReticleRectangle"/>,
    /// <see cref="ReticleCircle"/> and <see cref="ReticlePath"/>.</para>
    /// </summary>
    public enum ReticleLineStyle
    {
        /// <summary>
        /// A continuous, solid line. This is the default when no style is specified.
        /// </summary>
        Solid,
        /// <summary>
        /// A line drawn as a sequence of dashes.
        /// </summary>
        Dashed,
        /// <summary>
        /// A line drawn as a sequence of dots.
        /// </summary>
        Dotted,
    }
}
