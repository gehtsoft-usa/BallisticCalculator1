# B1 — multi-BC / custom drag-curve synthesis (pipeline reference)

Closes the published-BC-vs-4DOF drag gap by synthesizing a per-bullet custom drag curve from an
effective **BC(Mach)** profile and running it through the existing `GC`/`DrgDragTable` path — **no
hot-loop change**. Published box BCs run 2–9 % optimistic vs Hornady 4DOF radar drag (PLAN0 §b1,
`Experiment-Radar-Data/side-by-side/COMPARISON.md` §1); b1 brings ELD bullets to ~0.05 MOA.

Verified against source 2026-07-13.

## Components (engine)
- **`BcAtMach`** (`BallisticCalculator/Calculations/BcAtMach.cs`) — one `Mach` → `Bc` knot.
- **`DrgDragTableFactory.Build(AmmunitionLibraryEntry ammo, DragTableId baseTable, IEnumerable<BcAtMach> curve)`**
  (`Calculations/DrgDragTableFactory.cs`) — returns a `DrgDragTable`. `Cd_custom(M) = Cd_base(M) / BC(M)`
  on the base table's Mach grid; `BC` is piecewise-linear between knots, held flat beyond the end
  knots. Caller supplies the ammunition metadata (name/weight/diameter).
- **`DrgDragTable.Save(Stream|string)` / `Open(...)`** (`Drag/DrgDragTable.cs`) — CFM `.drg`
  round-trip (`:R` precision).
- **Usage:** run the synthesized table with ammo `new BallisticCoefficient(1.0, DragTableId.GC)`
  (the synthesis normalizes to `BcRef = 1`).

## The identity (why it works, and generalizes)
Engine drag: `a = PIR · densityFactor · Cd(M) · v² / BC` (`PIR = 2.08551e-4`). With the synthesized
table (`Cd_custom = Cd_std / BC_eff`) run at `BC = 1`, the base-table term cancels:

```
Cd_custom(M) = a_ref / (PIR · densityFactor · v²)
```

— an **absolute drag coefficient**, independent of the chosen base table *and* of atmosphere. So a
curve derived at one condition reproduces 4DOF across **muzzle velocity, altitude, and temperature**
(validated: ELD-M curve from sea-level baseline reproduces the 10 000 ft run to 0.06 MOA; BTHP
baseline curve reproduces the 100 °F and 7 000 ft runs).

## Estimation — deriving BC(Mach) from a 4DOF trajectory (done OFFLINE)
For a clean baseline 4DOF CSV (`CLAUDE/data/`; columns `Range[0] Velocity[1] Energy[2] ComeUp[3]
WindDrift[9] TimeOfFlight[12]`):
1. deceleration `a = -dv/dt` (central difference on the `Velocity`,`TimeOfFlight` columns);
2. `Mach = v / soundspeed`, where `densityFactor` and `soundspeed` come from
   `Atmosphere.AtAltitude(...)` for **that config's** atmosphere;
3. `BC_eff(M) = PIR · densityFactor · Cd_std(M) · v² / a` (`Cd_std` from `DragTable.Get(baseId)`);
4. smooth (3-point) and sample a few knots.

The **supersonic band (Mach ≳ 1.1) is clean**; the transonic/subsonic tail is noisy (finite-diff
near the drag peak). Keep knots in the clean band; a low-BC bullet that goes deep subsonic in range
(BTHP) is the hard case.

## Data provenance (current curves)
| curve | base | derived from |
|---|---|---|
| `bc_eldx.txt` | G7 | `B1_baseline_01` (ELD-X 220gr .308) |
| `bc_eldm.txt` | G7 | `B1_baseline_02` (ELD-M 147gr 6.5mm) |
| `bc_bthp.txt` | G1 | `B0-3` (BTHP 75gr .224, **regenerated**) |

**BTHP reference caveat:** the original 4DOF BTHP scans are corrupted — COMPARISON.md §0 documents
two inconsistent families (a download-race artifact); `B3_atmo_03` even carried a ~220 gr bullet's
data (3801 vs 1296 ft-lb muzzle energy). They were **regenerated** as `B0-1..5`
(`CLAUDE/data/B0-*-PARAMS.txt` = inputs, `B0-*.csv` = data): B0-1 hot 100 °F/80 %, B0-2 wind 20 mph,
B0-3 baseline, B0-4 MV 3200, B0-5 7000 ft. Sanity check any BTHP scan: a .224 75 gr @2790 goes
**subsonic ~900–1000 yd**; muzzle energy ~**1296 ft-lb**.

## Test (`BallisticCalculator.Test`)
- **`B1DragTest.SynthesizedCurveMatches4Dof`** — the b1 acceptance test. Per case: load the
  `bc_*.txt` curve + the `b1_*.txt` 4DOF reference, **build the table via the factory, then run the
  trajectory**, and assert (a) the synth run matches 4DOF within tolerance **and** (b) the
  published-BC control run is materially worse (`minPublishedDropMOA` floor) — so it can't pass
  trivially.
- **`DrgDragTableFactoryTest`** — factory/Save unit tests (flat-BC reproduces the standard table;
  Save round-trip, including the real `sierra_168_brl.drg` BRL file).

**Resources (embedded):**
- `bc_<bullet>.txt` — the multi-BC curve. Format: `#` comments ignored, **first real line = base
  `DragTableId`**, then `mach;bc` lines. Currently **3 knots** each (Strelok-style multi-BC).
- `b1_<case>.txt` — 8 Hornady-4DOF trajectories in the `TableLoader` format (50-yd rows), one per
  test case: `b1_eldx`, `b1_eldm`, `b1_eldm_10kft`, `b1_bthp`, `b1_bthp_hot`, `b1_bthp_3200`,
  `b1_bthp_7kft`, `b1_eldx_wind`.

**Results (3-knot multi-BC vs published BC, both vs 4DOF drop):**
| case | published BC | 3-pt multi-BC |
|---|---|---|
| ELD-X / ELD-M baseline | 0.88 / 1.63 MOA | 0.057 / 0.040 MOA |
| ELD-M @10 kft | 0.83 MOA | 0.056 MOA |
| BTHP baseline/hot/3200/7 kft | 5–9 MOA | 0.9–1.2 MOA |

ELD (high-BC boat-tail) is near-perfect with 3 knots; BTHP (low-BC, steep transonic) needs more
knots for <0.5 MOA but is still 6–8× better than a single box BC.

## Caveats
- **Wind:** windage matches (it heals with drag, PLAN0 a1); the wind cases' **drop carries 4DOF's
  vertical wind-jump** (aerodynamic jump, PLAN0 b4 — not modeled), so the wind case asserts
  velocity + windage only.
- **Non-standard density:** the ELD drag gap naturally *shrinks* at non-standard density
  (COMPARISON.md §2), so a standard-derived curve slightly over-corrects there (ELD-X hot was ~0.57
  MOA — excluded from the set for that reason).

## How to regenerate / add a bullet (the offline generator was intentionally removed)
Tests must **not** read `CLAUDE/data/` at runtime, so regeneration is a throwaway step:
1. Add a temporary test in `BallisticCalculator.Test` that reads `CLAUDE/data/<config>.csv`.
2. Compute `BC_eff(M)` per the Estimation section; smooth; pick 3–8 knots (dense near Mach 0.9–1.2).
3. Write `bc_<bullet>.txt` (base table id + `mach;bc`) and embed it in the `.csproj`.
4. Generate `b1_<case>.txt`: convert the 4DOF CSV to the `TableLoader` format —
   header `ammo;<bc>;<wgt>gr;<mv>ft/s` / `rifle;<sight>in;100yd` / `wind;<mph>mph;<deg>°` /
   `atmosphere;<T>°F;<H>;<P>inHg;<alt>ft` / `shot;0°;0°` / units row, then
   `yd;drop(ComeUp);dropMOA;windage(WindDrift);windMOA;velocity;mach;energy;time;0.0;0.0` at 50-yd rows.
5. Add an `InlineData` row to `B1DragTest`; set tolerances from a measurement pass (a temporary
   `Measure`-style fact that prints max per-case error, embedded-resources only).
6. **Delete the throwaway generator** before committing.
