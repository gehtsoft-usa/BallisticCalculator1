using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// <para>The barrel adjustments produced by a zeroing calculation.</para>
    /// <para>Carries the vertical ([clink=BallisticCalculator.ZeroCalculatedParameters.ZeroDropAdjustment.55B]ZeroDropAdjustment[/clink])
    /// and horizontal ([clink=BallisticCalculator.ZeroCalculatedParameters.ZeroWindageAdjustment.kzE]ZeroWindageAdjustment[/clink])
    /// angles the sight is zeroed at, so they can be transferred onto a
    /// [clink=BallisticCalculator.ShotParameters]ShotParameters[/clink] by its
    /// [clink=BallisticCalculator.ShotParameters.Apply.JkC]Apply[/clink] method.</para>
    /// </summary>
    [BXmlElement("zero-calculated-parameters")]
    public class ZeroCalculatedParameters
    {
        /// <summary>
        /// <para>Vertical barrel elevation established by zeroing.</para>
        /// <para>Positive points the barrel up.</para>
        /// </summary>
        [BXmlProperty("zero-drop-adjustment")]
        public Measurement<AngularUnit> ZeroDropAdjustment { get; set; }

        /// <summary>
        /// <para>Horizontal barrel angle established by zeroing.</para>
        /// <para>Positive tilts the barrel left, matching trajectory windage (positive left, negative right).
        /// When omitted, treated as zero.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("zero-windage-adjustment", Optional = true)]
        public Measurement<AngularUnit>? ZeroWindageAdjustment { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ZeroCalculatedParameters()
        {
        }

        /// <summary>
        /// Parameterized/serialization constructor.
        /// </summary>
        /// <param name="zeroDropAdjustment">Vertical zeroing elevation.</param>
        /// <param name="zeroWindageAdjustment">Horizontal zeroing angle (positive left, negative right); optional.</param>
        [JsonConstructor]
        public ZeroCalculatedParameters(Measurement<AngularUnit> zeroDropAdjustment, Measurement<AngularUnit>? zeroWindageAdjustment = null)
        {
            ZeroDropAdjustment = zeroDropAdjustment;
            ZeroWindageAdjustment = zeroWindageAdjustment;
        }
    }
}
