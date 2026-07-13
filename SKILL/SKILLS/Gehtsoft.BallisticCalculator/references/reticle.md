# Reticle model — building and drawing

Define a scope reticle in code (or load one that was serialized) and render it to SVG. Two namespaces:
`BallisticCalculator.Reticle.Data` (the model) and `BallisticCalculator.Reticle.Draw` (rendering).
All coordinates are angular — `Measurement<AngularUnit>` (usually `Mil`). Built-in example:
`BallisticCalculator.Reticle.MilDotReticle`.

```csharp
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Reticle.Draw;
using Gehtsoft.Measurements;
```

## Coordinate system
A 2-D angular plane. **X** increases left→right (negative = left of zero); **Y** increases bottom→top
(negative = below zero). A reticle has a `Size` (field of view) and a `Zero` point (where the scope is
zeroed). At render time coordinates are converted internally to `Mil`, and the canvas Y axis is flipped
(reticle Y up → canvas Y down) for you.

---

## A. Building a reticle

`ReticleDefinition` (default ctor only) is the container:

| Member | Type | Notes |
|---|---|---|
| `Name` | `string` | |
| `Size` | `ReticlePosition` | field of view (width = X, height = Y) |
| `Zero` | `ReticlePosition` | zero point. **Set it** — the draw controller dereferences `Zero.X/.Y` and throws if null. |
| `Elements` | `ReticleElementsCollection` (read-only) | `.Add(...)` your shapes here |
| `BulletDropCompensator` | `ReticleBulletDropCompensatorPointCollection` (read-only) | hold-over markers |

`ReticlePosition`: `new ReticlePosition(double x, double y, AngularUnit unit)` (convenience) or
`new ReticlePosition(Measurement<AngularUnit> x, Measurement<AngularUnit> y)`; props `X`, `Y`.

**Elements** (all derive from `ReticleElement`; add via `reticle.Elements.Add(new …{ })`). Colors are
HTML color names (null → `"black"`); `LineWidth` is a nullable `Measurement<AngularUnit>` (null → thinnest).

- `ReticleLine` — `Start`, `End` (`ReticlePosition`), `LineWidth?`, `Color`.
- `ReticleCircle` — `Center`, `Radius` (`Measurement<AngularUnit>`), `Fill?` (`bool?`), `LineWidth?`, `Color`.
- `ReticleRectangle` — `TopLeft`, `Size` (width/height as X/Y), `Fill?`, `LineWidth?`, `Color`.
- `ReticleText` — `Position` (bottom-left), `TextHeight` (`Measurement<AngularUnit>`), `Text`, `Anchor?`
  (`TextAnchor.Left|Right|Center`), `Color`.
- `ReticlePath` — `Fill?`, `LineWidth?`, `Color`, and `Elements` (`ReticlePathElementsCollection`, read-only)
  of path segments:
  - `ReticlePathElementMoveTo { Position }`, `ReticlePathElementLineTo { Position }`,
  - `ReticlePathElementArc { Position, Radius, ClockwiseDirection (bool), MajorArc (bool) }`.

**Bullet-drop-compensator markers** describe hold-over points; the app labels them with distances at
render time (§B). `reticle.BulletDropCompensator.Add(new ReticleBulletDropCompensatorPoint {
Position, TextOffset (+right/−left), TextHeight })`.

```csharp
var reticle = new ReticleDefinition {
    Name = "Simple Crosshair",
    Size = new ReticlePosition(10, 10, AngularUnit.Mil),
    Zero = new ReticlePosition(5, 5, AngularUnit.Mil),      // required for drawing
};
reticle.Elements.Add(new ReticleLine {
    Start = new ReticlePosition(-5, 0, AngularUnit.Mil),
    End   = new ReticlePosition( 5, 0, AngularUnit.Mil),
    LineWidth = AngularUnit.Mil.New(0.02), Color = "black" });
reticle.Elements.Add(new ReticleLine {
    Start = new ReticlePosition(0, -5, AngularUnit.Mil),
    End   = new ReticlePosition(0,  5, AngularUnit.Mil),
    LineWidth = AngularUnit.Mil.New(0.02), Color = "black" });
reticle.Elements.Add(new ReticleCircle {
    Center = new ReticlePosition(0, 0, AngularUnit.Mil),
    Radius = AngularUnit.Mil.New(0.1), Fill = true, Color = "red" });
```

A filled `ReticlePath` must form a closed shape; the draw controller auto-calls `Close()` when
`Fill == true`, but an unclosed filled path drawn manually throws. `MilDotReticle.cs` is the canonical,
fuller worked example (crosshair circle, axis lines, tick dots, BDC points).

---

## B. Drawing an existing reticle → SVG

Get a canvas from the factory, drive it with `ReticleDrawController`, then read the SVG string.

```csharp
IReticleCanvas canvas = SvgCanvasFactory.Create(
    title: reticle.Name, width: "400px", height: "400px",
    viewBoxWidth: 10000,                                   // internal resolution; keep large for smooth output
    YtoXratio: reticle.Size.Y.In(AngularUnit.Mil) / reticle.Size.X.In(AngularUnit.Mil));

var controller = new ReticleDrawController(reticle, canvas);
controller.DrawReticle();                                  // draws every element

string svg = SvgCanvasFactory.ToSvg(canvas);               // full <svg>…</svg> document string
```

`ReticleDrawController(ReticleDefinition reticle, IReticleCanvas canvas)` then:
- `DrawReticle()` — render all elements (the main call).
- `DrawElement(ReticleElement)` — render one element (custom overlays).
- `DrawTarget(IEnumerable<TrajectoryPoint> trajectory, Measurement<DistanceUnit> targetSize,
  Measurement<DistanceUnit> targetDistance, string color)` — call **before** `DrawReticle()`.
- `DrawBulletDropCompensator(IEnumerable<TrajectoryPoint> trajectory, Measurement<DistanceUnit> zero,
  bool closeBdc, DistanceUnit units, string color)` — call **after** `DrawReticle()`; labels each BDC
  point the trajectory crosses (use a trajectory step ≤ 25 yd for precision).

Factory / canvas:
- `static IReticleCanvas SvgCanvasFactory.Create(string title, string width, string height,
  int viewBoxWidth = 10000, double YtoXratio = 1)` — `width`/`height` are CSS units (`"400px"`, `"5in"`).
- `static string SvgCanvasFactory.ToSvg(IReticleCanvas canvas)` — the SVG document as a string.
- `SvgCanvas` is `internal` — always go through the factory.

### Rendering to something other than SVG
`IReticleCanvas` is a plain interface; implement it to draw onto your own surface (WPF, GDI+, a bitmap):
`Clear()`, `Line/Circle/Rectangle/Text` primitives, and `CreatePath()`/`Path(...)` for paths
(`IReticleCanvasPath`: `MoveTo`, `LineTo`, `Arc(r, x, y, largeArc, clockwise)`, `Close`). Coordinates the
canvas receives are already translated to canvas pixels by the controller. Then drive it with the same
`ReticleDrawController`.
