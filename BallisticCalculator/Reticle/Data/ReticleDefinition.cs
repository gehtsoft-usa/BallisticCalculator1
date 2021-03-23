using BallisticCalculator.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// The definition of a reticle
    /// </summary>
    [BXmlElement("reticle")]
    public class ReticleDefinition
    {
        /// <summary>
        /// The name of the reticle
        /// </summary>
        [BXmlProperty(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The size of the reticle
        /// </summary>
        [BXmlProperty(Name = "size", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Size { get; set; }

        /// <summary>
        /// <para>The position of the zero coordinate in the reticle</para>
        /// <para>If no position is set, the zero point is take as the center of the reticle</para>
        /// </summary>
        [BXmlProperty(Name = "zero", ChildElement = true, FlattenChild = true, Optional = true)]
        public ReticlePosition Zero { get; set; }

        /// <summary>
        /// Collection of a reticle elements
        /// </summary>
        [BXmlProperty(Name = "elements", Collection = true)]
        public ReticleElementsCollection Elements { get; } = new ReticleElementsCollection();

        /// <summary>
        /// Collection of a bullet drop compensation points
        /// </summary>
        [BXmlProperty(Name = "bdc", Collection = true, Optional = true)]
        public ReticleBulletDropCompensatorPointCollection BulletDropCompensator { get; } = new ReticleBulletDropCompensatorPointCollection();
    }
}
