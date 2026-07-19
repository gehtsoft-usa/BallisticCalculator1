# Aerodynamic (crosswind) jump — implementation plan

Plan to add **aerodynamic jump** (a.k.a. crosswind jump) to the 3DOF engine: the vertical deflection a
spin-stabilized bullet gets from a *horizontal* crosswind. It is the missing gyroscopic sibling of the
spin-drift term we already model, and a natural follow-on to the Coriolis work (`CLAUDE/CORIOLIS.md`) —
same shape of solution (a closed-form correction layered onto the point-mass trajectory).

Status: **IMPLEMENTED** (2026-07-19). Engine: `TrajectoryCalculator.Calculate` applies Litz Eq 5.4 as a
per-output-point vertical offset, gated like spin drift (`aeroJumpAngleRad`). Tests: `B1DragTest`
`AerodynamicJump_ClosesHornadyWindDropGap` (Hornady 4DOF acceptance) + `AerodynamicJump_StructuralProperties`
(linearity, sign, range-independence). **Result — validated the original hypothesis:** on the
`b1_eldx_wind` case (220 gr ELD-X, 20 mph full-value crosswind) the wind-case drop error vs Hornady 4DOF
dropped from a flat **~0.83 MOA → ~0.11 MOA** at every range (200→1000 yd) — the wind case now matches
4DOF as well as the no-wind case. Residual (~0.11 MOA) is Eq 5.4 with our Miller `Sg` overshooting 4DOF's
effective jump by ~14% (documented model difference; see §7). No inclined-fire `cos` projection applied
yet (deferred, §7).

---

## 1. What it is (physics)

As the bullet leaves the muzzle it points along the bore, but the air it *feels* is the vector sum of its
forward velocity and the crosswind — i.e. the relative wind arrives at a small horizontal angle. A
gyroscopically stable bullet **weathercocks** to face that relative wind (turns horizontally toward it);
the gyroscopic response to a horizontal turn is a **vertical** tip, and the lift acting through that tip
deflects the trajectory **perpendicular to the wind**. So a horizontal crosswind produces a mostly
**vertical** deflection.

Key property that makes it cheap to model: the whole effect is imparted in the **initial yaw transient
right at the muzzle** (the first coning/epicyclic cycle, tens of calibers out). After that it is just a
**fixed angular deflection of the rest of the trajectory** — the same MOA at every range (unlike wind
*drift*, which is a lag effect that grows nonlinearly with time of flight). Litz: "if AJ is ¼″ at 100 yd,
it's 2.5″ at 1000 yd" — i.e. constant in angle.

It is **distinct from and additive to**:
- **Wind drift** (horizontal, integrated over the flight) — already modelled inside the integrator.
- **Spin drift** (horizontal, gyroscopic yaw-of-repose) — already modelled (`driftFactor·t^1.83`).
- **Coriolis/Eötvös** — already modelled (`CLAUDE/CORIOLIS.md`).

---

## 2. Two roads, and the decision

| Road | Formula basis | Inputs it needs | Verdict |
|---|---|---|---|
| **McCoy (rigorous)** | linearized 6DOF / coning theory (§3.1) | axial moment of inertia `Ix`, lift slope `C_Lα`, pitching-moment slope `C_Mα` | **Not viable** — none of `Ix`, `C_Lα`, `C_Mα` are derivable from a G1/G7 BC. Same data wall that blocks true 6DOF. Keep for understanding/validation only. |
| **Litz (practical)** | empirical, expressed via Miller `Sg`, twist, muzzle velocity, crosswind (§3.2) | quantities the engine **already computes** for spin drift | **Chosen.** Field-solver standard (Kestrel/AB). Reuses the existing drift inputs; no new public API required. |

**Decision:** implement the **Litz practical** form as a closed-form per-output-point correction, exactly
as we did for Coriolis. Validate by diffing on/off against a field reference (Kestrel / AB), and pin the
coefficient + sign to that data — do not trust a formula transcription blind (the Coriolis sign/constant
lesson).

---

## 3. The formulas

Constants/symbols: `W` = crosswind component (speed perpendicular to the line of fire, horizontal),
`V₀` = muzzle velocity, `Sg` = corrected Miller stability factor (engine already computes this in
`CalculateStabilityCoefficient`), twist as calibers-per-turn `n = riflingStep / diameter`.

### 3.1 McCoy (reference only — NOT implemented)
From R. L. McCoy, *Modern Exterior Ballistics* (and BRL Report MR-3877, 1990). Vertical crosswind
aerodynamic jump as an angle:

```
J_A = − ( Ix / (m·d²) ) · ( C_Lα / C_Mα ) · ( 2π / n ) · ( W / V₀ )
```
- `Ix` = axial moment of inertia, `m` = mass, `d` = reference diameter,
- `C_Lα` = lift-force coefficient slope, `C_Mα` = overturning (pitching) moment coefficient slope,
- `n` = twist in calibers/turn, `W` = crosswind, `V₀` = muzzle velocity.

`Ix/(m·d²)` is a dimensionless inertia parameter; `C_Lα/C_Mα` is the aeroballistic ratio we **do not
have**. Retained only to (a) explain the magnitude and (b) sanity-check the Litz form's scaling
(`J_A ∝ W/V₀`, one-time angular offset). *Verify the exact symbol set against McCoy before quoting.*

### 3.2 Litz (practical — IMPLEMENT THIS) — Equation 5.4, *Applied Ballistics for Long-Range Shooting*

Litz gives the crosswind vertical deflection as a linear fit in **just two variables**:

```
Y = 0.01·SG − 0.0024·L + 0.032            (Equation 5.4)
```
- **Y** = vertical deflection in **MOA per 1 mph of crosswind** — a **constant angle, the same at all
  ranges**.
- **SG** = gyroscopic (Miller) stability factor — the *corrected* `Sg` the engine **already computes** in
  `CalculateStabilityCoefficient` (function of twist, velocity, length, diameter, weight, temperature,
  pressure; Litz's "Chapter 10 / Appendix" Sg is this corrected Miller value).
- **L** = bullet length in **calibers** = `BulletLength / BulletDiameter` — the `length` local **already
  computed** in `CalculateStabilityCoefficient`.

Total jump for a crosswind of `W` mph: `AJ_MOA = Y · W`, applied as a **constant angle** (range-independent);
the vertical impact offset = `AJ_MOA` (as an angle) × range.

**Worked example (make this a unit test):** Berger 7 mm 180 gr VLD, 1:8″ twist, 2800 fps, standard
conditions ⇒ SG = 1.49; length 1.517″ / 0.284″ = **5.34 cal** ⇒
`Y = 0.01·1.49 − 0.0024·5.34 + 0.032 = 0.034 MOA/mph`. A 10 mph crosswind ⇒ **0.34 MOA**; Litz's 6-DOF
reference gives **0.36 MOA** (~0.02 MOA / ~0.2″ at 1000 yd error — the accepted tolerance for this fit).

**Sign (same passage):** "**up for a wind from the right, and down for a wind from the left**" — stated
for a conventional **right-twist** barrel. Equation 5.4 has **no twist term**, so it supplies *magnitude
only*; the vertical sign **flips for a left-twist** barrel (§4).

**Notes for the port:**
- No new inputs — `SG` and `L` are exactly the two intermediates already inside the spin-drift stability
  calc. (Same gate: needs `Rifling + BulletDiameter + BulletLength`.)
- `Y` can be computed **once per shot** (muzzle `Sg`); crosswind `W` is the **muzzle** crosswind (first
  wind zone), since the jump is imparted at the muzzle.
- Velocity dependence is folded into `Sg` (via its `(mv/2800)^⅓` term) — no separate `V₀` factor, unlike
  McCoy §3.1; that is fine, we match Litz, not McCoy.

---

## 4. Sign convention (must be pinned to reference data)

**Litz states it directly** (Eq 5.4 passage, right-hand twist): "up for a wind from the right, and down
for a wind from the left." So:
- **Crosswind from the RIGHT (3 o'clock)** ⇒ bullet strikes **HIGH** (jump up).
- **Crosswind from the LEFT (9 o'clock)** ⇒ bullet strikes **LOW** (jump down).
- **Left-hand twist flips** the vertical sign (Eq 5.4 carries magnitude only).

Map to *our* conventions:
- Wind `Direction`: **0° = toward target, 90° = from the right, 270°/−90° = from the left** (`CLAUDE.md`
  §3). So the horizontal crosswind component is `W = velocity·sin(Direction)` — **positive = from the
  right**.
- `Drop` is vs line of sight, **more negative = lower**; "strike high" ⇒ add a **positive** offset to
  `Drop`.
- Twist: `TwistDirection.Right` / `Left` (already used by spin drift, `driftDirection = −1` right / `+1`
  left).

⇒ Mapping (right twist, wind from right `W>0` ⇒ up ⇒ positive `Drop` offset):
```
Y_moa_per_mph   = 0.01*Sg - 0.0024*L + 0.032                   # Eq 5.4 (§3.2), per-shot constant
verticalJumpSign = (TwistDirection == Right) ? +1 : -1
Δdrop(range)    = verticalJumpSign · (Y_moa_per_mph · W_mph_fromRight) · range   # angle×range, from-right +
```
where `W_mph_fromRight = windVelocity.In(mph) · sin(Direction)` (positive = from the right). The physics
sign is **known** (Litz states it); what still needs a quick **pin against a reference tool** at
implementation is (a) that our `Drop`/wind-`Direction` sign bookkeeping comes out as above, and (b) the
left-twist flip — a whole-sign mismatch would be a convention slip, not a physics error, but must be
nailed and documented (the Coriolis lesson).

---

## 5. Implementation sketch (`TrajectoryCalculator.Calculate`)

Mirror the spin-drift machinery exactly — same gate, same "fold a closed-form term into an output
channel" pattern; here the channel is `Drop`, not `Windage`.

1. **Gate:** only when `Rifling != null && BulletDiameter != null && BulletLength != null` (identical to
   spin drift — needs `Sg` and twist direction). No new public inputs.
2. **Precompute once, pre-loop** (near the drift/Coriolis constants ~`:316`):
   - muzzle crosswind `W` from the first wind zone via the existing `WindVectorRaw` decomposition (the
     lateral component), in a defined unit.
   - `AJ_angle` from §3.2 (function of `W`, `Sg`, twist, `V₀`) — a constant angle for the shot.
   - `verticalJumpSign` from twist direction (§4).
3. **Apply per output point**, in the same block where Coriolis/drop are assembled (before building the
   `TrajectoryPoint`, ~`:349`), as a range-linear vertical offset:
   ```
   drop_m     += verticalJumpSign * ajAngleRad * distanceMeters;   // vs line of sight
   dropFlat_m += verticalJumpSign * ajAngleRad * rx;               // vs muzzle (flat range)
   ```
   Apply **after** the Coriolis Eötvös scaling (Eötvös scales the gravitational fall; AJ is an added
   launch-angle offset — they are independent and additive).
4. **Do NOT apply in `SightAngle`.** Zeroing stays ballistic-only; AJ is a wind effect and there is no
   crosswind assumed at zeroing (same stance as Coriolis; keep the two consistent).
5. **Secondary cross term (defer):** a horizontal crosswind's jump is *primarily* vertical, but there is
   a small horizontal AJ component (and a vertical wind would give a horizontal jump). Model only the
   vertical-from-horizontal-crosswind primary first; note the omission. No aero-jump from vertical winds
   initially.

Interaction notes: independent of and additive to wind drift (integrator), spin drift (`Windage`), and
Coriolis (`Windage` + Eötvös `Drop`). It shares the crosswind input with wind drift but acts as a muzzle
launch-offset, not an integrated lag.

---

## 6. Validation & tests

Same discipline as Coriolis (`CLAUDE/CORIOLIS.md` §6):
1. **Reference diff:** run a fixed bullet/twist at several crosswinds with AJ **on** vs **off**; the
   `Drop` delta should be a **constant angle** (linear in range) and match the reference (Kestrel/AB)
   **MOA-per-mph** to within tolerance. Build `be_`-style templates if capturing a full run.
2. **Sign/twist coverage:** right twist + wind-from-right ⇒ higher (`Drop` delta > 0); wind-from-left ⇒
   lower; left twist flips; headwind/tailwind (Direction 0/180, `sin=0`) ⇒ zero AJ.
3. **Range-independence guard:** assert `AJ_angle = Δdrop(range)/range` is the same at 500/1000/1500 yd
   (this is the defining property; a bug that makes it grow with TOF would fail here).
4. **Closed-form unit guard (Litz Eq 5.4):** assert the implemented per-mph coefficient equals
   `0.01·Sg − 0.0024·L + 0.032`. Concrete anchor — **Berger 7 mm 180 gr VLD, 1:8″, 2800 fps, standard**:
   `Sg ≈ 1.49`, `L = 5.34 cal` ⇒ `Y = 0.034 MOA/mph`; a 10 mph crosswind ⇒ **0.34 MOA** vertical
   (Litz's 6-DOF reference 0.36; accept ~0.02 MOA). This is the primary acceptance test.
5. **Cross-family magnitude sanity (McCoy example):** 7.62 NATO M80 (9.5 g, 870 m/s, 1:313 mm), 5 m/s
   crosswind ⇒ ≈ **1.6 mm vertical @ 200 m ≈ 0.032 MOA** (constant at all ranges). Order-of-magnitude
   only — AJ is small (tenths of MOA even in stiff wind) vs wind drift.
6. **Regression:** inert when there is no crosswind or no rifling/bullet dims; existing suite unchanged.

---

## 7. Risks & open questions
- **The coefficient is the whole risk.** Code is ~a dozen lines mirroring spin drift; correctness hinges
  entirely on the Litz constant/units (§3.2) and the sign (§4). Blocked until sourced from the book.
- **Sign convention** flips easily across sources/frames — must be pinned to a reference tool, documented.
- **Angular-constant model vs our per-output frame:** AJ is a fixed muzzle angle; we apply it as
  `angle·range`. Confirm the reference also treats it as range-independent MOA (it does, per Litz) so the
  linear-in-range application is faithful.
- **Multi-zone wind:** RESOLVED — AJ uses the *muzzle* (first) wind zone only, since the jump is a
  muzzle transient; downrange zones do not change it, and a calm muzzle zone gives zero jump even if a
  later zone is windy. Locked by `AerodynamicJump_UsesMuzzleWindZone`.
- **Data honesty:** this is an empirical field-tool match (like Coriolis), *not* first-principles 6DOF —
  the rigorous McCoy form needs aero data we don't carry. Document as a deliberate approximation.
- **Interaction with inclined fire:** spin drift is `×cos(shotAngle)`; decide whether AJ needs an
  analogous projection (it's a vertical effect on an inclined LoS — likely `×cos` on the perpendicular
  component; confirm against reference or derive).

---

## 8. Checklist
- [x] §3.2 Litz formula sourced — Eq 5.4 `Y = 0.01·SG − 0.0024·L + 0.032` (MOA per mph), SG = Miller,
  L = length in calibers. Uses only existing spin-drift intermediates.
- [x] §4 Sign: right twist + wind-from-right ⇒ up (Litz); left twist / wind-from-left flip. Verified via
  `AerodynamicJump_StructuralProperties` (sign, linearity, head/tailwind zero) and the Hornady drop match.
- [x] §5.1 Gated on `Rifling + BulletDiameter + BulletLength` (reuses the spin-drift gate / `calculateDrift`).
- [x] §5.2 Precompute `aeroJumpAngleRad` (muzzle crosswind, `Sg`, `L`, twist sign).
- [x] §5.3 Apply range-linear vertical offset to `Drop`/`DropFlat`, after the Eötvös scaling.
- [x] §5.4 `SightAngle` left AJ-free (it takes no wind; nothing to do).
- [x] §6 Hornady acceptance (0.83→0.11 MOA) + structural (sign/linearity/range-independence) + Eq 5.4
  magnitude guard; full suite green (237).
- [x] §7 Multi-zone wind resolved (muzzle-zone only; `AerodynamicJump_UsesMuzzleWindZone`).
- [ ] §7 Inclined-fire `cos` projection — **deferred** (level supersonic is ~90% of use; documented).
- [x] Update `CLAUDE.md` §5 + §7 + README + SKILL + `CHANGES_TO_PORT.md`.

---

## 9. Sources
- R. L. McCoy, *Modern Exterior Ballistics* (2nd ed.), pp. 267–272; BRL Report MR-3877 (1990) — rigorous
  crosswind-AJ formula (§3.1).
- B. Litz, *Applied Ballistics for Long-Range Shooting*, **Equation 5.4** `Y = 0.01·SG − 0.0024·L + 0.032`
  (vertical MOA per mph of crosswind), with the Berger 7 mm 180 gr VLD worked example (§3.2); spin-drift
  form `1.25·(Sg+1.2)·TOF^1.83` (already in the engine).
- J. Boatright, "A Coning Theory of Bullet Motions" (arXiv:1205.2071) and Boatright & Ruiz, "Calculating
  Aerodynamic Jump for Firing Point Conditions" — analytic CWAJ via coning theory (deeper background).
- Magnitude anchor: stocks-rifle.com "Wind Caused Vertical" (McCoy M80 worked example).
