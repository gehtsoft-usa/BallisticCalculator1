using BallisticCalculator.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>The definition of a reticle</para>
    /// <para>A reticle is a set of graphical primitives drawn in the 2D space defined with coordinates
    /// with angular measurements. </para>
    /// <para>A reticle has size defined in angular measurements (field of vision), in most cases the field of vision
    /// is the same vertically and horizontally.</para>
    /// <para>A reticle also has a "zero" point, which is typically the point where the scope is zeroed to. It may or may be not a geometrical center of the reticle</para>
    /// <para>The X coordinate direction is left to right, e.g. negative offset is left of zero point, and positive offset is right of zero point</para>
    /// <para>The Y coordinate direction is bottom to top , e.g. negative offset is bottom of zero point, and positive offset is top of zero point</para>
    /// </summary>
    [BXmlElement("reticle")]
    public class ReticleDefinition : ICloneable
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
        /// Collection of reticle elements
        /// </summary>
        [BXmlProperty(Name = "elements", Collection = true)]
        public ReticleElementsCollection Elements { get; } = new ReticleElementsCollection();

        /// <summary>
        /// Collection of a bullet drop compensation points
        /// </summary>
        [BXmlProperty(Name = "bdc", Collection = true, Optional = true)]
        public ReticleBulletDropCompensatorPointCollection BulletDropCompensator { get; } = new ReticleBulletDropCompensatorPointCollection();

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => Clone();

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public ReticleDefinition Clone()
        {
            var copy = new ReticleDefinition()
            {
                Name = this.Name,
                Size = this.Size.Clone(),
                Zero = this.Zero.Clone()
            };

            for (int i = 0; i < Elements.Count; i++)
                copy.Elements.Add(this.Elements[i].Clone());

            for (int i = 0; i < BulletDropCompensator.Count; i++)
                copy.BulletDropCompensator.Add(this.BulletDropCompensator[i].Clone());

            return copy;
        }
    }
}
