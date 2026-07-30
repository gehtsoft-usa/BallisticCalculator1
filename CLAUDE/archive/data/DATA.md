# Hornady 4DOF reference dataset — data description

This folder holds a set of **Hornady 4DOF trajectories** to use as a reference for
validating our `BallisticCalculator`. For 50 configurations (`config.csv`) we drove
the online Hornady 4DOF calculator (see `plan.md` for the automation) and saved one
trajectory table per configuration to `output/`.

The idea: feed the **same shot conditions** into our calculator and compare its
predicted trajectory (drop, velocity, time of flight, drift) against Hornady's.

## Folder layout

```
hornady/
├─ config.csv                 # the 50 input configurations + per-bullet BC (this file drives everything)
├─ DATA.md                    # this document
├─ plan.md                    # automation spec / field reference for the 4DOF site
├─ README.md                  # how to build & run the automation
├─ HornadyBallistics.csproj   # the Playwright .NET batch runner
├─ Program.cs / HornadyRunner.cs / Models/CalcConfig.cs
└─ output/
   ├─ <Name>.csv              # 50 result trajectories, one per config row (the reference data)
   ├─ run.log                 # batch log
   ├─ SAMPLE_B1_baseline_01_mechanics.csv   # first hand-driven validation run (10 mph default wind — demo only)
   ├─ _replaced_220BTHP/      # superseded results from the original "30 CAL 220 GR BTHP" bullet (see history)
   └─ _stale_race_removed/    # 12 stale files quarantined during the download-race fix (see history)
```

## `config.csv` — input configurations (50 rows)

One row = one 4DOF calculation = one comparison case. Columns:

| # | Column | Meaning | Unit / notes |
|---|--------|---------|--------------|
| 1 | `Name` | Config id; also the output file name (`output/<Name>.csv`) | — |
| 2 | `Bullet` | Exact 4DOF dropdown bullet name | must match option text |
| 3 | `MuzzleVelocity` | Muzzle velocity | ft/s |
| 4 | `BarrelTwist` | Rifling twist (distance per turn) | in/rev |
| 5 | `BoreDiameter` | Bore/bullet diameter fed to 4DOF (affects spin drift / gyro only) | in |
| 6 | `SightHeight` | Sight height over bore | in |
| 7 | `WindSpeed` | Wind speed | mph |
| 8 | `WindAngle` | Wind direction (clock/deg; 90 = full-value crosswind) | ° |
| 9 | `Altitude` | Altitude | ft |
| 10 | `Pressure` | Station pressure at that altitude | inHg |
| 11 | `Temperature` | Temperature | °F |
| 12 | `Humidity` | Relative humidity | % |
| 13 | `ShootingAngle` | Line-of-sight / shot angle (uphill +, downhill −) | ° |
| 14 | `BC_G1` | Published Hornady **G1** ballistic coefficient of the bullet | — |
| 15 | `BC_G7` | Published Hornady **G7** ballistic coefficient (blank if none published) | — |

Notes:
- **Zero range is not a column.** Every run used the 4DOF default **100 yd** zero.
- **Output settings** (shared by all runs): range **0→1500 yd**, interval **25 yd**
  (→ 61 rows), columns Velocity/Energy/TOF/WindDrift/SpinDrift/AerodynamicJump enabled.
- **`BoreDiameter`** is the 4DOF *rifle* input. It does **not** affect drop/velocity/TOF
  (4DOF's drag is internal to the selected bullet); it only feeds 4DOF's spin-drift /
  gyroscopic-stability calc. Values were set to the true bullet caliber
  (.308 / .264 / .224).
- **`BC_G1`/`BC_G7` are per-bullet constants** repeated on every row so each row is a
  self-contained comparison case. They are for **our** calculator; 4DOF itself does not
  use a BC (it uses an internal Doppler-radar drag curve).

## Bullets in this set

Three bullets are cycled across the configs. Full reference:

| Bullet (config text) | Item # | Dia (in) | Weight (gr) | Bullet length (in) | SD | G1 | G7 |
|---|---|---|---|---|---|---|---|
| `HORNADY 30 CAL 220 GR ELD-X` | 3078 | 0.308 | 220 | **1.630** | 0.331 | **0.650** | **0.325** |
| `HORNADY 6.5 MM 147 GR ELD MATCH` | 26333 | 0.264 | 147 | ~1.37 *(unconfirmed)* | ~0.301 | **0.697** | **0.351** |
| `HORNADY 22 CAL 75 GR BTHP` | 2279 | 0.224 | 75 | ~0.985 *(unconfirmed)* | 0.214 | **0.395** | *none published* |

Config → bullet distribution: ELD‑X 19 rows, ELD‑M 16, BTHP 15 (50 total).

BC sources (Hornady published values, retrieved 2026‑07‑12):
- ELD‑X 220: https://www.hornady.com/bullets/rifle/30-cal-308-220-gr-eld-x (G1 0.650 / G7 0.325, OAL 1.630")
- ELD Match 147: https://www.hornady.com/bullets/rifle/6.5mm-.264-147-gr-eld-match (G1 0.697 / G7 0.351)
- BTHP Match 75: https://www.hornady.com/bullets/rifle/22-cal-224-75-gr-bthp-match (G1 0.395; SD 0.214; no G7 published)

## `output/<Name>.csv` — the reference trajectories (23 columns)

Downloaded verbatim from 4DOF (the site names every download `trajectory.csv`; the
runner renames it to `<Name>.csv`). One row per range step, 61 rows (0→1500 / 25).
Values are quoted, full precision. Columns, in order:

```
Range, Velocity, Energy,
ComeUp, ComeUpMOA, ComeUpMrads,
SpinDrift, SpinDriftMOA, SpinDriftMrads,
WindDrift, WindDriftMOA, WindDriftMrads,
TimeOfFlight,
VerticalWindJump, VerticalWindJumpMOA, VerticalWindJumpMrads,
TotalComeUp, TotalComeUpMOA, TotalComeUpMrads,
TotalWindDrift, TotalWindDriftMOA, TotalWindDriftMrads,
Gyro
```

- `Range` in yd, `Velocity` in ft/s, `Energy` in ft·lb, `TimeOfFlight` in s; angular
  quantities in inches **and** MOA **and** Mrads.
- `VerticalWindJump*` = aerodynamic jump. `Gyro` = gyroscopic stability factor Sg.
- `Range` carries rounding artifacts (e.g. `174.99998`, `1500.0001`); round to the
  nearest 25 when joining.
- Trust `Range, Velocity, Energy, ComeUp, SpinDrift, WindDrift, TimeOfFlight,
  VerticalWindJump, Gyro` for validation. (`TotalComeUp*` came through as 0 in this
  dataset.)

## Mapping to our `BallisticCalculator` inputs

| Our model field | From | Status |
|---|---|---|
| `Ammunition.MuzzleVelocity` | `config.csv` MuzzleVelocity | ✅ |
| `Ammunition.Weight` | bullet (220 / 147 / 75 gr) | ✅ |
| `Ammunition.BulletDiameter` | .308 / .264 / .224 | ✅ |
| `Ammunition.BallisticCoefficient` | `config.csv` `BC_G7` (preferred) or `BC_G1` | ✅ (G7 missing for 22 BTHP → use G1 there) |
| `Ammunition.BulletLength` | bullet length above | ⚠️ only ELD‑X (1.630") confirmed; 6.5 & 22 to confirm |
| `Rifle.Sight.SightHeight` | `config.csv` SightHeight | ✅ |
| `Rifle.Rifling.RiflingStep` | `config.csv` BarrelTwist | ✅ |
| `Rifle.Rifling.Direction` | not given → assume **right-hand** | ⚠️ assumption |
| Zero distance | 100 yd (4DOF default) | ✅ |
| `Atmosphere` (altitude/pressure/temp/humidity) | `config.csv` | ✅ (pressure = station pressure, not sea-level) |
| `ShotParameters` (step 25, max 1500, shot angle) | fixed + `config.csv` ShootingAngle | ✅ |
| `Wind` (speed, direction) | `config.csv` WindSpeed/WindAngle | ✅ |

Still outstanding for a full comparison: **bullet length** for the 6.5 ELD‑M and 22
BTHP (needed only for our spin-drift / gyro path), and a **G7** for the 22 BTHP (use
G1 meanwhile).

Caveat on interpretation: our calculator uses a **BC + standard drag table (G1/G7)**
while 4DOF uses a **measured radar drag curve**. Differences therefore reflect both the
model (3DOF vs 4DOF) and the drag representation. For a drag-matched comparison, the
Warner Tool Flatline bullets in `../` (radar curves + G1/G7 in the `.txt` files) are the
cleaner route.

## History / data-integrity notes

- Bullet #1 was originally the 4DOF default `HORNADY 30 CAL 220 GR BTHP`, which has **no
  published BC** (legacy/internal entry). It was replaced with `HORNADY 30 CAL 220 GR
  ELD-X` and those 19 configs re-run; old results are in `output/_replaced_220BTHP/`.
- `BoreDiameter` for the .30-cal rows was corrected `0.256 → 0.308` at the same time.
- A **download race** (download firing before the 4DOF backend finished recomputing) had
  produced 12 stale files; the runner now verifies each downloaded CSV's 1500‑yd velocity
  against the on-screen results and retries. The 12 stale files are in
  `output/_stale_race_removed/`.
- Final state verified: 50/50 files present, correct 23‑col × 61‑row structure, velocity
  strictly decreasing, TOF increasing, v₀ = configured muzzle velocity, wind/atmosphere
  effects consistent, all 19 ELD‑X files confirmed freshly regenerated.
