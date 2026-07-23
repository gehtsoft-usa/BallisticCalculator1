using System;
using System.Diagnostics;
using System.Linq;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Debug
{
    /// <summary>
    /// PLAN1 §1.1 accuracy/speed experiment: does the midpoint (RK2) integrator hold the
    /// reference-suite accuracy at a much coarser step than semi-implicit Euler?
    ///
    /// Method: for each config, a fine-Euler run (tiny MaximumCalculationStepSize) is the "truth".
    /// Euler and RK2 are then run at progressively coarser steps and the worst-case per-point
    /// deviation from truth is reported in the output units the shooter reads (MOA / fps / ms),
    /// alongside wall-clock time for many repeats so the speed lever is measured, not asserted.
    /// Zeroing is done up front (once, via CalculateZeroParameters) — this measures the trajectory only.
    /// </summary>
    public static class DebugRK2
    {
        private sealed class Config
        {
            public string Name;
            public Ammunition Ammo;
            public Rifle Rifle;
            public Atmosphere Atmo;
            public Wind[] Wind;
            public double MaxRangeYd;
        }

        private const double TruthMaxStepM = 0.001;    // ~0.11 mm actual step — converged reference (100× finer than default)
        private const int TimingRepeats = 150;

        public static void Do(string[] args)
        {
            var configs = BuildConfigs();

            foreach (var cfg in configs)
            {
                Console.WriteLine();
                Console.WriteLine($"=== {cfg.Name}  (to {cfg.MaxRangeYd:0} yd) ===");

                var cal = new TrajectoryCalculator();
                var sightAngle = cal.CalculateZeroParameters(cfg.Ammo, cfg.Atmo, cfg.Rifle, cfg.Rifle.Zero).ZeroDropAdjustment;

                // Dense (5 yd) + fine truth so candidate points can be compared at their *actual*
                // emitted distance by interpolation — this removes the output-overshoot artifact
                // that otherwise dominates coarse-step error and masks the integrator order.
                var truth = Run(cfg, sightAngle, IntegrationMethod.Euler, TruthMaxStepM, truthStepYd: 5);

                Console.WriteLine($"{"integrator",-10} {"maxStep",8} {"step~",9} {"steps/kyd",9}  " +
                    $"{"dDrop",9} {"dWind",9} {"dVel",8} {"dTOF",8}  {"time",8}");
                Console.WriteLine($"{"",-10} {"(m)",8} {"(cm)",9} {"",9}  " +
                    $"{"(MOA)",9} {"(MOA)",9} {"(fps)",8} {"(ms)",8}  {"(ms/150)",8}");

                foreach (var (method, maxStepM) in Variants())
                {
                    var traj = Run(cfg, sightAngle, method, maxStepM);
                    double ms = Time(cfg, sightAngle, method, maxStepM);
                    var (dDrop, dWind, dVel, dTof) = MaxError(traj, truth);
                    double stepCm = ActualStepMeters(cfg, maxStepM) * 100.0;
                    double stepsPerKyd = 914.4 / ActualStepMeters(cfg, maxStepM);

                    Console.WriteLine(
                        $"{method,-10} {maxStepM,8:0.###} {stepCm,9:0.000} {stepsPerKyd,9:0} " +
                        $" {dDrop,9:0.0000} {dWind,9:0.0000} {dVel,8:0.00} {dTof,8:0.00}  {ms,8:0.0}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("dDrop/dWind = worst per-point |Δ| vs fine-Euler truth, in MOA; " +
                              "dVel fps; dTOF ms. time = " + TimingRepeats + " repeats.");
        }

        // GetCalculationStep quantizes the actual step by powers of ten, so maxStep must cross a
        // decade to change it. This ladder gives a clean 10× step sequence (≈1.1mm, 1.1cm, 11cm,
        // 1.1m) to expose the convergence order of each integrator.
        private static (IntegrationMethod, double)[] Variants() => new[]
        {
            (IntegrationMethod.Euler,       0.01),  // ~1.1 mm
            (IntegrationMethod.Euler,       0.1),   // ~1.1 cm  (production default)
            (IntegrationMethod.Euler,       1.0),   // ~11 cm
            (IntegrationMethod.Euler,       10.0),  // ~1.1 m
            (IntegrationMethod.MidpointRK2, 0.01),  // ~1.1 mm
            (IntegrationMethod.MidpointRK2, 0.1),   // ~1.1 cm
            (IntegrationMethod.MidpointRK2, 1.0),   // ~11 cm   (the candidate — 10× coarser than default)
            (IntegrationMethod.MidpointRK2, 10.0),  // ~1.1 m   (100× coarser)
        };

        private const double CandidateStepYd = 25;

        private static (TrajectoryCalculator cal, ShotParameters shot) Setup(Config cfg,
            Measurement<AngularUnit> sightAngle, IntegrationMethod method, double maxStepM, double stepYd)
        {
            var cal = new TrajectoryCalculator
            {
                Integrator = method,
                MaximumCalculationStepSize = new Measurement<DistanceUnit>(maxStepM, DistanceUnit.Meter),
            };
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(stepYd, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(cfg.MaxRangeYd, DistanceUnit.Yard),
                ZeroDropAdjustment = sightAngle,
            };
            return (cal, shot);
        }

        private static TrajectoryPoint[] Run(Config cfg, Measurement<AngularUnit> sightAngle,
            IntegrationMethod method, double maxStepM, double truthStepYd = CandidateStepYd)
        {
            var (cal, shot) = Setup(cfg, sightAngle, method, maxStepM, truthStepYd);
            return cal.Calculate(cfg.Ammo, cfg.Rifle, cfg.Atmo, shot, cfg.Wind);
        }

        private static double Time(Config cfg, Measurement<AngularUnit> sightAngle,
            IntegrationMethod method, double maxStepM)
        {
            var (cal, shot) = Setup(cfg, sightAngle, method, maxStepM, CandidateStepYd);
            cal.Calculate(cfg.Ammo, cfg.Rifle, cfg.Atmo, shot, cfg.Wind);   // warm up / JIT
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimingRepeats; i++)
                cal.Calculate(cfg.Ammo, cfg.Rifle, cfg.Atmo, shot, cfg.Wind);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        // Worst-case deviation vs truth, comparing each candidate point to the truth trajectory
        // linearly interpolated at the candidate's *actual* emitted distance (removes overshoot).
        private static (double dDrop, double dWind, double dVel, double dTof) MaxError(
            TrajectoryPoint[] a, TrajectoryPoint[] truth)
        {
            var t = truth.Where(p => p != null).ToArray();
            double dDrop = 0, dWind = 0, dVel = 0, dTof = 0;
            foreach (var p in a)
            {
                if (p == null)
                    continue;
                double d = p.Distance.In(DistanceUnit.Meter);
                dDrop = Math.Max(dDrop, Math.Abs(p.DropAdjustment.In(AngularUnit.MOA) - Interp(t, d, q => q.DropAdjustment.In(AngularUnit.MOA))));
                dWind = Math.Max(dWind, Math.Abs(p.WindageAdjustment.In(AngularUnit.MOA) - Interp(t, d, q => q.WindageAdjustment.In(AngularUnit.MOA))));
                dVel = Math.Max(dVel, Math.Abs(p.Velocity.In(VelocityUnit.FeetPerSecond) - Interp(t, d, q => q.Velocity.In(VelocityUnit.FeetPerSecond))));
                dTof = Math.Max(dTof, Math.Abs(p.Time.TotalMilliseconds - Interp(t, d, q => q.Time.TotalMilliseconds)));
            }
            return (dDrop, dWind, dVel, dTof);
        }

        // Linear interpolation of a selected field over the (dense) truth trajectory at distance d (m).
        private static double Interp(TrajectoryPoint[] t, double d, Func<TrajectoryPoint, double> sel)
        {
            for (int i = 1; i < t.Length; i++)
            {
                double d1 = t[i].Distance.In(DistanceUnit.Meter);
                if (d1 >= d || i == t.Length - 1)
                {
                    double d0 = t[i - 1].Distance.In(DistanceUnit.Meter);
                    double f0 = sel(t[i - 1]), f1 = sel(t[i]);
                    double frac = d1 > d0 ? (d - d0) / (d1 - d0) : 0;
                    return f0 + frac * (f1 - f0);
                }
            }
            return sel(t[t.Length - 1]);
        }

        // Replicates TrajectoryCalculator.GetCalculationStep so the harness can report the actual
        // integration step used by the candidate runs (output Step = CandidateStepYd).
        private static double ActualStepMeters(Config cfg, double maxStepM)
        {
            double s = new Measurement<DistanceUnit>(CandidateStepYd, DistanceUnit.Yard).In(DistanceUnit.Meter) / 2.0;
            if (s > maxStepM)
            {
                int so = (int)Math.Floor(Math.Log10(s));
                int mo = (int)Math.Floor(Math.Log10(maxStepM));
                s /= Math.Pow(10, so - mo + 1);
            }
            return s;
        }

        private static Config[] BuildConfigs()
        {
            var stdAtmo = new Atmosphere();

            var m308G7 = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond),
                bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(1.210, DistanceUnit.Inch));
            var r308 = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null),
                rifling: new Rifling(new Measurement<DistanceUnit>(11, DistanceUnit.Inch), TwistDirection.Right));

            var m338G7 = new Ammunition(
                weight: new Measurement<WeightUnit>(250, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.320, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(2960, VelocityUnit.FeetPerSecond),
                bulletDiameter: new Measurement<DistanceUnit>(0.338, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(1.700, DistanceUnit.Inch));
            var r338 = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(2.0, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null),
                rifling: new Rifling(new Measurement<DistanceUnit>(9.3, DistanceUnit.Inch), TwistDirection.Right));

            var m22 = new Ammunition(
                weight: new Measurement<WeightUnit>(40, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.132, DragTableId.RA4),
                muzzleVelocity: new Measurement<VelocityUnit>(1080, VelocityUnit.FeetPerSecond),
                bulletDiameter: new Measurement<DistanceUnit>(0.223, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(0.400, DistanceUnit.Inch));
            var r22 = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(50, DistanceUnit.Yard), null, null),
                rifling: new Rifling(new Measurement<DistanceUnit>(16, DistanceUnit.Inch), TwistDirection.Right));

            var m308G1 = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.450, DragTableId.G1),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond),
                bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(1.210, DistanceUnit.Inch));
            var wind10 = new[] { new Wind(new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour), new Measurement<AngularUnit>(90, AngularUnit.Degree)) };

            return new[]
            {
                new Config { Name = ".308 168gr G7 @2700, no wind", Ammo = m308G7, Rifle = r308, Atmo = stdAtmo, Wind = null, MaxRangeYd = 1000 },
                new Config { Name = ".338 250gr G7 @2960, no wind", Ammo = m338G7, Rifle = r338, Atmo = stdAtmo, Wind = null, MaxRangeYd = 1500 },
                new Config { Name = ".22LR 40gr RA4 @1080 (subsonic)", Ammo = m22, Rifle = r22, Atmo = stdAtmo, Wind = null, MaxRangeYd = 300 },
                new Config { Name = ".308 168gr G1 @2700, 10mph @90deg", Ammo = m308G1, Rifle = r308, Atmo = stdAtmo, Wind = wind10, MaxRangeYd = 1000 },
            };
        }
    }
}
