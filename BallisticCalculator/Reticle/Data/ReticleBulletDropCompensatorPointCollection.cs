using System.Collections;
using System.Collections.Generic;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>Collection of ReticleBulletDropCompensatorPoint</para>
    /// <para>See also <see cref="ReticleBulletDropCompensatorPoint">ReticleBulletDropCompensatorPoint</see></para>
    /// </summary>
    public class ReticleBulletDropCompensatorPointCollection : ReticleCollectionBase<ReticleBulletDropCompensatorPoint>
    {
        /// <summary>
        /// Sorts BDC by drop
        /// </summary>
        public void Sort()
        {
            mElements.Sort((a, b) => -a.Position.Y.CompareTo(b.Position.Y));
        }
    }
}
