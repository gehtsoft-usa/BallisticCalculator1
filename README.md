# BallisticCalculator

A new version of a ballistic calculator API, a new generation of a light-weight, LGPL library of modeling projectile trajectory in atmosphere

The library provides trajectory calculations for projectiles, including for various applications, including air rifles, bows, firearms, artillery, and so on.

The 3DOF (three degrees of freedom, point-mass) model that is used in this calculator is rooted in [old C sources](http://www.jbmballistics.com/ballistics/downloads/downloads.shtml) of version 2 of the public version of the JBM calculator, ported to C#, optimized, fixed, and extended with elements described in Litz's "Applied Ballistics" book and ideas of the friendly project by Alexandre Trofimov.

Changes made since porting original C sources:

* "Deciphering" formulas and making them readable for anyone familiar with the school curriculum of physics.

* New drag calculation method with higher accuracy (40+ approximation points for calculating polynomial coefficients vs 5-6).

* New atmosphere parameters calculating basing on NASA formulas

* New algorithm of step size definition to find the right balance between performance and accuracy.

* Drift calculation is added using Litz's formulas

* Coriolis / Eötvös (Earth-rotation) deflection is added via `ShotParameters.Latitude`, matching the
  field references (Ballistic Explorer and Kestrel). Note: `ShotParameters.BarrelAzimuth` is now the
  firing bearing (0° = North, clockwise) used only to orient this effect — it no longer tilts the
  trajectory (a change from earlier behavior; previously azimuth near 90° also broke the calculation).

* Aerodynamic (crosswind) jump — the vertical deflection a spin-stabilized bullet gets from a
  horizontal crosswind — is added using Litz's *Applied Ballistics* formula (needs rifling twist and
  bullet dimensions, like spin drift). It matched Hornady's 4DOF reference to ~0.1 MOA.

* Custom, measured drag curves are supported alongside the standard G1/G2/G5/G6/G7/G8/GI/GS/RA4
  tables: `.drg` radar files can be read and written, and a curve can be synthesized from a
  multi-BC (banded ballistic coefficient) profile of the kind bullet makers publish.

* `Atmosphere` reports density altitude, so the combined effect of pressure, temperature and humidity
  on the air can be read as the single number field shooters exchange.

* A midpoint (RK2) integrator and a set of analysis tools (maximum point-blank range / danger space,
  moving-target lead, Monte-Carlo hit probability / WEZ, barrel twist recommendations, and a custom
  drag curve derived from radar velocities) were added in 1.1.11, together with a new zeroing API
  (`CalculateZeroParameters`).

* Accuracy of the calculation is within 0.5%/0.2moa (less than 2inch per 1000 yards) of the most
  modern calculators.

The engine is checked against five independent references, each as a test in the suite: Hornady 4DOF
(synthesized drag curves, ~0.05 MOA), Hornady 3DOF (bit-identical), Ballistic Explorer and Kestrel for
the Earth-rotation deflection (~0.4%), the RA4/GI/G5/G6 program output (≤0.19 MOA), and an Exbal
multi-BC report (0.02 MOA over 1000 yards).

**Upgrading?** 1.1.11 has breaking changes (zeroing, `BarrelAzimuth`, sight adjustments), and
`Atmosphere.Density` was corrected after it — see [BREAKING_CHANGES.md](BREAKING_CHANGES.md) for what
changed and how to migrate.

Please refer to [online version of the documentation](http://docs.gehtsoftusa.com/BallisticCalculator1/)

You can get the latest official release from [nuget.org](https://www.nuget.org/packages/BallisticCalculator)

If you want to use the latest development version of the package, use [Gehtsoft public nuget channel](https://www.myget.org/F/gehtsoft-public/api/v3/index.json)

## Agent skills

The [`SKILL/`](SKILL/) folder holds two **Agent Skills** — plain `SKILL.md` folders that teach an AI
coding assistant (Claude Code, Codex CLI, or any tool supporting the standard) to use this library
correctly without rediscovering its API from the sources. See
[SKILL/INSTALL.md](SKILL/INSTALL.md) for installation; both are independent.

* [**`ballistic-calculator`**](SKILL/SKILLS/ballistic-calculator/) — the library itself: trajectories,
  the `Measurement<TUnit>` unit types, standard and custom drag tables, `.drg` radar curves, multi-BC
  synthesis, BXml/JSON persistence, and the analysis tools. It covers the conventions that are easy to
  get wrong — humidity as a 0–1 fraction, left-positive windage, wind direction where 90° is *from the
  right*, spin drift folded into windage.

* [**`reticle-designer`**](SKILL/SKILLS/reticle-designer/) — designing a scope reticle from a
  plain-language description and emitting it as a `.reticle` file plus a rendered SVG. It documents the
  complete `.reticle` format, reticle design principles (mrad vs MOA subtension, FFP/SFP,
  stroke-weight hierarchy, hash ladders, christmas-tree wind holds, clutter budget, BDC anchors), and
  eight worked example reticles. It also ships `scripts/reticle.py`, which needs only Python 3.8+:

  * `check` reports what the BXml deserializer would silently drop or misread — the format has no
    schema, so a misspelled attribute is ignored and `fill="True"` reads as *false*. It also projects
    where each `<bdc>` anchor's distance label will be drawn and flags collisions, which are otherwise
    invisible until something renders with a live trajectory.
  * `render` writes an SVG byte-identical to the library's own renderer, plus an ASCII raster that
    encodes stroke weight, so a reticle can be reviewed without an image viewer or a .NET build.

The library is available in

* .NET: https://github.com/gehtsoft-usa/BallisticCalculator1

* Go: https://github.com/gehtsoft-usa/go_ballisticcalc

* Java: https://github.com/nikolaygekht/ballistic.calculator.java

For those who are looking for a JavaScript version, I highly recommend [Yet Another Ballistic Calculator](https://ptosis.ch/ebalka/ebalka.html) project of our friend Alexandre Trofimov.

The current status of the project is BETA version. The physics and the public API are considered
settled — the model is validated against the references listed above and the API is documented and
covered by tests. What "beta" still means here: the API may change in response to real-world use, and
breaking changes will keep being recorded in [BREAKING_CHANGES.md](BREAKING_CHANGES.md).

RISK NOTICE

The library performs very limited simulation of a complex physical process and so it performs a lot of approximations. Therefore the calculation results MUST NOT be considered as completely and reliably reflecting actual behavior or characteristics of projectiles. While these results may be used for educational purpose, they must NOT be considered as reliable for the areas where incorrect calculation may cause making a wrong decision, financial harm, or can put a human life at risk.

THE CODE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE MATERIALS OR THE USE OR OTHER DEALINGS IN THE MATERIALS.
