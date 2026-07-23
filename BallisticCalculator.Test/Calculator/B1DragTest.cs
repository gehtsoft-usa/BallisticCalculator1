using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AwesomeAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    // b1: a per-bullet effective ballistic-coefficient-vs-Mach curve (embedded bc_*.txt),
    // synthesized into a custom drag table via DrgDragTableFactory, reproduces the Hornady
    // 4DOF reference (embedded b1_*.txt) far better than the published box BC.
    //
    // Each row: trajectory resource, bc-curve resource, velocity tol (relative),
    // drop tol (MOA), windage tol (MOA), assert drop?, and the minimum drop error (MOA) the
    // *published-BC* control run must exceed (guards that b1 closes a real gap; 0 = skip).
    public class B1DragTest
    {
        [Theory]
        [InlineData("b1_eldx",       "bc_eldx", 0.003, 0.10, 0.05, true,  0.5)]
        [InlineData("b1_eldm",       "bc_eldm", 0.003, 0.10, 0.05, true,  1.0)]
        [InlineData("b1_eldm_10kft", "bc_eldm", 0.010, 0.10, 0.05, true,  0.5)]
        [InlineData("b1_bthp",       "bc_bthp", 0.030, 1.50, 0.05, true,  3.0)]
        [InlineData("b1_bthp_hot",   "bc_bthp", 0.030, 1.20, 0.05, true,  3.0)]
        [InlineData("b1_bthp_3200",  "bc_bthp", 0.035, 1.20, 0.05, true,  3.0)]
        [InlineData("b1_bthp_7kft",  "bc_bthp", 0.030, 1.50, 0.05, true,  3.0)]
        // wind case: drop carries 4DOF's vertical wind-jump (not modeled) — assert velocity + windage only.
        [InlineData("b1_eldx_wind",  "bc_eldx", 0.003, 0.0,  0.10, false, 0.0)]
        public void SynthesizedCurveMatches4Dof(string trajRes, string bcRes,
            double velTol, double dropTolMOA, double windTolMOA, bool assertDrop, double minPublishedDropMOA)
        {
            var (baseTable, knots) = LoadBcCurve(bcRes);
            var template = LoadTrajectory(trajRes);
            var cal = new TrajectoryCalculator();
            double maxYd = template.Trajectory[template.Trajectory.Count - 1].Distance.In(DistanceUnit.Yard);
            var winds = template.Wind == null ? null : new[] { template.Wind };

            // synthesized-drg run: swap the published BC for the custom (GC) curve
            template.Ammunition.BallisticCoefficient = new BallisticCoefficient(1.0, DragTableId.GC);
            var entry = new AmmunitionLibraryEntry { Name = trajRes, Ammunition = template.Ammunition };
            var table = DrgDragTableFactory.Build(entry, baseTable, knots);

            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(maxYd, DistanceUnit.Yard),
                ZeroDropAdjustment = cal.CalculateZeroParameters(template.Ammunition, template.Atmosphere, template.Rifle, template.Rifle.Zero, dragTable: table).ZeroDropAdjustment,
            };
            var traj = cal.Calculate(template.Ammunition, template.Rifle, template.Atmosphere, shot, winds, table);

            traj.Length.Should().Be(template.Trajectory.Count);
            for (int i = 0; i < traj.Length; i++)
            {
                var p = traj[i];
                var r = template.Trajectory[i];
                double yd = r.Distance.In(DistanceUnit.Yard);
                double refV = r.Velocity.In(r.Velocity.Unit);
                p.Velocity.In(r.Velocity.Unit).Should().BeApproximately(refV, refV * velTol, $"vel@{yd:N0}");
                if (yd <= 0)
                    continue;
                if (assertDrop)
                    p.Drop.In(DistanceUnit.Inch).Should().BeApproximately(r.Drop.In(DistanceUnit.Inch), InchTol(dropTolMOA, yd), $"drop@{yd:N0}");
                p.Windage.In(DistanceUnit.Inch).Should().BeApproximately(r.Windage.In(DistanceUnit.Inch), InchTol(windTolMOA, yd), $"wind@{yd:N0}");
            }

            // control: the published-BC (plain 3DOF) run must be materially worse than the
            // synthesized curve — proves b1 closes a real drag-data gap, not a trivial pass.
            if (minPublishedDropMOA > 0)
            {
                var control = LoadTrajectory(trajRes);   // fresh load keeps the published BC
                var cshot = new ShotParameters
                {
                    Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                    MaximumDistance = new Measurement<DistanceUnit>(maxYd, DistanceUnit.Yard),
                    ZeroDropAdjustment = cal.CalculateZeroParameters(control.Ammunition, control.Atmosphere, control.Rifle, control.Rifle.Zero).ZeroDropAdjustment,
                };
                var ctrl = cal.Calculate(control.Ammunition, control.Rifle, control.Atmosphere, cshot, winds);
                double maxCtrlDropMOA = 0;
                for (int i = 0; i < ctrl.Length; i++)
                {
                    double yd = control.Trajectory[i].Distance.In(DistanceUnit.Yard);
                    if (yd <= 0) continue;
                    double err = Math.Abs(ctrl[i].Drop.In(DistanceUnit.Inch) - control.Trajectory[i].Drop.In(DistanceUnit.Inch));
                    maxCtrlDropMOA = Math.Max(maxCtrlDropMOA, MoaOf(err, yd));
                }
                maxCtrlDropMOA.Should().BeGreaterThan(minPublishedDropMOA,
                    "the published-BC 3DOF run should be materially worse than the synthesized curve");
            }
        }

        // Aerodynamic (crosswind) jump — Litz Applied Ballistics Eq 5.4 (CLAUDE/AERO_JUMP.md).
        // Our original hypothesis for the b1_eldx_wind drop gap was exactly this: 4DOF's "drop"
        // carries a vertical crosswind jump we didn't model. This proves it closes the gap.
        // Config is data-driven: b1_eldx_wind_twist is b1_eldx_wind + 1:7" twist and bullet dims
        // (so Sg — hence the jump — can be computed); b1_eldx_wind (no twist/dims) is the jump-off
        // baseline. Both run on the synthesized (b1) drag curve so the *drag* baseline already
        // matches 4DOF; the only remaining drop gap is the jump.
        [Fact]
        public void AerodynamicJump_ClosesHornadyWindDropGap()
        {
            var reference = LoadTrajectory("b1_eldx_wind_twist");            // Hornady 4DOF drop (with jump)
            var withJump  = RunSynth(LoadTrajectory("b1_eldx_wind_twist"));  // twist+dims ⇒ jump modelled
            var noJump    = RunSynth(LoadTrajectory("b1_eldx_wind"));        // no dims ⇒ jump absent (as before)

            double maxErrWith = 0, maxErrNo = 0, jumpMoaMin = double.MaxValue, jumpMoaMax = 0;
            for (int i = 0; i < withJump.Length; i++)
            {
                double yd = reference.Trajectory[i].Distance.In(DistanceUnit.Yard);
                if (yd <= 0) continue;
                double refDrop = reference.Trajectory[i].Drop.In(DistanceUnit.Inch);
                maxErrWith = Math.Max(maxErrWith, MoaOf(Math.Abs(withJump[i].Drop.In(DistanceUnit.Inch) - refDrop), yd));
                maxErrNo   = Math.Max(maxErrNo,   MoaOf(Math.Abs(noJump[i].Drop.In(DistanceUnit.Inch)   - refDrop), yd));
                double jumpMoa = MoaOf(withJump[i].Drop.In(DistanceUnit.Inch) - noJump[i].Drop.In(DistanceUnit.Inch), yd);
                jumpMoaMin = Math.Min(jumpMoaMin, jumpMoa);
                jumpMoaMax = Math.Max(jumpMoaMax, jumpMoa);
            }

            // The unmodeled jump is a real, large drop gap vs 4DOF...
            maxErrNo.Should().BeGreaterThan(0.6, "the unmodeled crosswind jump is a real ~0.8 MOA drop gap");
            // ...that aerodynamic jump largely closes. Residual is the Litz-Eq5.4-vs-4DOF model
            // difference (our Miller Sg drives Eq 5.4 ~16% above 4DOF's effective jump here).
            maxErrWith.Should().BeLessThan(0.25, "aerodynamic jump closes most of the drop gap");
            maxErrWith.Should().BeLessThan(maxErrNo * 0.4, "with-jump is materially better than without");

            // The jump we apply is a constant angle at all ranges (defining property) and matches
            // Litz Eq 5.4 for this bullet/crosswind (Sg from the same Miller calc the engine uses).
            double sg = MillerSg(220, 0.308, 7, 1.630, 2600, 29.92, 59);
            double expectJumpMoa = (0.01 * sg - 0.0024 * (1.630 / 0.308) + 0.032) * 20.0;   // Eq 5.4 × 20 mph
            jumpMoaMin.Should().BeApproximately(jumpMoaMax, 0.005, "jump is range-independent (constant angle)");
            jumpMoaMax.Should().BeApproximately(expectJumpMoa, 0.02, "jump magnitude matches Litz Eq 5.4");
        }

        // Structural guards: linearity in crosswind, sign by wind side and twist direction, zero
        // for a pure head/tailwind. Bullet/rifle config comes from the b1_eldx_wind_twist template;
        // only the scenario wind (and, for one case, the twist direction under test) is varied.
        [Fact]
        public void AerodynamicJump_StructuralProperties()
        {
            double JumpMoaAt1000(double windMph, double dirDeg, TwistDirection? twist = null)
            {
                var winds = new[] { new Wind(VelocityUnit.MilesPerHour.New(windMph), new Measurement<AngularUnit>(dirDeg, AngularUnit.Degree)) };
                var on  = RunSynth(LoadTrajectory("b1_eldx_wind_twist"), winds, twist);   // jump on (has dims/twist)
                var off = RunSynth(LoadTrajectory("b1_eldx_wind"), winds);                // jump off (no dims)
                int i = Array.FindIndex(on, p => Math.Abs(p.Distance.In(DistanceUnit.Yard) - 1000) < 1);
                return MoaOf(on[i].Drop.In(DistanceUnit.Inch) - off[i].Drop.In(DistanceUnit.Inch), 1000);
            }

            double right20 = JumpMoaAt1000(20, 90);                          // wind from right, template's right twist
            double right10 = JumpMoaAt1000(10, 90);
            double left20  = JumpMoaAt1000(20, 270);                         // wind from left
            double leftTwist20 = JumpMoaAt1000(20, 90, TwistDirection.Left); // flip twist under test
            double head20  = JumpMoaAt1000(20, 0);                           // pure tail/headwind axis

            right20.Should().BeGreaterThan(0, "right twist + wind from right ⇒ jump up (Drop less negative)");
            right10.Should().BeApproximately(right20 / 2, 0.01, "jump is linear in crosswind speed");
            left20.Should().BeApproximately(-right20, 0.01, "wind from the left flips the vertical sign");
            leftTwist20.Should().BeApproximately(-right20, 0.01, "left-hand twist flips the vertical sign");
            head20.Should().BeApproximately(0, 1e-6, "a pure head/tailwind has no crosswind component ⇒ no jump");
        }

        // Aerodynamic jump is a muzzle transient, so it must be driven by the wind zone at the
        // muzzle (the first zone) and be unaffected by downrange zones.
        [Fact]
        public void AerodynamicJump_UsesMuzzleWindZone()
        {
            double JumpAt1000(Wind[] winds)
            {
                var on  = RunSynth(LoadTrajectory("b1_eldx_wind_twist"), winds);
                var off = RunSynth(LoadTrajectory("b1_eldx_wind"), winds);
                int i = Array.FindIndex(on, p => Math.Abs(p.Distance.In(DistanceUnit.Yard) - 1000) < 1);
                return MoaOf(on[i].Drop.In(DistanceUnit.Inch) - off[i].Drop.In(DistanceUnit.Inch), 1000);
            }
            var deg90 = new Measurement<AngularUnit>(90, AngularUnit.Degree);
            var to200 = new Measurement<DistanceUnit>(200, DistanceUnit.Yard);
            Wind W(double mph, Measurement<DistanceUnit>? max = null) =>
                new Wind(VelocityUnit.MilesPerHour.New(mph), deg90, max);

            double single      = JumpAt1000(new[] { W(20) });
            double muzzleWindy = JumpAt1000(new[] { W(20, to200), W(0) });   // windy to 200 yd, calm after
            double muzzleCalm  = JumpAt1000(new[] { W(0, to200),  W(20) });  // calm to 200 yd, windy after

            single.Should().BeGreaterThan(0.5, "sanity: the muzzle-windy jump is a real, sizable value");
            muzzleWindy.Should().BeApproximately(single, 0.005,
                "jump is set by the muzzle wind zone; a downrange zone must not change it");
            muzzleCalm.Should().BeApproximately(0, 1e-6,
                "no crosswind at the muzzle ⇒ no aerodynamic jump, even if a later zone is windy");
        }

        // Miller stability with Litz's velocity + temp/pressure corrections (mirrors the engine's
        // CalculateStabilityCoefficient) — used to predict Eq 5.4 in the test above.
        private static double MillerSg(double grains, double diaIn, double twistIn, double lenIn, double mvFps, double presInHg, double tempF)
        {
            double t = twistIn / diaIn, l = lenIn / diaIn;
            double sd = 30 * grains / (t * t * diaIn * diaIn * diaIn * l * (1 + l * l));
            double fv = Math.Pow(mvFps / 2800.0, 1.0 / 3.0);
            double ftp = ((tempF + 460) / (59 + 460)) * (29.92 / presInHg);
            return sd * fv * ftp;
        }

        // Run a template on the synthesized (GC) b1 drag curve. Bullet/rifle/atmosphere all come
        // from the template; `winds` overrides the scenario wind (defaults to the template's own);
        // `twistOverride` flips only the twist direction (reusing the template's step) for the
        // sign-under-test case.
        private static TrajectoryPoint[] RunSynth(TableLoader template, Wind[] winds = null, TwistDirection? twistOverride = null)
        {
            var cal = new TrajectoryCalculator();
            var (baseTable, knots) = LoadBcCurve("bc_eldx");
            double maxYd = template.Trajectory[template.Trajectory.Count - 1].Distance.In(DistanceUnit.Yard);

            template.Ammunition.BallisticCoefficient = new BallisticCoefficient(1.0, DragTableId.GC);
            Rifle rifle = twistOverride == null
                ? template.Rifle
                : new Rifle(template.Rifle.Sight, template.Rifle.Zero,
                    new Rifling(template.Rifle.Rifling.RiflingStep, twistOverride.Value));

            var table = DrgDragTableFactory.Build(
                new AmmunitionLibraryEntry { Name = "eldx", Ammunition = template.Ammunition }, baseTable, knots);
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(maxYd, DistanceUnit.Yard),
                ZeroDropAdjustment = cal.CalculateZeroParameters(template.Ammunition, template.Atmosphere, rifle, rifle.Zero, dragTable: table).ZeroDropAdjustment,
            };
            winds ??= template.Wind == null ? null : new[] { template.Wind };
            return cal.Calculate(template.Ammunition, rifle, template.Atmosphere, shot, winds, table);
        }

        private static double InchTol(double moa, double yards)
        {
            double t = Measurement<AngularUnit>.Convert(moa, AngularUnit.MOA, AngularUnit.InchesPer100Yards) * yards / 100.0;
            return t < 0.001 ? 0.001 : t;
        }

        private static double MoaOf(double inches, double yards) =>
            Measurement<AngularUnit>.Convert(inches / (yards / 100.0), AngularUnit.InchesPer100Yards, AngularUnit.MOA);

        private static TableLoader LoadTrajectory(string name)
        {
            using var s = typeof(B1DragTest).Assembly.GetManifestResourceStream($"BallisticCalculator.Test.resources.{name}.txt");
            return new TableLoader(s);
        }

        // bc_*.txt format: comment lines (#) ignored, first real line = base DragTableId,
        // remaining lines = "mach;bc".
        private static (DragTableId baseTable, BcAtMach[] knots) LoadBcCurve(string name)
        {
            using var s = typeof(B1DragTest).Assembly.GetManifestResourceStream($"BallisticCalculator.Test.resources.{name}.txt");
            using var r = new StreamReader(s);
            DragTableId? baseId = null;
            var knots = new List<BcAtMach>();
            string line;
            while ((line = r.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                if (baseId == null)
                {
                    baseId = (DragTableId)Enum.Parse(typeof(DragTableId), line);
                    continue;
                }
                var parts = line.Split(';');
                knots.Add(new BcAtMach(
                    double.Parse(parts[0], CultureInfo.InvariantCulture),
                    double.Parse(parts[1], CultureInfo.InvariantCulture)));
            }
            return (baseId.Value, knots.ToArray());
        }
    }
}
