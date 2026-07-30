# Coriolis effect + azimuth robustness — implementation plan

Plan to add **Earth-rotation (Coriolis / Eötvös) deflection** to the 3DOF engine, and to fix the
**barrel-azimuth handling** that currently breaks near 90°. Inspired by community PRs
[#46 (azimuth @ 90°)](https://github.com/nikolaygekht/BallisticCalculator.Net/pull/46) and
[#47 (Coriolis)](https://github.com/nikolaygekht/BallisticCalculator.Net/pull/47), but **re-designed**
because (a) both PRs were written against the pre-`48d1209` `Measurement<T>` hot loop that no longer
exists, and (b) PR #47's Coriolis model uses hand-tuned empirical constants (`1.25` "Kestrel factor",
`1.85` Eötvös multiplier) instead of first-principles physics.

Status: **IMPLEMENTED** (2026-07-19). Engine: `ShotParameters.Latitude` + closed-form corrections in
`TrajectoryCalculator.Calculate` (azimuth decoupled from the muzzle vector). Tests:
`CoriolisTest.cs` (azimuth robustness, Kestrel acceptance, closed-form guards, sign/hemisphere) and
`TrajectoryCalculatorTest.CoriolisTrajectoryTest` (BE absolute acceptance via `be_coriolis_*`
templates, multi-range to 2000 yd). One refinement vs the plan: the Eötvös scales the **gravitational
fall below the vacuum bore line**, not raw `ry` — this preserves the exact −sightHeight muzzle drop
(scaling raw `ry` shifted the muzzle by ~0.02″). Accuracy vs BE: windage ~exact, drop within ~0.16%
(drag-baseline-limited through the transonic zone).

---

## 1. What the PRs did, and why we are not porting them verbatim

| PR | Intent | Problem with a literal port |
|---|---|---|
| #46 | Stop the trajectory collapsing when `BarrelAzimuth ≈ 90°` | Patched `velocityVector.X`/`rangeVector.X` in the old loop. Root cause is the *coordinate-frame design*, not those two lines — better fixed structurally (§3). |
| #47 | Add Coriolis + Eötvös deflection via `ShotParameters.Latitude` | Closed-form `t²` displacement with an undocumented `omega × 1.25` horizontal and `× 1.85` vertical multiplier, tuned to one Kestrel 5700 at one range/bullet. Right *goal* (match Kestrel), but the constants are unexplained and unvalidated beyond 12 points — reproduce them deliberately (§6), don't inherit them. |

We keep the **public API idea** (a nullable `ShotParameters.Latitude`, reuse of `BarrelAzimuth`) and
adopt **matching the Kestrel 5700 as the acceptance target** (§6) — Kestrel is the field reference the
shooting community trusts. But we get there by integrating a **per-step acceleration `a = −2Ω × v`** in
the existing Euler loop (correct for all azimuths, both hemispheres, Eötvös for free), then **diffing
against the Kestrel reference set and calibrating only if a real, documented residual remains** — rather
than copying PR #47's two undocumented constants blind. Traced to source, PR #47's base per-step form
already equals correct point-mass Coriolis (`Ω·sinφ·v·t²`), so its `1.25` horizontal / `1.85` vertical
multipliers make Kestrel's model ~25% / ~85% *stronger* than textbook — a substantive claim worth
reproducing deliberately (and understanding), not inheriting as magic numbers.

---

## 2. Current state (raw-double hot loop, `BallisticCalculator/Calculations/TrajectoryCalculator.cs`)

Frame inside `Calculate` (see comments at `:260`, `:265`):
- `x` = toward target (down-range), `y` = vertical (up), `z` = lateral (windage). Position in metres,
  velocity in `velUnit`, gravity converted to `velUnit/s` (`:286`).

The azimuth bug — two coupled lines assume the line of fire **is** the `x`-axis:
- **`:261-263`** initial velocity tilts the muzzle vector *into* `z` by azimuth:
  `vx = vel0·cosElev·cosAz`, `vz = vel0·cosElev·sinAz`. At `Az=90°` → `vx≈0`.
- **`:404`** `dt = calcStepMeters / (vx / mpsToVel)` → divide-by-zero when `vx≈0` ⇒ trajectory dies.
- **`:439`** `distanceMeters = rx / lineOfSightCos` measures along `x` only, so with `vz≠0` the reported
  distance is wrong even when the loop survives.

There is **no `Latitude`** on `ShotParameters` (`:45` has only `BarrelAzimuth`), and **no Coriolis term**.
All existing tests use azimuth `0`/`null`, so `cosAz=1, sinAz=0` and the buggy branch is never exercised.

---

## 3. Design decision — azimuth semantics (pick this before coding)

**Recommended (Option B): azimuth is the *firing bearing*, trajectory stays in the line-of-fire frame.**

Redefine `BarrelAzimuth` as the compass bearing of the shot (0° = North, +clockwise → East) used **only**
to orient Coriolis (and, later, could orient absolute wind). The bullet is always integrated along `x`:

```
:261-263   double vx = vel0 * barrelElevCos;   // was * barrelAzCos
           double vy = vel0 * barrelElevSin;
           double vz = 0;                        // azimuth no longer tilts the muzzle vector
```

Why this is the right call:
- **It subsumes PR #46 for free** — `vx` no longer depends on azimuth, so the `:404` divide-by-zero and
  the `:439` distance error simply cannot occur. No `horizontalVelocity`/magnitude patches needed.
- **Zero regression**: for `Az ∈ {0, null}`, `barrelAzCos=1, barrelAzSin=0` ⇒ identical numbers, all
  current tests unchanged.
- **Consistent with the existing wind model** (`WindVectorRaw`, `:465`) which is already expressed
  relative to the line of fire (`wind.Direction` 0° = toward target), not the compass.
- Matches how mainstream solvers (Applied Ballistics/Kestrel) treat Coriolis: integrate in the
  range/cross/vertical frame, project Earth rotation in by azimuth + latitude.

Alternative (Option A — literal PRs): keep azimuth tilting the muzzle vector, then patch `dt` to use
`√(vx²+vz²)`, patch distance to use `√(rx²+rz²)`, and project global windage back onto the barrel line.
More moving parts, changes distance/windage semantics for `Az≠0`, and buys nothing over Option B.
**Only choose A if a downstream consumer already depends on azimuth physically deflecting the path.**

The rest of this plan assumes **Option B**.

---

## 4. Physics — the reference (Applied-Ballistics) closed-form model

**Decision (2026-07-18): match the field references (BE + Kestrel), which agree to 0.4% (§6.8).** We do
**not** ship the true per-step `−2Ω×v` integrator: it disagrees with every field tool (~1.2× on horizontal,
and a different range law on vertical). Instead we implement the closed forms the references use — which,
crucially, were **derived analytically from the SET data and validated against BE *and* Kestrel across all
latitudes, azimuths, ranges, and both configs** (§6.7/§6.8). No fudge factors survive; the once-mysterious
"1.8× vertical" is just Eötvös-as-modified-gravity with muzzle velocity.

Constants: **Ω = 7.2921159e-5 rad/s**, **g = 32.174 ft/s²** (9.80665 m/s²). `φ` = latitude (N +, S −),
`AZ` = firing azimuth (0° = North, clockwise → 90° = East), `V₀` = muzzle velocity, and per output point
`Range` (down-range distance) and `TOF` (time of flight).

**Horizontal (windage) — azimuth-independent:**
```
Δwindage = Ω · sin(φ) · Range · TOF        (deflects RIGHT in N hemisphere; sign via sin φ)
```
Matches BE/Kestrel **exactly**. (This is ~0.83× a true `∫x·dt` integration — the reference form uses the
constant-velocity approximation; we match the reference, not the purer integral.)

**Vertical (Eötvös) — modelled as a gravity modification, using V₀:**
```
g_eff  = g − 2Ω · cos(φ) · sin(AZ) · V₀
drop_coriolis = drop_baseline · (g_eff / g)
             = drop_baseline · (1 − 2Ω·cos(φ)·sin(AZ)·V₀ / g)
```
i.e. `Δdrop = −drop_baseline · 2Ω·cos(φ)·sin(AZ)·V₀/g` (East ⇒ less drop; West ⇒ more; hemisphere-symmetric
because `cos φ` is even). Matches BE/Kestrel to **~1%**; the residual is a small constant to fine-tune
against the SET data at implementation (candidate causes: exact `g`, eastward-velocity = `V₀·cos(elevation)`).

Sanity checks vs data (lat 45): horizontal 21.40 in @2000 (BE 21.40 ✓); Eötvös E `+38.6` vs BE `+38.9`,
W `−38.6` vs `−39.2`; zero at pole (`cos 90=0`) and at az 0/180 (`sin=0`) ✓; hemisphere flips horizontal
(sin) but not vertical (cos) ✓ (SET13).

These are **per-output-point** closed forms — no per-step cross product, no integration changes. See §5.

---

## 5. Implementation steps

### 5.1 API — `ShotParameters` (`BallisticCalculator/Calculations/ShotParameters.cs`)
Add after `BarrelAzimuth` (`:45`), mirroring its attributes exactly:
```csharp
/// <summary>
/// Geographic latitude of the shot, used for the Coriolis / Eötvös deflection.
/// North positive, South negative. When null, Earth rotation is ignored.
/// </summary>
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
[BXmlProperty("latitude", Optional = true)]
public Measurement<AngularUnit>? Latitude { get; set; }
```
`BarrelAzimuth` already exists and is reused unchanged (0° = North per §3).

### 5.2 Engine — `TrajectoryCalculator.Calculate`

Coriolis is applied as **per-output-point closed-form corrections** (§4) — *not* per-step integration.
This is surgical: it doesn't perturb TOF/velocity/range (matching BE, whose TOF is identical Coriolis
on/off), needs no cross product, and directly reproduces the reference formulas.

1. **Decouple azimuth from the muzzle vector (§3)** — edit initial velocity at `:261-263` so azimuth no
   longer tilts the trajectory: `vx = vel0*barrelElevCos; vz = 0;`. Azimuth becomes a **pure scalar** into
   the Coriolis terms. This also **eliminates PR #46's azimuth-90 bug** (vx no longer collapses) — no
   `horizontalVelocity`/magnitude patches needed. Keep `barrelAzSin` (`:258`) for the vertical term.
2. **Precompute per-shot Coriolis constants** near the other pre-loop constants (`~:315`):
   ```csharp
   bool coriolis = shot.Latitude != null;
   const double OMEGA = 7.2921159e-5;                    // Earth rotation, rad/s
   double sinLat = coriolis ? shot.Latitude.Value.Sin() : 0;
   double cosLat = coriolis ? shot.Latitude.Value.Cos() : 0;
   double sinAz  = barrelAzSin;                          // sin(firing azimuth)
   // Horizontal coefficient: Δwindage_m = hCoef * Range_m * TOF_s   (right-deflection)
   double hCoef  = OMEGA * sinLat;
   // Vertical Eötvös as a gravity ratio (dimensionless), constant per shot:
   double v0mps  = ammunition.MuzzleVelocity.In(VelocityUnit.MetersPerSecond);
   double gmps   = 9.80665;
   double vRatio = coriolis ? (1.0 - 2.0 * OMEGA * cosLat * sinAz * v0mps / gmps) : 1.0;
   ```
   *(Optional single fine-tune constant `CoriolisEotvosScale` (default 1.0) on the `2Ω…` term to close the
   ~1% vertical residual vs the SET data — decide its value empirically in §6, keep it documented.)*
3. **Apply the corrections at each output point** — inside the `if (distanceMeters >= nextRangeDistMeters)`
   block (`~:349`), *before* building the `TrajectoryPoint` (before `:355`):
   ```csharp
   if (coriolis)
   {
       // Horizontal: deflects RIGHT in N hemisphere; our Windage is left +, right − ⇒ subtract
       windage_m -= hCoef * distanceMeters * timeSeconds;
       // Vertical Eötvös: scale the ballistic drop by g_eff/g
       ry     *= vRatio;      // dropFlat (vs muzzle)
       drop_m *= vRatio;      // perpendicular drop (angled fire); == ry when level
   }
   ```
   These feed the existing `windageMeas`/`dropMeas` and their angular adjustments (`:355`,`:373-375`).
   Validate the exact drop baseline against the SET data (scaling `ry` — which starts at −sightHeight —
   vs. scaling only the gravitational fall; the difference is the −2.5″ sight-height term, ~0.02% at range,
   but confirm it doesn't shift the near-muzzle rows).
4. **Do not apply Coriolis in `SightAngle`** — keep zeroing purely ballistic. (BE *does* re-zero with
   Coriolis on — Sight Adj shifts 5.004→4.982 for an east shot — but the Eötvös at the 100 yd zero is
   `~0.008 × drop@100` ≈ 0.02″, i.e. <0.02 MOA; not worth coupling Coriolis into the zeroing solver.)
   Record as a deliberate choice; note it as the likely source of any tiny near-zero residual vs BE.

### 5.3 Serialization
`Latitude` gets both `[BXmlProperty]` and `[JsonIgnore(WhenWritingNull)]`, so it round-trips through the
existing `BXml` + `System.Text.Json` paths automatically. Add a round-trip assertion in the shot-parameters
serialization test alongside the `BarrelAzimuth` case (search `BallisticCalculator.Test/Serialization/`).

---

## 6. Validation & tests (`BallisticCalculator.Test/Calculator/TrajectoryCalculatorTest.cs`)

Use **AwesomeAssertions** (`.Should()`), not FluentAssertions.

**Reference target — settled (§6.7/§6.8).** The engine implements the derived closed form of §4, which
already matches **both** Ballistic Explorer (§6.6/§6.7, SET0–13) **and** Kestrel (§6.5, PR #47; §6.8
cross-check SET14–16), the two agreeing to 0.4%. Validation is therefore a **regression check** that our
engine reproduces the SET data, not an open calibration hunt.

- **Horizontal** `Ω·sinφ·Range·TOF` — exact vs both references.
- **Vertical** `drop·(1 − 2Ω·cosφ·sinAZ·V₀/g)` — ~1% vs both; optional `CoriolisEotvosScale` to close it.

### Test cases

1. **Azimuth robustness (from PR #46)** — run the `g1_nowind` template with
   `BarrelAzimuth = 90°`, `Latitude = null`: assert `> 1` points, no nulls, last `Distance > 900 yd`.
   Add `0/180/270°` too. With the §3 azimuth decoupling these are **numerically identical** to the `Az=0`
   run (azimuth without latitude is a no-op) — that identity is itself the assertion.
2. **BE acceptance `[Theory]`** — drive the run matrix with the **BE config** (§6.6) and assert
   `Windage`/`Drop` at 500/1000/1500/2000 yd against the SET0–13 values (`CLAUDE/data/SET*.txt` /
   `coriolis_reference.csv`). Compare **on−off deltas** so the ballistic baseline cancels. **Map BE's
   drift sign** (BE right = +, ours right = −) before asserting (§6.6 ⚠). Tolerance: horizontal exact
   (~0.5%), vertical ~1.5% (the Eötvös residual). Covers SET1–13: sinφ sweep, azimuth sweep, West
   symmetry, equator/pole zeros, hemisphere flip.
3. **Kestrel acceptance `[Theory]`** — the PR #47 table (§6.5) with the **Kestrel config**; Kestrel's sign
   already matches ours (no flip). Same closed form, second independent reference (§6.8 SET14–16 confirm
   BE↔Kestrel agreement). Tolerance 0.5 MOA to start.
4. **Closed-form unit guard** (device-independent). Directly assert the §4 formulas at a mid-range point:
   `Δwindage == Ω·sinφ·Range·TOF` and `drop == drop_baseline·(1 − 2Ω·cosφ·sinAZ·V₀/g)`. Pins the model so
   a future edit (e.g. `CoriolisEotvosScale`) can't silently change it.
5. **Sign/hemisphere coverage** — N-hemi deflects right (windage < 0 ours), S-hemi left, equator ≈ 0
   horizontal, poles max horizontal & ~zero Eötvös, E/W azimuth lowers/raises drop, Eötvös
   hemisphere-symmetric (SET13). Covered by the §6.7 data.
6. **Regression** — full existing suite passes byte-for-byte (the closed form is inert when
   `Latitude = null`, and the §3 muzzle-vector change is a no-op at `Az ∈ {0, null}`).

### 6.5 Reference data — Kestrel 5700 (from PR #47)

Twelve points at **2000 yd**, provenance PR #47 (`27pchrisl`, Kestrel 5700 readout). Fixed configuration:

- **Ammunition**: 69 gr, BC **0.365 G1**, muzzle velocity **2600 fps**.
- **Rifle**: sight height **3.2 in**, zero **100 yd**, no rifling/spin drift.
- **Atmosphere**: altitude 0, **29.92 inHg**, **59 °F**, humidity **0**.
- **Shot**: step **1 yd**, max **2000 yd**, `ShotAngle = 0`, no wind. `Latitude`/`BarrelAzimuth` per row.
- Match point: trajectory point within **0.5 yd** of 2000 yd. Signs in **our** convention
  (windage: left +, right −).

| Latitude ° | Azimuth ° | Expected windage (MOA) | Expected elevation/drop (MOA) | Meaning |
|---:|---:|---:|---:|---|
| 45 | 0 | −1.03 | −222.52 | 45°N, North — baseline, deflects right |
| 0 | 0 | 0.00 | −222.52 | equator — ~zero horizontal Coriolis |
| 90 | 0 | −1.46 | −222.52 | pole — max horizontal, no Eötvös |
| 45 | 90 | −1.03 | −220.64 | East — Eötvös lifts, reduces drop |
| 45 | 180 | −1.03 | −222.52 | South |
| 45 | 270 | −1.03 | −224.40 | West — Eötvös lowers, increases drop |
| 30 | 0 | −0.73 | −222.52 | 30°N, North |
| 60 | 0 | −1.27 | −222.52 | 60°N, North |
| −45 | 0 | +1.03 | −222.52 | 45°S, North — deflects left |
| −45 | 90 | +1.03 | −220.64 | 45°S, East |
| −45 | 180 | +1.03 | −222.52 | 45°S, South |
| −45 | 270 | +1.03 | −224.40 | 45°S, West |

`[InlineData]` form (latitudeDeg, azimuthDeg, expectedWindageMOA, expectedElevationMOA):
```csharp
[InlineData(45,   0, -1.03, -222.52)]
[InlineData( 0,   0,  0.00, -222.52)]
[InlineData(90,   0, -1.46, -222.52)]
[InlineData(45,  90, -1.03, -220.64)]
[InlineData(45, 180, -1.03, -222.52)]
[InlineData(45, 270, -1.03, -224.40)]
[InlineData(30,   0, -0.73, -222.52)]
[InlineData(60,   0, -1.27, -222.52)]
[InlineData(-45,  0,  1.03, -222.52)]
[InlineData(-45, 90,  1.03, -220.64)]
[InlineData(-45,180,  1.03, -222.52)]
[InlineData(-45,270,  1.03, -224.40)]
```

**Notes on this data.**
- The elevation column is dominated by ballistic drop (−222.52 MOA); Eötvös is the small ±1.88 MOA
  east/west swing. The clean model must reproduce that **swing**, not just the total.
- Horizontal deflection scales with `sinφ` (0.73 / 1.03 / 1.27 / 1.46 ≈ sin 30/45/60/90 × ~1.46) —
  a good internal consistency check on any implementation.
- **Widen before locking constants:** these are 12 points from one device at one range/bullet. Before
  committing any `KestrelCoriolisScale`, capture additional Kestrel runs at other ranges/latitudes
  (e.g. 1000 & 1500 yd) so a scale factor is fit to a curve, not a single operating point. Store larger
  sets as a `TableLoader` template (`CLAUDE/TRAJECTORY_TEST.md` format) rather than more `[InlineData]`.
- If the clean model reproduces the **horizontal** column but undershoots **Eötvös** (the `1.85` case),
  that isolates the discrepancy to the vertical term — investigate whether Kestrel folds aerodynamic
  jump or a spin/gravity coupling into it before assuming a raw scale.

### 6.6 Reference data — Ballistic Explorer (primary, user-generated)

Fixed configuration (from the user's BE session, 2026-07-18). **Our acceptance `[Theory]` (§6.2) must
use this exact config**, not the §6.5 Kestrel config.

- **Ammunition**: 65 gr, muzzle velocity **2600 fps**, BC **0.365 G1**.
- **Rifle**: sight height **2.50 in**, zero **100 yd**, no rifling/spin drift.
- **Atmosphere**: altitude 0, **29.53 inHg** (station pressure at 0 alt ⇒ 4-arg ctor,
  `pressureAtSeaLevel=false`), **59 °F**, humidity **0.78** (BE shows 78 % — divide by 100 for our ctor).
- **Shot**: no wind, range slope 0° (level), `ShotAngle = 0`. `Latitude`/`BarrelAzimuth` per run matrix.
- Note: BE's displayed "Sight Adj (MOA)" is a residual/UX artifact per the user — **do not** use it as a
  cross-check. Validate the setup via the R0 (Coriolis-off) trajectory match instead.

**Confirmed observation (2026-07-18):** BE gives the **same windage profile for az 0/45/90 at lat 45**.
This is correct physics — horizontal Coriolis deflection is azimuth-independent (∝ `sinφ` only); the
azimuth term cancels (`azCos²+azSin²=1`). Matches the Kestrel table (identical −1.03 windage across
azimuths). ⇒ The azimuth effect must be read from the **drop** (Eötvös ∝ `cosφ·sin(az)`), not windage;
compare drop at az 90 (East, less drop) vs az 270 (West, more drop), `(drop_W − drop_E)/2` = clean Eötvös.

Run matrix + differencing method: see the "Run matrix" and isolation notes below (identical to §6.5's
structure, reused for BE). Raw BE captures: **`CLAUDE/data/SET0.txt … SET12.txt`** (50-yd granularity to
2000 yd). Fillable template: **`CLAUDE/data/coriolis_reference.csv`**.

**BE-specific checks when capturing:** confirm azimuth is 0°=N/clockwise; note BE's windage & drop sign
conventions (map to ours: left +, right −); verify enabling Coriolis does not shift the 100 yd zero.

**⚠️ Sign convention in tests — semantic, not a bug.** BE's drift sign may be the **opposite** of ours
(BE appears to report right-deflection as **positive**; our `Windage` is **left +, right −**). When
asserting against BE data, **compare magnitudes and the *relative* sign pattern** (N-hemi vs S-hemi flip,
East vs West), not the raw signed value — or normalise BE's sign to our convention first (flip it) and
document the flip. A whole-sign mismatch is a convention difference to be mapped, **not** a physics error;
only an inconsistent/mixed sign pattern indicates a real problem.

### 6.7 BE captured results & derived model (2026-07-18, SET0–SET13)

14 runs, config per §6.6, values at 2000 yd (baseline SET0 drop = −4671.8 in; "Eötvös" = drop − baseline;
BE "Drift" +=right, opposite our sign):

| Set | lat | az | Drift (in) | Eötvös Δdrop (in) |
|---|---:|---:|---:|---:|
| SET0 | — | off | 0.00 | 0.0 |
| SET1 | 45 | 0 | 21.40 | 0.0 |
| SET2 | 45 | 45 | 21.40 | +27.5 |
| SET3 | 45 | 90 (E) | 21.40 | +38.9 |
| SET4 | 30 | 0 | 15.13 | 0.0 |
| SET5 | 60 | 0 | 26.21 | 0.0 |
| SET6 | 90 | 0 | 30.27 | 0.0 |
| SET7 | −45 | 0 | −21.40 | 0.0 |
| SET8 | 0 | 0 | 0.00 | 0.0 |
| SET9 | 45 | 270 (W) | 21.40 | −38.9 |
| SET10 | 30 | 90 (E) | 15.13 | +47.7 |
| SET11 | 60 | 90 (E) | 26.21 | +27.5 |
| SET12 | 90 | 90 (E) | 30.27 | 0.0 |
| SET13 | −45 | 90 (E) | −21.40 | +38.9 |

**Derived BE Coriolis model** (all confirmed to the decimal):
- **Horizontal drift** `= Ω·sinφ·Range·TOF`, **azimuth-independent** (SET1=SET2=SET3), sign flips by
  hemisphere (SET7). Equals BE's classic *simplified* closed form — **~20 % below a true per-step −2Ω×v
  integration** (velocity-weighted ∫x dt ⇒ ~36.4 in vs BE 30.27 at lat90/2000). So our engine's natural
  per-step integrator will **overshoot BE horizontal ~1.2×** unless we adopt BE's formula or scale ~0.83.
- **Vertical Eötvös** `∝ cosφ·sin(az)`: East lifts (less drop), West drops (more), symmetric; zero at the
  pole (SET12, cosφ=0) and at az 0/180. **Hemisphere-symmetric** — SET13 (lat −45/az 90) gives the same
  +38.9 lift as SET3 (lat +45/az 90), since `cos(−45)=cos(45)`; only the *horizontal* term flips sign by
  hemisphere. At 2000 yd Eötvös is **~1.82× the horizontal** at equal trig — independently reproducing
  PR #47's Kestrel `1.85`. Two unrelated field references agree the vertical is boosted above textbook.
- The vertical/horizontal ratio is **not** constant with range (1.10 → 1.82 over 500→2000 yd for
  lat45/az90) — so a scalar on the *horizontal* term can't reproduce it. **Resolved by the derived form:**
  the Eötvös is a constant fraction of the **drop** (`2Ω·cosφ·sinAZ·V₀/g`), and drop grows faster than
  `Range·TOF`, which is exactly why the ratio-to-horizontal rises with range. Modelling it as a
  gravity-ratio on the drop (§4) captures the range law with no per-range tuning.

**DECISION (2026-07-18): match the references (both components) via the derived closed form.**
Since BE and Kestrel agree to 0.4% (§6.8) on **both** horizontal and vertical, matching them is worthwhile
and unambiguous. We therefore ship the **closed-form model of §4**, not the per-step `−2Ω×v` integrator.

The vertical formula — which I could not reverse from first principles earlier — was **derived from this
SET data and confirmed against BE + Kestrel** (validation run 2026-07-18):
- **Horizontal** `Ω·sinφ·Range·TOF` → matches to the decimal at every lat/range.
- **Vertical** `drop·(1 − 2Ω·cosφ·sinAZ·V₀/g)` (Eötvös = gravity modified by muzzle-velocity eastward
  component) → matches to ~1% everywhere; the "1.8× boost" was this, not a fudge.

So there is **no residual scale factor to chase** — the closed form *is* the reference behavior. The only
open tuning is the ~1% vertical constant (optional `CoriolisEotvosScale`, §5.2). Implementation is §4/§5;
the three-way "ours vs BE vs Kestrel" check becomes a *regression* test (we already know it matches).

**Dataset complete (SET0–SET13)** for the BE config. The optional S-hemisphere Eötvös check
(lat −45/az 90, SET13) is captured and confirms hemisphere-symmetric Eötvös.

### 6.8 BE-vs-Kestrel direct cross-check — RESULT (SET14–16)

The BE captures (§6.7) and the Kestrel table (§6.5) use different configs, so this cross-check re-ran BE
under the **Kestrel config** for a head-to-head. Config used: **69 gr**, 0.365 G1, 2600 fps; sight height
**3.2 in**; zero 100 yd; 0 ft / **29.53 inHg** / 59 °F / **0 % humidity**; no wind; level; Coriolis on;
lat 45. (Note: BE kept pressure 29.53, not the intended 29.92 — immaterial, since Coriolis tracks TOF,
which a 1.3 % pressure change barely moves; the sub-0.5 % agreement below confirms it doesn't matter.)

@ 2000 yd; Eötvös baseline = SET14 (az 0, Eötvös=0); 1 MOA = 20.944 in @ 2000 yd:

| Quantity | BE inches | BE → MOA | Kestrel MOA | Agreement |
|---|---:|---:|---:|---:|
| SET14 az 0 windage | 21.48 | 1.026 | 1.03 | 0.4 % |
| SET15 az 90 Eötvös (E, lift) | +39.2 | +1.872 | +1.88 (222.52→220.64) | 0.4 % |
| SET16 az 270 Eötvös (W, drop) | −39.2 | −1.872 | −1.88 (222.52→224.40) | 0.4 % |

**Result: BE and Kestrel agree to ~0.4 % at identical inputs**, for both horizontal *and* the ~1.8×
vertical Eötvös. ⇒ The vertical boost is a **confirmed, shared reference behavior**, not one device's
quirk. If §6.7's decision is ever to chase the vertical, **both references point at the same ~1.8×
target** — so the choice is purely "true physics (scale 1.0) vs. match-the-references," not "which
reference."

**Sign conventions confirmed:** Kestrel reports right-deflection **negative** (−1.03) — same as **our**
engine (left +, right −). **BE is opposite** (right positive). Map BE's sign when testing against it;
Kestrel's sign already matches ours.

---

## 7. Risks & open questions
- **Azimuth semantics change** (§3) is technically breaking for anyone relying on the old (buggy) behavior
  where azimuth physically deflected the path. Alpha status + no tests exercising it make this low-risk;
  call it out in the changelog / README risk notice.
- **Matching references, not textbook**: we ship the AB-style closed form (matches BE + Kestrel), which is
  *less* rigorous than a true `−2Ω×v` integration (horizontal ~0.83× the exact integral; Eötvös uses
  constant muzzle-velocity). This is a deliberate, documented choice to agree with the field tools shooters
  trust. If a future goal is maximal physical accuracy instead, that's a separate mode.
- **~1% vertical residual**: the Eötvös form lands ~1% under BE/Kestrel. Decide at implementation whether
  to close it with `CoriolisEotvosScale` (or refining `g`/eastward-velocity) or accept it within tolerance.
- **Coriolis in zeroing** deliberately omitted (§5.2.4) — BE re-zeros with it on, but the effect at 100 yd
  is <0.02 MOA; document it as the likely source of any tiny near-zero residual vs BE.
- **Wind + Coriolis interaction**: independent and additive (Coriolis is a post-hoc correction on the
  output point; wind acts inside the integrator via `velocityAdj`). Note it if wind ever moves to an
  absolute-compass frame.
- **Documentation**: update `CLAUDE.md` §3 (ShotParameters), §5 (algorithm — new Coriolis subsection),
  and the SKILL/reference docs once implemented.

---

## Attribution
This work is derived from community contributions by **27pchrisl** (PRs #46 and #47). When these
features are committed, credit them in the commit message, e.g.:
`Co-authored-by: 27pchrisl <27pchrisl@users.noreply.github.com>` (confirm the exact email/handle from
the PR commits via `gh pr view 46 --json commits` before committing).

---

## 8. Checklist
- [x] §5.1 Add `ShotParameters.Latitude` (+ attributes).
- [x] §3/§5.2.1 De-couple azimuth from the muzzle vector (`:261-263`) — also fixes PR #46 azimuth-90.
- [x] §5.2.2 Precompute closed-form constants (`coriolisHCoef`, `coriolisVRatio`).
- [x] §5.2.3 Apply per-output-point corrections (windage `−Ω·sinφ·Range·TOF`; Eötvös scales the
  gravitational fall below the vacuum bore line by `vRatio` — refined from the plan's raw-`ry` scaling
  to keep the exact −sightHeight muzzle drop).
- [x] §5.2.4 `SightAngle` left Coriolis-free (decision recorded in code + CLAUDE.md §5).
- [x] §5.3 Serialization round-trip test for `Latitude` (`CalculatorSerializerTest`, 2 cases).
- [x] §6.1 Azimuth-0/90/180/270 robustness (identity vs Az=0 when `Latitude = null`).
- [x] §6.2 BE absolute acceptance — `TrajectoryCalculatorTest.CoriolisTrajectoryTest` via
  `be_coriolis_*` templates (SET0/1/3/6/7/9, multi-range to 2000 yd, incl. azimuth-null case).
- [x] §6.3 Kestrel acceptance `[Theory]` (§6.5 table; sign already ours).
- [x] §6.4 Closed-form unit guards (windage `−Ω·sinφ·Range·TOF`; fall × `vRatio`).
- [x] §6.5 Sign/hemisphere coverage.
- [x] §6 Eötvös fine-tune: **not needed** — bore-fall scaling matches BE/Kestrel within tolerance,
  no `CoriolisEotvosScale` shipped.
- [x] §6 Full existing suite green — 235 tests pass (closed form inert when `Latitude = null`).
- [x] §7 Update `CLAUDE.md` + README risk notice + SKILL docs (`SKILL/.../ballistic-calculator/SKILL.md`).
