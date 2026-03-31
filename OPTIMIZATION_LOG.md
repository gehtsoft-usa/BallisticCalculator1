# Optimization Log for TrajectoryCalculator1.Calculate

Baseline: **29.56 ms** (g1_wind, 500 iterations, TrajectoryCalculator)

| Iter | Change | Result (ms) | Δ% | Keep/Discard |
|------|--------|-------------|-----|-------------|
| 1 | Convert hot loop from Measurement<T>/Vector<T> to raw double arithmetic (velocity in native unit, position in meters). Altitude tracked as Measurement to preserve unit-conversion FP behavior. TravelTime calls kept for TimeSpan precision matching. | 7.59 | -74.3% | **KEEP** |
| 2 | Inline TravelTime calls: replace 2 Measurement struct creations + .In() conversions + BallisticMath.TravelTime per iteration with direct `calcStepMeters / (vx / mpsToVel)` and `drMag / (velocityMag / mpsToVel)` wrapped in TimeSpan. | 4.36 | -42.6% | **KEEP** |
| 3 | Replace `MeasurementMath.Abs(lastAtAltitude - alt) > altDelta` with raw double `Math.Abs(altMeters - lastAtAltMeters) > 1.0`. Shadow `altMeters` tracks altitude in meters alongside the Measurement `alt`. Eliminates Measurement subtraction, Abs, and epsilon comparison (~6 transcendental math calls) per iteration. | 1.71 | -60.8% | **KEEP** |
| 4 | Remove TimeSpan round-trip from `dt`: compute as raw double instead of `TimeSpan.FromSeconds(...).TotalSeconds`. | FAIL | — | **DISCARD** (TimeSpan tick truncation is load-bearing for matching original physics) |
| 5 | Defer `alt += Measurement(dry, Meter)` to only when atmosphere refresh triggers, accumulating raw `dryAccum` between refreshes. | FAIL | — | **DISCARD** (batching changes FP accumulation pattern of meter→feet conversions) |
| 6 | Hoist `velocityMag = Math.Sqrt(...)` out of loop top — reuse value computed at bottom of previous iteration. Eliminates 1 `Math.Sqrt` + 3 multiplications + 2 additions per iteration. | 1.65 | -3.5% | **KEEP** |
| 7 | Replace `drMag/velMag` with algebraic simplification `dt` to eliminate `Math.Sqrt` for drMag. | FAIL | — | **DISCARD** (algebraically equal but FP paths differ, changing TimeSpan ticks) |
| 8 | Replace `alt += Measurement(dry, Meter)` with raw double `altValue += dry * meterToAltUnit`. Pre-compute `meterToAltUnit` conversion factor once. Reconstruct Measurement only for `AtAltitude` calls. Eliminates Measurement struct creation + operator+ with `.In()` conversion per iteration. | 0.80 | -51.5% | **KEEP** |
| 9 | Replace `TimeSpan.FromSeconds(...).TotalSeconds` with manual `(long)(x * 1e7) * 1e-7` tick truncation. | FAIL | — | **DISCARD** (.NET `FromSeconds` has rounding behavior that simple truncation doesn't match) |
