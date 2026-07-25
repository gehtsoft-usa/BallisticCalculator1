using System;
using System.Collections;
using System.Collections.Generic;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// The base class for reticle collections
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ReticleCollectionBase<T> : IReadOnlyList<T>
    {
        /// <summary>
        /// Collection of elements
        /// </summary>
        protected readonly List<T> mElements = new List<T>();

        /// <summary>
        /// Returns number of the elements
        /// </summary>
        public int Count => mElements.Count;

        /// <summary>
        /// Gets or sets the element by the index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get => mElements[index];
            set => mElements[index] = value;
        }

        /// <summary>
        /// Adds a new element in the collection
        /// </summary>
        /// <param name="element"></param>
        public void Add(T element) => mElements.Add(element);

        /// <summary>
        /// Inserts a new element into the collection at the specified position
        /// </summary>
        /// <param name="index">The zero-based position at which the element is inserted; it must be between 0 and Count.</param>
        /// <param name="element">The element to insert.</param>
        public void Insert(int index, T element) => mElements.Insert(index, element);

        /// <summary>
        /// Removes the element from the collection by its position
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => mElements.RemoveAt(index);

        /// <summary>
        /// Removes all elements
        /// </summary>
        public void Clear() => mElements.Clear();

        /// <summary>
        /// Returns enumerator of the elements
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => mElements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
