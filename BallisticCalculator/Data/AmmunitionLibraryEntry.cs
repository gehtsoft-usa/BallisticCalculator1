using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// An entry into a projectile library
    /// </summary>
    public class AmmunitionLibraryEntry
    {
        /// <summary>
        /// The name of the projectile
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The source of the information
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Source { get; set; }

        /// <summary>
        /// The name of the caliber (e.g. 7.62x39)
        /// </summary>
        public string Caliber { get; set; }

        /// <summary>
        /// The type of the projectile (e.g FMJ)
        /// </summary>
        public string AmmunitionType { get; set; }

        /// <summary>
        /// The length of the barrel at which the muzzle velocity is measured
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<DistanceUnit>? BarrelLength { get; set; }

        /// <summary>
        /// The projectile parameters
        /// </summary>
        public Ammunition Ammunition { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AmmunitionLibraryEntry()
        {

        }

        /// <summary>
        /// Parameterized/serialization constructor
        /// </summary>
        /// <param name="ammunition"></param>
        /// <param name="name"></param>
        /// <param name="source"></param>
        /// <param name="caliber"></param>
        /// <param name="ammunitionType"></param>
        /// <param name="barrelLength"></param>
        [JsonConstructor]
        public AmmunitionLibraryEntry(Ammunition ammunition, string name, string source, string caliber, string ammunitionType, Measurement<DistanceUnit>? barrelLength = null)
        {
            Ammunition = ammunition;
            Name = name;
            Source = source;
            Caliber = caliber;
            AmmunitionType = ammunitionType;
            BarrelLength = barrelLength;
        }
    }

}

