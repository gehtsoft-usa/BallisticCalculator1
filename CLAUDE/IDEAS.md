# Ideas & Backlog

Candidate features distilled from a review of the **BulletDesigner** FreeCAD workbench
(`/mnt/d/xep/2see/BulletDesigner`, MIT-licensed Python) against this library
(LGPL, C#).

## Context you need before touching any of these

- **BulletDesigner is a bullet-geometry design tool**, not a ballistics engine. Its
  ballistic math is a deliberately simplified *design aid*: empirical form-factor
  fudge terms, a hardcoded 22-point G7 table, a 2D point-mass RK4 integrator, no
  wind, no drift integration. Do **not** treat its numbers as reference-grade.
- **This library is the reference-grade side.** We already have: 40+ point drag
  polynomial approximation; full G1/G2/G5/G6/G7/G8/GI/GS/RA4 tables; NASA-based
  atmosphere; wind; Litz spin drift; adaptive step; ~0.5% accuracy vs. modern
  calculators.
- **Licensing:** MIT → LGPL is compatible. But their ballistic code is Python and
  mostly empirical, so **reimplement concepts in our idiom — do not port line by
  line.**
- **We already own the Miller stability formula.** See
  `BallisticCalculator/Calculations/TrajectoryCalculator.cs:486`
  (`CalculateStabilityCoefficient`). It matches BulletDesigner's formula: same
  velocity correction `(V/2800)^(1/3)`, same atmospheric `f_tp` term. This means
  features #4 and #5 below build on math we already have.

### Placement decision (deferred, on purpose)

For each feature below we decide **later, per feature**, where it lives in the API
(new static helper class vs. methods on existing types such as `Rifling` /
`Ammunition` / `TrajectoryCalculator`). This document intentionally captures
**WHAT** to build and the underlying math — **not WHERE**. Do not let placement
block the spec.

---

## Bonus finding — a probable bug in *their* stability calc (validates ours)

While comparing calculators (the original candidate "compare their and our
ballistic calc"), one discrepancy surfaced and is worth recording:

- **Theirs** (`Utils/Calculations.py`, `calculate_stability_factor_miller`, step 4)
  applies the atmospheric correction to Sg as `Sg × √f_tp`.
- **Ours** applies it linearly: `sd * fv * ftp` (TrajectoryCalculator.cs:502).

The Miller atmospheric correction on **Sg itself** is *linear* in the (inverse)
air-density ratio, so **ours appears correct and theirs appears to over-dampen the
correction**. The `√` is only legitimate on the *twist-rate solve* (because
`Sg ∝ 1/T²` ⟹ `T ∝ 1/√Sg`) — and there they do use `√f_tp` correctly (see #4).

**Action:** no change needed to our code from this; recorded as validation. If we
ever cross-check against BulletDesigner numbers, expect our Sg to differ by exactly
`√f_tp` at non-standard atmosphere.

Conclusion of the "compare the calcs" candidate: **our trajectory engine is
strictly superior; take nothing from their integrator.** The two useful takeaways
were (a) confirmation of our Miller formula and (b) the bug above.

---

## Feature #4 — Recommended (optimal) twist rate by projectile

**Status:** recommended to implement. Low risk, well-founded, reuses math we own.
**Value:** given a projectile, tell the user the barrel twist that yields a target
stability — the natural inverse of the stability number we already compute.

### What it does

Input: projectile geometry + intended muzzle conditions. Output: the recommended
rifling step (twist) as a `Measurement<DistanceUnit>` (e.g. inches-per-turn),
optionally with the target Sg it was solved for.

### The math (two regimes, mirror of BulletDesigner)

Their source: `calculate_recommended_twist_rate` in `Utils/Calculations.py`.

**Regime A — monolithic copper/brass (inverted Miller, target Sg):**
Solve Miller's `Sg = (30·m) / (t²·d³·l·(1+l²))` for the twist `t` at a chosen
target Sg (they use 1.8 for monolithic). In their imperial form:

```
T_required = d_eff · sqrt( (30·m) / (Sg_target · d_eff³ · l · (1 + l²)) )
                    · (2800 / V_fps)^(1/6)
```

Then the atmospheric correction on the **twist** (note the sqrt — correct here):

```
T_corrected = T_required · sqrt(f_tp)
   where f_tp = (29.92 / P_inHg) · ((T_F + 460) / 519)
```

- `m` = mass (grains), `d_eff` = effective diameter (in), `l = L/d_eff`,
  `V_fps` = muzzle velocity, `Sg_target` default 1.8 (monolithic) / 1.5 (lead).
- The `(2800/V)^(1/6)` is the inverse of the stability velocity term
  `(V/2800)^(1/3)` propagated through the square root.

**Regime B — lead-core (Greenhill):**

```
V ≤ 2800 fps:  T = 150 · d_eff² / L
V > 2800 fps:  T = 150 · d_eff² / L · sqrt(V / 2800)
```

Their regime selection is by material **density**: 7.0–9.5 g/cm³ ⇒ monolithic
(Regime A, Sg 1.8); > 10 g/cm³ ⇒ lead (Regime B / Sg 1.5). We should make the
regime / target-Sg an explicit input rather than inferring from density, since we
don't carry material density on `Ammunition`.

### Notes / decisions for us

- **`d_eff` (effective diameter):** BulletDesigner distinguishes nominal (groove)
  diameter from bearing-band diameter for land-riding bullets and insists the
  twist calc use band diameter. We currently only have `BulletDiameter`. Decide
  whether to add an optional effective-diameter input or just document the caveat.
- **Inputs we already have** on `Ammunition`: `Weight`, `MuzzleVelocity`,
  `BulletDiameter`, `BulletLength`. Atmosphere gives `f_tp`. So Regime A needs no
  new data beyond an optional `Sg_target`.
- **Cross-check:** feeding the resulting twist back into our existing
  `CalculateStabilityCoefficient` should reproduce `Sg_target` (modulo the √f_tp /
  f_tp atmosphere subtlety noted above — resolve which convention we standardize
  on when implementing).
- Round-to-common-twist (their `round()` to 7/8/9/10/12/14) is a UI nicety; the
  library should return the raw value and leave rounding to callers.

---

## Feature #5 — Transonic analysis

**Status:** recommended to implement. Cheap; layers on top of our trajectory output.
**Value:** report where along the flight the projectile enters/exits the transonic
band and whether it is still gyroscopically stable there (the classic long-range
"does it stay together past the transition" question).

### What it does

Given a computed trajectory (which we already produce — every `TrajectoryPoint`
carries Mach/velocity), report:

- **Transonic entry** — first range/time where Mach drops to ≤ 1.1.
- **Transonic exit / fully subsonic** — first range/time where Mach drops to ≤ 0.9.
- **Stability at transonic entry** — evaluate Sg (we have the formula) and flag if
  below the threshold (their default 1.8 monolithic / 1.5 lead). This is where
  marginally-stable bullets tend to go unstable.

Their thresholds (`Mach 1.1` entry, `Mach 0.9` exit) are conventional; expose them
as parameters with those defaults.

### Notes for us

- This is essentially a **post-processing pass over an existing trajectory**, plus
  one Sg evaluation. No integrator work needed — ours is already better than theirs.
- Sg is (to first order) constant with range in the Miller model; "stability at
  transonic entry" is really "muzzle Sg, reported at the transonic range." If we
  want range-varying Sg we'd need velocity-dependent Sg — worth a note but not
  required for parity.
- Output shape TBD (a small result record with entry/exit range+time+velocity+Mach
  and a stability verdict).

---

## Feature #3 — Estimate ballistic coefficient from bullet shape

**Status:** valuable *concept*, **weak implementation** — reimplement from a
published method; do NOT port their fudge factors. This is the biggest genuine gap
(we have no way to estimate BC from geometry when a manufacturer BC is unknown),
but also the highest-uncertainty item — treat as R&D, not a port.

### What theirs does (documented in full for reference)

Source: `calculate_ballistic_coefficient_g1` in `Utils/Calculations.py`. Core
relation is sound: `BC_G1 = SD / i`, where `SD = weight_lb / d_in²` and `i` is a
geometry-derived form factor. The *form factor* is where it gets heuristic. Their
`i` is built by chaining multiplicative corrections, each a clamped linear ramp
around a reference, with hand-tuned magic constants:

1. **Nose fineness vs. a "Mayewski-like" reference** (ref nose fineness `3.28`):
   `i *= 1 - 0.04·clamp((nose_len_ratio - 3.28)/3.28, -1, 1)`.
2. **Extra nose-ratio term:** `i *= 1 - 0.045·clamp((nose_len_ratio - 3)/4, -1, 1)`.
3. **Ogive-radius term:** `i *= 1 - 0.03·clamp((ogive_radius_cal - 6)/8, -1, 1)`.
4. **Ogive-type nudge:** Tangent 1.00 / Secant 0.985 / Elliptical 0.975.
5. **Boat-tail reduction** (only if angle & length > 0): a blended
   `0.03 + 0.05·(0.6·angle_term + 0.4·length_term)` reduction, angle ramp centered
   on 5–12°, length ramp on boat-tail/total up to 0.20.
6. **Meplat penalty:** `i *= 1 + 0.22·(meplat_ratio^1.4)`.
7. **Hollow-point penalty** (hp only): `i *= 1 + 0.15·(hp_ratio²)`.
8. **Mach-regime correction:** piecewise bump (subsonic ~+2–5%, transonic hump up
   to +10% near Mach 1.2 decaying to +3% supersonic).
9. **Clamp** `i` to `[0.40, 1.80]`, then `BC = SD/i`.
10. **Atmospheric density scaling** relative to ICAO standard, clamped to
    `[0.6, 1.4]`.

There is also a **second, independent** and even cruder BC path inside
`calculate_bullet_dimensions_from_weight` (base form factors Tangent 0.85 / Secant
0.80 / Elliptical 0.75 with ±5–10% step corrections). The two BC estimators in
their own codebase do **not** agree with each other — a red flag.

### My criticism (why not to port it)

- **Unsourced magic constants.** The `0.04 / 0.045 / 0.03 / 0.22 / 0.15` weights
  and the `3.28` / `6` / `3` reference points are hand-tuned, not traceable to a
  published drag model or measured data set. No validation against known bullets is
  present in the repo.
- **Clamped linear ramps stacked multiplicatively** have no physical basis; they're
  a curve-fit shaped by intuition. Small input changes outside the ramp windows do
  nothing (saturated), and inside they behave linearly where real aerodynamics are
  not.
- **Two disagreeing estimators** in one project (see above) means neither is
  trustworthy as a reference.
- **Mach correction folded into a *static* BC.** BC (as SD/i vs. a drag table)
  should already carry velocity dependence through the table; multiplying a static
  G1 BC by an ad-hoc Mach bump double-counts and conflates "BC" with "instantaneous
  Cd ratio."
- **Atmospheric scaling of BC is conceptually off.** BC is a property of the
  projectile; atmosphere belongs in the trajectory solve (which we already do
  correctly), not baked into the BC number.

### What we should do instead (research directions)

- **Prefer a published form-factor method** with traceable coefficients, e.g.:
  - Classical drag-form estimates (McCoy / *Modern Exterior Ballistics*), which give
    zero-yaw drag / form factor from nose length, ogive radius (calibers),
    boat-tail angle & length, meplat diameter, and base diameter.
  - Or the well-known empirical fits (e.g. the approaches used by JBM / Litz for
    predicting G7 form factor from geometry) — pick one with published constants we
    can cite.
- **Estimate the G7 form factor (i7) first, not G1.** For modern boat-tail spitzers,
  G7 is the better reference and the geometry→i7 relationship is tighter and better
  documented. Convert to G1 only if a caller needs it (see #1).
- **Validate against a known-bullet table.** Before shipping, back-test the estimator
  against a set of bullets with published G1/G7 BCs (Sierra/Hornady/Berger data) and
  report the error distribution. If we can't get within a stated tolerance, ship it
  clearly labeled as a rough estimate with the measured error band.
- **Keep BC static and atmosphere-free.** Let the existing trajectory engine handle
  velocity and atmosphere; the estimator's job is geometry → form factor → BC only.
- **Inputs available today:** `BulletDiameter`, `BulletLength`, `Weight`. We would
  need to add nose/ogive/boat-tail/meplat geometry inputs (not currently modeled) —
  scope this as part of the feature.

---

## Feature #1 — Real G1 ⇄ G7 (drag-table) BC converter

**Status:** idea / nice-to-have. Do NOT port theirs.
**Value:** convert a BC between drag standards (most often G1 → G7) so a user with a
manufacturer G1 number can run the more accurate G7 model, and vice versa.

### Why theirs is inadequate

Source: `calculate_g7_bc_from_g1` in `Commands/TrajectoryCalculator.py`. It divides
G1 BC by a **single constant chosen only by boat-tail angle**:

```
flat base 2.3 | 5–7° 2.1 | 7–9° 2.0 | 9–11° 1.95  (else interpolate/extrapolate)
```

A single scalar cannot be right: the **G1↔G7 ratio is velocity-dependent** because
the G1 and G7 reference drag curves have different shapes across Mach. A constant
factor is only an average and can be off substantially at the velocity extremes.

### How to do it properly (we already have the pieces)

We ship **both** the G1 and G7 drag tables and the machinery to evaluate Cd(Mach)
for each. The physically meaningful invariant is that the **same projectile
experiences the same actual drag** regardless of which reference we express it in.
So:

- The correct conversion equates drag at a chosen velocity (or over a velocity band):
  `BC_G7 / BC_G1 = Cd_G1(M) / Cd_G7(M)` at the relevant Mach `M` — i.e. the ratio of
  the two reference drag curves, not a constant.
- **Option A (velocity-specified):** convert at a user-supplied reference velocity /
  Mach using the table Cd ratio at that Mach. Simple, honest, table-driven.
- **Option B (trajectory-matched, most accurate):** solve for the `BC_G7` whose G7
  trajectory best matches the G1 trajectory over the intended range band (least-
  squares on velocity or drop). This is what "BC truing" does in practice and reuses
  our existing integrator.
- Either way, **expose the reference velocity** as an input and document that a
  single-number cross-standard BC is inherently an approximation tied to a velocity.

### Notes

- This composes with #3: if the shape estimator produces i7/BC_G7, this converter
  gives callers a G1 number on request.
- Lower priority than #4/#5; only pursue if there's user demand for cross-standard
  BC numbers.

---

## Summary table

| # | Feature | Take their code? | Priority | Reason |
|---|---------|------------------|----------|--------|
| 4 | Recommended twist rate | Reimplement (math is standard) | High | Inverse of stability we already own; low risk |
| 5 | Transonic analysis | Reimplement (trivial) | High | Post-process our superior trajectory |
| 3 | BC by shape | **No** — concept only | Medium (R&D) | Real gap, but their impl is unsourced fudge |
| 1 | G1⇄G7 converter | **No** | Low | Do it table-driven & velocity-aware, not a constant |
| 2 | Compare calculators | n/a | Done | Ours strictly better; found their √f_tp Sg bug |
