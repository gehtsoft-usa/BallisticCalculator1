namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// The type of a reticle path element
    /// </summary>
    public enum ReticlePathElementType
    {
        /// <summary>
        /// Move to the position w/o drawing
        /// </summary>
        MoveTo,

        /// <summary>
        /// Draw a line to
        /// </summary>
        LineTo,

        /// <summary>
        /// Draw an arc to
        /// </summary>
        Arc,
    }
}
