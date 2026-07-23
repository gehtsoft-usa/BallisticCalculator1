# PLAN1 — candidate next round (project review 2026-07-22)

Status review and prioritized suggestions produced after the 2026-07-22 session
(xUnit v3 migration, coverage 74.9%→~88%, RA4/GI/G5/G6 reference tests, RA4 BC-parser
fix, Sonar 26→0). Nothing here is committed-to; this is the menu for the next round.
PLAN0 remains the record of the 3DOF-vs-4DOF review and its completed items.

## Status snapshot (as of commit `ed253d4`)

- Engine validated against four independent references: Hornady 4DOF (synthesized
  drag curves, ~0.05 MOA), Hornady 3DOF (bit-identical), Ballistic Explorer Coriolis
  (~0.4%), and RA4/GI/G5/G6 program output (≤0.19 MOA — see
  `BallisticCalculator.Test/Calculator/StandardDragTableReferenceTest.cs`).
- 295 tests green; Sonar 0 issues; quality gate OK.
- Physics backlog remaining from PLAN0/IDEAS: **b2** (spin-drift recalibration),
  **aero-jump inclined-fire cos projection** (deferred in AERO_JUMP.md),
  **transonic analysis** (IDEAS feature #5, half-shipped), **BC-by-shape** (feature #3, R&D).

## Verified finding: SightAngle is NOT a performance target

Measured 2026-07-22 with temporary instrumentation (counter on the approximation
loop, then reverted). Iterations to the default 0.1 mm accuracy:

| Configuration | Iterations |
|---|---|
| .308 G7 0.45 @2700, 100 yd zero | 3 |
| .308 G1 0.45 @2700, 300 yd zero | 2 |
| .22LR RA4 0.132 @1106, 100 yd zero | 3 |
| .338 G7 0.32 @2960, 1000 yd zero | 3 |
| Subsonic G1 0.15 @1050, 200 yd zero | 3 |

Why: the update rule (`sightAngle += −miss/zeroDistance` via `CmPer100Meters`) is a
Newton iteration whose derivative approximation `d(impact)/d(angle) ≈ zeroDistance`
is nearly exact for any realistic zeroing geometry; each iteration integrates only
*to* the zero distance (breaks at the crossing). Total zeroing cost ≈ 3 short
integrations (~6k Euler steps for a 100 yd zero). The `100` in the loop is a safety
cap, not the behavior. **Do not "optimize" this.**

## 1. Improvements (priority order)

### 1.1 Integrator: midpoint (RK2) + coarser step — THE performance lever
PLAN0 §0 already concluded accuracy is not integration-limited ("performance-
overpaying"): explicit Euler at ~1.1 cm ⇒ ~120k steps per 1000 yd. A midpoint drag
evaluation supports ~10× coarser steps at equal/better accuracy ⇒ ~10× hot-loop
speedup for two extra multiplies per step. Also speeds up `SightAngle` automatically
(shared loop). Previously too risky; now de-risked by the reference suite
(0.02–0.1 MOA resolution across 4 independent sources).

**Prerequisite — extract the duplicated integrator.** `Calculate` and `SightAngle`
carry two copies of the hot loop; every past fix (a5, a6) had to be applied twice.
Extract the shared core FIRST (guarded by the suite), then change the integrator once.

**Add a benchmark harness** (BenchmarkDotNet) before the change so the gain is
measured, not asserted, and perf regressions are caught later.

### 1.2 Thread-safety contract + batch API
`TrajectoryCalculator` appears stateless. Document it as thread-safe, add a test
running trajectories in parallel. Unlocks Monte-Carlo features (see 2.4) cheaply.

### 1.3 Targeting decision (net8.0 vs netstandard2.0)
The 2026-07-22 commit retargeted the library netstandard2.0 → net8.0 (and made 5
`ArgumentNullException.ThrowIfNull` call sites valid). This drops .NET Framework /
older-core consumers from the NuGet package. If unintended, multi-target
`netstandard2.0;net8.0` — only friction is the ThrowIfNull sites (`#if` or polyfill).

### 1.4 Small hygiene
- `ReticleElementsCollectiDon` is the last 0%-covered type (7 lines; one cheap test).
- New public APIs (`Tools/BarrelTwist`, `Tools/BallisticCoefficientConverter`,
  `DrgDragTableFactory`) likely need docgen `doc/` entries.
- README still says ALPHA; with this validation record, consider beta + changelog.

## 2. Feature ideas (same context of use, value-to-effort order)

### 2.1 Transonic analysis (finish IDEAS #5) — days, not weeks
Entry (Mach ≤1.1) / exit (Mach ≤0.9) range+TOF+velocity and a stability verdict,
post-processed from the existing trajectory + existing `GyroscopicStability`.
Answers "where does it go transonic and does it survive?".

### 2.2 Maximum point-blank range / danger space
Given a vital-zone height h, solve the zero keeping the path within ±h/2; report
MPBR and danger-space bands per range. Pure post-processing + one SightAngle-style
solve. Classic hunting/military feature absent from the sister ports.

### 2.3 BC truing (calibration) API
Inverse of `DrgDragTableFactory`, already sketched in PLAN0 b1 ("calibration and
synthesis are the two directions of the same mapping; build as one utility"): given
observed drops/velocities at ranges, fit BC (or BC(M) knots), return corrected
ammo/drag curve. What every practical shooter does at the range.

### 2.4 Hit-probability / WEZ analysis
Monte Carlo over MV SD, wind-call error, group size ⇒ P(hit) on target size vs
range. Needs 1.1 (fast engine) + 1.2 (parallel batch). Flagship feature of
commercial calculators (Applied Ballistics).

### 2.5 Moving-target lead
`lead = atan(targetSpeed × TOF / range)` per output point. An afternoon on existing
outputs.

### 2.6 Density-altitude output + sensitivity
Expose DA on `Atmosphere` (one formula); optionally d(drop)/d(DA) per range —
matches how field shooters bracket conditions.

### 2.7 Spin-drift recalibration (PLAN0 b2)
The remaining physics item: clean ×1.3–1.4 over-prediction vs 4DOF. Yaw-of-repose
model would give cos θ and downrange Sg growth from first principles instead of
Litz's level-fire fit.

## Recommended entry points

- **Improvement track:** 1.1 (loop extraction → RK2 → benchmarks) — the safety net
  for it was built in the 2026-07-22 session.
- **Feature track:** 2.1 (transonic analysis) — smallest gap between "specced" and
  "shipped".
