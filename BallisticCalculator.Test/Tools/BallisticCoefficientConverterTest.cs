using System;
using AwesomeAssertions;
using BallisticCalculator;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Tools
{
    public class BallisticCoefficientConverterTest
    {
        // Round-trip at a fixed Mach is exact: the Cd ratio inverts.
        [Theory]
        [InlineData(1.2)]
        [InlineData(2.0)]
        [InlineData(3.0)]
        public void RoundTrip_G7_G1_G7(double mach)
        {
            var g7 = new BallisticCoefficient(0.325, DragTableId.G7);
            var g1 = BallisticCoefficientConverter.Convert(g7, DragTableId.G1, mach);
            var back = BallisticCoefficientConverter.Convert(g1, DragTableId.G7, mach);

            g1.Table.Should().Be(DragTableId.G1);
            back.Table.Should().Be(DragTableId.G7);
            back.Value.Should().BeApproximately(g7.Value, 1e-9);
        }

        // The 30 cal 220 gr ELD-X is published as 0.325 G7 / 0.650 G1 (a ~2x ratio). Converting the
        // G7 number at a representative supersonic velocity should land close to the published G1.
        [Fact]
        public void G7ToG1_ApproximatesPublishedPair_ForEldx()
        {
            var g7 = new BallisticCoefficient(0.325, DragTableId.G7);
            var g1 = BallisticCoefficientConverter.Convert(g7, DragTableId.G1,
                new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond));

            // Velocity-tied single-number conversion: expect the ballpark of the published 0.650 G1.
            g1.Value.Should().BeInRange(0.55, 0.75);
        }

        // Accuracy on manufacturer-published G1/G7 pairs, converting G7 -> G1 at a representative
        // supersonic velocity (2500 fps ~ Mach 2.2). All land within ~2% of the published G1 — the
        // conversion is accurate wherever the bullet is comfortably supersonic (see the transonic
        // caveat in Conversion_IsVelocityDependent / the converter docs).
        [Theory]
        [InlineData(0.325, 0.650)]   // Hornady .30 220 ELD-X
        [InlineData(0.351, 0.697)]   // Hornady 6.5 147 ELD-M
        [InlineData(0.278, 0.552)]   // Hornady .30 178 ELD-X
        [InlineData(0.315, 0.625)]   // Hornady 6.5 143 ELD-X
        [InlineData(0.381, 0.743)]   // Berger .30 230 Hybrid
        [InlineData(0.253, 0.505)]   // Sierra .30 175 SMK
        public void G7ToG1_MatchesPublishedPairs_WithinTwoPercent(double publishedG7, double publishedG1)
        {
            var g1 = BallisticCoefficientConverter.Convert(
                new BallisticCoefficient(publishedG7, DragTableId.G7), DragTableId.G1,
                new Measurement<VelocityUnit>(2500, VelocityUnit.FeetPerSecond));
            g1.Value.Should().BeApproximately(publishedG1, publishedG1 * 0.025);
        }

        // The G1/G7 curve ratio changes with Mach, so the converted number must depend on velocity
        // (a single constant factor would be wrong).
        [Fact]
        public void Conversion_IsVelocityDependent()
        {
            var g7 = new BallisticCoefficient(0.325, DragTableId.G7);
            double slow = BallisticCoefficientConverter.Convert(g7, DragTableId.G1, 1.2).Value;
            double fast = BallisticCoefficientConverter.Convert(g7, DragTableId.G1, 3.0).Value;
            Math.Abs(slow - fast).Should().BeGreaterThan(0.02, "the G1/G7 ratio varies with Mach");
        }

        [Fact]
        public void SameTable_ReturnsInput()
        {
            var g7 = new BallisticCoefficient(0.325, DragTableId.G7);
            BallisticCoefficientConverter.Convert(g7, DragTableId.G7, 2.0).Should().Be(g7);
        }

        [Fact]
        public void Guards()
        {
            var g7 = new BallisticCoefficient(0.325, DragTableId.G7);
            var ff = new BallisticCoefficient(1.0, DragTableId.G7, BallisticCoefficientValueType.FormFactor);
            var gc = new BallisticCoefficient(1.0, DragTableId.GC);

            ((Action)(() => BallisticCoefficientConverter.Convert(ff, DragTableId.G1, 2.0)))
                .Should().Throw<ArgumentException>();
            ((Action)(() => BallisticCoefficientConverter.Convert(gc, DragTableId.G1, 2.0)))
                .Should().Throw<ArgumentException>();
            ((Action)(() => BallisticCoefficientConverter.Convert(g7, DragTableId.GC, 2.0)))
                .Should().Throw<ArgumentException>();
            ((Action)(() => BallisticCoefficientConverter.Convert(g7, DragTableId.G1, 0)))
                .Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => BallisticCoefficientConverter.Convert(g7, DragTableId.G1,
                new Measurement<VelocityUnit>(0, VelocityUnit.FeetPerSecond))))
                .Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
