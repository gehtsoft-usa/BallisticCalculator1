# Breaking changes

Newest first. Each entry says **what changed**, **why**, and **how to migrate**.

---

## 1.1.12

### 1. `Atmosphere.Density` now uses the station pressure, not the sea level pressure

The constructor computed the density from the `pressure` **argument** instead of the resolved
`Pressure` property. With `pressureAtSeaLevel: true` and a non-zero altitude those are different
numbers: `Pressure` is back-computed down to the station, while the argument is the sea level value.
The density therefore came out too high, in proportion to the two pressures — **21 % at 5000 ft**.

Only the public `Density` property was affected. The trajectory engine reads the resolved `Pressure`
through the internal `AtAltitude`, so **no trajectory, zero or drag result changes**; the property is
now consistent with the pressure the engine was already using.

Callers who worked around the old value — passing the station pressure with
`pressureAtSeaLevel: false` just to make `Density` come out right — can now pass the sea level
pressure directly and read both correctly.

### 2. `Atmosphere.CreateICAOAtmosphere` passed the humidity as the base altitude

An argument-order slip fed `humidity` into the `baseAltitude` parameter of the internal pressure
model, so a call with `humidity: 0.78` built the standard atmosphere from a base 0.78 m above sea
level. The error was ~0.008 % of the pressure, and the default `humidity: 0` was unaffected, but the
returned `Pressure` changes very slightly for any call that passed a humidity.

### 3. New: `Atmosphere.DensityAltitude`

Additive, no migration needed. The standard-atmosphere altitude matching this atmosphere's density,
humidity included — the single number that summarizes the air's effect on drag.

Its baseline is the sea level density implied by the ICAO constants (29.92 inHg / 288.15 K /
287.058), **not** `Atmosphere.StandardDensity`. The latter is the engine's drag normalization and
differs by ~0.005 %, which is ~1.9 ft of density altitude. They look interchangeable; they are not.

---

## 1.1.11.3

### 1. `DrgDragTableFactory.Build` returns the physical drag coefficient

`Build` used to return `Cd_base(M) / BC(M)` — a curve on a reciprocal-BC scale that had to be run
with a ballistic coefficient **value** of `1.0`. Every other custom table in the library (a loaded
`.drg` file, `RadarDragTableFactory.Create`, the public `DrgDragTable` constructor) holds the
projectile's **physical Cd** and is run with a **form factor of 1**.

Two scales behind one type meant a `Build` table could not be persisted: `Save` wrote it as a `.drg`,
`Open` read it back as physical Cd, and the drag came out `1/SD` too large — 2.8× for a 285 gr .338,
so the bullet fell out of the sky. Mixing the pairings by hand did the same thing silently, with no
exception.

`Build` now multiplies by the bullet's sectional density, so **one convention is used everywhere: the
file's**. Consequences:

- **`Build` requires the bullet weight and diameter** in the entry's `Ammunition` (they set the scale
  of the curve). It throws `ArgumentException` without them.
- **`Build` overwrites the entry's `BallisticCoefficient`** with `new BallisticCoefficient(1,
  DragTableId.GC, BallisticCoefficientValueType.FormFactor)`. The BC was never an input; stamping it
  makes the table and its ammunition impossible to mismatch. It also defaults an empty `Source` to
  `"bc curve"`.
- `Build` → `Save` → `Open` now round-trips: same points, same effective BC, same trajectory. Only the
  muzzle velocity is lost, because the `.drg` header has no slot for it.
- Third-party `.drg` files are unaffected — `Open` and `Save` did not change.

Trajectories are **unchanged** for correct usage: the sectional density in the curve cancels against
the form factor in the ammunition.

**Before**
```csharp
ammo.BallisticCoefficient = new BallisticCoefficient(1.0, DragTableId.GC);   // magic value
var entry = new AmmunitionLibraryEntry { Name = "220 gr .308", Ammunition = ammo };
var table = DrgDragTableFactory.Build(entry, DragTableId.G7, curve);
var traj = calc.Calculate(ammo, rifle, atmosphere, shot, null, table);
```

**After**
```csharp
// weight and diameter are required; the BC is set by the factory
var entry = new AmmunitionLibraryEntry { Name = "220 gr .308", Ammunition = ammo };
var table = DrgDragTableFactory.Build(entry, DragTableId.G7, curve);
var traj = calc.Calculate(table.Ammunition.Ammunition, rifle, atmosphere, shot, null, table);
```

---

## 1.1.11

### 1. Zeroing: `SightAngle` replaced by `CalculateZeroParameters`

`TrajectoryCalculator.SightAngle(...)` is **removed**. Zeroing is now done with
`TrajectoryCalculator.CalculateZeroParameters(...)`, which solves the **vertical and horizontal**
barrel adjustments by driving the full trajectory — so spin drift, Coriolis, aerodynamic jump, and
(optionally) wind are all folded into the zero, not just a bare ballistic drop.

- The result type is `ZeroCalculatedParameters { ZeroDropAdjustment, ZeroWindageAdjustment? }`.
- `ShotParameters.SightAngle` is renamed to **`ZeroDropAdjustment`**.
- Apply the result with `shot.Apply(zeroCalculatedParameters)` (sets both adjustments), or read
  `.ZeroDropAdjustment` and set it yourself.
- **Argument order changed**: `CalculateZeroParameters(ammunition, atmosphere, rifle, zero, …)` —
  atmosphere comes **before** rifle, and the zero (`ZeroingParameters`) is an explicit argument.

**Before**
```csharp
var shot = new ShotParameters
{
    Step = …, MaximumDistance = …,
    SightAngle = calc.SightAngle(ammo, rifle, atmosphere),
};
var trajectory = calc.Calculate(ammo, rifle, atmosphere, shot);
```

**After**
```csharp
var shot = new ShotParameters { Step = …, MaximumDistance = … };
shot.Apply(calc.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero));
// or, drop only:
// shot.ZeroDropAdjustment = calc.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero).ZeroDropAdjustment;
var trajectory = calc.Calculate(ammo, rifle, atmosphere, shot);
```

For a custom `GC` drag table, pass it to **both** calls:
`calc.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero, dragTable: table)` and
`calc.Calculate(ammo, rifle, atmosphere, shot, wind, table)`.

`ZeroingParameters` also gained a `HorizontalOffset` (nullable, same sign as windage: left +, right −)
alongside the existing `VerticalOffset`, so the zero point can be offset horizontally.

### 2. New role of `BarrelAzimuth`

`ShotParameters.BarrelAzimuth` **no longer tilts the trajectory**. The bullet is always integrated
along the line of fire; the azimuth now only *orients* the Coriolis / Eötvös deflection, together with
`Latitude`.

- A non-zero `BarrelAzimuth` with no `Latitude` is now a **no-op** — previously it deflected the path.
- To aim off horizontally, use the windage adjustments (see #3), not the azimuth.

(The azimuth was decoupled from the path when Coriolis support was added; 1.1.11 completes the picture
by providing the windage-adjustment fields that replace the old azimuth-as-horizontal-aim usage.)

### 3. Angular sight adjustments are passed as fields, not pre-aggregated

`ShotParameters` now carries the sight settings as **discrete angular fields**, and the calculator
combines them internally into the launch vector. You no longer fold every correction into a single
sight angle (or into the azimuth) yourself.

New fields (all `Measurement<AngularUnit>`, in addition to `ZeroDropAdjustment`):

| Field | Meaning |
|---|---|
| `ZeroWindageAdjustment` | horizontal zero (usually set via `Apply`); positive = left |
| `ShotDropAdjustment` | extra elevation dialed for this shot (clicks); positive = up |
| `ShotWindageAdjustment` | extra windage dialed for this shot (clicks); positive = left |

The calculator accumulates them:

- vertical = `ZeroDropAdjustment` + `ShotDropAdjustment` + `ShotAngle`
- horizontal = `ZeroWindageAdjustment` + `ShotWindageAdjustment`

Sign follows the trajectory `Windage` convention (**left +, right −**), so a positive windage
adjustment aims left and a **+N** windage adjustment cancels an **−N** (right) drift. Leaving the
windage fields unset keeps the horizontal launch at zero (bit-identical to the pre-1.1.11 engine).

**Before** — a single pre-computed angle, horizontal aim only via the (now-removed) azimuth tilt:
```csharp
shot.SightAngle = /* your own sum of zero + hold-over + incline */;
// horizontal correction had to go through BarrelAzimuth
```

**After** — set the field that applies and let the calculator combine them:
```csharp
shot.Apply(calc.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero)); // zero drop + windage
shot.ShotDropAdjustment    = new Measurement<AngularUnit>(3, AngularUnit.Mil); // dialed come-up
shot.ShotWindageAdjustment = new Measurement<AngularUnit>(1, AngularUnit.Mil); // dialed windage (left)
shot.ShotAngle             = new Measurement<AngularUnit>(10, AngularUnit.Degree); // uphill shot
```
