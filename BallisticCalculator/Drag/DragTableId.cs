using System;
using System.Collections.Generic;
using System.Text;

namespace BallisticCalculator
{
    /// <summary>
    /// The identifier of the drag table
    /// </summary>
    public enum DragTableId
    {
        /// <summary>
        /// Also known as Ingalls, G1 projectiles are flatbase bullets with 2 caliber nose ogive and are the most common type of bullet.
        /// </summary>
        G1,

        /// <summary>
        /// Bullets in the G2 range are Aberdeen J projectiles
        /// </summary>
        G2,

        /// <summary>
        /// G5 bullets are short 7.5 degree boat-tails, with 6.19 caliber long tangent ogive
        /// </summary>
        G5,

        /// <summary>
        /// G6 are flatbase bullets with a 6 cailber secant ogive
        /// </summary>
        G6,

        /// <summary>
        /// Bullets with the G7 BC are long 7.5 degree boat-tails, with 10 caliber tangent ogive, and are very popular with manufacturers for extremely low-drag bullets.
        /// </summary>
        G7,

        /// <summary>
        /// G8s are flatbase with a 10 caliber secant ogive
        /// </summary>
        G8,

        /// <summary>
        /// GI
        /// </summary>
        GI,

        /// <summary>
        /// GS are sphere
        /// </summary>
        GS,

        /// <summary>
        /// Custom coefficient (specify table directly)
        /// </summary>
        GC,

        /// <summary>
        /// RA4 is drag model for slugs, rimfire and airguns
        /// </summary>
        RA4,
    }
}
