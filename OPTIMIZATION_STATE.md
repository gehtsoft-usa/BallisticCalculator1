# Optimization State for TrajectoryCalculator1.Calculate

## Goal
Maximize performance of `TrajectoryCalculator.Calculate` as measured by the benchmark, without breaking correctness (1e-7 tolerance vs original).

## Current Performance
- **Baseline (TrajectoryCalculator):** 29.56 ms
- **Current (TrajectoryCalculator1):** 0.80 ms
- **Speedup:** 37x (97.3% reduction)
- **Benchmark:** g1_wind test case, 500 iterations, Release build

## How to Run

```bash
# Build and run benchmark (from repo root)
dotnet run --project BallisticCalculator.Benchmark/BallisticCalculator.Benchmark.csproj -c Release

# Run existing tests (should still pass)
dotnet test BallisticCalculator.Test/BallisticCalculator.Test.csproj
```

The benchmark:
1. Pre-computes inputs (SightAngle + template data) for all 7 test cases
2. Captures baseline from **TrajectoryCalculator** (original)
3. Validates **TrajectoryCalculator1** (optimized) against baseline at 1e-7 tolerance — all 7 test cases, 1 iteration each
4. Benchmarks TrajectoryCalculator1 on g1_wind only (500 iterations, 20 warmup)
5. Post-benchmark re-validation

## Files
| File | Purpose |
|------|---------|
| `BallisticCalculator/Calculations/TrajectoryCalculator.cs` | Original (baseline, DO NOT MODIFY) |
| `BallisticCalculator/Calculations/TrajectoryCalculator1.cs` | Optimized copy (subject of optimization) |
| `BallisticCalculator.Benchmark/Program.cs` | Benchmark harness (DO NOT MODIFY) |
| `BallisticCalculator.Benchmark/TableLoader.cs` | Test data loader (copy from test project) |
| `BallisticCalculator.Benchmark/resources/*.txt` | Test data files |
| `OPTIMIZATION_LOG.md` | Iteration-by-iteration log of every experiment |
| `OPTIMIZATION_STATE.md` | This file |

## Optimization Rules
1. ONE change per iteration. Never combine multiple changes.
2. After every edit, compile and run the benchmark.
3. If correctness fails (any mismatch), revert immediately.
4. If the metric improved by >= 2%, KEEP the change. Otherwise DISCARD and revert.
5. Log every iteration to `OPTIMIZATION_LOG.md`.

## What Was Optimized (the approach)
The original `Calculate` method uses `Measurement<T>` and `Vector<T>` types throughout its hot loop (~40,000 iterations for a 1000-yard trajectory). These provide unit safety but have enormous overhead:

- **`Measurement.CompareTo`** (used by `<`, `>`, `<=`, `>=`): converts both values to base unit, then calls `Math.Pow`, `Math.Round`, `Math.Log10` **twice** for epsilon computation. That's 6 transcendental math calls per comparison.
- **`Measurement.operator+/-`**: calls `.In()` which goes through `Convert()` → `ToBase()` → `FromBase()` with enum boxing on netstandard2.0.
- **`Vector<T>` operations**: create new structs with 3 Measurement fields each, tripling the overhead.
- **`BallisticMath.TravelTime`**: creates Measurement structs, calls `.In()` twice, wraps in TimeSpan.

The optimization replaces all hot-loop `Measurement<T>`/`Vector<T>` operations with raw `double` arithmetic using pre-computed conversion factors, while preserving the exact FP computation order where required for accuracy.

## Key Constraints Discovered

### FP-Sensitive Paths (DO NOT CHANGE)
These computations are load-bearing for 1e-7 accuracy. Changing them breaks correctness:

1. **`dt` must go through `TimeSpan.FromSeconds(...).TotalSeconds`** — The original truncates dt to TimeSpan tick precision (~100ns). Removing this round-trip causes ~1e-3 errors across all test cases (iter 4). Manual `(long)(x*1e7)*1e-7` also doesn't match .NET's internal rounding (iter 9).

2. **Altitude must accumulate per-step in its original unit (e.g., Feet)** — The original does `alt += Measurement(dry, Meter)` which converts meters→feet each step via the Inch base unit. The per-step FP round-trip error pattern is load-bearing. Batching additions (iter 5) or accumulating in meters causes ~1e-6 time errors in g1_nowind_up (30° uphill shot with large vertical velocity).

3. **`drMag / (velocityMag / mpsToVel)` cannot be simplified to `dt`** — While algebraically equal (`drMag = dt/mpsToVel * velocityMag`), the FP evaluation paths differ, producing different TimeSpan ticks. Causes ~1e-4 errors (iter 7).

4. **Velocity must stay in the muzzle velocity's native unit (e.g., ft/s)** — Converting everything to m/s changes the FP arithmetic sequence for drag/velocity updates, causing ~1e-3 accumulated errors (discovered during iter 1 development).

5. **Division by `mpsToVel` must be used (not multiply by `velToMps`)** — `.In(MPS)` internally does `value / 3.2808399` (division via generated delegate). Multiplying by `1/3.2808399` (the reciprocal) gives different FP results at ULP level, which accumulates to ~1e-6 in the uphill case (discovered during iter 1 development).

### Why g1_nowind_up is the Hardest Test Case
The 30° uphill shot has:
- Large `barrelElevation` angle → significant vy component → large altitude changes per step
- Altitude stored in Feet (from `atmosphere;...;0ft`) → every `dry` (meters) gets converted to feet via Inch base unit
- `distance = rx / cos(30°)` amplifies any position error by 1/0.866
- These compound over ~40,000 iterations making it the most sensitive to FP ordering changes

## Architecture of Current Hot Loop

### Unit Conventions (documented in code header)
```
velocity (vx, vy, vz, wx, wy, wz) — velUnit (muzzle velocity's native unit, e.g. ft/s)
position (rx, ry, rz)              — meters
altitude (altValue)                 — altUnit (atmosphere's native unit, e.g. ft)
altitude (altMeters)                — meters (shadow copy for fast 1m threshold check)
time (dt)                           — seconds, truncated to TimeSpan tick precision
```

### Pre-computed Conversion Factors
| Variable | Value | Usage |
|----------|-------|-------|
| `mpsToVel` | `Convert(1, MPS, velUnit)` e.g. 3.2808399 | Divide velocity by this to get m/s |
| `meterToAltUnit` | `Convert(1, Meter, altUnit)` | Multiply meters by this for altitude accumulation |
| `adjustToFps` | `Convert(1, velUnit, FPS)` | Part of drag factor |
| `inchToMeter` | `Convert(1, Inch, Meter)` | Drift conversion |

### Remaining Measurement<T> Usage in Hot Loop
| What | Frequency | Why it can't be eliminated |
|------|-----------|---------------------------|
| `atmosphere.AtAltitude(Measurement, ...)` | Every ~1m altitude change | API requires Measurement parameter |
| `machMeasurement.In(velUnit)` | Every ~1m altitude change | Converts atmosphere output |
| `TimeSpan.FromSeconds(...)` | 2× per iteration (dt + time) | dt truncation + time accumulation required |
| Output block Measurement constructions | ~21 times total | TrajectoryPoint constructor requires them |

## Ideas for Further Optimization
These haven't been tried yet:

- **`machMeasurement.In(velUnit)` inside atmosphere refresh** — could pre-compute conversion and apply as raw double multiply. Only fires on refresh, so small impact.
- **Reduce `TimeSpan.FromSeconds` calls** — the time accumulation one is hard to eliminate (iter 7 failed), but there may be a way to match .NET's exact rounding behavior with integer math.
- **`Atmosphere.AtAltitude` internals** — if the method could accept raw doubles, it would eliminate the last Measurement construction in the hot path. But this requires modifying the Atmosphere class.
- **Loop unrolling or SIMD** — the 3-component vector math (vx/vy/vz, drx/dry/drz) could potentially benefit from vectorization.
- **Avoid `Math.Sqrt` for `drMag`** — currently needed for time accumulation; algebraic elimination failed (iter 7) but there may be another approach.

## Gehtsoft.Measurements Library Reference
Source code is at `/mnt/d/develop/components/BusinessSpecificComponents/Gehtsoft.Measurements/`. Key facts:
- **Distance base unit:** Inch (not Meter!) — Meter conversion is `Divide 25.4, Multiply 1000` (two operations via Inch)
- **Velocity base unit:** MetersPerSecond — FPS conversion is `Divide 3.2808399`
- **`CompareTo`** uses epsilon: `eps(v) = pow(10, round(log10(v)) - 12)` — very expensive
- **Conversion delegates** are generated via `System.Linq.Expressions` at static init time
- **Enum comparisons** in `Convert()` box on netstandard2.0
