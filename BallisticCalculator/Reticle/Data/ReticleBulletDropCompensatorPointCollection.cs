using System.Collections;
using System.Collections.Generic;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// Collection of <see cref="ReticleBulletDropCompensatorPoint">bullet drop compensator points</see>
    /// </summary>
    public class ReticleBulletDropCompensatorPointCollection : ReticleCollectionBase<ReticleBulletDropCompensatorPoint>
    {
        /// <summary>
        /// Sorts BDC by drop
        /// </summary>
        public void Sort()
        {
            mElements.Sort((a, b) => a.Drop.CompareTo(b.Drop));
        }
    }

}
