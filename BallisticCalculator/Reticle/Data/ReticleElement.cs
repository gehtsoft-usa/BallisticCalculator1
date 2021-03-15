using BallisticCalculator.Serialization;
using System;
using System.Text;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>An element of a reticle</para>
    /// <para>
    /// The implementation types are <see cref="ReticleCircle" />,
    /// <see cref="ReticleLine" />, <see cref="ReticlePath" />, <see cref="ReticleRectangle" />, and
    /// <see cref="ReticleText" />.
    /// </para>
    /// </summary>
    [BXmlSelect(typeof(ReticleCircle), typeof(ReticlePath),
                typeof(ReticleLine), typeof(ReticleRectangle), typeof(ReticleText))]
    public abstract class ReticleElement
    {
        /// <summary>
        /// The type of the element
        /// </summary>
        public ReticleElementType ElementType { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type"></param>
        protected ReticleElement(ReticleElementType type)
        {
            ElementType = type;
        }

        /// <summary>
        /// Cast to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T As<T>() where T : ReticleElement => this as T;
    }
}
