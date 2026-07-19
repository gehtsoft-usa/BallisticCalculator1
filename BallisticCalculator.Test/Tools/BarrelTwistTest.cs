using System;
using AwesomeAssertions;
using BallisticCalculator;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Tools
{
    public class BarrelTwistTest
    {
        // 30 cal 220 gr ELD-X, 2600 fps, 1:7" — the engine's Miller Sg for this is ~2.78 (PLAN0).
        private static Ammunition Eldx() => new Ammunition(
            weight: WeightUnit.Grain.New(220),
            ballisticCoefficient: new BallisticCoefficient(0.325, DragTableId.G7),
            muzzleVelocity: VelocityUnit.FeetPerSecond.New(2600),
            bulletDiameter: DistanceUnit.Inch.New(0.308),
            bulletLength: DistanceUnit.Inch.New(1.630));

        [Fact]
        public void Stability_MatchesEngineMiller_ForEldx()
        {
            BarrelTwist.Stability(Eldx(), DistanceUnit.Inch.New(7))
                .Should().BeApproximately(2.78, 0.02);   // matches the engine / PLAN0 value
        }

        [Fact]
        public void RecommendedTwist_IsInverseOfStability()
        {
            var ammo = Eldx();
            // Known point: Sg 2.78 -> ~1:7".
            BarrelTwist.RecommendedTwist(ammo, 2.78).In(DistanceUnit.Inch).Should().BeApproximately(7.0, 0.05);

            // Round-trip across a range of targets, at standard and non-standard atmosphere.
            var hot = new Atmosphere(DistanceUnit.Foot.New(0), new Measurement<PressureUnit>(29.92, PressureUnit.InchesOfMercury),
                new Measurement<TemperatureUnit>(100, TemperatureUnit.Fahrenheit), 0.5);
            foreach (var atmo in new[] { (Atmosphere)null, hot })
                foreach (var sg in new[] { 1.2, 1.5, 2.0, 2.5 })
                {
                    var twist = BarrelTwist.RecommendedTwist(ammo, sg, atmo);
                    BarrelTwist.Stability(ammo, twist, atmo).Should().BeApproximately(sg, 1e-9, $"round-trip Sg={sg}");
                }
        }

        // The Sg=1.5 twist should reproduce manufacturers' published minimum twists (Litz/Berger
        // design to Sg 1.5). Grounds the default minimum-stability choice in real data.
        [Theory]
        [InlineData(180, 0.284, 1.517, 2900, 9.0)]   // Berger 7mm 180 VLD — published min 1:9"
        [InlineData(185, 0.308, 1.495, 2650, 10.0)]  // Berger .30 185 Juggernaut — published min 1:10"
        public void RecommendedTwist_At1_5_MatchesPublishedMinimum(double gr, double dia, double len, double mv, double publishedMinTwist)
        {
            var ammo = new Ammunition(
                weight: WeightUnit.Grain.New(gr), ballisticCoefficient: new BallisticCoefficient(0.3, DragTableId.G7),
                muzzleVelocity: VelocityUnit.FeetPerSecond.New(mv),
                bulletDiameter: DistanceUnit.Inch.New(dia), bulletLength: DistanceUnit.Inch.New(len));
            BarrelTwist.RecommendedTwist(ammo, 1.5).In(DistanceUnit.Inch)
                .Should().BeApproximately(publishedMinTwist, 0.4, "Sg 1.5 reproduces the published minimum twist");
        }

        [Fact]
        public void Recommend_OrdersTwistsAndHitsTargets()
        {
            var ammo = Eldx();
            var r = BarrelTwist.Recommend(ammo);   // grounded defaults 1.5 / 2.0 / 3.0
            r.MinimumStability.Should().Be(1.5);
            r.OptimalStability.Should().Be(2.0);
            r.MaximumStability.Should().Be(3.0);

            // Higher stability needs a faster twist ⇒ shorter step: min (slowest) > optimal > max (fastest).
            r.MinimumTwist.In(DistanceUnit.Inch).Should().BeGreaterThan(r.OptimalTwist.In(DistanceUnit.Inch));
            r.OptimalTwist.In(DistanceUnit.Inch).Should().BeGreaterThan(r.MaximumTwist.In(DistanceUnit.Inch));

            // Each recommended twist actually reaches its stability target.
            BarrelTwist.Stability(ammo, r.MinimumTwist).Should().BeApproximately(r.MinimumStability, 1e-9);
            BarrelTwist.Stability(ammo, r.OptimalTwist).Should().BeApproximately(r.OptimalStability, 1e-9);
            BarrelTwist.Stability(ammo, r.MaximumTwist).Should().BeApproximately(r.MaximumStability, 1e-9);
        }

        [Fact]
        public void Guards()
        {
            var noDims = new Ammunition(weight: WeightUnit.Grain.New(220),
                ballisticCoefficient: new BallisticCoefficient(0.325, DragTableId.G7),
                muzzleVelocity: VelocityUnit.FeetPerSecond.New(2600));

            ((Action)(() => BarrelTwist.Stability(noDims, DistanceUnit.Inch.New(7)))).Should().Throw<ArgumentException>();
            ((Action)(() => BarrelTwist.RecommendedTwist(noDims, 1.5))).Should().Throw<ArgumentException>();
            ((Action)(() => BarrelTwist.RecommendedTwist(Eldx(), 0))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => BarrelTwist.Recommend(Eldx(), null, 2.0, 1.5, 1.0))).Should().Throw<ArgumentException>();
        }
    }
}
