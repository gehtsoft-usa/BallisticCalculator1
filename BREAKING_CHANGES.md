# Breaking changes

Newest first. Each entry says **what changed**, **why**, and **how to migrate**.

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
