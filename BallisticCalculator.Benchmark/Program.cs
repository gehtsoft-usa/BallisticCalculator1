using System.Diagnostics;
using Gehtsoft.Measurements;
using BallisticCalculator;
using BallisticCalculator.Benchmark;

const double TOLERANCE = 1e-7;
const int WARMUP_ITERATIONS = 20;
const int BENCHMARK_ITERATIONS = 500;
const string BENCHMARK_CASE = "g1_wind";

string[] testCases = { "g1_nowind", "g1_nowind_up", "g1_twist", "g7_nowind", "g1_wind", "g1_wind_hot", "g1_wind_cold" };

// Phase 1: Pre-compute inputs (SightAngle + template data) for each test case
Console.WriteLine("=== Phase 1: Preparing test inputs ===");
var inputs = new Dictionary<string, TestInput>();

foreach (var name in testCases)
{
    var template = TableLoader.FromResource(name);
    var cal = new TrajectoryCalculator();

    var shot = new ShotParameters()
    {
        Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
        MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
        SightAngle = cal.SightAngle(template.Ammunition, template.Rifle, template.Atmosphere),
        ShotAngle = template.ShotParameters?.ShotAngle,
        CantAngle = template.ShotParameters?.CantAngle,
    };

    var winds = template.Wind == null ? null : new Wind[] { template.Wind };

    inputs[name] = new TestInput(template.Ammunition, template.Rifle, template.Atmosphere, shot, winds);
    Console.WriteLine($"  {name}: input prepared");
}

// Phase 2: Capture baseline results using original TrajectoryCalculator
Console.WriteLine();
Console.WriteLine("=== Phase 2: Capturing baseline results (TrajectoryCalculator) ===");
var baselines = new Dictionary<string, TrajectoryPoint[]>();

foreach (var name in testCases)
{
    var inp = inputs[name];
    var result = new TrajectoryCalculator().Calculate(inp.Ammunition, inp.Rifle, inp.Atmosphere, inp.Shot, inp.Winds);
    baselines[name] = result;
    Console.WriteLine($"  {name}: {result.Length} points captured");
}

// Phase 3: Validate TrajectoryCalculator1 against baseline (all test cases, 1 iteration)
Console.WriteLine();
Console.WriteLine($"=== Phase 3: Validating TrajectoryCalculator1 vs baseline (tolerance = {TOLERANCE}) ===");
bool allPassed = true;

foreach (var name in testCases)
{
    var inp = inputs[name];
    var result = new TrajectoryCalculator1().Calculate(inp.Ammunition, inp.Rifle, inp.Atmosphere, inp.Shot, inp.Winds);
    bool passed = ValidateAgainstBaseline(name, baselines[name], result);
    if (!passed) allPassed = false;
}

if (!allPassed)
{
    Console.WriteLine();
    Console.WriteLine("VALIDATION FAILED - TrajectoryCalculator1 results differ beyond tolerance!");
    return 1;
}

Console.WriteLine("  All validations PASSED.");

// Phase 4: Benchmark TrajectoryCalculator1 on g1_wind only
Console.WriteLine();
Console.WriteLine($"=== Phase 4: Benchmark TrajectoryCalculator1 '{BENCHMARK_CASE}' (warmup={WARMUP_ITERATIONS}, iterations={BENCHMARK_ITERATIONS}) ===");

var benchInput = inputs[BENCHMARK_CASE];

// Warmup
for (int i = 0; i < WARMUP_ITERATIONS; i++)
    new TrajectoryCalculator1().Calculate(benchInput.Ammunition, benchInput.Rifle, benchInput.Atmosphere, benchInput.Shot, benchInput.Winds);

// Benchmark
var sw = Stopwatch.StartNew();
for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
    new TrajectoryCalculator1().Calculate(benchInput.Ammunition, benchInput.Rifle, benchInput.Atmosphere, benchInput.Shot, benchInput.Winds);
sw.Stop();

double avgMs = sw.Elapsed.TotalMilliseconds / BENCHMARK_ITERATIONS;
Console.WriteLine($"  {BENCHMARK_CASE,-20} avg: {avgMs:F4} ms ({BENCHMARK_ITERATIONS} iters, total: {sw.Elapsed.TotalMilliseconds:F1} ms)");

// Phase 5: Post-benchmark validation (all cases, 1 iteration)
Console.WriteLine();
Console.WriteLine("=== Phase 5: Post-benchmark validation ===");
allPassed = true;
foreach (var name in testCases)
{
    var inp = inputs[name];
    var result = new TrajectoryCalculator1().Calculate(inp.Ammunition, inp.Rifle, inp.Atmosphere, inp.Shot, inp.Winds);
    bool passed = ValidateAgainstBaseline(name, baselines[name], result);
    if (!passed) allPassed = false;
}

if (allPassed)
    Console.WriteLine("  All post-benchmark validations PASSED.");
else
{
    Console.WriteLine("  POST-BENCHMARK VALIDATION FAILED!");
    return 1;
}

return 0;

// --- Helper methods ---

static bool ValidateAgainstBaseline(string name, TrajectoryPoint[] baseline, TrajectoryPoint[] current)
{
    if (baseline.Length != current.Length)
    {
        Console.WriteLine($"  FAIL {name}: length mismatch ({baseline.Length} vs {current.Length})");
        return false;
    }

    bool passed = true;
    for (int i = 0; i < baseline.Length; i++)
    {
        var b = baseline[i];
        var c = current[i];

        passed &= CheckValue(name, i, "Distance", b.Distance.In(DistanceUnit.Meter), c.Distance.In(DistanceUnit.Meter));
        passed &= CheckValue(name, i, "Velocity", b.Velocity.In(VelocityUnit.MetersPerSecond), c.Velocity.In(VelocityUnit.MetersPerSecond));
        passed &= CheckValue(name, i, "Mach", b.Mach, c.Mach);
        passed &= CheckValue(name, i, "Drop", b.Drop.In(DistanceUnit.Meter), c.Drop.In(DistanceUnit.Meter));
        passed &= CheckValue(name, i, "Windage", b.Windage.In(DistanceUnit.Meter), c.Windage.In(DistanceUnit.Meter));
        passed &= CheckValue(name, i, "Time", b.Time.TotalSeconds, c.Time.TotalSeconds);
        passed &= CheckValue(name, i, "Energy", b.Energy.In(EnergyUnit.FootPound), c.Energy.In(EnergyUnit.FootPound));
        passed &= CheckValue(name, i, "DropAdj", b.DropAdjustment.In(AngularUnit.MOA), c.DropAdjustment.In(AngularUnit.MOA));
        passed &= CheckValue(name, i, "WindAdj", b.WindageAdjustment.In(AngularUnit.MOA), c.WindageAdjustment.In(AngularUnit.MOA));
    }

    if (passed)
        Console.WriteLine($"  PASS {name}: {baseline.Length} points validated");
    return passed;
}

static bool CheckValue(string testName, int pointIndex, string field, double expected, double actual)
{
    double diff = Math.Abs(expected - actual);
    double tolerance = Math.Max(TOLERANCE, Math.Abs(expected) * TOLERANCE);

    if (diff > tolerance)
    {
        Console.WriteLine($"  FAIL {testName}[{pointIndex}].{field}: expected={expected:E15}, actual={actual:E15}, diff={diff:E5}");
        return false;
    }
    return true;
}

record TestInput(Ammunition Ammunition, Rifle Rifle, Atmosphere Atmosphere, ShotParameters Shot, Wind[]? Winds);
