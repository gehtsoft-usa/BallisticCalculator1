---
name: ballistic-calculator
description: >
  Use whenever writing, reviewing, or debugging .NET/C# code that uses the BallisticCalculator
  NuGet package (namespace BallisticCalculator) or its Gehtsoft.Measurements unit types — even
  when the user doesn't name the package explicitly but is clearly doing external/interior
  ballistics in .NET (bullet drop, velocity, energy, windage, spin drift, time of flight, zeroing
  a scope) for rifles, air rifles, bows, or artillery. Covers the full public API (Ammunition,
  Rifle, Atmosphere, Wind, ShotParameters, TrajectoryCalculator, TrajectoryPoint); standard,
  custom, .drg, and multi-BC drag tables; saving/loading data and building your own file format
  around it (BXml and System.Text.Json serialization); and building or rendering scope reticles
  (e.g. to SVG). Also use it for this library's Gehtsoft.Measurements Measurement<T> unit types
  (DistanceUnit, VelocityUnit, AngularUnit, WeightUnit, PressureUnit, …). Skip for: generic physics
  or projectile-motion exercises, a different ballistics library (e.g. GNU/JBM), game-engine or Unity
  projectile motion, plain unit conversions, and non-code ballistics questions (load-data or
  forensics advice). Self-contained: no access to the library source or binaries is required —
  prefer this over scanning the package to rediscover the API.
---

# BallisticCalculator

A .NET library (`netstandard2.0`) that models the trajectory of a projectile through the
atmosphere using a 3DOF (point-mass) model.

- NuGet: **`BallisticCalculator`** (namespace `BallisticCalculator`).
- Depends on **`Gehtsoft.Measurements`** (namespace `Gehtsoft.Measurements`) — every physical
  quantity is a strongly-typed `Measurement<TUnit>`.
- License: LGPL 2.1.

```csharp
using BallisticCalculator;
using Gehtsoft.Measurements;
```

The workflow is always: **(1)** describe the ammunition, rifle, and atmosphere → **(2)** compute
the sight (zero) angle → **(3)** call `Calculate` → **(4)** read the returned `TrajectoryPoint[]`.

---

## 1. Measurements (the value type used everywhere)

Every dimensional value is a `Measurement<TUnit>` struct — a `double` value plus a unit. You pick
the unit at construction and again when you read it; metric and US units interoperate freely.

```csharp
var d = new Measurement<DistanceUnit>(100, DistanceUnit.Yard); // value + unit
var d2 = new Measurement<DistanceUnit>("100yd");               // parse "<number><unit>"
double meters = d.In(DistanceUnit.Meter);                      // convert & read as double
double raw    = d.Value;                                       // value in its own unit (100)
DistanceUnit u = d.Unit;                                       // DistanceUnit.Yard
var zero = Measurement<DistanceUnit>.ZERO;                     // 0
double mm = Measurement<DistanceUnit>.Convert(1, DistanceUnit.Inch, DistanceUnit.Meter); // static double->double

var d3 = 100.As(DistanceUnit.Yard);                            // extension sugar: (double|int).As(unit)
var d4 = DistanceUnit.Yard.New(100);                           // unit-first sugar: unit.New(value)
```

Operators: `+` `-` (same unit), `*` `/` by a scalar, and comparisons. Nullable `Measurement<T>?`
marks optional inputs. The three construction forms above (`new`, `.As`, `.New`) are equivalent —
use whichever reads best.

**Unit enums** (use the exact member names):

| Enum | Members |
|------|---------|
| `DistanceUnit` | Millimeter, Centimeter, Meter, Kilometer, Inch, Foot, Yard, Mile, NauticalMile, Line, RussianLine, Point, Pica |
| `VelocityUnit` | MetersPerSecond, KilometersPerHour, FeetPerSecond, MilesPerHour, Knot |
| `AngularUnit` | Radian, Degree, MOA, Mil, MRad, Thousand, InchesPer100Yards, CmPer100Meters, Percent, Turn, Gradian |
| `WeightUnit` | Grain, Ounce, Gram, Pound, Kilogram, Neuton, Dram, TroyOz, Tonne, USTonne, UKTonne |
| `PressureUnit` | Pascal, KiloPascal, Bar, Millibar, Atmosphere, TechincalAtmosphere, MillimetersOfMercury, InchesOfMercury, PoundsPerSquareInch, MillimetersOfWater |
| `TemperatureUnit` | Fahrenheit, Celsius, Kelvin, Rankin, Reaumur, Delisle |
| `EnergyUnit` | FootPound, Joule, BTU, HpH, Wh |
| `DensityUnit` | GramPerCubicCentimeter, KilogramPerCubicMeter, PoundsPerCubicInch, OuncesPerCubicFeet, PoundsPerCubicFoot |

(`TechincalAtmosphere` is spelled that way in the library.)

---

## 2. Describing the shot (inputs)

### Ammunition
```csharp
public Ammunition(
    Measurement<WeightUnit> weight,
    BallisticCoefficient ballisticCoefficient,
    Measurement<VelocityUnit> muzzleVelocity,
    Measurement<DistanceUnit>? bulletDiameter = null,  // required only for spin drift
    Measurement<DistanceUnit>? bulletLength   = null)  // required only for spin drift
```
Properties: `Weight`, `BallisticCoefficient`, `MuzzleVelocity`, `BulletDiameter?`, `BulletLength?`,
`CustomTableFileName`. `double GetBallisticCoefficient()` returns the effective BC.

### BallisticCoefficient (struct) and drag tables
```csharp
new BallisticCoefficient(0.325, DragTableId.G7)                                   // a coefficient
new BallisticCoefficient(1.0, DragTableId.GC, BallisticCoefficientValueType.FormFactor)
new BallisticCoefficient("0.325G7")                                              // parse text form
```
- `DragTableId`: `G1, G2, G5, G6, G7, G8, GI, GS, RA4` (standard curves) and `GC` (custom — see §6).
- `BallisticCoefficientValueType`: `Coefficient` (a BC number) or `FormFactor`.
- Text form: `"0.325G7"`, or `"F1GC"` (leading `F` = form factor; last two chars = table id).

### Sight, Rifling, ZeroingParameters, Rifle
```csharp
new Sight(Measurement<DistanceUnit> sightHeight,
          Measurement<AngularUnit> verticalClick,
          Measurement<AngularUnit> horizontalClick)          // clicks may be Measurement<AngularUnit>.ZERO

new Rifling(Measurement<DistanceUnit> riflingStep, TwistDirection direction)  // step = distance per turn; TwistDirection.Left|Right

new ZeroingParameters(Measurement<DistanceUnit> distance, Ammunition ammunition, Atmosphere atmosphere)
// ammunition/atmosphere may be null; when set they override the shot's ammo/atmo FOR ZEROING ONLY.
// Optional property: VerticalOffset (Measurement<DistanceUnit>?, + is up) shifts the zero impact point.

new Rifle(Sight sight, ZeroingParameters zero, Rifling rifling = null)         // rifling optional
```

### Atmosphere (immutable; derived values computed in the constructor)
```csharp
new Atmosphere()                                              // sea level standard
new Atmosphere(Measurement<DistanceUnit> altitude,
               Measurement<PressureUnit> pressure,            // STATION pressure at that altitude
               Measurement<TemperatureUnit> temperature,
               double humidity)                               // humidity is a FRACTION 0..1, not %
new Atmosphere(Measurement<DistanceUnit> altitude,
               Measurement<PressureUnit> pressure,
               bool pressureAtSeaLevel,                       // true => pressure is sea-level-corrected
               Measurement<TemperatureUnit> temperature,
               double humidity)
Atmosphere.CreateICAOAtmosphere(Measurement<DistanceUnit> altitude, double humidity = 0)
```
Read-only properties: `Altitude`, `Pressure`, `Temperature`, `Humidity`, `SoundVelocity`, `Density`.
Static: `Atmosphere.StandardDensity`.

### Wind
```csharp
new Wind(Measurement<VelocityUnit> velocity,
         Measurement<AngularUnit> direction,
         Measurement<DistanceUnit>? maximumRange = null)
```
- **Direction:** 0° blows toward the target, 90° blows from the right (right-to-left across the
  path), 180° toward the shooter, 270°/−90° from the left.
- `Calculate` takes a `Wind[]`. Each wind applies out to its `MaximumRange`; the last one (or one
  with a null range) applies to the end. **Sort the array by ascending `MaximumRange`.**

### ShotParameters (output granularity + geometry)
```csharp
var shot = new ShotParameters
{
    Step            = new Measurement<DistanceUnit>(100, DistanceUnit.Yard),  // row spacing
    MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard), // extent
    SightAngle      = /* from TrajectoryCalculator.SightAngle — REQUIRED */,
    ShotAngle       = null,   // Measurement<AngularUnit>? line-of-sight incline, + up / - down
    CantAngle       = null,   // Measurement<AngularUnit>?
    BarrelAzimuth   = null,   // Measurement<AngularUnit>? firing bearing (0°=N, clockwise→E); Coriolis only
    Latitude        = null,   // Measurement<AngularUnit>? geographic latitude (N +, S −); enables Coriolis
};
```
- `Latitude` enables the Earth-rotation (Coriolis / Eötvös) deflection: `Windage` gains
  `−Ω·sinφ·Range·TOF` (right in N hemisphere), and drop is scaled by `1 − 2Ω·cosφ·sin(azimuth)·V₀/g`
  (East ⇒ less drop, West ⇒ more). `BarrelAzimuth` only orients this — it does not tilt the path.
  Coriolis is not applied during `SightAngle` (zeroing stays purely ballistic).

---

## 3. Running the calculation

```csharp
public class TrajectoryCalculator
{
    // Zero angle for the rifle's zero distance. Pass dragTable only when the BC uses table GC.
    Measurement<AngularUnit> SightAngle(Ammunition ammunition, Rifle rifle, Atmosphere atmosphere,
                                        DragTable dragTable = null,
                                        Measurement<DistanceUnit>? accuracy = null); // default 0.1 mm

    TrajectoryPoint[] Calculate(Ammunition ammunition, Rifle rifle, Atmosphere atmosphere,
                                ShotParameters shot, Wind[] wind = null, DragTable dragTable = null);

    Measurement<DistanceUnit> MaximumCalculationStepSize { get; set; } // default 10 cm
    static Measurement<DistanceUnit> MaximumDrop  { get; }             // 10000 ft — stop condition
    static Measurement<VelocityUnit> MinimumVelocity { get; }          // 50 ft/s — stop condition
}
```

Always compute `SightAngle` first and assign it to `ShotParameters.SightAngle` before `Calculate`.
`ShotAngle`, if set, is added to the barrel elevation inside `Calculate`.

The result array can be **shorter** than `MaximumDistance/Step + 1`: the run stops early if velocity
drops below 50 ft/s or drop exceeds 10000 ft. Iterate the returned array; don't assume its length.

---

## 4. Reading the output (`TrajectoryPoint`, read-only)

| Property | Type | Meaning |
|----------|------|---------|
| `Time` | `TimeSpan` | Time of flight to this point. |
| `Distance` | `Measurement<DistanceUnit>` | Distance along the line of sight. |
| `DistanceFlat` | `Measurement<DistanceUnit>` | Horizontal distance from the muzzle. |
| `Velocity` | `Measurement<VelocityUnit>` | Projectile speed. |
| `Mach` | `double` | Speed relative to the local speed of sound. |
| `Drop` | `Measurement<DistanceUnit>` | Vertical position vs the line of sight. At the muzzle = −sight height. |
| `DropFlat` | `Measurement<DistanceUnit>` | Vertical position vs the muzzle (bore line). |
| `Windage` | `Measurement<DistanceUnit>` | Horizontal deflection. **Left +, right −.** Includes spin drift when modelled. |
| `Energy` | `Measurement<EnergyUnit>` | Kinetic energy. |
| `DropAdjustment` | `Measurement<AngularUnit>` | Angular scope correction for the drop at this distance. |
| `WindageAdjustment` | `Measurement<AngularUnit>` | Angular correction for the windage at this distance. |
| `LineOfSightElevation` | `Measurement<DistanceUnit>` | Height of the sight line at this distance. |
| `LineOfDepartureElevation` | `Measurement<DistanceUnit>` | Height of the bore line at this distance. |
| `OptimalGameWeight` | `Measurement<WeightUnit>` | Litz optimal game weight estimate. |

```csharp
foreach (var p in trajectory)
    Console.WriteLine($"{p.Distance.In(DistanceUnit.Yard):N0} yd  " +
                      $"{p.Velocity.In(VelocityUnit.FeetPerSecond):N0} fps  " +
                      $"drop {p.Drop.In(DistanceUnit.Inch):N1} in  " +
                      $"wind {p.Windage.In(DistanceUnit.Inch):N1} in");
```

---

## 5. Complete examples

### Minimal
```csharp
var ammo = new Ammunition(
    weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
    ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
    muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond));

var rifle = new Rifle(
    sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch),
                     Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
    zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));

var atmosphere = new Atmosphere();
var calc = new TrajectoryCalculator();

var shot = new ShotParameters
{
    MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
    Step = new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
    SightAngle = calc.SightAngle(ammo, rifle, atmosphere),
};

TrajectoryPoint[] trajectory = calc.Calculate(ammo, rifle, atmosphere, shot);
```

### Non-standard atmosphere + wind + spin drift
Spin drift is modelled only when the rifle has `Rifling` **and** the ammunition has both a bullet
diameter and length; it is folded into `Windage` (there is no separate output).
```csharp
var ammo = new Ammunition(
    weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
    ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
    muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond),
    bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
    bulletLength: new Measurement<DistanceUnit>(1.2, DistanceUnit.Inch));

var rifle = new Rifle(
    sight: new Sight(new Measurement<DistanceUnit>(2.0, DistanceUnit.Inch),
                     Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
    zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null),
    rifling: new Rifling(new Measurement<DistanceUnit>(11.25, DistanceUnit.Inch), TwistDirection.Right));

var atmosphere = new Atmosphere(
    altitude: new Measurement<DistanceUnit>(5000, DistanceUnit.Foot),
    pressure: new Measurement<PressureUnit>(24.9, PressureUnit.InchesOfMercury),
    temperature: new Measurement<TemperatureUnit>(40, TemperatureUnit.Fahrenheit),
    humidity: 0.30);

var wind = new[]
{
    new Wind(new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour),
             new Measurement<AngularUnit>(90, AngularUnit.Degree),
             new Measurement<DistanceUnit>(500, DistanceUnit.Yard)),   // out to 500 yd
    new Wind(new Measurement<VelocityUnit>(5, VelocityUnit.MilesPerHour),
             new Measurement<AngularUnit>(45, AngularUnit.Degree)),    // beyond 500 yd
};

var calc = new TrajectoryCalculator();
var shot = new ShotParameters
{
    MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
    Step = new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
    SightAngle = calc.SightAngle(ammo, rifle, atmosphere),
};
var trajectory = calc.Calculate(ammo, rifle, atmosphere, shot, wind);
```

---

## 6. Custom drag curves

Standard `G1..RA4` curves are applied automatically when the BC names a standard table — nothing
extra to do. **If, and only if, the task involves a *non-standard* drag curve — a custom/measured
drag table, a radar `.drg` file, or a multi-BC (BC-vs-Mach) profile — read
[`references/custom-drag.md`](references/custom-drag.md) and follow it.** That file has the full,
copy-pasteable recipes for all three (`DragTable` subclass, `DrgDragTable.Open`, and
`DrgDragTableFactory.Build` with `BcAtMach`), including the exact signatures. Do not reconstruct
this API from memory or hand-roll a drag table — the helpers exist and the reference has them.

All three techniques use table id **`GC`** and require passing the `DragTable` instance to **both**
`SightAngle` and `Calculate` (`DragTable.Get(GC)` throws — you supply the instance).

---

## 7. Conventions & gotchas

- **Humidity is a fraction 0..1**, not a percentage (0.5 = 50 %).
- **Pressure** in the 4-argument `Atmosphere` constructor is the station pressure *at that altitude*;
  use the 5-argument overload with `pressureAtSeaLevel: true` to pass a sea-level value.
- **`SightAngle` must be computed and assigned to `ShotParameters.SightAngle`** before `Calculate`.
- **Windage sign:** left is positive, right is negative. **`Drop` at the muzzle = −sight height.**
- **Spin drift** needs `Rifling` **and** `BulletDiameter` **and** `BulletLength`; otherwise windage is
  wind-only. It is folded into `Windage` (no separate value). Right twist drifts right, left drifts left.
- **`GC` (custom) drag** requires passing the `DragTable` to **both** `SightAngle` and `Calculate`;
  `DragTable.Get(DragTableId.GC)` throws — you must supply the instance.
- The returned array may contain **fewer rows** than requested (subsonic/steep runs stop early).
- No aerodynamic (vertical wind) jump term is modelled; a pure crosswind affects windage, not drop.

---

## 8. Specialized topics (reference files)

These live in `references/` and load only when the task needs them — read the matching file and follow
it rather than reconstructing the API from memory:

- **Custom / non-standard drag curves** (custom `DragTable`, radar `.drg`, multi-BC synthesis) →
  [`references/custom-drag.md`](references/custom-drag.md) (see §6).
- **Serialization & persistence** — saving/loading `Ammunition`/`Rifle`/`Atmosphere`/libraries via BXml
  or `System.Text.Json`, embedding library objects in your own file format, and decorating your own
  classes for the BXml serializer → [`references/serialization.md`](references/serialization.md).
- **Reticles** — building a reticle definition in code and rendering it (e.g. to SVG), including
  bullet-drop-compensator markers → [`references/reticle.md`](references/reticle.md).
