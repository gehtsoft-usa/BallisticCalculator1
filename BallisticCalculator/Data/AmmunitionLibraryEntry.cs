using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class AmmunitionLibraryEntry
    {
        public string Name { get; set; }
        public string Source { get; set; }
        public string Caliber { get; set; }
        public string AmmunitionType { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<DistanceUnit>? BarrelLength { get; set; }

        public Ammunition Ammunition { get; set; }

        public AmmunitionLibraryEntry()
        {

        }

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

