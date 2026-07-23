# Migrating from a pre-1.1.11 version

Read this when you meet code written against an older `BallisticCalculator` — the tell-tales are
`TrajectoryCalculator.SightAngle(...)`, `ShotParameters.SightAngle`, or a `BarrelAzimuth` used to
aim off horizontally. Three breaking changes landed in **1.1.11**.

## 1. Zeroing: `SightAngle` → `CalculateZeroParameters`

`TrajectoryCalculator.SightAngle(...)` is gone. Compute the zero with `CalculateZeroParameters`, which
solves **drop and windage** by driving the full trajectory (spin drift, Coriolis, aero jump, and any
wind are folded into the zero). It returns `ZeroCalculatedParameters { ZeroDropAdjustment,
ZeroWindageAdjustment? }`. `ShotParameters.SightAngle` is renamed to **`ZeroDropAdjustment`**.

Watch the **argument order**: `CalculateZeroParameters(ammunition, atmosphere, rifle, zero, …)` —
atmosphere before rifle, and the `ZeroingParameters` is explicit (usually `rifle.Zero`).

```csharp
// BEFORE
shot.SightAngle = calc.SightAngle(ammo, rifle, atmosphere);              // (ammo, rifle, atmo)

// AFTER — set both zero adjustments…
shot.Apply(calc.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero));   // (ammo, ATMO, rifle, zero)
// …or drop only:
shot.ZeroDropAdjustment = calc.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero).ZeroDropAdjustment;
```

For a custom `GC` table, pass it to **both** `CalculateZeroParameters` (its `dragTable` argument) and
`Calculate`. `ZeroingParameters` also gained `HorizontalOffset` (left +, right −) beside `VerticalOffset`.

## 2. `BarrelAzimuth` no longer tilts the trajectory

Older code could use `BarrelAzimuth` to deflect the path (aim off horizontally). It no longer does —
the bullet is always integrated along the line of fire, and `BarrelAzimuth` now only orients the
Coriolis / Eötvös deflection together with `Latitude`. **A non-zero azimuth with no `Latitude` is a
no-op.** Move any horizontal-aim use of the azimuth to the windage adjustments (below).

## 3. Angular sight adjustments are fields, not a pre-summed angle

Don't fold every correction into one sight angle (or into the azimuth). `ShotParameters` now carries
discrete angular fields and the calculator combines them:

- vertical  = `ZeroDropAdjustment` + `ShotDropAdjustment` + `ShotAngle`
- horizontal = `ZeroWindageAdjustment` + `ShotWindageAdjustment`

Sign is the `Windage` convention (**left +, right −**): a positive windage adjustment aims left, so a
`+N` windage adjustment cancels an `−N` (right) drift. Unset windage fields ⇒ horizontal launch is zero
(bit-identical to the old engine).

```csharp
shot.Apply(calc.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero)); // zero drop + windage
shot.ShotDropAdjustment    = new Measurement<AngularUnit>(3, AngularUnit.Mil);  // dialed come-up
shot.ShotWindageAdjustment = new Measurement<AngularUnit>(1, AngularUnit.Mil);  // dialed windage (left)
shot.ShotAngle             = new Measurement<AngularUnit>(10, AngularUnit.Degree); // uphill shot
```
