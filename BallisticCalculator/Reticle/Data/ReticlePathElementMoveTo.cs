using BallisticCalculator.Serialization;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// MoveTo path element
    /// </summary>
    [BXmlElement("reticle-path-move-to")]
    public class ReticlePathElementMoveTo : ReticlePathElement
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ReticlePathElementMoveTo() : base(ReticlePathElementType.MoveTo)
        {

        }
    }
}
