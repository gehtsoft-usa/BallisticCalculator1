---
name: reticle-designer
description: >-
  Design a rifle-scope reticle from a plain-language description and emit it as a
  BallisticCalculator `.reticle` file (BXml) plus a rendered SVG preview. Covers the complete
  `.reticle` element/attribute specification, reticle design principles (mrad vs MOA subtension,
  FFP/SFP, stroke-weight hierarchy, hash-mark ladders, christmas-tree wind holds, clutter budget,
  BDC anchors) and worked, verified patterns for duplex, mil-dot, hash-ladder, christmas-tree,
  chevron, circle-dot, dense-grid and rangefinding-stadia reticles. Also covers reproducing an
  existing commercial reticle from its manufacturer's technical data sheet.
  Use this skill whenever the user wants to create, modify, review or render a scope reticle,
  crosshair, mil-dot / MOA grid, holdover ladder, christmas tree, BDC ladder or rangefinding
  stadia — including any mention of a `.reticle` file, `ReticleDefinition`, `ReticleDrawController`,
  `SvgCanvasFactory`, or "draw me a reticle", and including requests to clone or model a named
  commercial reticle (Horus H58/H59, TReMoR, Mil-C, MOAR, Tremor3, EBR, and the like). Trigger it
  even when the user only says something like "make a crosshair with 0.2 mil hashes out to 5 mils"
  or "turn this reticle picture into a file", and trigger it before hand-writing reticle XML or
  hand-rolling reticle geometry, because the file format silently ignores misspelled attributes and
  mis-cased booleans.
---

# Reticle designer

Turn a description of a reticle into a working `.reticle` file that the `BallisticCalculator`
library can load, draw and label with real trajectory data.

The job has three parts that are easy to conflate but should be kept separate:

1. **Design** — decide the geometry. This is where the domain knowledge lives: subtension, stroke
   weights, how much detail the eye can actually use. See `references/design-principles.md`.
2. **Encode** — write that geometry as BXml. Mechanical, but the format fails *silently*, so the
   details matter. Full spec in `references/file-format.md`.
3. **Verify** — render it and look at it. Never skip this; see *Check and preview* below.

## Coordinate system — get this right first

A reticle is a 2-D plane where **every coordinate is an angle**, not a length. That is what makes a
reticle reusable across distances: a mark 2 mrad below the crosshair is 2 mrad below at every range.

- **X** runs left → right. Negative is left of the aiming point, positive is right.
- **Y** runs bottom → **top**. Negative is *below* the aiming point — so holdover marks, which sit
  below the crosshair, have **negative Y**. This is the opposite of screen coordinates and is the
  most common sign mistake.
- The origin `(0,0)` is the **zero point** — where the scope is zeroed, i.e. the main aiming point.

Two header values place that plane inside the drawing:

- `size-x` / `size-y` — the field of view you want to draw, as a width and a height.
- `zero-x` / `zero-y` — where the origin sits, measured **from the top-left corner** (`zero-x` right
  from the left edge, `zero-y` **down** from the top edge). Not an offset from the centre.

So a centred crosshair is `zero = size / 2`. To leave more room below the aiming point for a
holdover ladder, make `zero-y` *smaller* than half the height:

```
size-y="20mrad" zero-y="7mrad"   ->  7 mrad visible above zero, 13 mrad below
```

Everything else in the file is drawn relative to that origin, in whatever angular unit you like —
units may be mixed freely within one file since each value carries its own suffix.

## Subtension cheat-sheet

Needed to translate a verbal brief ("holds a 10-inch target at 600 yards") into angles.

| | at 100 m | at 100 yd | in MOA | in mrad |
|---|---|---|---|---|
| 1 **mrad** (milliradian) | 10 cm | 3.60 in | 3.438 | 1 |
| 1 **MOA** | 2.908 cm | 1.047 in | 1 | 0.2909 |
| 1 **mil** *(this library's `mil`)* | 9.8175 cm | 3.534 in | 3.375 | 0.98175 |

Ranging: `range_m = target_mm / mrad`, `range_yd = target_in × 27.78 / mrad`,
`range_yd = target_in × 95.5 / MOA`.

> ⚠️ **`mil` is not a milliradian here.** `Gehtsoft.Measurements` defines `mil` as the military mil,
> 1/6400 of a circle — 1.86 % smaller than a milliradian. Everyone in the shooting world says "mil"
> and means *milliradian*, so **write `mrad`** for any mil-based reticle. Over a 10-mil ladder the
> difference is 0.19 mrad, roughly 7 cm at 400 m — a real miss. `mil` is the correct choice only if
> the user genuinely wants NATO mils (artillery).
>
> Real reticle documentation confirms which is meant. The Horus/Leupold H58 subtension sheet states
> `1.0 mil = 3.438 MOA = 3.600"` at 100 yd and "a circle = 6283 mils" — that is the milliradian,
> `mrad`. A manufacturer's spec sheet quoting 3.375 MOA or 6400 mils per circle would mean `mil`.
> When a source gives you MOA-per-mil or mils-per-circle, use it; do not guess from the word "mil".
>
> The library's own `MilDotReticle` and `data/reticle/mildot.reticle` had this bug and were corrected
> to `mrad` in 1.1.12 — see `BREAKING_CHANGES.md`. If a user's existing file uses `mil` to mean
> milliradian, swapping the suffix fixes it and the numbers need no recalculation.

## Workflow

### 1. Extract the brief

From the description, pin down: the unit system, the field of view, the central aiming feature, the
main stadia/posts, the hash-mark spacing and extent, any holdover ladder or wind grid, which marks
carry number labels, and the colours.

Fill gaps with the conventional defaults in `references/design-principles.md` rather than asking
about each one — a reticle brief is nearly always underspecified, and a sensible complete design
that the user can correct beats a questionnaire. Ask only when a choice changes the geometry and has
no reasonable default: most often *mrad or MOA?* when the user says "mil" ambiguously, and *what
ranges should the holdover marks correspond to?* if they want a BDC tied to a specific load.

**If the brief names an existing reticle** (H58, Mil-C, TReMoR, ATACR MOAR…), the geometry is a
research task before it is a design task, and `references/design-principles.md` has a section on
doing it properly. The two things that matter most: hunt down the **manufacturer's technical data
sheet** rather than working from review-site images — they are dimensioned drawings carrying exact
line widths, text heights and dot diameters, and you read them as *images* because the numbers are in
the drawing, not the text layer. And treat a picture as evidence about the *depiction*, not the
reticle: its surrounding circle is normally the ocular field of view rather than an etched element,
and its outermost numerals mark where that rendering was cropped, not how far the reticle extends.
An FFP reticle legitimately appears at a different extent at every magnification, so decide which
framing you are reproducing and put it in the reticle `name`.

Note what a reticle **cannot** know: distances. It stores angles only. A holdover mark is at
−4.2 mrad, not "at 500 yards" — the two are related only through a specific cartridge, muzzle
velocity and atmosphere. If the user asks for marks at named distances, you need a drop table: get
it from `TrajectoryCalculator` (the `ballistic-calculator` skill covers that API) or ask for one,
convert each drop to its angular hold, and record the ranges in `<bdc>` anchors so the app can label
them at draw time.

### 2. Write the geometry down before writing XML

Reticles are dense, repetitive and symmetric, which is exactly the shape of thing where a typo hides
well. List the marks as a table first — position, length, width, colour — and check it for symmetry
and even spacing. A wrong number is obvious in a 12-row table and invisible in 80 lines of XML.

### 3. Generate, don't type, anything repetitive

A ladder of 40 hash marks written by hand drifts: a `0.15` where every sibling has `0.2`, a missing
mark at 3.5. Write a throwaway Python script that loops over the mark positions and prints the
elements. It is faster, and the symmetry becomes structural instead of something you have to
proof-read.

### 4. Check and preview — always

```bash
python3 scripts/reticle.py check   my.reticle          # what the deserializer would silently drop
python3 scripts/reticle.py render  my.reticle -o my.svg # SVG + ASCII preview
```

`check` exists because **this format has no schema and fails silently**: a misspelled attribute is
ignored, `fill="True"` reads as *false*, and `line-style="dashed"` throws. It also projects where each
`<bdc>` anchor's distance label will land and reports what it collides with — those labels are created
at render time from a trajectory, so a clash is invisible in the file, in the preview and in a static
SVG, and giving an anchor the same offset as the row numeral beside it stacks the two exactly. `render` reproduces the
library's own renderer — same unit conversions, same single-precision arithmetic, same integer
flooring — closely enough that its SVG came out byte-identical to `SvgCanvasFactory`'s on all eleven
reticles it was tested against, so a preview that looks right is not hiding a rounding surprise.

Then **read the ASCII preview**, which is there so you can inspect geometry without an image viewer.
Lines are drawn as `.` under 0.1 mrad wide, `#` from 0.1 to 0.3, and `@` above — so the raster shows
the weight hierarchy, not just the positions. What to look for:

- Is it symmetric where it should be? Asymmetry is glaring in the raster and invisible in the source.
- Is the aiming point still clear, or has the centre filled in with marks?
- Is there a visible `@` → `#` → `.` gradient from the edge inward, or is everything one weight?
- Do the marks land at the positions in your table? Check a couple against the axis ruler.
- Is anything clipped at the edges, or crowded into a solid block of glyphs?

The raster cannot show colour, and one cell spans `size-x / cols` of angle, so features finer than
that merge — a solid-looking region means "too busy here", not necessarily a bug. Fix what the preview
shows, re-render, look again. Two or three rounds is normal.

### 5. Deliver

Give the user the `.reticle` file, the `.svg`, and a short **subtension table** saying what each mark
means — "each short hash = 0.2 mrad = 2 cm at 100 m" — plus the ranging formula for the unit used.
The table is what makes the reticle usable; a drawing on its own is not a tool.

## File skeleton

```xml
<reticle name="Example" size-x="12mrad" size-y="12mrad" zero-x="6mrad" zero-y="6mrad">
  <elements>
    <reticle-line start-x="-5mrad" start-y="0mrad" end-x="5mrad" end-y="0mrad"
                  line-width="0.05mrad" line-color="black" />
    <reticle-line start-x="0mrad" start-y="-5mrad" end-x="0mrad" end-y="5mrad"
                  line-width="0.05mrad" line-color="black" />
    <reticle-circle center-x="0mrad" center-y="0mrad" radius="0.1mrad"
                    fill="true" color="black" />
    <reticle-text position-x="0.5mrad" position-y="-2.15mrad" text-height="0.3mrad"
                  text="2" text-color="black" anchor="Left" />
  </elements>
  <bdc>
    <bdc position-x="0mrad" position-y="-2mrad" text-offset="0.6mrad" text-height="0.3mrad" />
  </bdc>
</reticle>
```

Elements, in one line each — full attribute lists, types and defaults are in
`references/file-format.md`:

| Element | Geometry | Notes |
|---|---|---|
| `reticle-line` | `start-x/y`, `end-x/y` | the workhorse: stadia, posts, every hash mark |
| `reticle-circle` | `center-x/y`, `radius` | floating dots (`fill="true"`), ranging rings |
| `reticle-rectangle` | `position-x/y` (top-left), `size-x/y` | grows right and **down** from position |
| `reticle-text` | `position-x/y` (baseline left), `text-height`, `text` | number labels |
| `reticle-path` | `<elements>` of move-to / line-to / arc | chevrons, tapered posts, arc segments |
| `bdc` | `position-x/y`, `text-offset`, `text-height` | **invisible anchor**, not a drawn mark |

Elements are drawn in document order, so later elements paint over earlier ones. That is also how you
**punch a hole** through a thick element — draw it continuous, then overpaint a `fill="true"` `white`
shape after it, rather than breaking it into segments around the gap. See `references/file-format.md`,
*Drawing order, and how to punch a hole*.

`<bdc>` points deserve emphasis because they surprise people: they draw *nothing*. They are anchors
that `ReticleDrawController.DrawBulletDropCompensator` labels with distances computed from a real
trajectory. The visible tick at that hold is a separate `reticle-line`. Put a `<bdc>` at each
holdover you want auto-labelled, and note the collection wrapper and its items share the tag name
`bdc`.

## The gotchas that bite every time

These all parse without complaint and produce a wrong drawing, which is why `check` looks for them:

- **The colour attribute has a different name on every element.** `reticle-line` uses
  `line-color`; `reticle-circle`, `reticle-rectangle` and `reticle-path` use `color`;
  `reticle-text` uses `text-color`. A `color` on a line is silently dropped.
- **Booleans must be lowercase `true`.** The deserializer evaluates `text == "true"`, so `True`,
  `TRUE`, `1` and `yes` all mean **false** with no error.
- **Enums are case-sensitive.** `line-style` is `Solid|Dashed|Dotted`, `anchor` is
  `Left|Right|Center` — `Enum.Parse` throws on `dashed`.
- **`zero-x`/`zero-y` are required in practice.** The attribute is optional and the XML docs promise
  the centre as a default, but `ReticleDrawController`'s constructor dereferences `Zero.X`/`Zero.Y`
  and throws `NullReferenceException`.
- **`reticle-text` needs an explicit `text-color`.** Every other element gets a `?? "black"`
  fallback in the draw controller; text does not, so the SVG ends up with an empty `fill`.
- **Y is up, holdovers are negative.**
- **Windage and reticle X have OPPOSITE signs.** `TrajectoryPoint.Windage` is positive to the **left**;
  reticle `X` is positive to the **right**. A drift-compensated hold mark is therefore at
  `x = -WindageAdjustment`, `y = DropAdjustment` — negate the windage, never the drop. Pass windage
  through unchanged and a BDC ladder leans the wrong way, which still looks like a plausible reticle.
  See the BDC-ladder section of `references/design-principles.md`.
- **`mil` ≠ `mrad`** (see above).
- **`radius` is a radius; spec sheets quote diameter.** A drawing saying `DOT = .05 MIL Ø` becomes
  `radius="0.025mrad"`. Copy the number across unchanged and every dot is twice the intended size.
- **A text position is an edge, not a centre — by default the LEFT edge.** `reticle-text` places
  itself at its `anchor` point, and the default `anchor="Left"` means the position is the text's left
  border with the glyphs running rightward from it. So putting a label to the *left* of something
  means allowing for the label's own width as well as the gap: budget about `0.6 × text-height` per
  character. The clean fix for static text is `anchor="Right"`, which makes the position the right
  border so the width looks after itself. **`<bdc>` labels get no such choice** — the labeller
  hardcodes the default anchor, so a negative `text-offset` must cover the whole label width *plus*
  the clearance you want.
- **Real strokes are finer than they look.** Dense grid reticles keep everything under ~0.06 mrad
  (the Horus H59 sheet specifies 0.014 / 0.028 / 0.039 / 0.055). Eyeballing weights from a printed
  diagram lands several times too heavy.

## Rendering the file

To turn a `.reticle` into an image from C#, aspect ratio is the one thing to get right — the
translator scales all lengths by the *horizontal* scale, so a canvas whose proportions differ from
the reticle's distorts every radius and stroke width:

```csharp
using var stream = File.OpenRead("my.reticle");
var reticle = stream.BallisticXmlDeserialize<ReticleDefinition>();

var canvas = SvgCanvasFactory.Create(reticle.Name, "500px", "500px",
    viewBoxWidth: 10000,
    YtoXratio: reticle.Size.Y.In(AngularUnit.Mil) / reticle.Size.X.In(AngularUnit.Mil));

var controller = new ReticleDrawController(reticle, canvas);
controller.DrawReticle();
string svg = SvgCanvasFactory.ToSvg(canvas);
```

Add `controller.DrawBulletDropCompensator(trajectory, zero, closeBdc: false, DistanceUnit.Yard,
"black")` **after** `DrawReticle()` to label the `<bdc>` anchors, and
`controller.DrawTarget(...)` **before** it to underlay a target. Use a trajectory step of 25 yd or
finer or the labels land on the wrong distance.

Building the same reticle in code instead of XML (`new ReticleDefinition { … }`, as
`MilDotReticle.cs` does) is the right choice when the geometry is computed from a trajectory at run
time. For a fixed design, prefer the file: it is inspectable, diffable, and needs no rebuild.

## Reference material

- `references/file-format.md` — the complete `.reticle` specification: every element, every
  attribute, types, defaults, value syntax, unit names, and the renderer's rounding behaviour.
  Read this when encoding anything beyond the skeleton above, especially paths and arcs.
- `references/design-principles.md` — how to make a reticle that works: subtension and unit choice,
  FFP vs SFP, stroke-weight hierarchy, the duplex principle, clutter budget, hash-ladder and
  christmas-tree conventions, centre-aiming-point options, labelling, colour. Read this at step 1,
  before choosing any numbers.
- `references/patterns.md` — worked geometries for the common reticle families (duplex, mil-dot, hash
  ladder, christmas tree, chevron, circle-dot, rangefinding stadia), the numbers behind each, and the
  generator idiom that produces them. Start here once you know which family the brief calls for.
- `assets/*.reticle` — those eight patterns as complete, verified files. Every one loads in the
  library and renders identically through `scripts/reticle.py` and `SvgCanvasFactory`, so they are
  safe to copy and adapt. Adapt rather than copy: re-derive the numbers for the user's unit system,
  field of view and load.
- `scripts/reticle.py` — the `check` / `render` / `preview` tool described above. Its SVG output is
  byte-identical to the library's for all eleven reticles it was tested against, so a preview that
  looks right is not hiding a rounding surprise.
