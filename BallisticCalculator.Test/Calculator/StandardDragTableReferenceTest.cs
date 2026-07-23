using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AwesomeAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// <para>Validates the less-common standard drag tables (RA4, GI, G5, G6) end-to-end against
    /// reference trajectories produced by an independent ballistics program, and in doing so
    /// exercises the RA4/GI/G5/G6 <see cref="DragTable"/> classes (which no other test touched).</para>
    /// <para>The reference files are the original program output (tab-separated "Report Trace"
    /// format) embedded verbatim; the BC's <see cref="DragTableId"/> is supplied per file because
    /// the library's <see cref="BallisticCoefficient"/> text parser cannot round-trip "RA4".</para>
    /// </summary>
    public class StandardDragTableReferenceTest
    {
        private readonly ITestOutputHelper mOutput;

        public StandardDragTableReferenceTest(ITestOutputHelper output) => mOutput = output;

        private sealed class Reference
        {
            public double Bc;
            public double MuzzleVelocityFps;
            public double WeightGrain;
            public double SightHeightInch;
            public double ZeroYards;
            public double TemperatureF;
            public double PressureInHg;
            public double HumidityPercent;
            public double AltitudeFeet;
            public readonly List<(double RangeYd, double VelocityFps, double PathInch, double DriftInch)> Points = new();
        }

        private static double Num(string s) => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static Reference Parse(string resource)
        {
            using Stream stream = typeof(StandardDragTableReferenceTest).Assembly
                .GetManifestResourceStream($"BallisticCalculator.Test.resources.{resource}.txt");
            using var reader = new StreamReader(stream, Encoding.Latin1);

            var r = new Reference();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var t = new List<string>();
                foreach (var part in line.Split('\t'))
                {
                    var p = part.Trim();
                    if (p.Length > 0)
                        t.Add(p);
                }
                if (t.Count == 0)
                    continue;

                // A data row starts with an integer range value.
                if (int.TryParse(t[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    // Range Velocity Energy Path Drop Drift TOF CDFC Mntum ZeroAdj
                    r.Points.Add((Num(t[0]), Num(t[1]), Num(t[3]), Num(t[5])));
                    continue;
                }

                for (int i = 0; i < t.Count; i++)
                {
                    switch (t[i])
                    {
                        case "Muzzle Vel:": r.MuzzleVelocityFps = Num(t[i + 1]); break;
                        case "Bullet Wgt:": r.WeightGrain = Num(t[i + 1]); break;
                        case "Sight Height:": r.SightHeightInch = Num(t[i + 1]); break;
                        case "Zeroed Rng:": r.ZeroYards = Num(t[i + 1]); break;
                        case "Temperature:": r.TemperatureF = Num(t[i + 1]); break;
                        case "Pressure:": r.PressureInHg = Num(t[i + 1]); break;
                        case "Humidity:": r.HumidityPercent = Num(t[i + 1]); break;
                        case "Altitude:": r.AltitudeFeet = Num(t[i + 1]); break;
                        default:
                            if (t[i].EndsWith("BC:", StringComparison.Ordinal))
                                r.Bc = Num(t[i + 1]);
                            break;
                    }
                }
            }
            return r;
        }

        // Tolerances are ~2x the observed worst-case deviation per table (see the ITestOutputHelper
        // line for the actuals). GI (a very old, coarse drag model) diverges most; the modern
        // boat-tail tables (G5/G6) and the rimfire RA4 track the reference to well under 0.1 MOA.
        [Theory]
        [InlineData("ref_ra4", DragTableId.RA4, 0.30, 0.15)]
        [InlineData("ref_gi", DragTableId.GI, 0.50, 0.30)]
        [InlineData("ref_g5", DragTableId.G5, 0.20, 0.10)]
        [InlineData("ref_g6", DragTableId.G6, 0.20, 0.10)]
        public void MatchesReferenceTrajectory(string resource, DragTableId table,
                                               double velocityTolerancePct, double dropToleranceMOA)
        {
            var reference = Parse(resource);

            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(reference.WeightGrain, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(reference.Bc, table),
                muzzleVelocity: new Measurement<VelocityUnit>(reference.MuzzleVelocityFps, VelocityUnit.FeetPerSecond));

            var rifle = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(reference.SightHeightInch, DistanceUnit.Inch),
                                 Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(reference.ZeroYards, DistanceUnit.Yard), null, null));

            var atmo = new Atmosphere(
                altitude: new Measurement<DistanceUnit>(reference.AltitudeFeet, DistanceUnit.Foot),
                pressure: new Measurement<PressureUnit>(reference.PressureInHg, PressureUnit.InchesOfMercury),
                temperature: new Measurement<TemperatureUnit>(reference.TemperatureF, TemperatureUnit.Fahrenheit),
                humidity: reference.HumidityPercent / 100.0);

            var cal = new TrajectoryCalculator();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(reference.Points[^1].RangeYd, DistanceUnit.Yard),
                ZeroDropAdjustment = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment,
            };

            var trajectory = cal.Calculate(ammo, rifle, atmo, shot);

            trajectory.Length.Should().Be(reference.Points.Count);

            double worstVelPct = 0, worstDropMOA = 0;
            for (int i = 0; i < trajectory.Length; i++)
            {
                var point = trajectory[i];
                var expected = reference.Points[i];

                point.Distance.In(DistanceUnit.Yard).Should().BeApproximately(expected.RangeYd, 0.5);

                double velPct = expected.VelocityFps == 0 ? 0
                    : Math.Abs(point.Velocity.In(VelocityUnit.FeetPerSecond) - expected.VelocityFps) / expected.VelocityFps * 100.0;
                worstVelPct = Math.Max(worstVelPct, velPct);

                double dropErrInch = Math.Abs(point.Drop.In(DistanceUnit.Inch) - expected.PathInch);
                double dropErrMOA = expected.RangeYd < 1 ? 0
                    : Measurement<AngularUnit>.Convert(dropErrInch / (expected.RangeYd / 100.0),
                        AngularUnit.InchesPer100Yards, AngularUnit.MOA);
                worstDropMOA = Math.Max(worstDropMOA, dropErrMOA);
            }

            mOutput.WriteLine($"{resource} ({table}): worst velocity {worstVelPct:F3}%, worst drop {worstDropMOA:F3} MOA over {trajectory.Length} points");

            worstVelPct.Should().BeLessThan(velocityTolerancePct, "muzzle-to-1000yd velocity should track the reference");
            worstDropMOA.Should().BeLessThan(dropToleranceMOA, "drop (path vs line of sight) should track the reference");
        }
    }
}
