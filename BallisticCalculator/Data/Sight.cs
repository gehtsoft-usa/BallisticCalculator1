using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// Sight/scope specification
    /// </summary>
    public class Sight
    {
        /// <summary>
        /// Height of the sight/scope over the bore axis
        /// </summary>
        public Measurement<DistanceUnit> SightHeight { get; set; }

        /// <summary>
        /// Vertical adjustment per one click
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? VerticalClick { get; set; }

        /// <summary>
        /// Horizontal adjustment per one click
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? HorizontalClick { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Sight()
        {

        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="sightHeight"></param>
        /// <param name="verticalClick"></param>
        /// <param name="horizontalClick"></param>
        [JsonConstructor]
        public Sight(Measurement<DistanceUnit> sightHeight, Measurement<AngularUnit> verticalClick, Measurement<AngularUnit> horizontalClick)
        {
            SightHeight = sightHeight;
            VerticalClick = verticalClick;
            HorizontalClick = horizontalClick;
        }
    }

}

