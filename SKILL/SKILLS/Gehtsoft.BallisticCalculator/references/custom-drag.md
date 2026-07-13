# Custom drag curves

For anything beyond the standard `G1..RA4` curves — a measured/custom drag table, a radar `.drg`
file, or a multi-BC (BC-vs-Mach) profile. Standard curves come from `DragTable.Get(DragTableId.G7)`
and are applied automatically when the BC names a standard table; the techniques here all use table
id **`GC`** and require passing the `DragTable` instance to **both** `SightAngle` and `Calculate`
(there is no `DragTable.Get(GC)` — it throws).

## A. A custom drag table in code
```csharp
class MyDrag : DragTable
{
    public override DragTableId TableId => DragTableId.GC;
    private static readonly DragTableDataPoint[] Points =   // (Mach, drag coefficient), ascending Mach
    {
        new DragTableDataPoint(0.00, 0.180), new DragTableDataPoint(0.90, 0.177),
        new DragTableDataPoint(1.00, 0.427), new DragTableDataPoint(1.20, 0.429),
        new DragTableDataPoint(2.00, 0.339), new DragTableDataPoint(3.00, 0.250),
    };
    public MyDrag() : base(Points) { }
}

var table = new MyDrag();
var ammo  = new Ammunition(weight, new BallisticCoefficient(0.5, DragTableId.GC), muzzleVelocity);
shot.SightAngle = calc.SightAngle(ammo, rifle, atmosphere, table);
var traj = calc.Calculate(ammo, rifle, atmosphere, shot, null, table);
```

## B. A radar `.drg` file
```csharp
DrgDragTable table = DrgDragTable.Open("308-168gr.drg");   // also Open(Stream)
// use `table` exactly like the custom table above (pass to SightAngle and Calculate)
table.Save("copy.drg");                                    // also Save(Stream)
```

## C. Multi-BC → synthesized drag table (`DrgDragTableFactory`)
Turn a 2–3 point BC-vs-Mach profile (as published for many bullets) into a custom curve. It scales a
standard base curve by the BC at each Mach (piecewise-linear between knots, flat beyond the ends). Run
the result with a BC of **1.0** and table **`GC`**.

`Build` uses only its `baseTable` and `bcCurve` arguments to compute the curve; the
`AmmunitionLibraryEntry` is attached to the returned table as **metadata only** (name/source/caliber
etc.) and its numeric fields do *not* affect the drag curve. `baseTable` must be a standard curve
(passing `GC` throws), and every knot's BC must be positive.

`AmmunitionLibraryEntry` (properties): `Ammunition` (`Ammunition`), `Name`, `Source`, `Caliber`,
`AmmunitionType` (all `string`), `BarrelLength` (`Measurement<DistanceUnit>?`).
```csharp
var entry = new AmmunitionLibraryEntry
{
    Name = "220 gr .308",
    Ammunition = new Ammunition(
        weight: new Measurement<WeightUnit>(220, WeightUnit.Grain),
        ballisticCoefficient: new BallisticCoefficient(1.0, DragTableId.GC),
        muzzleVelocity: new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond),
        bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch)),
};

var curve = new[]                                       // Mach -> effective BC
{
    new BcAtMach(1.20, 0.307),
    new BcAtMach(1.65, 0.301),
    new BcAtMach(2.25, 0.318),
};

DrgDragTable table = DrgDragTableFactory.Build(entry, DragTableId.G7, curve);
// table.Save("220gr-308.drg");  // optional

var ammo = new Ammunition(
    weight: new Measurement<WeightUnit>(220, WeightUnit.Grain),
    ballisticCoefficient: new BallisticCoefficient(1.0, DragTableId.GC),
    muzzleVelocity: new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond));
shot.SightAngle = calc.SightAngle(ammo, rifle, atmosphere, table);
var traj = calc.Calculate(ammo, rifle, atmosphere, shot, null, table);
```

## Signatures
`DragTable.Get(DragTableId)`, `DrgDragTable.Open(string|Stream)`,
`DrgDragTable.Save(string|Stream)`, `DrgDragTableFactory.Build(AmmunitionLibraryEntry, DragTableId, IEnumerable<BcAtMach>)`,
`new BcAtMach(double mach, double bc)`, `new DragTableDataPoint(double mach, double dragCoefficient)`.
