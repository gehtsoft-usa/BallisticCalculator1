# BallisticCalculator.Net

A lightweight LGPL .NET library that models projectile trajectories in the atmosphere
(air rifles, bows, firearms, artillery). NuGet package `BallisticCalculator`.
Sister ports exist in Go and Java. Status: ALPHA.

> **Work here without a global re-scan.** This file is the durable map of the engine and the
> `Gehtsoft.Measurements` library it is built on. The quick facts are up top; the reference
> sections (§2 onward) let you jump straight to the right source file:line instead of
> sweeping the tree. If behavior surprises you, re-check the cited file — treat this as a
> guide, not gospel.

## Method (the model in one paragraph)

A **3DOF (point-mass) model**. Origin: JBM ballistics v2 public C sources, ported to C#,
"deciphered" into readable physics, then optimized/extended with Litz's *Applied Ballistics*
ideas. Extensions over the original: a higher-accuracy drag curve (40+ approximation points
fitting per-node 2nd-degree polynomials, vs 5–6), NASA-based atmosphere (humid-air density,
barometric lapse), Litz spin-drift, and an adaptive integration step balancing speed vs
accuracy. The hot loop is **explicit Euler in raw doubles** (bypassing `Measurement<T>`).
Claimed accuracy: within ~0.5% / 0.2 MOA of modern calculators. Full algorithm: §5.

---

## 1. Solution layout & dependencies (excludes `Experiment-*` and `CLAUDE/`)

| Project | Path | TFM | Purpose |
|---|---|---|---|
| BallisticCalculator | `BallisticCalculator/` | netstandard2.0 | the engine (library) |
| BallisticCalculator.Test | `BallisticCalculator.Test/` | net8.0 | xUnit tests + reference-trajectory templates |
| BallisticCalculator.Debug | `BallisticCalculator.Debug/` | — | scratch/debug host |

Solution: `BallisticCalculator.sln`. Docs project + generated XML docs: `doc/`.
Public overview + risk notice: `README.md`.

Engine source, by concern (read the specific dir, not the whole tree):
- `BallisticCalculator/Calculations/` — `TrajectoryCalculator.cs` (integrator, `SightAngle`,
  `Calculate`), `ShotParameters.cs`, `TrajectoryPoint.cs`, `BallisticMath.cs`, `Vector.cs`.
- `BallisticCalculator/Data/` — `Ammunition`, `Atmosphere`, `Wind`, `Rifle`, `Sight`,
  `Rifling`, `ZeroingParameters`, `TwistDirection`, `AmmunitionLibraryEntry`.
- `BallisticCalculator/Drag/` — `BallisticCoefficient`, standard tables `G1..RA4`,
  `DragTable`/`DragTableNode` (interpolation), `DrgDragTable` (custom `.drg` radar curves).
- `BallisticCalculator/Serialization/` — custom `BXml` XML serialization (classes also carry
  `System.Text.Json` attributes, so they serialize to both XML and JSON).
- `BallisticCalculator/Reticle/` — reticle model. `Resources/Calibers.csv` — embedded caliber data.

Engine dependencies (`BallisticCalculator/BallisticCalculator.csproj`):
- **`Gehtsoft.Measurements` 1.1.16** — strongly-typed units (all physics quantities). See §2.
- `System.Runtime` 4.3.1.
- Embedded resource `Resources/Calibers.csv`.

Test stack: xUnit 2.9.3, **AwesomeAssertions** 9.3.0 (not FluentAssertions), Moq, NReco.Csv.
Canonical API usage: `BallisticCalculator.Test/Calculator/TrajectoryCalculatorTest.cs` (incl.
custom `DragTable` and `DrgDragTable.Open`); reference-template loading in `TableLoader.cs`.

## The `CLAUDE/` folder

`CLAUDE/` holds **current plans and their working data** — not part of the shipped project.
- `CLAUDE/PLAN0.md` — active plan (3DOF algorithm review vs the Hornady 4DOF reference set).
- `CLAUDE/IDEAS.md` — feature backlog.
- `CLAUDE/hornady/` — data for the current plan (Hornady 4DOF CSVs, `config.csv`, `DATA.md`).

---

## 2. Gehtsoft.Measurements essentials

Everything physical is a `Measurement<TUnit>` (a struct wrapping `double Value` + `TUnit Unit`).
- **Source** (sibling repo, read when this section isn't enough): `../Gehtsoft.Measurements/`.
- Compiled + XML docs: `~/.nuget/packages/gehtsoft.measurements/1.1.16/lib/netstandard2.0/`.

**Construction & access**
```csharp
new Measurement<DistanceUnit>(100, DistanceUnit.Yard)   // value + unit
100.As(DistanceUnit.Yard)                                // extension sugar
DistanceUnit.Yard.New(100)                               // unit-first sugar
m.Value                     // double, in its own unit
m.Unit                      // TUnit
m.In(DistanceUnit.Meter)    // double, converted to the requested unit
Measurement<DistanceUnit>.ZERO
Measurement<DistanceUnit>.Convert(1, DistanceUnit.Inch, DistanceUnit.Meter)  // static double->double
```

**Operators**: `+ -` (same unit), `* /` by scalar, comparisons. Nullable `Measurement<T>?` is
used for optional fields. For `Measurement<AngularUnit>`: instance `.Cos()` / `.Sin()` return `double`.

**`MeasurementMath`** (static helpers used by the engine):
`Atan(Measurement/Measurement)→Measurement<AngularUnit>`, `Tan/Cos/Sin(Measurement<AngularUnit>)→double`,
`Sqrt`, `Pow`, `Abs`, `Sign`, `KineticEnergy(weight, velocity)→Measurement<EnergyUnit>`,
`TravelTime`, `DistanceTraveled`, `Velocity`, `Pressure`, `Acos`, `Asin`.

**Unit enums (members that exist — use exact names):**
- `DistanceUnit`: Millimeter, Centimeter, Meter, Kilometer, Inch, Foot, Yard, Mile, NauticalMile, Line, RussianLine, Point, Pica.
- `VelocityUnit`: MetersPerSecond, KilometersPerHour, FeetPerSecond, MilesPerHour, Knot.
- `AngularUnit`: Radian, Degree, MOA, **MRad** (milliradian), Mil, Gradian, Turn, Thousand, Percent, **CmPer100Meters**, **InchesPer100Yards**.
- `WeightUnit`: Grain, Gram, Kilogram, Ounce, Pound, Dram, TroyOz, Tonne, USTonne, UKTonne, Neuton.
- `PressureUnit`: Pascal, KiloPascal, Bar, Atmosphere, **InchesOfMercury**, MillimetersOfMercury, PoundsPerSquareInch.
- `TemperatureUnit`: Celsius, Fahrenheit, Kelvin, Rankin, Reaumur, Delisle.
- `EnergyUnit`: Joule, **FootPound**, BTU, Wh, kWh, HpH.
- `DensityUnit`: KilogramPerCubicMeter, PoundsPerCubicFoot (used internally for air density).
- `AccelerationUnit`: EarthGravity, MeterPerSecondSquare (used for gravity conversion).

---

## 3. Data model (`BallisticCalculator/Data/`, `Drag/`, `Calculations/`)

### Ammunition (`Data/Ammunition.cs`)
```csharp
new Ammunition(Measurement<WeightUnit> weight,
               BallisticCoefficient ballisticCoefficient,
               Measurement<VelocityUnit> muzzleVelocity,
               Measurement<DistanceUnit>? bulletDiameter = null,   // required for drift
               Measurement<DistanceUnit>? bulletLength   = null)   // required for drift
```
- `BulletDiameter` **and** `BulletLength` are optional but **both required for spin-drift/gyro**.
- `GetBallisticCoefficient()` returns the BC directly when `ValueType==Coefficient`; when
  `FormFactor`, computes `weight_gr/7000 / diameter_in² / formFactor` (needs diameter & weight).

### BallisticCoefficient (`Drag/BallisticCoefficient.cs`) — a struct
```csharp
new BallisticCoefficient(0.325, DragTableId.G7)                              // coefficient
new BallisticCoefficient(1.0, DragTableId.GC, BallisticCoefficientValueType.FormFactor)
```
- Fields: `Value`, `Table` (`DragTableId`), `ValueType` (`Coefficient` | `FormFactor`).
- Text form parses/serializes like `"0.325G7"` or `"F1GC"` (leading `F` = form factor,
  last 2 chars = table id).

### Rifle / Sight / Rifling / ZeroingParameters
```csharp
new Rifle(Sight sight, ZeroingParameters zero, Rifling rifling = null)      // rifling optional
new Sight(Measurement<DistanceUnit> sightHeight,
          Measurement<AngularUnit> verticalClick, Measurement<AngularUnit> horizontalClick)
new Rifling(Measurement<DistanceUnit> riflingStep, TwistDirection direction) // step = dist/turn
new ZeroingParameters(Measurement<DistanceUnit> distance, Ammunition ammunition, Atmosphere atmosphere)
```
- `TwistDirection` (`Data/TwistDirection.cs`): `Left` (drifts left), `Right` (drifts right).
- `ZeroingParameters.VerticalOffset` (nullable) offsets the zero impact point (+ up).
- `Zero.Ammunition`/`Zero.Atmosphere` override the shot's ammo/atmo *for zeroing only* when set.

### Atmosphere (`Data/Atmosphere.cs`) — immutable, derived fields computed in ctor
```csharp
new Atmosphere()                                                   // sea level, 29.95 inHg, 15C, 78% RH
new Atmosphere(alt, pressure, temperature, humidity)               // pressure = station pressure AT altitude
new Atmosphere(alt, pressure, bool pressureAtSeaLevel, temperature, humidity)
Atmosphere.CreateICAOAtmosphere(altitude, humidity=0)              // ICAO standard, 29.92 inHg / 59F base
```
- **`humidity` is a fraction 0..1**, NOT percent (0.78 default). Divide percent inputs by 100.
- 4-arg ctor => `pressureAtSeaLevel=false` (the value is the actual pressure at that altitude).
  With `pressureAtSeaLevel=true` and altitude>0 it back-computes station pressure.
- Default pressure is **29.95** inHg (not 29.92); ICAO helper uses 29.92.
- Computes `Density` (kg/m³) and `SoundVelocity` (m/s) up front. Internal `AtAltitude(alt, out
  densityFactor, out mach)` gives density ratio vs `StandardDensity` (0.076474 lb/ft³) and local
  Mach speed — called along the trajectory as the bullet changes altitude.

### Wind (`Data/Wind.cs`)
```csharp
new Wind(Measurement<VelocityUnit> velocity, Measurement<AngularUnit> direction,
         Measurement<DistanceUnit>? maximumRange = null)
```
- **Direction convention**: 0° = toward target (tailwind→headwind axis), 90° = from the right,
  270°/−90° = from the left, 180° = toward shooter.
- `Calculate` takes `Wind[]`; multiple winds must be sorted ascending by `MaximumRange` (each
  applies out to its `MaximumRange`; last/`null` applies to the end).

### ShotParameters (`Calculations/ShotParameters.cs`)
```csharp
new ShotParameters {
    Step, MaximumDistance,               // output table granularity & extent
    SightAngle,                          // REQUIRED — get from TrajectoryCalculator.SightAngle
    ShotAngle = null,                    // line-of-sight incline, + up / - down
    CantAngle = null,
    BarrelAzimuth = null,                // compass bearing, 0°=N clockwise→E; scalar into Coriolis only
    Latitude = null                      // geographic latitude (N +, S −); null ⇒ no Earth rotation
}
```
- `BarrelAzimuth` **does not tilt the trajectory** (it did, buggily, pre-Coriolis) — the bullet is
  always integrated along the line of fire. It and `Latitude` only orient the Coriolis/Eötvös terms.
- `Latitude` set (with `BarrelAzimuth` optional) enables Coriolis (§5 Coriolis subsection). Both `null`
  and `Az ∈ {0, null}` are exact no-ops vs the pre-Coriolis engine.

### TrajectoryPoint (`Calculations/TrajectoryPoint.cs`) — one output row
Read-only properties: `Time` (TimeSpan), `Distance` (along LoS), `DistanceFlat`,
`Velocity`, `Mach`, `Drop` (vs line of sight), `DropFlat` (vs muzzle), `Windage`
(**left +, right −**; includes spin drift when drift is enabled and Coriolis when `Latitude` is set), `Energy`,
`LineOfSightElevation`, `LineOfDepartureElevation`, `DropAdjustment` (angular),
`WindageAdjustment` (angular), `OptimalGameWeight`. `Drop` at the muzzle = −sight height.

---

## 4. Drag tables (`BallisticCalculator/Drag/`)

- `DragTable` abstract base; concrete standard tables `G1,G2,G5,G6,G7,G8,GI,GS,RA4` (each a
  hardcoded `DragTableDataPoint[]` of `(Mach, DragCoefficient)`). Point counts e.g. **G1=81,
  G7=86** points, Mach 0→5. `DragTable.Get(DragTableId)` returns a lazily-cached singleton.
- **`GC` = custom**: `Get(GC)` throws; you must pass a `DragTable` instance directly to
  `SightAngle`/`Calculate`.
- **Interpolation**: the ctor fits, per interior point, a 2nd-degree polynomial over the three
  adjacent points → each `DragTableNode` stores coefficients `A,B,C` with
  `CalculateDrag(mach) = C + mach*(B + A*mach)` (`DragTableNode.cs`). `Find(mach)` is a binary
  search to the **nearest** node; the hot loop then walks `Previous` as velocity drops.
- **Custom radar/measured curves — `DrgDragTable`** (`Drag/DrgDragTable.cs`):
  `DrgDragTable.Open(stream|fileName)` parses `.drg` files. Format: header line
  `CFM|BRL, name, weight(kg), diameter(m), ...`; then point lines `"<Cd> <Mach>"`
  (whitespace-separated, Cd first). `TableId==GC`; exposes an `AmmunitionLibraryEntry`.

---

## 5. TrajectoryCalculator (`Calculations/TrajectoryCalculator.cs`)

Two public methods:

```csharp
Measurement<AngularUnit> SightAngle(Ammunition, Rifle, Atmosphere,
                                    DragTable dragTable = null,          // required if BC table == GC
                                    Measurement<DistanceUnit>? accuracy = null);   // default 0.1 mm

TrajectoryPoint[] Calculate(Ammunition, Rifle, Atmosphere, ShotParameters shot,
                            Wind[] wind = null, DragTable dragTable = null);
```

Tunable properties: `MaximumCalculationStepSize` (default **0.1 m**). Static:
`MaximumDrop` (**10000 ft** — stop), `MinimumVelocity` (**50 ft/s** — stop).

### Algorithm (both methods share the same integrator)
- **Explicit Euler** integration in raw doubles (the hot loop avoids `Measurement<T>` overhead).
  Unit conventions inside the loop: velocity in the muzzle-velocity's unit; position in meters;
  altitude in the atmosphere's unit; time truncated to `TimeSpan` tick precision.
- **Calculation step** (`GetCalculationStep`): output `Step` is halved, then if still larger
  than `MaximumCalculationStepSize` it is divided down by powers of ten (~1 cm actual step for a
  25 yd output step — accuracy is not integration-limited).
- **Drag** per step: `accel = PIR * (velUnit→fps) * (1/BC) * densityFactor * Cd(mach) * |v_air|`
  where **`PIR = 2.08551e-04 = (π/8)·(ρ0/144)`**, `v_air = v − wind`, `Cd` from the drag node.
  Velocity is decremented by `dt*drag*v_air`; then gravity `earthGravity*dt` on the vertical.
- **Mach** uses the *air-relative* speed; the drag node walks `Previous` as Mach falls.
- **Sound speed / density** are refreshed only when altitude changes by >1 m (perf guard).
- **`SightAngle`**: iterates (≤100 passes) adjusting barrel elevation until the trajectory
  crosses `Zero.Distance` within `accuracy` (default 0.1 mm), starting from a 150 MOA guess.
- **Termination**: velocity < 50 ft/s, drop below −10000 ft, or output array full. Steep-angle
  shots can end one output step early.

### Drift / spin drift (only if `Rifling != null && BulletDiameter != null && BulletLength != null`)
- **Miller stability** `Sg` (`CalculateStabilityCoefficient`):
  `sd = 30·w / (t²·d³·L·(1+L²))` with `t=twist/d`, `L=length/d`, `w` in grains, `d` in inches;
  `× fv = (mv/2800)^(1/3)` `× ftp = ((T°F+460)/519)·(29.92/P_inHg)`.
- **Spin drift** added to windage: `driftFactor · time^1.83 · dir · (inch→m)`,
  `driftFactor = 1.25·(Sg + 1.2)`, `dir = −1` for right twist, `+1` for left.
- ⚠️ Spin drift is **folded into `Windage`** — there is no separate spin-drift output.
- **No aerodynamic (crosswind) jump term** is modeled.

### Coriolis / Eötvös (only if `ShotParameters.Latitude != null`)
Earth-rotation deflection, applied as **per-output-point closed-form corrections** (not per-step
integration — TOF/velocity/range are untouched). Matches the field references (Ballistic Explorer +
Kestrel, which agree to ~0.4%); see `CLAUDE/CORIOLIS.md`. Constants `Ω = 7.2921159e-5 rad/s`,
`g = 9.80665 m/s²`. `φ = Latitude` (N +, S −), `AZ = BarrelAzimuth` (0° = N, clockwise → E, scalar).
- **Azimuth is decoupled from the trajectory** (`:261-263`): the bullet is always integrated along
  `x` (`vz = 0`); azimuth/latitude only feed the terms below. This also removed the old
  `BarrelAzimuth ≈ 90°` divide-by-zero. Inert (bit-identical) when `Latitude = null`, and for
  `Az ∈ {0, null}`.
- **Horizontal** (folded into `Windage`, alongside spin drift): `Δwindage = −Ω·sinφ·Range·TOF`
  (right in N hemisphere ⇒ negative in our left +/right − sign). Azimuth-independent.
- **Vertical Eötvös** (modelled as modified gravity `g_eff = g − 2Ω·cosφ·sinAZ·V₀`): scales the
  **gravitational fall below the vacuum bore line** by `vRatio = g_eff/g`, leaving launch/sight
  geometry (hence the exact −sightHeight muzzle drop) untouched. East ⇒ less drop, West ⇒ more;
  hemisphere-symmetric (`cos φ`). Applied to both `Drop` and `DropFlat`.
- **Not applied in `SightAngle`** (zeroing stays purely ballistic; effect at a 100 yd zero is
  <0.02 MOA). Accuracy vs BE: windage ~exact, drop within ~0.16% (drag-baseline-limited).

### Aerodynamic (crosswind) jump (only if `Rifling + BulletDiameter + BulletLength`, i.e. same gate as spin drift)
Vertical deflection from a **horizontal crosswind** on a spin-stabilized bullet — Litz *Applied
Ballistics* Eq 5.4 (`CLAUDE/AERO_JUMP.md`). A **constant angle at all ranges** (imparted at the muzzle),
folded into `Drop`/`DropFlat` (the vertical mirror of how spin drift folds into `Windage`):
- `Y[MOA per mph] = 0.01·Sg − 0.0024·L + 0.032`, `Sg` = Miller stability, `L` = bullet length in
  calibers (`BulletLength/BulletDiameter`). Both reuse the spin-drift intermediates — no new inputs.
- Applied as `drop += aeroJumpAngleRad · distance` (range-linear), after the Eötvös scaling. Muzzle
  crosswind only (first wind zone). **Up** for a wind from the right with a **right** twist; sign flips
  for left twist / wind from the left. Not applied in `SightAngle`.
- Validated against Hornady 4DOF: cuts the wind-case drop error ~0.83 MOA → ~0.11 MOA (residual is
  Eq 5.4 with our Miller `Sg` running ~14% above 4DOF's effective jump). Inclined-fire `cos` projection
  not yet applied (deferred).

### Wind vector (`WindVectorRaw`)
Decomposes wind into range/cross components using the shot's sight+shot angle and cant; a
crosswind with a shot angle produces a small vertical component.

---

## 6. Other physics constants & formulas
- Air density (`Atmosphere.CalculateDensity`): humid-air model via Herman Wobus saturated-vapor
  polynomial; dry const 287.058, vapor const 461.495 J/(kg·K).
- Sound velocity: `331.3·sqrt(T_K/273.15)` m/s (`Atmosphere.CalculateSoundVelocity`; corrected in `48d1209` from the old `331·sqrt(T_K/273)`).
- Pressure vs altitude: barometric with lapse `−0.0065 K/m`, `g=9.80665`, `M=0.0289644`,
  `R=8.31432`.
- `OptimalGameWeight = w_gr² · v_fps³ · 1.5e-12` (lb) (`BallisticMath.cs`).
- Angular adjustment: `CalculateAdjustment(linear, distance) = atan(linear/distance)`
  (0 when distance ≤ 0).

---

## 7. Gotchas checklist
- **Humidity is 0..1**, not percent.
- **Pressure**: the 4-arg `Atmosphere` ctor treats the value as *station pressure at altitude*.
- **Drift needs all three**: `Rifling` + `BulletDiameter` + `BulletLength`, else windage is
  wind-only and no spin drift / gyro.
- **Windage sign**: left positive, right negative. **Drop** at muzzle = −sight height.
- **`Windage` includes spin drift** when drift is on, **and Coriolis** when `Latitude` is set — not
  separable from the output alone.
- **Coriolis needs `Latitude`** (N +, S −); `BarrelAzimuth` (0° = N clockwise) no longer tilts the
  path — it only orients the Eötvös term. Coriolis is skipped in `SightAngle` by design.
- **Aerodynamic (crosswind) jump** rides on the **same gate as spin drift** (`Rifling` +
  `BulletDiameter` + `BulletLength`): with those set, a crosswind adds a vertical term to `Drop` too,
  not just windage. So enabling spin-drift inputs also changes `Drop` under wind.
- **`SightAngle` must be computed and put on `ShotParameters.SightAngle`** before `Calculate`;
  `ShotAngle` is *added* to barrel elevation inside `Calculate`.
- **`GC` custom BC** requires passing the `DragTable` to both `SightAngle` and `Calculate`.
- **50 ft/s floor & early stop**: subsonic long-range or steep-angle runs may return fewer rows
  than `MaximumDistance/Step + 1`; guard for trailing `null`s in the returned array.

---

## 8. Minimal usage

```csharp
var ammo = new Ammunition(
    weight:  new Measurement<WeightUnit>(220, WeightUnit.Grain),
    ballisticCoefficient: new BallisticCoefficient(0.325, DragTableId.G7),
    muzzleVelocity: new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond),
    bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
    bulletLength:   new Measurement<DistanceUnit>(1.630, DistanceUnit.Inch));

var rifle = new Rifle(
    sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch),
                     Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
    zero:  new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null),
    rifling: new Rifling(new Measurement<DistanceUnit>(7, DistanceUnit.Inch), TwistDirection.Right));

var atmo = new Atmosphere();                       // or (alt, pressure, tempF, humidity01)
var cal  = new TrajectoryCalculator();
var shot = new ShotParameters {
    Step = new Measurement<DistanceUnit>(25, DistanceUnit.Yard),
    MaximumDistance = new Measurement<DistanceUnit>(1500, DistanceUnit.Yard),
    SightAngle = cal.SightAngle(ammo, rifle, atmo),
};
TrajectoryPoint[] traj = cal.Calculate(ammo, rifle, atmo, shot,
    new[] { new Wind(new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour),
                     new Measurement<AngularUnit>(90, AngularUnit.Degree)) });
```
