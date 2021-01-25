using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The parameters of zeroing
    /// </summary>
    public class ZeroingParameters
    {
        /// <summary>
        /// The ammunition used to zero
        /// 
        /// If the parameter is null, shot ammunition will be used
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Ammunition Ammunition { get; set; }

        /// <summary>
        /// The atmosphere at zeroing
        /// 
        /// If the parameter is null, an atmosphere at should will be used
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Atmosphere Atmosphere { get; set; }

        /// <summary>
        /// The distance of zeroing
        /// </summary>
        public Measurement<DistanceUnit> Distance { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ZeroingParameters()
        {

        }

        /// <summary>
        /// Parameterized/serialization constructor.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="ammunition"></param>
        /// <param name="atmosphere"></param>
        [JsonConstructor]
        public ZeroingParameters(Measurement<DistanceUnit> distance, Ammunition ammunition, Atmosphere atmosphere)
        {
            Ammunition = ammunition;
            Atmosphere = atmosphere;
            Distance = distance;
        }

    }

}

