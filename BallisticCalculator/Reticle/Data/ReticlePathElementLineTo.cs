using BallisticCalculator.Serialization;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// LineTo path element
    /// </summary>
    [BXmlElement("reticle-path-line-to")]
    public class ReticlePathElementLineTo : ReticlePathElement
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ReticlePathElementLineTo() : base(ReticlePathElementType.LineTo)
        {

        }
    }
}
