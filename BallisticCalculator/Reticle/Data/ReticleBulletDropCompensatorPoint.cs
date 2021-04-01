﻿using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Text;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>The information about bullet drop compensator point</para>
    /// <para>
    /// Typically, an applications shows a distance that corresponds to the
    /// specific point.
    /// </para>
    /// </summary>
    [BXmlElement("bdc")]
    public class ReticleBulletDropCompensatorPoint
    {
        /// <summary>
        /// Position of the BDC point at reticle
        /// </summary>
        [BXmlProperty(Name = "position", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Position { get; set; }

        /// <summary>
        /// <para>Offset of the text from the BDC point</para>
        /// <para>Positive value means text at right of BDC, negative - text at left of BDC</para>
        /// </summary>
        [BXmlProperty("text-offset")]
        public Measurement<AngularUnit> TextOffset { get; set; }

        /// <summary>
        /// The height of the BDC text
        /// </summary>
        [BXmlProperty("text-height")]
        public Measurement<AngularUnit> TextHeight { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleBulletDropCompensatorPoint()
        {
        }
    }
}
