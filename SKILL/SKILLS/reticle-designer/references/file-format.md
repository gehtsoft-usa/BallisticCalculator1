# The `.reticle` file format — complete specification

A `.reticle` file is the BXml serialization of `BallisticCalculator.Reticle.Data.ReticleDefinition`.
BXml is the library's own attribute-driven XML serializer (`BallisticCalculator.Serialization`), not
`XmlSerializer` or `DataContractSerializer`, and its behaviour on unexpected input is what makes this
document worth reading: **it validates nothing**. Attributes it does not recognise are skipped;
values it cannot interpret become defaults. A file can be 100 % wrong and load without a single
exception.

Everything below is derived from the `[BXmlElement]` / `[BXmlProperty]` attributes on the model
classes and from `BallisticXmlDeserializer` / `BallisticXmlSerializer`.

## Contents

- [Document structure](#document-structure)
- [Value syntax](#value-syntax)
  - [Angular measurements](#angular-measurements)
  - [Booleans](#booleans)
  - [Enums](#enums)
  - [Strings and colours](#strings-and-colours)
- [The root element](#the-root-element-reticle)
- [Drawing elements](#drawing-elements)
  - [reticle-line](#reticle-line)
  - [reticle-circle](#reticle-circle)
  - [reticle-rectangle](#reticle-rectangle)
  - [reticle-text](#reticle-text)
  - [reticle-path](#reticle-path)
- [BDC anchors](#bdc-anchors)
- [Why the attribute names look inconsistent](#why-the-attribute-names-look-inconsistent)
- [How the renderer turns this into pixels](#how-the-renderer-turns-this-into-pixels)
- [Loading and saving](#loading-and-saving)
- [Failure modes, ranked by how often they happen](#failure-modes-ranked-by-how-often-they-happen)

## Document structure

```
<reticle>                            required root
  <elements>                         required, exactly one
    <reticle-line/>                  zero or more, in any order
    <reticle-circle/>
    <reticle-rectangle/>
    <reticle-text/>
    <reticle-path>
      <elements>                     required, exactly one
        <reticle-path-move-to/>       one or more, order is the drawing order
        <reticle-path-line-to/>
        <reticle-path-arc/>
      </elements>
    </reticle-path>
  </elements>
  <bdc>                              optional, at most one
    <bdc/>                           zero or more
  </bdc>
</reticle>
```

No XML namespace, no DOCTYPE, no processing instruction is required. The file the library writes has
no XML declaration either. UTF-8 throughout.

Document order inside `<elements>` is **z-order**: later elements paint over earlier ones. Put fills
and heavy posts first, fine lines and labels last.

## Value syntax

### Angular measurements

Every geometric value is a `Measurement<AngularUnit>`, serialized as **a number immediately followed
by a unit name**, parsed with invariant culture:

```
size-x="12mrad"    radius="0.1mrad"    start-y="-4.25moa"    line-width="0.02mrad"
```

- Decimal separator is always `.` regardless of locale.
- Exponent notation works (`1e-2mrad`) — the parser looks for the unit as a *suffix* precisely so
  that it does not cut an exponent in half.
- Whitespace between number and unit is tolerated (`12 mrad`) but not what the library writes; keep
  it out.
- The unit is per value, so units may be mixed freely in one file. `chevron.reticle` does exactly
  this: `mil` in the header, `moa` in the elements.
- Longest matching suffix wins, which is how `mrad` is not read as `rad`.

Unit names, exactly as accepted:

| Name | Unit | Definition |
|---|---|---|
| `mrad` | milliradian | 1/1000 radian — **this is what shooters mean by "mil"** |
| `moa` | minute of angle | 1/60 degree, 1/21600 circle |
| `mil` | military mil | **1/6400 circle** = 0.98175 mrad = 3.375 MOA |
| `rad` | radian | the base unit |
| `°` or `deg` | degree | 1/360 circle |
| `ths` | thousand | 1/3000 circle (Soviet/Finnish artillery) |
| `arcsec` | second of angle | 1/3600 degree |
| `turn` | full circle | |
| `gon` or `ᵍ` | gradian | 1/400 circle |
| `cm/100m` | centimetres per 100 m | non-linear: `atan(v/10000)` |
| `in/100yd` | inches per 100 yd | non-linear: `atan(v/3600)` |
| `%` or `percent` | incline percent | non-linear: `atan(v/100)` |

`Save` writes the **primary** name, which for degrees is the symbol `°` — so a file that round-trips
through the serializer may contain `4.5°` where you wrote `4.5deg`. Both read back identically.

> The `mil` / `mrad` distinction is the single most consequential detail in this format. See the
> warning in `SKILL.md`; use `mrad` unless NATO mils are genuinely wanted.

### Booleans

```
fill="true"      correct
fill="false"     correct
fill="True"      *** silently means FALSE ***
fill="1"         *** silently means FALSE ***
```

The deserializer's rule is literally `propertyText == "true"`. There is no error path. This is the
most damaging silent failure in the format because a `fill="True"` on a chevron produces an outline
where a solid shape was intended, and nothing anywhere reports a problem.

### Enums

Parsed with `Enum.Parse(type, text)`, which is **case-sensitive** and **throws** on a bad value — so
unlike booleans, a wrong enum is loud. Use the exact C# member spelling:

- `line-style`: `Solid` (default) | `Dashed` | `Dotted`
- `anchor`: `Left` (default) | `Right` | `Center` — note the American spelling

### Strings and colours

Plain XML attribute text; escape `&`, `<` and `"` as usual. Colours are **HTML/CSS colour names**
(`black`, `red`, `darkred`, `green`, `white`, …) passed through to the SVG `stroke`/`fill` attribute
untouched, so any CSS colour syntax an SVG consumer accepts will work — `#ff8800` and
`rgb(255,136,0)` included — at the cost of leaving the documented "html color name" contract.

## The root element `<reticle>`

| Attribute | Type | Required | Meaning |
|---|---|---|---|
| `name` | string | yes | reticle name; becomes the SVG `<title>` |
| `size-x` | measurement | yes | field-of-view width |
| `size-y` | measurement | yes | field-of-view height |
| `zero-x` | measurement | *see below* | origin offset from the **left** edge |
| `zero-y` | measurement | *see below* | origin offset **downward** from the **top** edge |

`zero-x`/`zero-y` are marked `Optional = true` and `ReticleDefinition`'s own XML documentation says
the centre is used when they are absent — but no code implements that default, and
`ReticleDrawController`'s constructor reads `reticle.Zero.X` directly. **A file without them
deserializes fine and then throws `NullReferenceException` the moment you try to draw it.** Always
write both; for a centred origin, half of `size`.

Because `zero` is measured from the top-left rather than as a signed offset from the centre, the
visible extent around the origin is:

```
x from -zero-x            to  size-x - zero-x
y from  zero-y - size-y   to  zero-y            (y is up, so zero-y is the TOP edge)
```

A holdover-heavy reticle therefore wants `zero-y` well under half of `size-y`.

## Drawing elements

All five carry an optional `line-width` (a `Measurement<AngularUnit>`, i.e. stroke width expressed as
an *angle*) and an optional colour whose attribute name differs per element. Omitting `line-width`
gives the thinnest line the canvas can draw.

### Drawing order, and how to punch a hole

Elements are emitted to the canvas in **document order**, so a later element paints over an earlier one.
There is no z-index, no grouping and no clipping — order is the only control you have, and it is
load-bearing.

That gives the only way to represent a **hole through a thick element**, which several real reticles have:
the oval cut-outs in the lower post of Trijicon's ACOG .308 crosshair, the open centre of a heavy chevron,
a gap in a filled ring. Draw the thick element as **one continuous shape**, then draw a **`fill="true"`
shape in `white` over it, after it**:

```xml
<!-- one continuous post ... -->
<reticle-line start-x="0moa" start-y="-14moa" end-x="0moa" end-y="-33moa"
              line-width="2.6moa" line-color="black" />
<!-- ... then the holes, painted over it -->
<reticle-circle center-x="0moa" center-y="-19.1moa" radius="1.1moa"
                fill="true" line-width="0.25moa" color="white" />
<reticle-circle center-x="0moa" center-y="-25.5moa" radius="1moa"
                fill="true" line-width="0.25moa" color="white" />
```

`white` is a real colour here, not a transparency — it is passed through to the SVG `fill` and covers what
is beneath it. Two things to get right:

- **Order.** Put the overpaint after the element it cuts into. A generator that emits all lines and then
  all circles happens to work; one that emits circles first silently loses the holes.
- **Do not fake it with gaps.** Breaking the thick element into segments either side of the hole looks
  similar in a preview but is wrong: it misrepresents the etching (the post really is continuous), the
  seams show at high magnification, and the "hole" becomes an absence of ink rather than a shape, so it
  reads differently against a light target.

The same idiom inverts: a white element under a black one is invisible, so overpainting is strictly a
foreground operation.

### `reticle-line`

Two endpoints. The workhorse element — stadia, posts, tick marks, ladder rungs.

| Attribute | Type | Required | Default |
|---|---|---|---|
| `start-x`, `start-y` | measurement | yes | |
| `end-x`, `end-y` | measurement | yes | |
| `line-width` | measurement | no | thinnest renderable |
| `line-color` | string | no | `black` |
| `line-style` | enum | no | `Solid` |

```xml
<reticle-line start-x="-5mrad" start-y="0mrad" end-x="5mrad" end-y="0mrad"
              line-width="0.04mrad" line-color="black" />
```

⚠️ The colour attribute is `line-color`, **not** `color`. This is the only element that spells it
that way, and getting it wrong costs you the colour silently.

### `reticle-circle`

| Attribute | Type | Required | Default |
|---|---|---|---|
| `center-x`, `center-y` | measurement | yes | |
| `radius` | measurement | yes | |
| `fill` | bool | no | `false` |
| `line-width` | measurement | no | thinnest renderable |
| `color` | string | no | `black` |
| `line-style` | enum | no | `Solid` |

```xml
<reticle-circle center-x="0mrad" center-y="0mrad" radius="0.1mrad" fill="true" color="black" />
```

A filled circle is drawn with both `fill` and `stroke` set to the colour, so its visual radius is
`radius + line-width/2`. For a precise floating dot, leave `line-width` off.

⚠️ `radius` is a **radius**. Manufacturer spec sheets quote dot sizes as **diameters** (`DOT = .04
MIL Ø`), so halve them: `radius="0.02mrad"`. This produces no error and no warning — just dots at
twice the intended size, which is easy to miss because they still look small.

### `reticle-rectangle`

| Attribute | Type | Required | Default |
|---|---|---|---|
| `position-x`, `position-y` | measurement | yes | **top-left** corner |
| `size-x`, `size-y` | measurement | yes | width and height, both positive |
| `fill` | bool | no | `false` |
| `line-width` | measurement | no | thinnest renderable |
| `color` | string | no | `black` |
| `line-style` | enum | no | `Solid` |

The rectangle extends **right and downward** from `position`, so its bottom edge is at
`position-y − size-y` in reticle coordinates. Negative sizes produce a degenerate rectangle.

A thin filled rectangle is often a better tick mark than a `reticle-line`, because its width is
exact rather than a stroke that straddles the centre line.

### `reticle-text`

| Attribute | Type | Required | Default |
|---|---|---|---|
| `position-x`, `position-y` | measurement | yes | text **baseline**, at the anchor point |
| `text-height` | measurement | yes | font size as an angle |
| `text` | string | yes | |
| `anchor` | enum | no | `Left` |
| `text-color` | string | no | **none — set it** |

```xml
<reticle-text position-x="0.6mrad" position-y="-2.15mrad" text-height="0.3mrad"
              text="2" text-color="black" anchor="Left" />
```

⚠️ **`text-color` has no working default.** `ReticleDrawController` passes `line.Color ?? "black"`
for a line but plain `text.Color` for text, so an absent colour reaches the SVG as an empty
`fill=""`. Always set it.

`anchor` controls which point `position` refers to: `Left` = start of the text (SVG `text-anchor:
start`), `Center` = centred, `Right` = end of the text.

⚠️ **The position is an edge, not a centre.** With the default `Left`, `position-x` is the text's
**left border** and the glyphs run rightward from it. So placing a label to the *left* of something
means allowing for the label's own width on top of the gap — roughly `0.6 × text-height` per
character, and a digit is no narrower than a letter in Verdana. Simply negating the x of a label that
worked on the right will push its left edge out but leave its right edge exactly where the collision
was. The fix is `anchor="Right"`, which makes `position-x` the right border so the width takes care of
itself; reach for it whenever a label sits left of what it annotates. Vertical centring is not provided: text sits on its
baseline, so to centre a label on a mark, drop the position by roughly `text-height / 2` — which is
exactly what the BDC labeller does (`y = position.Y − text-height/2`).

### `reticle-path`

An arbitrary open or closed outline. Use it for chevrons, tapered posts, arc segments and anything
that must be a solid shape.

| Attribute | Type | Required | Default |
|---|---|---|---|
| `fill` | bool | no | `false` |
| `line-width` | measurement | no | thinnest renderable |
| `color` | string | no | `black` |
| `line-style` | enum | no | `Solid` |

Segments live inside a **single `<elements>` child**:

```xml
<reticle-path fill="true" color="black">
  <elements>
    <reticle-path-move-to position-x="-1.4moa" position-y="-4.1moa" />
    <reticle-path-line-to position-x="-2.75moa" position-y="-4.1moa" />
    <reticle-path-line-to position-x="0moa" position-y="0moa" />
    <reticle-path-line-to position-x="2.75moa" position-y="-4.1moa" />
    <reticle-path-line-to position-x="1.4moa" position-y="-4.1moa" />
    <reticle-path-line-to position-x="0moa" position-y="-2.0moa" />
  </elements>
</reticle-path>
```

Segment types:

| Element | Attributes | Meaning |
|---|---|---|
| `reticle-path-move-to` | `position-x`, `position-y` | lift the pen and move |
| `reticle-path-line-to` | `position-x`, `position-y` | straight segment to the point |
| `reticle-path-arc` | `position-x`, `position-y`, `radius`, `clockwise`, `major-arc` | arc of the given radius ending at the point |

`reticle-path-arc` maps onto the SVG elliptical-arc command with equal radii. `position` is the arc's
**end** point, not its centre; the start is wherever the previous segment left off. Two circles of
`radius` pass through both points, and each offers a short and a long way round — `clockwise` picks
the sweep direction and `major-arc` picks the longer of the two arcs. Both are **required**, and both
are booleans, so the lowercase-`true` rule applies. If `radius` is smaller than half the distance
between the two points no such circle exists and SVG silently scales the radius up.

Filling behaviour worth knowing:

- When `fill="true"` the controller appends a close (`z`) for you, so the outline need not return to
  its start point — but it must be *closable* into a sensible shape.
- When `fill="true"` the path is drawn with **no stroke at all**; `line-width` and `line-style` are
  ignored. To get a filled shape with a visible outline of a different weight, draw the path twice.
- Drawing an unclosed path with `fill` through the canvas API directly throws
  `ArgumentException("Path cannot be filled if it is not closed")`; going through
  `ReticleDrawController` avoids this.

## BDC anchors

```xml
<bdc>
  <bdc position-x="0mrad" position-y="-2mrad" text-offset="0.6mrad" text-height="0.3mrad" />
  <bdc position-x="0mrad" position-y="-4mrad" text-offset="0.6mrad" text-height="0.3mrad" />
</bdc>
```

| Attribute | Type | Required | Meaning |
|---|---|---|---|
| `position-x`, `position-y` | measurement | yes | the hold point |
| `text-offset` | measurement | yes | label offset: **positive = right, negative = left** |
| `text-height` | measurement | yes | label font size |

Three things to know:

1. **They draw nothing.** A `<bdc>` point produces no mark. It is an anchor that
   `ReticleDrawController.DrawBulletDropCompensator(trajectory, …)` converts into a `reticle-text`
   holding a distance, computed by walking a real trajectory and finding where its drop crosses the
   anchor's `position-y`. The visible tick at that hold must be its own `reticle-line`.
2. **The collection wrapper and its items share the tag `bdc`.** `<bdc><bdc/></bdc>` is correct, not
   a mistake.
3. Only `position-y` is matched against the trajectory; `position-x` merely places the label
   horizontally. The trajectory step must be 25 yd or finer, because the labeller reports the
   distance of the *next* trajectory point after the crossing.

### Reserving room for the label

The label is real ink that nothing in this file accounts for, and it does not exist until someone
renders with a live trajectory — so an overlap is invisible in the file, in a preview, and in any
static SVG. Exactly where it lands:

- **x** starts at `position-x + text-offset` and grows **rightward**, whatever the sign of the
  offset, because the labeller constructs its `ReticleText` without setting `Anchor` and the default
  is `Left`. The offset positions the label's **left border**, so a negative offset does not mirror
  the text — it must be big enough to hold the whole label *and* the gap you want. This is the same
  trap as `anchor="Left"` on static text, except here you cannot switch to `anchor="Right"`, because
  the labeller does not expose it.
- **Leave a real margin, not a computed one.** Sizing the offset so the label clears by a fraction of
  a character is not a margin: glyph advance varies with the font the consumer actually renders with,
  a distance in yards has more digits than one in hundreds of metres, and a viewer may add padding.
  Half a character of clearance reads as an overlap in practice. Budget a whole character or more.
- **Verify by rendering with a trajectory.** This is the one part of a reticle that `check` and
  `preview` can only estimate, because the labels do not exist in the file. Call
  `DrawBulletDropCompensator` with a real trajectory, then read the `<text>` elements out of the
  resulting SVG and confirm where they landed. Arithmetic on paper is not the same evidence.
- **The label is the range of the next trajectory point after the crossing, not the crossing.** With
  a 20 m step, marks intended as 400/600/800 label as 420/620/820 — the error is up to one full step
  and always in the same direction, so a finer step reduces it but never centres it. Choose a step
  that divides the ranges you want printed.
- **y** is `position-y − text-height/2`, so the labeller centres the text on the hold for you.
- **width** is roughly `0.6 × text-height × digits`. Distance labels run to four digits, so budget
  about `2.4 × text-height` — a 0.4 mrad label needs nearly 1 mrad of clear width.

The usual mistake is giving a `<bdc>` the same `text-offset` as the drawn row numeral beside that
hold, which stacks the distance directly on top of the numeral. Put the drawn numerals on one side
and the BDC labels on the other, or push the labels clear of the widest element in the row.
`scripts/reticle.py check` projects every label's footprint and reports what it lands on.

`closeBdc` selects which side of the zero to label: `false` labels holds beyond the zero distance
(the normal holdover case), `true` labels nearer ones (hold-under). A `<bdc>` above the origin is
only meaningful with `closeBdc = true`.

## Why the attribute names look inconsistent

Because they come straight from the C# property names, and BXml has two habits worth internalising:

- **`FlattenChild`.** A `ReticlePosition` child property named `size` with sub-properties `x` and `y`
  is written as the flattened pair `size-x` / `size-y` on the parent element rather than as a nested
  `<position>` element. Every coordinate pair in the format works this way — hence `start-x`,
  `end-y`, `center-x`, `position-y`.
- **Per-property names.** The colour attribute is named independently on each class (`line-color`,
  `color`, `text-color`) because each class declares its own `[BXmlProperty(Name = …)]`. There is no
  shared base attribute to make them uniform, so the inconsistency is load-bearing: use the exact
  name for the element you are writing.

Quick reference for the colour attribute:

| Element | Colour attribute |
|---|---|
| `reticle-line` | `line-color` |
| `reticle-circle` | `color` |
| `reticle-rectangle` | `color` |
| `reticle-path` | `color` |
| `reticle-text` | `text-color` |

## How the renderer turns this into pixels

Relevant when a mark does not appear, appears the wrong size, or shifts by a pixel between runs.
`ReticleDrawController` + `CoordinateTranslator` + `SvgCanvas`:

1. Everything is converted to **`mil`** internally, whatever unit the file used.
2. `scaleX = viewBoxWidth / size-x`, `scaleY = viewBoxHeight / size-y`. A point maps to
   `x = (x + zero-x) × scaleX`, `y = (zero-y − y) × scaleY` — the Y flip that turns reticle-up into
   canvas-down.
3. **Lengths — radii, stroke widths, text heights — are always scaled by `scaleX`**, never `scaleY`.
   So if the canvas aspect ratio does not match `size-y / size-x`, every circle and stroke comes out
   distorted. Pass `YtoXratio: size-y / size-x` when creating the canvas.
4. All of this runs in **single-precision `float`**, and the canvas then **floors** coordinates to
   integers. That is why output can differ by one unit from a double-precision calculation, and why
   `scripts/reticle.py` deliberately reproduces the narrowing.
5. Clamping, which explains "my hairline is too thick" and "my tiny dot is too big":
   - stroke width is clamped to a minimum of 1 viewbox unit (for unfilled shapes),
   - a circle radius below 0.5 units is clamped to 1 unit,
   - at the default `viewBoxWidth` of 10000 over a 12 mrad field of view, one unit ≈ 0.0012 mrad,
     so anything finer than that renders as the minimum. Raise `viewBoxWidth` for more headroom.
6. `line-style` becomes `stroke-dasharray` scaled to the stroke width: dashed is `4w 3w`, dotted is
   `w 2w` plus `stroke-linecap="round"`. Both are computed from the *clamped* width.
7. Text is emitted as SVG `<text>` in `font-family="Verdana"` with `font-size` equal to the scaled
   `text-height`. Glyph widths are the font's, so a long label's horizontal extent is not something
   the reticle model knows about — check labels in the preview rather than trusting the numbers.

## Loading and saving

```csharp
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Serialization;

using var stream = File.OpenRead("my.reticle");
var reticle = stream.BallisticXmlDeserialize<ReticleDefinition>();

reticle.BallisticXmlSerialize("out.reticle");           // by file name
reticle.BallisticXmlSerialize(outputStream);            // or to a stream
```

Round-tripping is lossless for everything in this document, with two cosmetic changes: values are
rewritten in their own unit using the primary unit name (so `deg` becomes `°`), and formatting and
attribute order follow the serializer rather than your source. `ReticleDefinition` is also
`ICloneable`, deep-copying elements and BDC points, which is handy for generating variants.

The same classes carry `System.Text.Json` attributes, so a `ReticleDefinition` also serializes to
JSON — useful for embedding in an app's own config, though `.reticle` is the interchange format.

## Failure modes, ranked by how often they happen

`python3 scripts/reticle.py check <file>` detects every one of these. Run it before rendering.

| Symptom | Cause |
|---|---|
| Colour ignored, everything black | wrong colour attribute for the element |
| Outline where a solid shape was wanted | `fill="True"` — must be lowercase |
| `NullReferenceException` on draw | missing `zero-x` / `zero-y` |
| Text invisible or oddly coloured | missing `text-color` |
| Marks on the wrong side of the crosshair | forgot that +Y is **up**, so holdovers are negative |
| Everything 2 % off the intended subtension | wrote `mil` where `mrad` was meant |
| Circles are ellipses, strokes uneven | canvas aspect ratio ≠ `size-y / size-x` |
| A mark is missing entirely | outside the field of view, or below the 1-unit render floor |
| `Enum.Parse` exception on load | `line-style`/`anchor` in the wrong case |
| Attribute apparently has no effect | misspelled — BXml skips unknown attributes silently |
| BDC labels at wrong distances | trajectory step coarser than 25 yd |
| Nothing drawn for a path | segments not wrapped in `<elements>`, or no leading move-to |
