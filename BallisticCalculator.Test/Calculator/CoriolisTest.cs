using AwesomeAssertions;
using Gehtsoft.Measurements;
using System;
using System.Linq;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// Tests for the Earth-rotation (Coriolis / Eötvös) deflection and the azimuth-frame
    /// decoupling. See CLAUDE/CORIOLIS.md for the model and the reference data.
    /// </summary>
    public class CoriolisTest
    {
        private const double Omega = 7.2921159e-5;      // Earth rotation, rad/s
        private const double GMps = 9.80665;            // m/s^2

        // Kestrel 5700 reference configuration (CORIOLIS.md §6.5).
        private static (Ammunition, Rifle, Atmosphere) KestrelConfig()
        {
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(69, WeightUnit.Grain),
                muzzleVelocity: new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond),
                ballisticCoefficient: new BallisticCoefficient(0.365, DragTableId.G1));

            var rifle = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(3.2, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));

            var atmo = new Atmosphere(
                altitude: new Measurement<DistanceUnit>(0, DistanceUnit.Foot),
                pressure: new Measurement<PressureUnit>(29.92, PressureUnit.InchesOfMercury),
                temperature: new Measurement<TemperatureUnit>(59, TemperatureUnit.Fahrenheit),
                humidity: 0);

            return (ammo, rifle, atmo);
        }

        private static ShotParameters KestrelShot(Measurement<AngularUnit> sightAngle, double? latDeg, double? azDeg) =>
            new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(1, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(2000, DistanceUnit.Yard),
                ZeroDropAdjustment = sightAngle,
                Latitude = latDeg == null ? (Measurement<AngularUnit>?)null : new Measurement<AngularUnit>(latDeg.Value, AngularUnit.Degree),
                BarrelAzimuth = azDeg == null ? (Measurement<AngularUnit>?)null : new Measurement<AngularUnit>(azDeg.Value, AngularUnit.Degree),
            };

        private static TrajectoryPoint PointAt(TrajectoryPoint[] trajectory, double yards) =>
            trajectory.Where(p => p != null).First(p => Math.Abs(p.Distance.In(DistanceUnit.Yard) - yards) < 0.5);

        /// <summary>
        /// §6.1 — Azimuth robustness. With the §3 decoupling, a non-zero azimuth without latitude
        /// is a pure no-op: the trajectory must be numerically identical to the Az=0 run (this
        /// identity replaces the old azimuth-90 divide-by-zero collapse).
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(90)]
        [InlineData(180)]
        [InlineData(270)]
        public void Azimuth_WithoutLatitude_IsIdenticalToZero(double azDeg)
        {
            var template = TableLoader.FromResource("g1_nowind");
            var cal = new TrajectoryCalculator();
            var sightAngle = cal.CalculateZeroParameters(template.Ammunition, template.Atmosphere, template.Rifle, template.Rifle.Zero).ZeroDropAdjustment;

            ShotParameters Shot(double? az) => new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
                ZeroDropAdjustment = sightAngle,
                BarrelAzimuth = az == null ? (Measurement<AngularUnit>?)null : new Measurement<AngularUnit>(az.Value, AngularUnit.Degree),
            };

            var withAz = cal.Calculate(template.Ammunition, template.Rifle, template.Atmosphere, Shot(azDeg));
            var baseline = cal.Calculate(template.Ammunition, template.Rifle, template.Atmosphere, Shot(null));

            withAz.Length.Should().BeGreaterThan(1);
            withAz.Should().NotContainNulls();
            withAz.Last().Distance.In(DistanceUnit.Yard).Should().BeGreaterThan(900);

            withAz.Length.Should().Be(baseline.Length);
            for (int i = 0; i < withAz.Length; i++)
            {
                withAz[i].Distance.Should().Be(baseline[i].Distance, $"@{i}");
                withAz[i].Drop.Should().Be(baseline[i].Drop, $"@{i}");
                withAz[i].Windage.Should().Be(baseline[i].Windage, $"@{i}");
            }
        }

        // Kestrel's Coriolis-on elevation hold at 2000 yd with azimuth 0/180 (i.e. Eötvös = 0,
        // sin az = 0). This is the pure ballistic drop-in-MOA for this config and appears as the
        // expElevationMOA of every az∈{0,180} row below. It is NOT a tolerance — it is the
        // reference the azimuth-dependent Eötvös swing is measured against so that our own drag
        // baseline cancels (see below). Value read from the PR #47 Kestrel 5700 table (CORIOLIS.md §6.5).
        private const double KestrelBaselineElevationMOA = -222.52;

        /// <summary>
        /// §6.3/§6.5 — Kestrel 5700 acceptance at 2000 yd (PR #47 table, CORIOLIS.md §6.5).
        /// <para>Windage baseline is zero (no wind, no spin drift), so it is asserted absolutely.
        /// In angular form WindageAdjustment ≈ atan(Ω·sinφ·Range·TOF / Range) = atan(Ω·sinφ·TOF) —
        /// Range cancels, leaving only our time-of-flight vs Kestrel's. The two differ by the
        /// long-range G1-vs-Kestrel drag divergence (~0.2% on TOF), and the table is rounded to
        /// 0.01 MOA; the 0.15 MOA window covers both (observed residual ≤ ~0.05 MOA).</para>
        /// <para>Elevation is dominated by the ballistic drop (≈ −222.52 MOA), so we assert the
        /// azimuth-driven Eötvös *swing* as a delta vs our own Coriolis-off baseline. The expected
        /// swing is expElevationMOA − <see cref="KestrelBaselineElevationMOA"/> (0 for az 0/180,
        /// ±1.88 for az 90/270). Differencing cancels the drag baseline, so the residual is only
        /// the exact Eötvös factor times a small baseline mismatch plus Kestrel's 0.01 MOA
        /// rounding — the 0.12 MOA window covers it (observed ≤ ~0.03 MOA).</para>
        /// </summary>
        [Theory]
        [InlineData(45, 0, -1.03, -222.52)]
        [InlineData(0, 0, 0.00, -222.52)]
        [InlineData(90, 0, -1.46, -222.52)]
        [InlineData(45, 90, -1.03, -220.64)]
        [InlineData(45, 180, -1.03, -222.52)]
        [InlineData(45, 270, -1.03, -224.40)]
        [InlineData(30, 0, -0.73, -222.52)]
        [InlineData(60, 0, -1.27, -222.52)]
        [InlineData(-45, 0, 1.03, -222.52)]
        [InlineData(-45, 90, 1.03, -220.64)]
        [InlineData(-45, 180, 1.03, -222.52)]
        [InlineData(-45, 270, 1.03, -224.40)]
        public void Kestrel_Acceptance(double latDeg, double azDeg, double expWindageMOA, double expElevationMOA)
        {
            const double windageToleranceMOA = 0.15;    // TOF/drag divergence + 0.01 MOA table rounding
            const double eotvosToleranceMOA = 0.12;      // baseline mismatch + 0.01 MOA table rounding

            var (ammo, rifle, atmo) = KestrelConfig();
            var cal = new TrajectoryCalculator();
            var sightAngle = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            var on = cal.Calculate(ammo, rifle, atmo, KestrelShot(sightAngle, latDeg, azDeg));
            var off = cal.Calculate(ammo, rifle, atmo, KestrelShot(sightAngle, null, null));

            var pOn = PointAt(on, 2000);
            var pOff = PointAt(off, 2000);

            pOn.WindageAdjustment.In(AngularUnit.MOA).Should().BeApproximately(expWindageMOA, windageToleranceMOA);

            double expSwing = expElevationMOA - KestrelBaselineElevationMOA;
            double gotSwing = pOn.DropAdjustment.In(AngularUnit.MOA) - pOff.DropAdjustment.In(AngularUnit.MOA);
            gotSwing.Should().BeApproximately(expSwing, eotvosToleranceMOA);
        }

        /// <summary>
        /// §6.4 — Closed-form unit guard: the horizontal deflection must equal exactly
        /// −Ω·sinφ·Range·TOF (deflecting right in the N hemisphere ⇒ negative in our sign).
        /// Pins the model against silent future changes.
        /// </summary>
        [Fact]
        public void ClosedForm_Windage_EqualsOmegaSinPhiRangeTof()
        {
            var (ammo, rifle, atmo) = KestrelConfig();
            var cal = new TrajectoryCalculator();
            var sightAngle = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            var on = cal.Calculate(ammo, rifle, atmo, KestrelShot(sightAngle, 45, 0));
            var p = PointAt(on, 1000);

            double sinPhi = new Measurement<AngularUnit>(45, AngularUnit.Degree).Sin();
            double expected = -(Omega * sinPhi * p.Distance.In(DistanceUnit.Meter) * p.Time.TotalSeconds);

            // 1e-6 m tolerates the TimeSpan tick truncation on the output Time vs the internal
            // full-precision time-of-flight (both feed the same formula); windage itself is ~0.1 m.
            p.Windage.In(DistanceUnit.Meter).Should().BeApproximately(expected, 1e-6);
        }

        /// <summary>
        /// §6.4 — Closed-form unit guard: the Eötvös vertical must scale the *gravitational fall*
        /// (the deviation below the vacuum bore line, LineOfDepartureElevation) by exactly
        /// g_eff/g = 1 − 2Ω·cosφ·sin(az)·V₀/g — leaving the bore geometry untouched. The integrator
        /// is untouched, so the on/off fall ratio is that constant to floating-point precision.
        /// </summary>
        [Fact]
        public void ClosedForm_Drop_ScaledByEotvosRatio()
        {
            var (ammo, rifle, atmo) = KestrelConfig();
            var cal = new TrajectoryCalculator();
            var sightAngle = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            var on = cal.Calculate(ammo, rifle, atmo, KestrelShot(sightAngle, 45, 90));
            var off = cal.Calculate(ammo, rifle, atmo, KestrelShot(sightAngle, null, null));

            var pOn = PointAt(on, 1000);
            var pOff = PointAt(off, 1000);

            double cosPhi = new Measurement<AngularUnit>(45, AngularUnit.Degree).Cos();
            double sinAz = new Measurement<AngularUnit>(90, AngularUnit.Degree).Sin();
            double v0mps = ammo.MuzzleVelocity.In(VelocityUnit.MetersPerSecond);
            double vRatio = 1.0 - 2.0 * Omega * cosPhi * sinAz * v0mps / GMps;

            // Fall below the vacuum bore line (LineOfDepartureElevation is Coriolis-independent).
            double fallOn = pOn.DropFlat.In(DistanceUnit.Meter) - pOn.LineOfDepartureElevation.In(DistanceUnit.Meter);
            double fallOff = pOff.DropFlat.In(DistanceUnit.Meter) - pOff.LineOfDepartureElevation.In(DistanceUnit.Meter);

            (fallOn / fallOff).Should().BeApproximately(vRatio, 1e-9);
        }

        // Ballistic Explorer absolute acceptance (multi-range, all output points) lives in
        // TrajectoryCalculatorTest.CoriolisTrajectoryTest, driven by the be_coriolis_* templates.

        /// <summary>
        /// §6.5 — Sign / hemisphere coverage sanity checks derived from the closed form:
        /// N hemisphere deflects right (windage &lt; 0), S left (&gt; 0), equator ≈ 0 horizontal,
        /// East lowers drop, West raises it, and the Eötvös swing is hemisphere-symmetric.
        /// </summary>
        [Fact]
        public void SignAndHemisphere_Coverage()
        {
            var (ammo, rifle, atmo) = KestrelConfig();
            var cal = new TrajectoryCalculator();
            var sightAngle = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            double Windage(double? lat, double? az) => PointAt(cal.Calculate(ammo, rifle, atmo, KestrelShot(sightAngle, lat, az)), 2000).Windage.In(DistanceUnit.Meter);
            double Drop(double? lat, double? az) => PointAt(cal.Calculate(ammo, rifle, atmo, KestrelShot(sightAngle, lat, az)), 2000).DropFlat.In(DistanceUnit.Meter);

            // Horizontal: N right (negative), S left (positive), equator ~ zero.
            Windage(45, 0).Should().BeLessThan(0);
            Windage(-45, 0).Should().BeGreaterThan(0);
            Windage(0, 0).Should().BeApproximately(0, 1e-9);
            // Hemisphere flips horizontal sign symmetrically.
            Windage(45, 0).Should().BeApproximately(-Windage(-45, 0), 1e-9);

            // Vertical: East lifts (less drop → less negative), West lowers (more negative).
            double dropEast = Drop(45, 90);
            double dropWest = Drop(45, 270);
            double dropNorth = Drop(45, 0);
            dropEast.Should().BeGreaterThan(dropNorth);
            dropWest.Should().BeLessThan(dropNorth);
            // Eötvös is hemisphere-symmetric (cos φ is even): lat −45 East == lat +45 East.
            Drop(-45, 90).Should().BeApproximately(dropEast, 1e-9);
            // Pole has no Eötvös (cos 90 = 0).
            Drop(90, 90).Should().BeApproximately(dropNorth, 1e-9);
        }
    }
}
