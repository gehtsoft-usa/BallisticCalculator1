using System;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// A collection of reticle path elements
    /// </summary>
    public class ReticlePathElementsCollection : ReticleCollectionBase<ReticlePathElement>, ICloneable
    {
        /// <summary>
        /// Swaps two elements in the path
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        public void Swap(int index1, int index2)
        {
            var x = mElements[index1];
            mElements[index1] = mElements[index2];
            mElements[index2] = x;
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public ReticlePathElementsCollection Clone()
        {
            var collection = new ReticlePathElementsCollection();
            for (int i = 0; i < Count; i++)
                collection.Add(this[i].Clone());
            return collection;
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => Clone();
    }
}
