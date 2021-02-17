using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The parameters of zeroing
    /// </summary>
    [BXmlElement("zeroing-parameter")]
    public class ZeroingParameters
    {
        /// <summary>
        /// The ammunition used to zero
        /// 
        /// If the parameter is null, shot ammunition will be used
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty(ChildElement = true, Optional = true)]
        public Ammunition Ammunition { get; set; }

        /// <summary>
        /// The atmosphere at zeroing
        /// 
        /// If the parameter is null, an atmosphere at should will be used
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty(ChildElement = true, Optional = true)]
        public Atmosphere Atmosphere { get; set; }

        /// <summary>
        /// The distance of zeroing
        /// </summary>
        [BXmlProperty("zero-distance")]
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

