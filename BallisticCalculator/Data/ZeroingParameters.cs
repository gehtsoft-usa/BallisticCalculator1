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
        /// <para>The ammunition used to zero</para>
        /// <para>If the parameter is null, shot ammunition will be used</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty(ChildElement = true, Optional = true)]
        public Ammunition Ammunition { get; set; }

        /// <summary>
        /// <para>The atmosphere at zeroing</para>
        /// <para>If the parameter is null, an atmosphere at should will be used</para>
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
        /// The vertical offset of the zero impact point from aim point
        /// 
        /// Positive values are up, negative values are down
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("vertical-offset", Optional = true)]
        public Measurement<DistanceUnit>? VerticalOffset { get; set; }

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

