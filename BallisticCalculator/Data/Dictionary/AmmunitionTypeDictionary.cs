using System;
using System.Collections;
using System.Collections.Generic;

namespace BallisticCalculator.Data.Dictionary
{
    /// <summary>
    /// <para>The list of ammunition types</para>
    /// </summary>
    public class AmmunitionTypeDictionary : IReadOnlyList<AmmunitionType>
    {
        private readonly List<AmmunitionType> mList = new List<AmmunitionType>();

        /// <summary>
        /// Gets an item by the index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public AmmunitionType this[int index] => mList[index];

        /// <summary>
        /// Gets the number of items
        /// </summary>
        public int Count => mList.Count;

        /// <summary>
        /// Constructor
        /// </summary>
        public AmmunitionTypeDictionary()
        {
        }

        /// <summary>
        /// Gets an enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<AmmunitionType> GetEnumerator() => mList.GetEnumerator();

        /// <summary>
        /// Gets an enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Adds an element to the list
        /// </summary>
        /// <param name="value"></param>
        public void Add(AmmunitionType value) => mList.Add(value);

        /// <summary>
        /// Removes an element from the list
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => mList.RemoveAt(index);

        /// <summary>
        /// Sorts the dictionary using the specified function
        /// </summary>
        /// <param name="sorter"></param>
        public void Sort(Comparison<AmmunitionType> sorter) => mList.Sort(sorter);
    }
}
