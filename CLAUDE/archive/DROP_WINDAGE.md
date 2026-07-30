# DROP & WINDAGE — accuracy and computation reference

How the engine computes vertical drop and horizontal windage, how accurate they are against
the external 3DOF reference set, and which code path received the 2026-07-13 (a.*) fixes.
All file:line references verified against source 2026-07-13. Companion docs:
`CLAUDE/TRAJECTORY_TEST.md` (test harness), `CLAUDE/PLAN0.md` (a1–a7/b* analysis).

> Note: the `*Diagnostic` test classes cited below (`HornadyAngled`, `Bullet2Hornady`,
> `AngleMechanism`, `IndependentInclined`, and the freeze experiment on `TrajectoryCalculator.cs`)
> were **temporary** and removed after this investigation; their numeric results are recorded here.

Engine: `BallisticCalculator/Calculations/TrajectoryCalculator.cs`
Output: `BallisticCalculator/Calculations/TrajectoryPoint.cs`
Test:   `BallisticCalculator.Test/Calculator/TrajectoryCalculatorTest.cs` (`TrajectoryTest`, lines 63–109)

---

## 1. Observable drop & windage accuracy

Measured maximum error per reference file (this session, after the a.* fixes), against the
tolerances currently asserted in `TrajectoryCalculatorTest.cs:64-70`:

| file | drop max err (MOA) | drop tol (MOA) | windage max err (MOA) | windage tol (MOA) | exercises |
|---|---|---|---|---|---|
| g1_nowind      | 0.079 | 0.10 | 0.000 | 0.05 | level, no wind |
| g1_nowind_up   | 0.378 | 0.40 | 0.000 | 0.05 | 10° up shot |
| g1_twist       | 0.068 | 0.10 | 0.0108| 0.015| twist -> spin drift |
| g7_nowind      | 0.062 | 0.10 | 0.000 | 0.05 | G7 |
| g1_wind        | 0.081 | 0.10 | 0.085 | 0.10 | wind |
| g1_wind_hot    | 0.044 | 0.10 | 0.088 | 0.10 | hot atmosphere |
| g1_wind_cold   | 0.042 | 0.10 | 0.037 | 0.05 | cold atmosphere |

### Interpretation

**Drop (angled shots) — RESOLVED: our engine matches Hornady's 3DOF; residual is transonic
drag data.** `g1_nowind_up` (10° up, G1 0.365, 65 gr, 2600 ft/s) was reworked to use Hornady
3DOF as the reference (option C, 2026-07-13). Established with **two** Hornady angled datasets
at matched 78 % humidity (`HornadyAngled` + `Bullet2Hornady` diagnostics):

| bullet @1000 yd | goes subsonic? | our vel − Hornady | our drop − Hornady |
|---|---|---|---|
| bullet 1: G1 0.365, 65 gr, 2600 | yes (Mach 0.88) | +0.5 fps | **+0.178 MOA** |
| bullet 2: G1 0.447, 165 gr, 3100 | no (Mach 1.20) | +0.8 fps | **+0.044 MOA** |

Conclusions (these SUPERSEDE earlier churn in this doc; several intermediate claims were wrong):
- **Our default engine (altitude tracking ON) matches Hornady** — 0.044 MOA on the supersonic
  bullet, 0.178 MOA on the one that goes subsonic. The residual concentrates in the
  transonic/subsonic zone → it is **drag-curve shape** (PLAN0 §b1), not inclined-fire geometry.
- **Hornady tracks in-flight altitude, exactly like our engine.** Proven by freezing our
  altitude (`TrajectoryCalculator.cs:438`): frozen bullet 2 runs **−8.0 fps / 0.6 %** off Hornady
  vs **+0.8 fps** for normal. So our altitude tracking is correct, not an "outlier" (an earlier
  turn wrongly concluded that, from a 50 %-humidity Hornady file — a density artifact).
- **The "Hornady ≈ our-frozen drop" seen on bullet 1 was a coincidence** — confirmed: on
  bullet 2 (no transonic zone) it vanishes and Hornady lands on our *normal* run. On bullet 1
  the transonic drag error happened to offset the altitude difference.
- **There is no separate inclined-fire gravity/convention residual** of note — the ~0.18 MOA on
  bullet 1 is drag data, and bullet 2 shows ~0 inclined-specific error.

The old model-generated reference (drop −527.4 @1000) was the outlier of the three; Hornady
(−525.3) matched our engine far better. Tolerances tightened to velocity 0.0015 / drop 0.20 MOA.

**Windage.** For pure wind cases the max error is ~0.088 MOA; for spin drift (`g1_twist`,
12 in right twist, level fire) ~0.011 MOA. Wind-drift error tracks the drag deficit, not
the wind code: per `CLAUDE/PLAN0.md` a1, the classic lag rule
(drift = crosswind × (TOF − range/MV)) reproduces both our drift and the reference's to
< 0.3 % — when the bullet slows differently, time of flight differs, and crosswind lag
drift differs proportionally. The wind decomposition itself needs no fix; windage heals as
drag data improves.

**What the test measures.** `TrajectoryTest` asserts, per point: `Distance`, `Velocity`
(relative, `velocityAccuracyInPercent`), `Drop`, `Windage` (MOA tolerance converted to
inches at range, floored at 0.001 in) — `TrajectoryCalculatorTest.cs:97-107`. It does
**NOT** assert `Time`, `Mach`, or `Energy` (so e.g. the a3 time-accumulation fix is
invisible to it).

**Reference resolution floor.** The reference files store drop to **0.1 in** (one decimal,
see any row of `g1_nowind_up.txt`, e.g. `550;-88.8;...`), so sub-0.1-in differences are at
the quantization floor of the reference itself — errors below that level are not meaningful.

---

## 2. Code involved in drop calculation

All in `TrajectoryCalculator.Calculate(...)` (`TrajectoryCalculator.cs:200-447`, hot loop
324-444) unless noted.

### Integrator state

- Coordinate frame (comment at line 263): `x` = towards target, `y` = drop (vertical),
  `z` = windage (lateral); positions in **meters**, velocities in the muzzle velocity's
  native unit (`velUnit`, line 246).
- Initialization, line 264:
  `double rx = 0, ry = -sightHeightMeters, rz = 0;` — the bullet starts **below the sight
  line by the sight height** (`sightHeightMeters` from `rifle.Sight.SightHeight`, line 248).
- The barrel is elevated by `SightAngle` (+ `ShotAngle` if present): lines 225-228, so
  `vy = vel0 * barrelElevSin` (line 260).
- Each integration step: gravity is applied to the vertical velocity at **line 425**
  (`vy = vy - factor * vay - earthGravity * dt;`, gravity pre-converted to velUnit/s at
  lines 284-286), and vertical position accumulates at lines 430/434
  (`dry = vy / mpsToVel * dt; ... ry += dry;`).
- Step size: `GetCalculationStep` (lines 450-460) halves the output step and reduces it by
  powers of 10 to at/below `MaximumCalculationStepSize` (10 cm default, line 18) — cm-scale
  integration steps for the standard 50-yd output step.

### Level shot

If `shot.ShotAngle == null` (`hasShotAngle == false`, line 226), drop is the raw vertical
position: `double drop_m = ry;` (line 356). Since `ry` starts at `-sightHeight` and the
line of sight is horizontal, `ry` **is** the distance below the LOS — no transform needed.

### Inclined shot (`hasShotAngle`)

The rotation block, lines 357-369:

```csharp
double drop_m = ry;
if (hasShotAngle)
{
    // Drop for inclined fire is measured perpendicular to the line of sight.
    // The sightHeight is added in the vertical frame but the final subtraction
    // is in the rotated frame; the resulting sightHeight*(cos-1) term is
    // intentional: it pins the muzzle drop to exactly -sightHeight (matching the
    // reference model), while every other point uses the perpendicular rotation.
    // Verified best-of-3 conventions against the reference (a7): dropping this
    // term worsens accuracy and breaks the exact muzzle match. Do not "simplify".
    double y = ry + sightHeightMeters;
    double y_rotated = -rx * lineOfSightSin + y * lineOfSightCos;
    drop_m = y_rotated - sightHeightMeters;
}
```

Convention: for inclined fire, "drop" is measured **perpendicular to the line of sight**
(the tilted LOS at `ShotAngle`, line 229), not vertically. The bore-relative height
`y = ry + sightHeight` is rotated into the LOS frame (`-rx·sinθ + y·cosθ`), then sight
height is subtracted back **in the rotated frame**. Algebraically this leaves a
`sightHeight·(cosθ − 1)` term relative to a pure rotation — that term is *intentional*: at
the muzzle (`rx=0, ry=-sightHeight`) it makes drop exactly `-sightHeight`, matching the
reference model (`g1_nowind_up.txt` row `0;-2.5;...` with `rifle;2.50in;...`). This
convention was tested best-of-3 against the reference this session (PLAN0 item a7): the
current formula is optimal for both muzzle match and downrange accuracy — **do not change
it**. The in-code comment (lines 359-365) exists to prevent well-meaning "simplification".

### Output fields (`TrajectoryPoint.cs`)

| Field | Source | Meaning |
|---|---|---|
| `Drop` | `drop_m` → line 383, `TrajectoryPoint.cs:50-55` | vertical (level) / perpendicular (inclined) distance from **line of sight** |
| `DropFlat` | `ry` directly → line 384, `TrajectoryPoint.cs:57-62` | drop relative to **muzzle** (vertical, un-rotated) |
| `DropAdjustment` | line 372 → `BallisticMath.CalculateAdjustment(dropMeas, distanceMeas)` | angular correction |
| `Distance` | `distanceMeters = rx / lineOfSightCos` (line 437) → line 379 | along the line of sight |
| `DistanceFlat` | `rx` → line 380 | horizontal, from muzzle |

`BallisticMath.CalculateAdjustment` (`BallisticMath.cs:16-22`) is the linear→angular
conversion: `atan(linearAdjustment / distance)` via `MeasurementMath.Atan`, returning 0 rad
when `distance ≤ 0` (protects the muzzle row).

---

## 3. Code involved in windage calculation

### Lateral position

`rz` (meters) accumulates from the lateral velocity `vz` each step:
`drz = vz / mpsToVel * dt;` (line 431), `rz += drz;` (line 435). `vz` starts at
`vel0 · cos(elev) · sin(barrelAzimuth)` (line 261) — zero unless a barrel azimuth is set.

### Wind contribution

`WindVectorRaw` (`TrajectoryCalculator.cs:463-492`) decomposes a `Wind` into velocity-frame
components `(wx, wy, wz)`:

- `rangeVelocity = wind.Velocity · cos(direction)`, `crossComponent = wind.Velocity · sin(direction)` (lines 478-479);
- rotated by the shot angle (`wx = rangeVelocity · sightCosine`, line 489) and the cant
  angle (`wy`/`wz`, lines 490-491) — so a crosswind under cant leaks into the vertical axis.

In the hot loop the wind is **subtracted from the projectile velocity** to form the
air-relative velocity (lines 405-407: `vax = vx - wx; vay = vy - wy; vaz = vz - wz;`), and
drag acts on that vector (lines 421-426, `vz = vz - factor * vaz;`). A pure crosswind
therefore never pushes the bullet directly; it makes the air-relative vector point
into the wind so drag accelerates the bullet downwind over time — the physical lag-drift
mechanism (which is why windage error tracks the drag deficit, §1). Multiple wind zones
switch on `rx >= nextWindRangeMeters` (lines 336-345), keyed by `Wind.MaximumRange`.

Wind direction convention — `Wind.cs:31-38` (doc comment on `Direction`):

```
* 0 degrees   - wind toward the target
* 90 degrees  - wind to the right of the shooter
* 270/-90 degrees  - wind to the left of the shooter
* 180 degrees  - wind toward the shooter
```

Traced through the code: at 90°, `crossComponent = +V` → `wz = +V` (cant 0) → `vaz = -V`
→ `vz` integrates positive → `rz > 0` → **positive (left) windage**, i.e. the 90° wind
blows across from the shooter's right toward his left.

### Spin drift

Enabled only when `rifle.Rifling`, `ammunition.BulletDiameter` and
`ammunition.BulletLength` are all present (lines 212-220). Precomputed constants
(lines 314-316):

- `driftDirection = Rifling.Direction == TwistDirection.Right ? -1 : 1` — right twist
  drifts right (negative windage), left twist drifts left (positive);
- `driftFactor = 1.25 * (stabilityCoefficient + 1.2)` — Litz empirical fit; `Sg` from the
  Miller rule in `CalculateStabilityCoefficient` (lines 495-512).

Applied at output time, lines 350-351:

```csharp
if (calculateDrift)
    windage_m += driftFactor * Math.Pow(timeSeconds, 1.83) * driftDirection * inchToMeter * lineOfSightCos;
```

i.e. `1.25·(Sg+1.2)·t^1.83` inches, signed, converted to meters. The trailing
`* lineOfSightCos` factor was **added this session (fix a2)**: the Litz formula is a
level-fire fit and yaw of repose scales with the gravity component perpendicular to the
velocity (cos θ); without it, angled fire over-predicted drift by up to +176 % at 60°
(PLAN0 a2 has the numeric confirmation). Note that spin drift is **folded into `Windage`**
— there is no separate spin-drift output field.

### Sign convention and outputs

- `TrajectoryPoint.Windage` (`TrajectoryPoint.cs:65-71`): *"The windage to the left is
  positive, to the right is negative"* — consistent with `driftDirection = -1` for right
  twist and the 90°-wind trace above.
- `WindageAdjustment` = `BallisticMath.CalculateAdjustment(windageMeas, distanceMeas)`
  (line 373) = `atan(windage/distance)` (`BallisticMath.cs:16-22`).

---

## 4. SightAngle vs Calculate — which code path got the recent fixes

`SightAngle(...)` (`TrajectoryCalculator.cs:60-188`) and `Calculate(...)` (lines 200-447)
contain **separate, duplicated integration loops**:

- `SightAngle`: outer approximation loop lines 117-186 (up to 100 Newton-style corrections
  of the angle, lines 176-184), inner physics loop lines 138-185. 2D only (`vx, vy` /
  `rx, ry`, lines 123-127): no `z` axis, no wind, no spin drift, no shot angle, no time
  accumulation — it only needs `ry` at the zero distance.
- `Calculate`: full 3D loop, lines 324-444.

### Fix-by-fix status (a-numbers from `CLAUDE/PLAN0.md`)

| Fix | What | Calculate | SightAngle |
|---|---|---|---|
| a6 | Sound speed `331.3·√(T/273.15)` in `Atmosphere.CalculateSoundVelocity` (`Atmosphere.cs:177-180`) | ✅ via `atmosphere.AtAltitude` (`TrajectoryCalculator.cs:310, 328`; `Atmosphere.cs:219-227`) | ✅ via `atmosphere.AtAltitude` (line 142) — **reaches both loops** because the constant lives in `Atmosphere`, not the loops |
| a2 | Spin drift × cos θ | ✅ line 351 | N/A — no drift in zeroing |
| a3 | Time accumulated as `double` seconds (`timeSeconds`, lines 308, 443; `TimeSpan` only at output, line 378) | ✅ | N/A — no time tracked |
| a4 | Termination margin `+ calcStepMeters / lineOfSightCos` (line 288) | ✅ | N/A — no shot angle in zeroing |
| a5 | **Bidirectional** drag-node walk | ✅ lines 415-418 | ❌ **NOT mirrored** — backward-only walk at lines 158-159 |

### The a5 gap in detail

`Calculate`, lines 415-418 — walks the drag-table node list in both directions as Mach
falls **or rises**:

```csharp
while (dragTableNode.Mach > currentMach)
    dragTableNode = dragTableNode.Previous;
while (dragTableNode.Next != null && dragTableNode.Next.Mach <= currentMach)
    dragTableNode = dragTableNode.Next;
```

`SightAngle`, lines 158-159 — still the old backward-only walk:

```csharp
while (dragTableNode.Mach > currentMach)
    dragTableNode = dragTableNode.Previous;
```

If Mach ever *rises* between steps in `SightAngle`, the cached node is not advanced and
`CalculateDrag` extrapolates the node's fitted polynomial outside its segment. In practice
this **cannot trigger during zeroing**: there is no wind, no steep downhill
re-acceleration, and deceleration is monotonic over a zero-range trajectory (sound speed
change over the few meters of bullet rise is far below the 1 m altitude-refresh threshold's
effect). It is a latent **inconsistency between the two duplicated loops**, not an active
bug — but any future refactor that unifies the loops (or adds wind/angle support to
zeroing) should port the two-line forward walk.
