# Analysis tools (`BallisticCalculator.Tools`)

Standalone helpers on top of the calculator. All live in namespace `BallisticCalculator.Tools`:

```csharp
using BallisticCalculator;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
```

The pure post-processing tools (`MovingTargetLead`, `PointBlankRange`) take an already-computed
`TrajectoryPoint[]` — you control the resolution through the trajectory's `Step`. `HitProbability`
drives the calculator itself. `RadarDragTableFactory` builds a drag table — see
[`custom-drag.md`](custom-drag.md) §D.

---

## BarrelTwist — gyroscopic stability and twist

Miller stability model. Stability needs the bullet weight, diameter, length and muzzle velocity on the
`Ammunition`; `atmosphere` defaults to sea-level standard.

```csharp
double sg = BarrelTwist.Stability(ammo, new Measurement<DistanceUnit>(8, DistanceUnit.Inch));     // Sg at that twist
Measurement<DistanceUnit> twist = BarrelTwist.RecommendedTwist(ammo, targetStability: 1.5);        // twist for a target Sg
TwistRecommendation r = BarrelTwist.Recommend(ammo);                                               // min/optimal/max twist + Sg
// r.MinimumTwist / OptimalTwist / MaximumTwist (Measurement<DistanceUnit>);
// r.MinimumStability / OptimalStability / MaximumStability (double).
```
Signatures:
`double Stability(Ammunition, Measurement<DistanceUnit> riflingStep, Atmosphere = null)`,
`Measurement<DistanceUnit> RecommendedTwist(Ammunition, double targetStability, Atmosphere = null)`,
`TwistRecommendation Recommend(Ammunition, Atmosphere = null, double minimumStability = 1.5, double optimalStability = 2.0, double maximumStability = 3.0)`.

---

## BallisticCoefficientConverter — BC between standard tables

Convert a BC from one standard table to another (e.g. G1 → G7). The conversion is velocity-aware, so
pass either a reference Mach or a reference velocity (the speed where the two curves are matched).

```csharp
var g1 = new BallisticCoefficient(0.5, DragTableId.G1);
BallisticCoefficient g7 = BallisticCoefficientConverter.Convert(g1, DragTableId.G7, referenceMach: 2.0);
BallisticCoefficient g7b = BallisticCoefficientConverter.Convert(
    g1, DragTableId.G7, new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond));
```
Signatures:
`BallisticCoefficient Convert(BallisticCoefficient source, DragTableId targetTable, double referenceMach)`,
`BallisticCoefficient Convert(BallisticCoefficient source, DragTableId targetTable, Measurement<VelocityUnit> referenceVelocity, Atmosphere = null)`.
The source table must not be `GC`.

---

## MovingTargetLead — aim-off for a moving target

Only the crossing component of the target's motion produces lead. `movingDirection` uses the same
convention as `Wind` (0° = along the line of sight, 90° = full crossing from the right). The result
follows the windage sign (positive = left). Convert with `.In(...)`.

```csharp
var p = trajectory.First(x => x.Distance.In(DistanceUnit.Yard) >= 300);   // the point at the target
var lead = MovingTargetLead.Lead(new Measurement<VelocityUnit>(8, VelocityUnit.MilesPerHour),
                                 new Measurement<AngularUnit>(90, AngularUnit.Degree), p);      // linear lead
var hold = MovingTargetLead.LeadAngle(new Measurement<VelocityUnit>(8, VelocityUnit.MilesPerHour),
                                      new Measurement<AngularUnit>(90, AngularUnit.Degree), p); // angular lead (MOA/Mil)
```
Signatures (linear `Measurement<DistanceUnit>`, angular `Measurement<AngularUnit>`):
`Lead(Measurement<VelocityUnit> targetSpeed, Measurement<AngularUnit> movingDirection, TimeSpan timeOfFlight)`,
`Lead(..., TrajectoryPoint point)`,
`LeadAngle(Measurement<VelocityUnit> targetSpeed, Measurement<AngularUnit> movingDirection, TimeSpan timeOfFlight, Measurement<DistanceUnit> range)`,
`LeadAngle(..., TrajectoryPoint point)`.

---

## PointBlankRange — maximum point-blank range / danger space

Post-processes a trajectory (computed with the intended zero) for the contiguous range span where the
path stays inside the vital zone, so a fixed hold still hits. Resolution = the trajectory's `Step`.

```csharp
PointBlankRangeResult pbr = PointBlankRange.Analyze(
    trajectory, new Measurement<DistanceUnit>(10, DistanceUnit.Inch), PointBlankAim.Center);
// pbr.MinimumRange, pbr.MaximumRange (max point-blank range), pbr.DangerSpace (= Max - Min),
// pbr.MaximumOrdinate + pbr.MaximumOrdinateRange, pbr.NearZero?, pbr.FarZero? (nullable).
```
- `PointBlankAim.Center` — line of sight through the zone center; corridor ±size/2 (optics).
- `PointBlankAim.Bottom` — line of sight at the bottom of the zone; corridor 0..+size (iron sights).
- Throws `InvalidOperationException` if the path never enters, or never leaves, the corridor (extend
  `MaximumDistance` or change the zone/vital size).

Signature: `PointBlankRangeResult Analyze(TrajectoryPoint[] trajectory, Measurement<DistanceUnit> vitalZoneSize, PointBlankAim aim = PointBlankAim.Center)`.

---

## HitProbability — Monte-Carlo hit probability / WEZ

Simulates shots against a circular target from the shooter's error budget, and returns the impact
cloud, the single-shot hit probability, and the shots needed for 50/75/90/95/98 % confidence.

```csharp
var parameters = new HitProbabilityParameters
{
    TargetSize = new Measurement<DistanceUnit>(20, DistanceUnit.Inch),  // diameter of the scoring circle
    MuzzleVelocityDeviationPercent = 5,                                 // MV standard deviation, %
    GroupSize = new Measurement<AngularUnit>(4, AngularUnit.MOA),       // per-axis sigma (best <=10-shot group)
    HorizontalPositionMultiplier = 1,   // 1 = supported; prone ~2, kneeling ~4, standing ~5
    VerticalPositionMultiplier   = 1,   // 1 = supported; prone ~2, kneeling ~3, standing ~4
    DistanceErrorPercent = 10,          // range estimation SD, %
    WindErrorPercent = 10,              // wind estimation SD, %
    Shots = 1000,                       // default 1000
    Seed = 20250723,                    // optional, for a reproducible cloud
};

// shot.MaximumDistance is the target range. wind is the TRUE wind (estimated with error); may be null.
HitProbabilityResult r = HitProbability.Estimate(calc, ammo, atmosphere, rifle, shot, wind, parameters);
// r.HitProbability (0..1); r.Shots (ShotImpact { Horizontal (+left), Vertical (+up) }, relative to aim);
// r.ShotsFor50Percent .. ShotsFor98Percent (int?, null when a hit is impossible).
```
- `GroupSize` is the per-axis standard deviation, taken directly from a best group of up to ~10 shots
  from a fully supported position (about 4 MOA ordinary, 1 MOA precision). The extreme spread of a large
  group is ~4× this. Shooting position widens it per axis via the two position multipliers (default 1).
- A hit is scored inside a circular zone of diameter `TargetSize`. The `ShotsForNN` values come from the
  single-shot `p` as the smallest `n` with `1 - (1-p)^n` at least the confidence.

Signature: `HitProbabilityResult Estimate(TrajectoryCalculator, Ammunition, Atmosphere, Rifle, ShotParameters shot, Wind[] wind, HitProbabilityParameters parameters, DragTable dragTable = null)`.

---

## RadarDragTableFactory — custom drag curve from radar velocities

Builds a `GC` drag table from `(distance, velocity)` radar pairs. See
[`custom-drag.md`](custom-drag.md) §D for the recipe and signature.
