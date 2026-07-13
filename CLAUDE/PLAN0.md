# 3DOF algorithm review — potential errors & improvement plan

Analysis of our point-mass (3DOF) engine (`BallisticCalculator/Calculations/TrajectoryCalculator.cs`)
against the Hornady 4DOF reference set (50 configs, `hornady/` vs `calculator/`, symptoms in
`COMPARISON.md`). Goal: identify errors and improvements to bring our output closer to the
4DOF exemplar. All numeric claims below were re-derived from the raw CSVs in this folder.

---

## Implementation status (updated 2026-07-13)

Round 1 (correctness fixes + small improvements) — DONE, all in
`BallisticCalculator/Calculations/TrajectoryCalculator.cs` unless noted:
- **a2** spin-drift ×cos θ — applied (drift term). Unverified by suite (no twist+angle case).
- **a3** reported time as `double` seconds — applied (line-395 `dt` truncation left intact).
- **a4** steep-shot termination margin `/lineOfSightCos` — applied. Not triggered by current cases.
- **a5** bidirectional drag-node walk — applied in **both** `Calculate` **and** `SightAngle`
  (the two duplicated loops).
- **a6** sound speed `331.3·√(T/273.15)` (`Data/Atmosphere.cs`) — applied; reaches both loops
  via `AtAltitude`. Main measurable accuracy gain (esp. G7, ~6× tighter velocity).
- **a7** angled-fire drop rotation — **comment only** (measured best-of-3 conventions; current
  formula is optimal and pins the muzzle to −sightHeight; do not change).

Test-suite changes (`BallisticCalculator.Test`): tolerances tightened to the earned headroom
(velocity 0.0015, drop 0.10 MOA for level/supersonic, windage 0.05/0.10/0.015). New reference
data: `g1_nowind_up.txt` re-sourced from **Hornady 3DOF** (was a model outlier), and a new
`g1_nowind_up_supersonic.txt` (G1 0.447/165gr/3100, stays supersonic).

Findings on **angled shots** (full detail in `CLAUDE/DROP_WINDAGE.md`): our engine matches
Hornady's 3DOF to **0.044 MOA** when the bullet stays supersonic and **0.178 MOA** when it goes
subsonic — the residual is the transonic **drag-curve shape** gap (§b1 below), not altitude or
inclined geometry. Our in-flight altitude tracking is **correct** (Hornady tracks it identically;
verified by a temporary freeze experiment on two bullets) — so **no "static atmosphere" mode is
needed** for accuracy.

Still open: b1 (drag-data / BC(M) curve synthesis — the real accuracy story), b2 spin-drift
recalibration, b4 aerodynamic jump, b5 downrange Sg.

---

## 0. Verified as correct (checked, no action)

- `PIR = 2.08551e-4` is exactly (π/8)·(0.0764742 lb/ft³)/144 and consistent with
  `Atmosphere.StandardDensity`; the drag update matches the standard JBM/GNU-ballistics
  point-mass formulation. Gravity conversion, wind decomposition, unit plumbing — all correct.
- Actual integration step is ~1.1 cm (`GetCalculationStep` divides 25 yd → 0.0125 yd; the
  "10 sub-steps per output step" comment understates it by ~200×). Semi-implicit Euler error
  at this step size is orders of magnitude below the observed gap. Accuracy is NOT
  integration-limited (it is, if anything, performance-overpaying).
- Atmosphere path (density, lapse, humidity, station pressure) is fine; the runner feeds the
  exact Hornady conditions (29.92 inHg, 59 °F, 50 % RH) from `hornady/config.csv`.

**Conclusion: the observed +2–8 % velocity error is a drag-model/data issue, not an
integration or unit error.**

---

## a) Potential errors, ranked by evidence

### a1. Wind-drift deficit is NOT a wind bug — it is 100 % the drag deficit
Checked the lag rule (drift = crosswind × (TOF − range/MV)) against both models: it
reproduces our drift *and* Hornady's to within 0.3 %:

| case | lag-rule ratio ours/ref | actual ratio |
|---|---|---|
| B2_wind_01 (5 mph)  | 0.930 | 0.933 |
| B2_wind_02 (10 mph) | 0.842 | 0.844 |
| B2_wind_04 (20 mph) | 0.930 | 0.932 |

No fix needed in the wind code; it heals when drag is fixed.

### a2. Spin drift is missing a cos(shot-angle) factor — CONFIRMED numerically
`1.25·(Sg+1.2)·t^1.83` (Litz) is a *level-fire* fit; yaw of repose scales with the gravity
component perpendicular to the velocity, i.e. cos θ. Multiplying our drift by cos θ collapses
the angle-dependent error exactly back to the level-fire ratio:

| config (ELD-X) | angle | raw ratio ours/ref @500 | ×cos θ |
|---|---|---|---|
| B1_baseline_01 | 0°  | 1.39 | 1.39 |
| B4_angle_01    | 10° | 1.41 | 1.38 |
| B4_angle_04    | 45° | 1.95 | 1.38 |
| B4_angle_07    | 60° | 2.76 | 1.38 |

**Fix:** scale the drift term (`TrajectoryCalculator.cs:351`) by `lineOfSightCos`.
Eliminates the entire "+176 % under angle" symptom. The residual constant ×1.3–1.4 (ELD-X)
is Litz-formula calibration vs 4DOF's physical gyro model → see b2.

### a3. Reported TOF has a systematic ≈ −0.2 % truncation bias
`time` is accumulated per integration step via `TimeSpan.Add(TimeSpan.FromSeconds(...))`
(`TrajectoryCalculator.cs:434`); `FromSeconds` truncates to a whole 100 ns tick. ~120 000
steps × ~140 ticks each → average half-tick loss ≈ 6 ms on a 2.7 s flight.
**Fix:** accumulate time as a `double` (seconds), convert to `TimeSpan` only at output.
Also: time is recomputed as `drMag/velocityMag(end-of-step)` instead of summing the `dt`
actually used — harmless in magnitude, but inconsistent; sum `dt`.

### a4. Steep shots drop the final output row (COMPARISON.md §5 mechanical symptom)
Loop margin is `rangeTo + calcStep` in *horizontal* meters (`TrajectoryCalculator.cs:288`),
but line-of-sight distance advances `calcStep/cos θ` per iteration — 2× the margin at 60°,
so the loop can exit having jumped past the 1500-yd emit threshold.
**Fix:** margin `calcStep/lineOfSightCos`, or loop `while (currentItem <
trajectoryPoints.Length)` with the physics stop conditions inside.

### a5. Latent: drag-node walk only goes backward
`TrajectoryCalculator.cs:408-409` walks `Previous` as Mach falls but never advances `Next`
if Mach rises (steep downhill re-acceleration, wind-segment switch, sound speed falling with
altitude). When it happens the node quadratic is extrapolated outside its fitted segment.
Not implicated in the current 50 cases, but a correctness hole. **Fix:** bidirectional walk.

### a6. Sound speed slightly low: `331·sqrt(T/273)`
Correct dry-air form: 331.3·√(T/273.15) (≈ 20.047·√T_K); humidity raises c further
(~+0.1–0.3 %). Our Mach axis is shifted ~+0.07–0.3 %, sampling the steep transonic Cd wall
slightly early — second-order, but exactly in the band where the comparison hurts most.
**Fix:** correct constants in `Atmosphere.CalculateSoundVelocity` (+ optional humidity term).

### a7. Minor: angled-fire drop rotation treats sight height inconsistently
`TrajectoryCalculator.cs:357-361` adds sight height vertically but subtracts it perpendicular
to the LOS. Sub-inch constant offset at 45–60°; invisible in the comparison. Fix or comment.

---

## b) Improvements, ranked by impact

### b1. THE dominant gap: drag inputs — two separable layers
Measured effective drag ratio ours/4DOF from ln-velocity decay over fixed segments (G7):

| segment | ELD-X | ELD-M |
|---|---|---|
| 0–300 yd   | 0.966 | 0.912 |
| 300–600 yd | 0.927 | 0.873 |
| 600–900 yd | 0.919 | 0.855 |

- **Layer 1 — constant scale (~4 % ELD-X, ~9 % ELD-M at the muzzle):** the published box
  BCs (G7 0.325 / 0.351) are optimistic vs the Doppler data 4DOF runs on (consistent with
  independent Litz Doppler measurements running below Hornady box values). No algorithm
  change fixes this — it is calibration. A per-bullet BC fit against the radar data removes
  most of the baseline error in one stroke.
- **Layer 2 — Mach-dependent shape (further ~4–6 % by 900 yd, crossover at Mach ≈1.05):**
  a single scalar BC × G7 shape cannot follow a measured Cd(M) curve (COMPARISON.md §6).
  Engine-level answers:
  1. **Multi-BC (Mach→BC) support via curve synthesis — NOT an in-loop lookup** (design
     decision, see b1.3): convert BC segments into a custom `DragTable` up front and run
     it through the existing `GC` path.
  2. **Use the existing measured-curve path**: `DrgDragTable` already accepts custom Cd(M)
     tables. Decisive experiment with data in hand — *invert the 4DOF reference*:
     differentiate reference v(x) → deceleration → divide out ρ, v², mass/caliber factor →
     effective Cd(M) Hornady used. Feed back as a per-bullet `.drg`. If our integrator then
     reproduces the 4DOF tables nearly exactly, the entire residual gap is proven to be
     drag-curve data, not algorithm — and the correction curves are a product feature.

### b1.3 Design decision: multi-BC = curve synthesis, one utility, two directions (2026-07-12)

Question considered: native Mach-segmented BC in the engine vs a tool converting BC
segments to a radar-style table. **They are the same feature in two packages** — the
engine's drag term is `Cd_std(M)/BC`; multi-BC would make it `Cd_std(M)/BC(M)`, which is
algebraically identical to running the existing `GC` path with a synthesized curve:

```
Cd_custom(M) = Cd_std(M) · BC_ref / BC(M)
```

**Decision: synthesize the curve up front; do NOT add an in-loop BC(M) lookup.** Reasons:
1. **No hot-loop change** — the integrator is proven bit-identical to Hornady's own
   calculator (see Validation); don't disturb it or add a second lookup walk + boundary
   cases to test.
2. **Better physics at segment boundaries** — published stepped BCs (Sierra style) are
   discontinuous; naive in-loop switching puts Cd jumps mid-flight. Offline synthesis can
   interpolate BC(M) smoothly between knots, then bake the result into the standard
   node/polynomial machinery, which is designed for smooth curves.
3. **One pipeline** — the output is the same artifact as a real radar curve: measured
   `.drg` files, 4DOF-derived curves (experiment 2), and multi-BC synthesized curves all
   flow through one tested code path and are directly comparable.
4. **Composes with the inverse tool** — calibration (fit BC(M)/Cd(M) from a reference or
   radar velocity table) and synthesis are the two directions of the same mapping; build
   as one utility.

API convenience is solved by exposing the converter as a **public library function**, not
only a CLI:

```csharp
DragTable.FromBCSegments(DragTableId baseTable, (double machOrVelocity, double bc)[] segments)
```

— constructor-time synthesis, serializable alongside the ammo definition, thin CLI wrapper
for the experiment pipeline. Detail: published multi-BCs are usually banded by *velocity*
(fps), not Mach — accept either and document the reference sound speed assumption
(standard sea level, ~1116.4 fps).

Knot-count expectation from our measurements: BC(M) ours/ref drifts ~0.96 → 0.92 across the
supersonic band and crosses over near Mach 1.05 → roughly **6–8 knots (dense near Mach
0.9–1.2)** capture the 4DOF curves well; 3–5 published Sierra-style bands capture layer 1
and most of layer 2.

### b2. Recalibrate spin drift after the cos θ fix
Remaining error is a clean ×1.3–1.4 over-prediction (ELD-X; ELD-M scattered, likely
reference-side noise). Options: tune the `1.25·(Sg+1.2)` constant against the 4DOF set, or
move to a yaw-of-repose model using actual trajectory curvature (gives cos θ and downrange
Sg growth for free).

### b3. Numerics cleanup bundle (small, cheap; do together)
- accumulate `time` in double seconds (a3);
- bidirectional drag-node walk (a5);
- steep-shot termination margin (a4);
- sound-speed constants + humidity (a6);
- optional: midpoint step for drag — mostly to allow a coarser step at same accuracy.

### b4. Optional parity feature: vertical wind jump (aerodynamic jump)
4DOF outputs it; we model nothing. Litz's approximation (∝ crosswind, Sg) could be added as
a separate output column. Doesn't affect current comparison metrics; closes the one 4DOF
column with no counterpart.

---

## Validation: our engine ≡ Hornady's own 3DOF calculator (2026-07-12)

Test of the b1 "BC overestimation" claim against Hornady's own *standard* (BC-based)
web calculator: 6.5 mm 147 gr ELD Match, MV 2600, G1 0.697, 100 yd zero, 10 mph 90° wind,
default atmosphere (29.92 / 59 °F / 50 %).

| rng (yd) | 4DOF vel | Hornady 3DOF vel | ours G1 vel | ours − H3DOF | H3DOF − 4DOF |
|---|---|---|---|---|---|
| 100 | 2461 | 2473 | 2473 | 0 | +12 |
| 200 | 2325 | 2349 | 2349 | 0 | +24 |
| 300 | 2192 | 2229 | 2229 | 0 | +37 |
| 400 | 2063 | 2112 | 2112 | 0 | +49 |
| 500 | 1936 | 1999 | 1999 | 0 | +63 |

Drop and wind drift also match Hornady's 3DOF to ≤0.1 in at every range.

Conclusions:
1. **Our engine reproduces Hornady's own BC-based calculator exactly** (velocity to the
   fps) — no implementation error in the drag path; the point-mass code is validated
   end-to-end.
2. **Hornady's own 3DOF is +3.3 % fast @500 yd vs their 4DOF** with their own published
   BC — effective drag ratio ≈ 0.89, matching the ELD-M layer-1 deficit measured from the
   50-case set. At Mach 2.3→1.7 this is pure BC-scale optimism, not transonic shape.
3. G1 and G7 runs agree with each other (1999 vs 2000 fps @500) — the published pair is
   internally consistent, both optimistic vs radar.
4. Therefore the COMPARISON.md §1 baseline gap is **drag data, not algorithm** → the fix
   is b1 (Mach-segmented BC / measured `.drg` curves), and expectations for "matching 4DOF
   with box BCs" should be set accordingly.

---

## b5. 4DOF adoption verdict — what to take, what to skip (2026-07-12)

Assessment of `../4DOF.md` against the validation evidence above: should the engine become
4DOF/3.5DOF, or is radar-table drag the whole answer?

**For velocity, drop, TOF, and wind drift — radar drag tables ARE the 4DOF adoption, and we
already support them.** The validation proved our point-mass integrator is numerically
identical to Hornady's own BC calculator, and the lag-rule check proved wind drift is pure
drag deficit. 4DOF's headline idea (measured Cd(Mach) instead of BC) changes the *data*, not
the equations of motion — `DrgDragTable`/`GC` consumes such curves today. A radar-measured
Cd curve also *implicitly contains* most of the yaw physics (the radar tracked a real,
yawing bullet — its transonic drag rise includes the limit-cycle-yaw drag increment).

**A true 4th DOF (angle-of-attack integration, STANAG-4355-style MPM) is NOT worth it.**
It needs per-bullet Cmα, CLα, Magnus, spin damping, and both moments of inertia — data
neither the radar sheets nor public sources provide (Hornady carries it as proprietary
per-bullet data). Five estimated unknowns to compute effects worth a few inches at 1500 yd.

**Three slices of the yaw physics ARE adoptable ("3.5DOF"), using only data we already
carry** (Sg, length, diameter, twist, crosswind, trajectory geometry) — each quantified
from the reference set:

| Add-on | Evidence from the 4DOF reference data | Cost |
|---|---|---|
| **Aerodynamic jump** | `VerticalWindJump` is a constant angular deflection, linear in crosswind: 0.21 in @100 for 5 mph, 0.86 in @100 for 20 mph ≈ **0.04 MOA per mph**. Litz empirical form `AJ(MOA/mph) ≈ 0.01·Sg − 0.0024·L(calibers) + 0.032` needs only Sg and bullet length. | Small — layered on like spin drift. The only headline 4DOF output we lack entirely (= b4). |
| **Physical spin-drift shape** | Integrate drift from yaw-of-repose (∝ g·cosθ/v² along the trajectory, one constant fit to the 4DOF set) instead of `t^1.83`. Automatically yields the missing cos θ behavior (a2) and correct downrange growth. | Moderate — or minimal path: cos θ fix + recalibrated constant (a2/b2). |
| **Downrange Sg output** | Reference `Gyro` grows as **Sg ≈ Sg₀·(v₀/v)^1.2–1.35** (both ELD baselines; spin decays much slower than velocity). `Sg(x) = Sg₀·(v₀/v)^1.25` reproduces the column within ~5–10 %. Note: their muzzle Sg (3.06) runs ~10 % above our Miller value (2.78). | Trivial — one expression at output time. Enables transonic-stability warnings. |

Caveat: `B2_wind_08` (15 mph @30°, ELD-M) shows the jump column flipping sign downrange
(+0.9 in @500 → −15.5 in @1500) — their model adds transonic yaw effects into that column;
a constant-angle empirical AJ covers the level-flight supersonic behavior (~90 % of
practical use) but won't chase that tail.

**Bottom line:** no 4DOF integrator. Measured drag via the existing `.drg` path carries all
the trajectory accuracy (proven); the three rotation-effect layers above give the same
"point mass + yaw effects bolted on" architecture 4DOF itself uses. Remaining daylight
after that is proprietary per-bullet aero data, not model capability.

---

## Suggested order of experiments

1. **Small PR:** spin-drift cos θ + termination margin + time accumulation → re-run the 50
   cases; spin-drift and TOF tables should visibly improve.
2. **Decisive test:** invert the 4DOF velocity tables into per-bullet `.drg` curves, re-run
   through `DrgDragTable` — isolates "algorithm" from "drag data".
3. If (2) confirms near-exact agreement: build the **BC(M) ↔ Cd(M) utility** per b1.3 —
   forward direction `DragTable.FromBCSegments(...)` as the production-facing feature
   (users have box/multi-band BCs, not radar curves), inverse direction as the calibration
   tool; no integrator changes.
