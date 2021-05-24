using System;
using System.Collections;
using System.Collections.Generic;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// A collection of reticle elements
    /// </summary>
    public class ReticleElementsCollection : ReticleCollectionBase<ReticleElement>, ICloneable
    {
        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public ReticleElementsCollection Clone()
        {
            var collection = new ReticleElementsCollection();
            for (int i = 0; i < Count; i++)
                collection.Add(this[i].Clone());
            return collection;
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => Clone();
    }
}
