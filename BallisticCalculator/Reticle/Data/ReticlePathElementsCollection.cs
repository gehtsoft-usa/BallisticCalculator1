namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// A collection of reticle path elements
    /// </summary>
    public class ReticlePathElementsCollection : ReticleCollectionBase<ReticlePathElement>
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
    }

}
