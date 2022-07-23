using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;

namespace BallisticCalculator.Data.Dictionary
{

    /// <summary>
    /// <para>Ammunition caliber</para>
    /// <para>You can use this value for <see cref="AmmunitionLibraryEntry.Caliber" /> property</para>
    /// </summary>
    public sealed class AmmunitionCaliber : IEquatable<AmmunitionCaliber>
    {
        /// <summary>
        /// Type of ammunition
        /// </summary>
        public AmmunitionCaliberType TypeOfAmmunition { get; }

        /// <summary>
        /// <para>Caliber group</para>
        /// <para>E.g. 7mm for all cartridges between 7 and 8 mm</para>
        /// </summary>
        public Measurement<DistanceUnit>? CaliberGroup { get; }

        /// <summary>
        /// Bullet diameter
        /// </summary>
        public Measurement<DistanceUnit> BulletDiameter { get; }

        /// <summary>
        /// The name of the caliber
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The alternative name of the caliber
        /// </summary>
        public IReadOnlyList<string> AlternativeNames { get; } 

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="typeOfAmmunition"></param>
        /// <param name="group"></param>
        /// <param name="bulletDiameter"></param>
        /// <param name="name"></param>
        /// <param name="alternativeName">Comma-separated list of alternative names</param>
        public AmmunitionCaliber(AmmunitionCaliberType typeOfAmmunition,
                                 Measurement<DistanceUnit>? group,
                                 Measurement<DistanceUnit> bulletDiameter,
                                 string name, 
                                 string alternativeName)
        {
            TypeOfAmmunition = typeOfAmmunition;
            CaliberGroup = group;
            BulletDiameter = bulletDiameter;
            Name = name;
            List<string> l = new List<string>();
            if (!string.IsNullOrEmpty(alternativeName))
            {
                var alternativeNames = alternativeName.Split(',');
                for (int i = 0; i < alternativeNames.Length; i++)
                {
                    var a = alternativeNames[i].Trim();
                    if (!string.IsNullOrEmpty(a))
                        l.Add(a);
                }
            }
            AlternativeNames = l;
        }

        /// <summary>
        /// Returns object as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns hash code value
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + TypeOfAmmunition.GetHashCode();
                hash = hash * 23 + (CaliberGroup?.GetHashCode() ?? 0.GetHashCode());
                hash = hash * 23 + BulletDiameter.GetHashCode(); 
                hash = hash * 23 + Name.GetHashCode();
                hash = hash * 23 + Name.GetHashCode(); 
                foreach (var an in AlternativeNames)
                    hash = hash * 23 + an.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Checks whether the object is equal to another object
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(AmmunitionCaliber other)
        {
            if (other == null)
                return false;
            
            if (TypeOfAmmunition != other.TypeOfAmmunition ||
                (CaliberGroup is null && other.CaliberGroup is not null) ||
                (CaliberGroup is not null && other.CaliberGroup is null) ||
                (CaliberGroup is not null && other.CaliberGroup is not null && 
                 CaliberGroup.Value != other.CaliberGroup.Value) ||
                BulletDiameter != other.BulletDiameter ||
                Name != other.Name ||
                AlternativeNames.Count != other.AlternativeNames.Count)
                return false;

            for (int i = 0; i < AlternativeNames.Count; i++)
                if (AlternativeNames[i] != other.AlternativeNames[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Checks whether the object is equal to another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is AmmunitionCaliber other)
                return Equals(other);
            return false;
        }

        /// <summary>
        /// Checks whether the name specified is one of the name of the a caliber
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Is(string name)
        {
            if (name == Name)
                return true;
            for (int i = 0; i < AlternativeNames.Count; i++)
                if (AlternativeNames[i] == name)
                    return true;
            return false;
        }
    }
}
