# Reticle design principles

How to choose the numbers. The file format will accept any geometry at all; this is about which
geometry makes a reticle a usable instrument rather than a drawing of one.

## Contents

- [The three jobs a reticle does](#the-three-jobs-a-reticle-does)
- [Choosing the unit system](#choosing-the-unit-system)
- [FFP or SFP](#ffp-or-sfp)
- [Reproducing a real reticle from pictures and spec sheets](#reproducing-a-real-reticle-from-pictures-and-spec-sheets)
- [Sizing the field of view](#sizing-the-field-of-view)
- [The aiming point](#the-aiming-point)
- [Stroke weight and the duplex principle](#stroke-weight-and-the-duplex-principle)
- [The clutter budget](#the-clutter-budget)
- [Hash-mark ladders](#hash-mark-ladders)
- [Wind holds and the christmas tree](#wind-holds-and-the-christmas-tree)
- [Labels](#labels)
- [Rangefinding stadia](#rangefinding-stadia)
- [BDC anchors and load-specific reticles](#bdc-anchors-and-load-specific-reticles)
- [Building a BDC ladder from a trajectory](#building-a-bdc-ladder-from-a-trajectory)
- [Colour and illumination](#colour-and-illumination)
- [Review checklist](#review-checklist)

## The three jobs a reticle does

Almost every design decision is a trade between them, so it pays to know which one the user cares
about before choosing any number:

1. **Aim** — put a precise, unambiguous point on the target. Wants a small, clean centre and nothing
   covering the point of impact.
2. **Hold** — offset for drop and wind without touching the turrets. Wants marks below and beside
   centre, enough of them, clearly countable under stress.
3. **Range and measure** — estimate distance or target size by subtension. Wants exact, evenly spaced
   graduations of known value.

Job 1 pushes toward emptiness, jobs 2 and 3 push toward marks. A design that refuses to choose ends
up a grey smear at high magnification and unusable at low. When a brief is vague, ask what the scope
is *for* — a benchrest target reticle and a field-shooting holdover reticle are near-opposites.

## Choosing the unit system

**Match the reticle to the turrets.** A mrad reticle on MOA turrets means converting under time
pressure, which is how people miss. If the user mentions turret click values (0.1 mrad, ¼ MOA), the
reticle unit follows from that. If they mention nothing, `mrad` is the modern default outside the US
hunting market.

- **mrad** — decimal and self-consistent: 1 mrad = 10 cm at 100 m, so ranging is
  `range_m = size_mm / mrad` with no constant to remember. Pairs with 0.1 mrad turrets, where one
  reticle mrad = 10 clicks.
- **MOA** — finer per unit (1 MOA ≈ 1 in at 100 yd) and natural if the user thinks in inches and
  yards. Pairs with ¼ MOA turrets, where 1 MOA = 4 clicks.

Write `mrad`, not `mil` — in this library `mil` is the 1/6400-circle military mil, 1.86 % off. See
the warning in `SKILL.md`.

Pick round graduations in the chosen unit. Marks at 0.2/0.5/1.0 mrad are usable; marks at 0.34 mrad
because they were converted from an MOA design are not — they are arithmetic homework in the field.
If a design must be ported between unit systems, **re-derive it on round numbers in the new unit**
rather than converting the old spacing.

## FFP or SFP

Where the reticle sits in the optical path decides whether its subtensions are honest.

- **First focal plane** — the reticle scales with the image, so every subtension is valid at every
  magnification. Holds and ranging work throughout the zoom range. This is where detailed hash
  ladders and christmas trees belong. The cost is that at minimum magnification the whole reticle
  shrinks and fine detail disappears, so keep the *fine* features from being the only thing
  distinguishing important marks.
- **Second focal plane** — the reticle stays a constant apparent size while the image zooms, so
  subtensions are correct at exactly one magnification, conventionally the maximum. The reticle stays
  legible at low power, which is why hunting scopes use it. Keep SFP designs **simpler**: the user
  who dials down to 4× loses the measuring function anyway, and a dense grid then buys nothing but
  clutter.

The `.reticle` model stores angles and knows nothing about focal planes, so this affects your choices
rather than the file. Two practical consequences: put the design magnification in the reticle `name`
for an SFP design (`"Hunter BDC (at 12x)"`), and if the user says "it goes blurry/tiny when I zoom
out", they have an FFP scope and want fewer, heavier marks.

## Reproducing a real reticle from pictures and spec sheets

When the brief names an existing reticle, most of the work is reading its geometry off published
material. There is one trap here that is easy to fall into twice in the same task, so it is worth
naming:

> **A picture tells you about the depiction, not about the reticle.** Its boundary, its crop and its
> apparent density are properties of how someone chose to draw it.

Two concrete forms:

- **The circle is usually not a reticle element.** Nearly every published reticle image is drawn
  inside a circle representing the *ocular field of view* — what you see through the tube. It is
  almost never etched on the glass. The tell is simple: if the stadia lines run through the circle and
  keep going, or numerals sit outside it, the circle is the frame, not part of the drawing.
- **The outermost numerals are not the reticle's extent.** They are where that particular rendering
  was cropped. This is structural for an FFP reticle, because the same reticle genuinely occupies a
  different angular extent at every magnification. The Horus H59 data sheet demonstrates it by drawing
  one reticle three times: ±10 mrad in its "high magnification view", ±40 in its illumination diagram,
  and ±60 in its "low magnification view". All three are correct. A reviewer's image showing ±17 is
  simply a fourth framing.

So decide explicitly *which framing you are reproducing* — usually the high-magnification view, since
that is what the user sees at top power and what most published pictures show — and say so in the
reticle `name`.

**Go looking for the manufacturer's technical data sheet.** Search for `<reticle name> technical data
sheet`; Horus, Nightforce, Vortex and others publish fully dimensioned engineering drawings as PDFs.
These are transformative compared with review-site images: they carry exact line widths, text heights,
dot diameters and mark spacings as callouts. Two practical notes — the PDF text layer is usually
useless because the numbers live in the drawing, so **read the pages as images**; and manufacturer URL
patterns are inconsistent, so if one vendor's copy 404s, try another vendor hosting the same sheet.

Finally, **mark every number you had to invent.** Real sheets leave gaps — per-row taper widths are
commonly absent — and a reconstruction where the assumed values are labelled `ASSUMED` in the
generator is one the user can correct in seconds. One where they are mixed in with sourced numbers is
one they have to re-derive from scratch.

## Sizing the field of view

`size-x` / `size-y` is the extent you draw, not a property of the scope. Two sensible strategies:

- **Draw the useful area.** Make the field of view a little larger than the outermost mark, so the
  drawing is dense with information — good for a documentation image or a reticle picker.
- **Draw the scope's real field of view** at the design magnification, so the preview shows the
  clutter as the shooter will actually see it. Typical scope fields of view run about 10–12 mrad at
  25×, 30–40 mrad at 8×, and more at lower powers. This is the honest way to judge whether a design
  is too busy.

Symmetric horizontally (`zero-x = size-x / 2`) is nearly always right. Vertically, holdover reticles
need far more room below than above: `zero-y ≈ size-y / 3` is a reasonable start, and remember
`zero-y` is measured **down from the top edge**, so a smaller value means more room below.

**Frame tight enough that the design fills the view.** Too wide a field of view is not a neutral
choice: it shrinks the marks into the middle and the reticle reads as sparse and wrong even when every
subtension is correct. If a holdover grid spans ±3 mrad, framing ±17 leaves it occupying a sixth of
the width and looking nothing like the reference. When reproducing an existing reticle, match the
framing of the picture the user will compare against.

## The aiming point

The centre is where precision is won or lost, because whatever you draw there covers the target. The
usual options, roughly from most to least precise:

- **Open centre (floating crosshair)** — the fine lines stop 0.2–0.6 mrad short of the middle,
  leaving a clean gap. Nothing obscures the aiming point; the eye centres the gap accurately. The
  standard choice for precision work.
- **Floating dot** — a small filled circle at the origin. Fast to pick up, still fine enough for most
  work. Real ones are far smaller than they look in a printed diagram: the Horus H59 centre dot is
  **0.05 mrad in diameter**, and its holdover dots are 0.04 mrad. Treat 0.05–0.15 mrad *diameter* as
  the usable band; beyond ~0.2 mrad the dot starts swallowing small targets, since 0.2 mrad covers
  20 cm at 1000 m.

  ⚠️ **`reticle-circle` takes a radius, and spec sheets quote diameter.** A sheet saying
  `DOT = .05 MIL Ø` means `radius="0.025mrad"`. Copying the number straight across makes every dot
  twice the intended size — and it still renders, so nothing complains.
- **Chevron / triangle** — the apex is the aiming point, and it is precise while the mass of the
  shape below stays visible against a busy background. Popular for field and military reticles. Draw
  it as a filled `reticle-path`, apex exactly at `(0,0)`.
- **Plain crossing fine lines** — simple, but the intersection is thicker than either line and hides
  the impact point. Acceptable at low magnification, weak for precision.

Do not combine them. A dot inside a small circle inside a chevron is three aiming points and
therefore none.

## Stroke weight and the duplex principle

Weight is the main tool for visual hierarchy — it is what lets the eye find the centre instantly on a
reticle carrying fifty marks.

There is a hard lower bound worth knowing: human acuity is roughly 0.3 mrad (about 1 arcminute), and
a scope multiplies apparent angle by its magnification, so **the finest feature that can be resolved
is about `0.3 / magnification` mrad**. At 10× that is 0.03 mrad; at 25×, 0.012 mrad. Anything finer
is invisible no matter what the file says — and on an FFP reticle at *minimum* magnification the
whole reticle shrinks, so check the low end.

Typical weights, as a starting point — with the four widths the Horus H59 technical data sheet
actually specifies, since a real dimensioned drawing beats a rule of thumb:

| Feature | Width | H59 sheet |
|---|---|---|
| Finest detail (spine hashes) | 0.01–0.02 mrad | **0.014 mrad** |
| Hash marks, ladder rungs, grid rows | 0.02–0.05 mrad | **0.028 mrad** |
| Landmark / emphasis lines | 0.04–0.08 mrad | **0.039 mrad** |
| Main crosshair | 0.05–0.2 mrad | **0.055 mrad** |
| Heavy outer duplex posts | 0.4–2.0 mrad | *(none — grid reticles omit posts)* |

Note how fine those are. A dense grid reticle keeps **everything** under 0.06 mrad, because at 20× a
0.055 mrad line already appears as ~1.1 mrad and reads as bold. Intuition drawn from looking at
printed reticle diagrams runs several times too heavy; if a design looks right on screen at
100 mrad-wide framing it is almost certainly too thick.

The **duplex principle** is the oldest and most reliable pattern in reticle design: heavy posts run
inward from the edge of the field of view and stop several mrad short of centre, where fine lines take
over. The heavy bars are found instantly in poor light and at low magnification and funnel the eye
inward; the fine centre keeps the precision. `mildot.reticle` does this — 0.2 mil posts from the edge
to 5 mil, then 0.01 mil lines to the centre.

Keep the number of distinct weights small: three or four across the whole reticle. Weight is a signal,
and a design with eight of them communicates nothing.

## The clutter budget

Every mark costs visibility. A reticle can hold only so much before it stops being readable, and the
limit is lower than it looks in a large drawing.

- **Minimum spacing.** Do not place marks closer than about 0.2 mrad apart in any area the shooter
  actually uses. Below that they merge into a dashed grey line and cannot be counted.
- **Keep the centre empty.** Reserve a radius of roughly 0.5–1 mrad around the origin for the aiming
  point alone. Marks there cover the target at exactly the moment precision matters.
- **Label sparingly.** See below — numbers cost several times more visual space than the ticks.
- **Count the marks.** Under stress a shooter counts ticks. Beyond about five in a row without a
  distinguishing longer or labelled mark, counting fails — which is why ladders use progressive tick
  lengths.
- **Prefer resolution where it is used.** Fine 0.1 mrad graduations are worth having near the centre,
  where small corrections happen; out at 10 mrad of drop, 1 mrad steps are plenty because the wind
  call is coarser than that anyway.

The preview from `scripts/reticle.py` is the check that matters here. If the ASCII raster looks solid
in a region, the real reticle is too busy there.

## Hash-mark ladders

The standard way to graduate an axis. Encode the scale in **tick length**, so it can be read without
labels:

| Graduation | Tick length (each side of the axis) |
|---|---|
| Major (whole mrad) | 0.4–0.6 mrad |
| Minor (half) | 0.2–0.3 mrad |
| Micro (0.2 or 0.1) | 0.1–0.15 mrad |

Conventions worth following because they match what shooters expect:

- **Horizontal axis: symmetric.** Wind comes from both sides. Same ticks left and right.
- **Vertical axis: asymmetric.** Bullets drop. Typically a short ladder above centre (0–2 mrad, for
  hold-under at close range) and a long one below (out to 10–20 mrad).
- **Full ticks or half ticks.** Marks crossing the axis symmetrically read as a scale; marks on one
  side only read as a direction. Half-ticks below the horizontal line only are a common way to keep
  the top of the field clean.
- **Break the pattern at intervals of five.** Every fifth mark longer or labelled gives the eye an
  anchor and makes counting reliable.
- **Extent follows the load.** Take the ladder to the maximum drop the user actually intends to hold,
  and no further. Marks at drops the cartridge cannot reach are pure clutter. If they name a maximum
  distance, compute the drop with `TrajectoryCalculator` and size the ladder to it.

## Wind holds and the christmas tree

For holding wind and drop simultaneously without dialling. Below the centre, add horizontal rows of
wind marks at each drop hold, forming a tapered grid — the "christmas tree".

- **Rows** at each holdover you support: typically every 1 mrad from about 2 mrad down.
- **Columns** at 0.5 or 1 mrad of wind either side, out to 2–4 mrad.
- **Taper the rows** so they get *wider* with drop. This is not decoration: wind deflection grows
  faster than drop with distance, so more wind hold is needed at the bottom of the tree. It also
  makes each row visually distinct, so the eye lands on the right one.
- The grid is what makes a reticle look "busy", so mark the wind holds with the *smallest* features
  in the design — short ticks or 0.05 mrad dots, not full crossing lines.

Because the vertical spacing of the rows is drop and the horizontal is wind, a christmas tree is
implicitly tied to a velocity band. Say so in the reticle name or docs; it is honest and it is what
commercial reticles do.

## Labels

Numbers are expensive: a `0.3 mrad` tall "10" occupies roughly 0.3 × 0.5 mrad of the field, several
times a tick mark, and unlike a tick it cannot be scanned past.

- **Height** 0.25–0.5 mrad. Below 0.25 it is illegible at moderate magnification; above 0.5 it
  dominates.
- **Offset** 0.5–1.5 mrad from the axis, always to the side, never on the hold point.
- **Label every second or fifth graduation**, not every one.
- **Anchor deliberately — the position is an edge.** `anchor="Right"` for labels left of a vertical
  line and `Left` for those to the right makes them grow away from the axis instead of into it. This
  is not cosmetic: with the default `Left` anchor the position is the text's *left border*, so a label
  moved to the left side without changing its anchor still grows rightward and its far edge sits
  exactly where it did before. Either switch the anchor or subtract the label's own width by hand.
- **Vertical centring is manual.** Text sits on its baseline, so subtract about `text-height / 2` from
  the mark's Y to centre a label on it — exactly what the BDC labeller does.
- **Label the value, not the unit.** "4" beside the 4-mrad mark. The unit belongs in the reticle name
  and the subtension table you hand the user.
- **Budget space for the BDC labels too, on the other side.** A `<bdc>` anchor's distance label is
  drawn at render time and occupies about `2.4 × text-height` of width — nearly 1 mrad for a 0.4 mrad
  label showing "1000". Because nothing in the file reserves that space and no preview shows it, the
  clash only surfaces when someone renders with a real trajectory. Giving a `<bdc>` the same offset as
  the row numeral beside it stacks the two exactly. Drawn numerals on one side, BDC labels on the
  other, is the fix; `scripts/reticle.py check` verifies it.

## Rangefinding stadia

If the reticle is meant to measure, the graduations must be exact and the user needs the formula.

- Any evenly graduated axis already ranges: `range_m = target_mm / mrad`,
  `range_yd = target_in × 27.78 / mrad`, `range_yd = target_in × 95.5 / MOA`.
- Dedicated stadia — a pair of marks or a bracket sized to a specific target (a 45 cm torso, a 20 cm
  chest) — trade generality for speed. If you add them, **state the assumed target size in the
  reticle name or a label**, because a bracket with no stated reference is unusable.
- Ranging accuracy degrades with the square of the error: reading a 0.1 mrad error on a 2 mrad target
  is a 5 % range error. So a ranging reticle needs its *finest* graduations, 0.1 mrad or better, and
  they only pay off on an FFP reticle where the subtension holds at any magnification.

## BDC anchors and load-specific reticles

A reticle stores angles; a distance only exists relative to a cartridge, muzzle velocity and
atmosphere. This is why `<bdc>` anchors carry no distances — the app computes them at draw time from
a real trajectory, so one reticle can be labelled for many loads.

Design consequences:

- **Prefer a plain graduated ladder** with `<bdc>` anchors at the holds, over baking distance numbers
  into `reticle-text`. The graduated version stays correct for any load; the baked version is wrong
  for every load but one, silently.
- If the user does want etched distances, get the specific load and atmosphere, compute the drops,
  and **say in the name what they assume** ("BDC 308 175gr 2600fps, sea level").
- Put a `<bdc>` anchor at every hold you want auto-labelled, and give them a consistent `text-offset`
  so the labels form a clean column.
- `text-offset` is positive to the right, negative to the left; put the labels on the opposite side
  from the wind holds so the two do not collide.

## Building a BDC ladder from a trajectory

When the brief names a gun and a load, stop drawing and start computing: for a BDC reticle the mark
positions **are** the trajectory. Each mark goes exactly where the bullet will be, because you put the
mark on the target and the bore follows:

```
mark y =  DropAdjustment          negative — below the aiming point
mark x = -WindageAdjustment       positive — to the RIGHT
```

⚠️ **Note the negation on x.** The two conventions are opposite and nothing warns you:

| | positive means |
|---|---|
| `TrajectoryPoint.Windage` / `WindageAdjustment` | **LEFT** |
| reticle `X` (`position-x`, `start-x`, …) | **RIGHT** |

Feed the windage straight through and the ladder leans the wrong way — a mistake that looks like a
plausible reticle, so it survives review. `Drop` needs no negation, because reticle Y and
`DropAdjustment` both take positive as up.

**That x term is spin drift, and real BDC reticles do include it.** A right-hand twist pushes the
bullet right, so the hold must be placed right, and a long-range BDC ladder therefore *leans* toward
the drift side rather than running straight down. The magnitude is modest but not negligible: about
0.7 mrad at 2000 m for .50 BMG from a 1:15" right-hand barrel — roughly 1.4 m of lateral error if
ignored. Drawing the spine as segments joining consecutive marks, rather than as one straight line,
makes the lean fall out of the geometry instead of being decoration.

Because the ladder encodes one cartridge at one muzzle velocity in one atmosphere, **put the load and
the zero in the reticle `name`**, and state the distance unit — a ladder numbered 4…20 is meaningless
until the reader knows whether that is hundreds of metres or hundreds of yards. Generate from a
parameterised script so a different zero or load is a re-run rather than a redraw.

### Identifying the load behind an existing BDC reticle

Working the other way is possible and worth knowing, because it turns an undocumented reticle into a
specification. Measure the tick positions in pixels, then exploit two invariances:

- **Drop *differences* between consecutive marks are independent of the zero range**, which cancels in
  the subtraction, **and of the drawing scale** in their ratios. So regressing measured pixel drops
  against a candidate trajectory — fitting only a scale and an intercept — tests the *shape* of the
  curve, and the shape is fixed by the load.
- The same regression **discriminates the distance unit**: interpreting the labels as hundreds of
  metres versus hundreds of yards changes the curve's shape, so one hypothesis fits better.

Compare candidates by residual. A load that reproduces 17 marks spanning 400–2000 m to under 1 % is
identified, not guessed. Cross-check on an axis you did *not* fit — the horizontal drift is ideal,
since matching it to a few percent confirms both the scale and the twist direction independently.

Remember the drawing is probably schematic (see above), so expect the *vertical* to be roughly honest
while horizontal extents are stylised, and treat any derived subtension that lands on an absurd value
as evidence of that rather than as a discovery.

## Colour and illumination

- **Black is the default and usually correct.** It has the most contrast against typical targets, and
  a black reticle stays readable on any background.
- **Colour for emphasis only.** A red centre dot or a coloured holdover band is a legitimate signal.
  Colour used decoratively costs contrast: `chevron.reticle` fades red → darkred → black down its
  ladder to encode range bands, which is emphasis doing work.
- Consider the background. Fine black lines vanish against dark game and shadow, which is the
  argument for heavy duplex posts and for an illuminated centre.
- Colours are HTML names passed straight through to the SVG, so the drawing is only ever a
  representation — real reticles are etched glass or wire, illuminated or not.

## Review checklist

Run this against the ASCII preview before delivering:

- [ ] Symmetric left-to-right where it should be.
- [ ] The aiming point is clear — nothing covers the origin.
- [ ] Three or four stroke weights, with a clear heavy → fine gradient from edge to centre.
- [ ] No two marks closer than ~0.2 mrad in a used area.
- [ ] No run of more than five identical marks without a longer or labelled one.
- [ ] Graduations on round numbers in the chosen unit.
- [ ] Labels off the hold points, consistently anchored, not on every mark.
- [ ] Ladder extends as far as the load justifies, no further.
- [ ] Nothing clipped at the edge of the field of view.
- [ ] The finest feature is above `0.3 / magnification` mrad.
- [ ] A `<bdc>` anchor at each hold that should be auto-labelled.
- [ ] The reticle `name` states the unit, and the design magnification if SFP.
- [ ] You can hand over a subtension table: what each mark is worth, and the ranging formula.

When reproducing an existing reticle, add:

- [ ] Numbers come from a manufacturer's data sheet where one exists, not from a review-site image.
- [ ] Circle diameters halved into radii.
- [ ] No element copied from a depiction's frame — no ocular circle, no crop mistaken for extent.
- [ ] The framing you reproduced is stated in the `name`.
- [ ] Every invented number is labelled `ASSUMED` where it is defined.
