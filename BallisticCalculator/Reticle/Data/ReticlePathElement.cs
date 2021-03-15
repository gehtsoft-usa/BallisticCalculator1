using BallisticCalculator.Serialization;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>The base class for path elements</para>
    /// <para>
    /// The implementations are <see cref="ReticlePathElementArc" />, <see cref="ReticlePathElementMoveTo" />, and
    /// <see cref="ReticlePathElementLineTo" />
    /// </para>
    /// </summary>
    [BXmlSelect(typeof(ReticlePathElementMoveTo), typeof(ReticlePathElementLineTo), typeof(ReticlePathElementArc))]
    public abstract class ReticlePathElement
    {
        /// <summary>
        /// The position of action
        /// </summary>
        [BXmlProperty(Name = "position", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Position { get; set; }

        /// <summary>
        /// The type of the action
        /// </summary>
        public ReticlePathElementType ElementType { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="elementType"></param>
        protected ReticlePathElement(ReticlePathElementType elementType)
        {
            ElementType = elementType;
        }

        /// <summary>
        /// Cast this object to the specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T As<T>() where T : ReticlePathElement => this as T;
    }
}
