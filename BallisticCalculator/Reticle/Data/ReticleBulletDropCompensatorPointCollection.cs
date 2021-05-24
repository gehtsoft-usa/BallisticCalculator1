using System;
using System.Collections;
using System.Collections.Generic;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>Collection of ReticleBulletDropCompensatorPoint</para>
    /// <para>See also <see cref="ReticleBulletDropCompensatorPoint">ReticleBulletDropCompensatorPoint</see></para>
    /// </summary>
    public class ReticleBulletDropCompensatorPointCollection : ReticleCollectionBase<ReticleBulletDropCompensatorPoint>, ICloneable
    {
        /// <summary>
        /// Sorts BDC by drop
        /// </summary>
        public void Sort()
        {
            mElements.Sort((a, b) => -a.Position.Y.CompareTo(b.Position.Y));
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public ReticleBulletDropCompensatorPointCollection Clone()
        {
            var collection = new ReticleBulletDropCompensatorPointCollection();
            for (int i = 0; i < Count; i++)
                collection.Add(this[i].Clone());
            return collection;
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => Clone();
    }
}
