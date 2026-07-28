# Reticle patterns — worked geometries

Eight complete, verified reticles covering the families most requests fall into. Each ships as a file
in `../assets/`, loads in the library, and renders identically through
`scripts/reticle.py render` and `SvgCanvasFactory`.

Use them as starting points, not templates to copy blindly: the right move is almost always to take
the family that matches the brief and re-derive its numbers for the user's unit system, field of view
and load. The rationale behind each number is in `design-principles.md`.

| File | Family | Field of view | Unit | Best for |
|---|---|---|---|---|
| `duplex.reticle` | Duplex | 20 × 20 | mrad | general hunting; the minimal correct reticle |
| `mildot-mrad.reticle` | Mil-dot | 12 × 12 | mrad | ranging and simple holds |
| `hash-ladder.reticle` | Hash ladder | 14 × 20 | mrad | precision holdover, the modern default |
| `hash-ladder-moa.reticle` | Hash ladder | 48 × 70 | moa | the same design in MOA |
| `christmas-tree.reticle` | Christmas tree | 16 × 22 | mrad | holding drop *and* wind without dialling |
| `chevron-mrad.reticle` | Chevron | 14 × 18 | mrad | fast field shooting, busy backgrounds |
| `circle-dot.reticle` | Circle-dot | 30 × 30 | mrad | close quarters, low magnification |
| `h58-style-grid.reticle` | Dense grid + wind dots | 21 × 21 | mrad | reproducing a commercial grid reticle |

Patterns 1–6 share one set of weights, so their numbers read consistently:

```
FINE  = 0.04 mrad   fine centre lines and every hash mark
MED   = 0.15 mrad   main stadia
HEAVY = 0.6  mrad   outer duplex posts
GAP   = 0.25 mrad   half-width of the clear centre gap
```

Pattern 7 deliberately does not: it uses the four weights a manufacturer's drawing actually specifies
(0.014 / 0.028 / 0.039 / 0.055 mrad), all far finer than the set above. That gap is informative — the
convenient round numbers here are a workable default, not what commercial reticles measure.

## Generate, don't type

Every pattern below except the duplex and circle-dot has more than twenty repeated elements. Writing
those by hand is how ladders end up with a `0.15` among the `0.2`s. Build a tiny emitter and loop:

```python
def line(x0, y0, x1, y1, w, u="mrad", color="black"):
    return (f'<reticle-line start-x="{x0}{u}" start-y="{y0}{u}" '
            f'end-x="{x1}{u}" end-y="{y1}{u}" '
            f'line-width="{w}{u}" line-color="{color}" />')

# a symmetric graduated axis in four lines, with the whole-mrad marks longer
out = []
for i in range(1, 14):
    x, half = i * 0.5, (0.5 if i % 2 == 0 else 0.22) / 2
    out += [line(s * x, -half, s * x, half, 0.04) for s in (-1, 1)]
```

The symmetry then comes from the loop instead of from proof-reading, and re-spacing the whole ladder
is a one-character edit.

## 1. Duplex

The minimal reticle that is actually good, and the pattern every other one builds on: heavy posts
from the edge of the field of view inward, stopping short of centre, where fine lines take over and
leave a clear gap at the aiming point. Nothing to count, nothing to measure — but instantly visible
in bad light and never covering the target.

| Feature | Value |
|---|---|
| Heavy posts | edge (±10 mrad) → ±4 mrad, 0.6 mrad wide |
| Fine lines | ±4 mrad → ±0.25 mrad, 0.04 mrad wide |
| Centre | open gap, 0.5 mrad across |

```xml
<reticle name="Duplex" size-x="20mrad" size-y="20mrad" zero-x="10mrad" zero-y="10mrad">
  <elements>
    <reticle-line start-x="-10mrad" start-y="0mrad" end-x="-4mrad" end-y="0mrad"
                  line-width="0.6mrad" line-color="black" />
    <reticle-line start-x="-4mrad" start-y="0mrad" end-x="-0.25mrad" end-y="0mrad"
                  line-width="0.04mrad" line-color="black" />
    <!-- and the same for +x, -y, +y -->
  </elements>
</reticle>
```

To turn it into a hunting reticle, add a single `<bdc>` anchor a couple of mrad down and a short tick
there; to make it a target reticle, drop the posts and extend the fine lines to the edge.

## 2. Mil-dot

The classic ranging reticle: dots at exactly 1 mrad along both axes, so a target spanning *n* dots is
`size_mm / n` metres away. Still the easiest reticle to teach.

| Feature | Value |
|---|---|
| Dots | ±1, ±2, ±3, ±4 mrad on both axes, radius 0.1 mrad, filled |
| Fine crosshair | ±5 mrad, 0.04 mrad wide |
| Heavy posts | ±5 → ±6 mrad, 0.35 mrad wide |
| FOV ring | radius 6 mrad, hairline |
| BDC anchors | −1 to −4 mrad |

```xml
<reticle-circle center-x="1mrad" center-y="0mrad" radius="0.1mrad" fill="true" color="black" />
```

Two notes. Genuine USMC mil-dots are *elongated* — roughly 0.2 mrad tall by 0.25 wide, so the gap
between them is the ranging unit rather than the dot; a thin filled `reticle-rectangle` reproduces
that better than a circle if fidelity matters. And this file is `mrad`, which is what a mil-dot
actually is: the library's `MilDotReticle` and `data/reticle/mildot.reticle` were built in `mil` and
were 1.86 % off until they were corrected in 1.1.12. See the unit warning in `SKILL.md`.

## 3. Hash ladder

The modern default for precision shooting: continuous 0.5 mrad graduations on both axes with
whole-mrad marks drawn longer, a floating dot at centre, and numeric labels every 2 mrad. Everything
is a hold *and* a measuring scale, and there is no load baked in.

| Feature | Value |
|---|---|
| Graduations | every 0.5 mrad, both axes |
| Whole-mrad ticks | 0.5 mrad long (0.25 each side) |
| Half-mrad ticks | 0.22 mrad long |
| Ladder extent | −12 mrad down, ±6.5 mrad across, +6 up |
| Centre | 0.08 mrad filled dot, stadia break at ±0.5 mrad |
| Labels | every 2 mrad, 0.3 mrad tall, offset right |
| BDC anchors | at each labelled mark |

The asymmetry is deliberate: `zero-y="6mrad"` against `size-y="20mrad"` gives 6 mrad above the
crosshair and 14 below, because bullets only ever drop. Progressive tick length is what makes the
ladder countable — see `design-principles.md`.

`hash-ladder-moa.reticle` is the same design re-derived on round MOA numbers (1 MOA graduations,
every fifth longer, labels every 5 MOA) rather than converted from the mrad version. Converting would
have produced graduations every 1.72 MOA, which no one can use.

## 4. Christmas tree

Adds wind holds to the ladder: horizontal rows of small ticks at each drop mark, widening with drop.
Lets a shooter hold drop and wind simultaneously and never touch a turret — at the cost of being the
busiest thing you can put in a scope.

| Feature | Value |
|---|---|
| Drop rows | every 1 mrad, −1 to −12 mrad |
| Wind columns | every 0.5 mrad |
| Row reach | `min(4.0, 0.5 + (drop − 2) × 0.35)` mrad each side, from −2 down |
| Wind ticks | 0.14 mrad tall, 0.04 wide — the smallest features in the design |
| Labels | every 2 mrad |

The taper is the whole point and it is physics, not styling: wind deflection grows faster with
distance than drop does, so the lower rows need more wind hold. It also makes each row visually
distinct so the eye lands on the right one.

```python
for i in range(1, 13):                       # drop rows, 1..12 mrad down
    y = -float(i)
    r.line(-0.3, y, 0.3, y, FINE)            # the drop mark itself
    if i >= 2:
        reach = min(4.0, 0.5 + (i - 2) * 0.35)
        k = 1
        while k * 0.5 <= reach + 1e-9:       # wind ticks, 0.5 mrad apart
            for s in (-1, 1):
                r.line(s * k * 0.5, y - 0.07, s * k * 0.5, y + 0.07, FINE)
            k += 1
```

Because the row spacing is drop and the column spacing is wind, the tree implicitly assumes a
velocity band. Say so in the reticle name.

## 5. Chevron

An inverted V whose apex is the aiming point. Precise at the tip while the mass of the shape stays
visible against clutter, which is why field and military reticles favour it. Drawn as a **filled
`reticle-path`** with a notch cut out so the apex stays clean.

Apex exactly at `(0,0)`, then down the right leg, back up the inner edge, and across — closing is
automatic when `fill="true"`:

```xml
<reticle-path fill="true" color="black">
  <elements>
    <reticle-path-move-to position-x="0mrad"     position-y="0mrad" />
    <reticle-path-line-to position-x="1.3mrad"   position-y="-1.6mrad" />
    <reticle-path-line-to position-x="0.85mrad"  position-y="-1.6mrad" />
    <reticle-path-line-to position-x="0mrad"     position-y="-0.55mrad" />
    <reticle-path-line-to position-x="-0.85mrad" position-y="-1.6mrad" />
    <reticle-path-line-to position-x="-1.3mrad"  position-y="-1.6mrad" />
  </elements>
</reticle-path>
```

| Feature | Value |
|---|---|
| Chevron | 2.6 mrad wide, 1.6 mrad deep, 0.45 mrad limb thickness |
| Stadia | ±1.5 → ±6.5 mrad, 0.15 mrad wide |
| Ladder | −2 to −11 mrad, starting clear of the chevron |

The stadia start at ±1.5 mrad and the ladder at −2 mrad so neither runs into the chevron. Remember
`fill="true"` means the path is drawn with **no stroke**: `line-width` on a filled path does nothing.
Compare `data/reticle/chevron.reticle` in the repository, which uses the same shape in MOA and fades
red → darkred → black down the ladder to encode range bands.

## 6. Circle-dot

For close range and low magnification: a heavy ring the eye centres on instantly, a fine dot for
precision when there is time, and three heavy posts. Deliberately carries no measuring function.

| Feature | Value |
|---|---|
| Ring | radius 4 mrad, 0.5 mrad stroke |
| Dot | radius 0.1 mrad, filled |
| Posts | edge → ±4.6 mrad, 0.9 mrad wide, left / right / bottom only |

Leaving the top post out keeps the upper field clear for a moving target, a common choice on
low-power variable optics. The wide 30 mrad field of view reflects a genuinely low-magnification
scope; drawing it in a 12 mrad box would misrepresent how much of the view the ring occupies.

Both features are legitimate aiming references, which is the one risk in this pattern: keep the ring
large enough (≥ 3 mrad) that no one mistakes it for the precision reference.

## 7. Dense grid with wind dots (H58-style)

What a commercial grid reticle actually looks like, built from a manufacturer's dimensioned drawing
rather than estimated. Worth studying for two reasons: it is the most demanding thing you will be
asked to reproduce, and its real dimensions are a corrective to intuition about stroke weight.

The distinguishing move versus pattern 4 is how it holds wind at long range. Instead of extending the
hash grid sideways — which fills the view with marks — the grid stays narrow and **isolated dots at
1 mrad intervals** carry the wind holds out to ±9 mrad. Same capability, far less clutter.

| Feature | Value |
|---|---|
| Grid rows | every 1 mrad, 1 → 11 mrad down, numerals every 2 |
| Hash subtensions | 0.2 mrad, large hash at each 1 mrad |
| Mark lengths | 0.40 mrad large, 0.15 mrad small |
| Wind dots | 0.04 mrad **diameter**, 1 mrad spacing, outside the hash grid |
| Main stadia | ±10.5 mrad, 0.5 mrad ticks, numerals each mrad |
| Rapid Range Bars | 1.25 mrad apart, 0.3 long, stepping 1.0 → 0.5 mrad |
| Landmark line | full-width at 10 mrad |
| Stroke hierarchy | 0.014 / 0.028 / 0.039 / 0.055 mrad |
| Text | 0.24 (bars) / 0.27 (stadia) / 0.40 (rows) mrad |

Three things this pattern teaches:

- **Four stroke weights, all under 0.06 mrad.** No posts, no heavy anything. At 20× a 0.055 mrad line
  already reads as bold. A grid reticle drawn with 0.15 mrad lines looks like a different product.
- **The full-width landmark line at 10 mrad** lets the eye find row 10 without counting ten rows — the
  "break the pattern" idea from `design-principles.md` applied to elevation. The real reticle has them
  at 10, 20 and 30; only the first falls inside a high-magnification framing.
- **The framing is part of the design.** This file reproduces the *high-magnification view*. The same
  reticle is etched far larger and appears at ±40 or ±60 mrad in low-power views. Frame it at ±17 and
  the grid shrinks into the middle and stops resembling the reference at all.

The per-row taper is the one invented number — no sheet publishes it — and it is marked as such where
it is defined. That is the habit to copy: label what you assumed so the user can correct it directly.

## 8. Rangefinding stadia

Not a separate file — a modification. A bracket sized to a specific target converts a subtension
reading into a range at a glance:

```xml
<reticle-line start-x="-2mrad" start-y="-6mrad" end-x="-2mrad" end-y="-6.4mrad"
              line-width="0.06mrad" line-color="black" />
<reticle-line start-x="2mrad"  start-y="-6mrad" end-x="2mrad"  end-y="-6.4mrad"
              line-width="0.06mrad" line-color="black" />
<reticle-text position-x="2.3mrad" position-y="-6.55mrad" text-height="0.3mrad"
              text="450/4" text-color="black" anchor="Left" />
```

Label it with the assumption — here a 450 mm target filling 4 mrad, so 112 m. A bracket whose
reference size is not stated is unusable, since the reader cannot recover the range from it.

For a general ranging capability, prefer fine graduations on the main axes (pattern 3) and hand the
user the formula: `range_m = target_mm / mrad`, `range_yd = target_in × 27.78 / mrad`,
`range_yd = target_in × 95.5 / MOA`.

## Composing a new design

Most briefs are one of these with adjustments. The order that works:

1. **Pick the family** from the table at the top, driven by what the scope is *for*.
2. **Set the frame** — unit, field of view, and `zero-y` well above centre if there is a ladder.
3. **Place the aiming point**, and only one.
4. **Draw the stadia and posts**, heavy outside and fine inside.
5. **Graduate the axes** on round numbers, with progressive tick lengths.
6. **Add wind holds** only if the user wants to hold rather than dial.
7. **Label sparingly**, then put a `<bdc>` anchor at every hold that should carry a distance.
8. **Check and preview**, and read the raster for symmetry, a clear centre, and crowding.

Then hand over the subtension table. A reticle without one is a picture.
