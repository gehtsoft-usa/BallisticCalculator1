using System;
using System.Text;

namespace BallisticCalculator.Data.Dictionary
{
    /// <summary>
    /// <para>Record of ammunition type dictionary</para>
    /// <para>You can use these values to fill <see cref="AmmunitionLibraryEntry.AmmunitionType"/> field</para>
    /// </summary>
    public sealed class AmmunitionType : IEquatable<AmmunitionType>
    {
        /// <summary>
        /// A short name
        /// </summary>
        public string Abbreviation { get; }

        /// <summary>
        /// A full name
        /// </summary>
        public string Name { get; }
        
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="abbreviation"></param>
        /// <param name="name"></param>
        public AmmunitionType(string abbreviation, string name)
        {
            Abbreviation = abbreviation ?? throw new ArgumentNullException(nameof(abbreviation));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Converts the object to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Abbreviation;
        }

        /// <summary>
        /// Returns hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Abbreviation.GetHashCode();
                hash = hash * 23 + Name.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Checks whether object equals to another object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(AmmunitionType other)
        {
            if (other == null)
                return false;
            return Abbreviation == other.Abbreviation && Name == other.Name;
        }

        /// <summary>
        /// Checks whether object equals to another object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is AmmunitionType other)
                return Equals(other);
            return ReferenceEquals(this, obj);
        }
    }

        
}
