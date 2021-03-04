using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Text;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// The information about bullet drop compensator point
    /// 
    /// Typically, an applications shows a distance that corresponds to the 
    /// specific point.
    /// </summary>
    [BXmlElement("bdc")]
    public class ReticleBulletDropCompensatorPoint
    {
        /// <summary>
        /// The amount of drop below the aim point
        /// </summary>
        [BXmlProperty("drop")]
        public Measurement<AngularUnit> Drop { get; set; }

        /// <summary>
        /// Position of the BDC point at reticle
        /// </summary>
        [BXmlProperty(Name = "position", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Position { get; set; }

        /// <summary>
        /// Offset of the text from the BDC point 
        /// 
        /// Positive value means text at right of BDC, negative - text at left of BDC
        /// </summary>
        [BXmlProperty("text-offset")]
        public Measurement<AngularUnit> TextOffset { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleBulletDropCompensatorPoint()
        {

        }
    }
}
