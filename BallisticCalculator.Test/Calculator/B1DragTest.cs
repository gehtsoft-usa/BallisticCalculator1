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
                SightAngle = cal.SightAngle(template.Ammunition, template.Rifle, template.Atmosphere, table),
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
                    SightAngle = cal.SightAngle(control.Ammunition, control.Rifle, control.Atmosphere),
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
