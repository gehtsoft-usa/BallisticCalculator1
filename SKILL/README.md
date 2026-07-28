# BallisticCalculator — Agent Skills

Self-contained **Agent Skills** that teach an AI coding assistant (Claude Code, Codex CLI, or any
tool that supports the `SKILL.md` standard) how to use the **`BallisticCalculator`** .NET NuGet package
correctly — without reading the library source or decompiling the package to rediscover its API.

| Skill | Folder | What it is for |
|---|---|---|
| `ballistic-calculator` | [`SKILLS/ballistic-calculator/`](SKILLS/ballistic-calculator/) | using the library: trajectories, units, drag tables, serialization |
| `reticle-designer` | [`SKILLS/reticle-designer/`](SKILLS/reticle-designer/) | designing a scope reticle from a description and emitting a `.reticle` file |

The two are independent — install either or both.

## Why

`BallisticCalculator` has a strongly-typed API (every physical value is a `Measurement<TUnit>`) and a
number of easy-to-miss conventions (humidity as a 0–1 fraction, left-positive windage, wind direction
where 90° = *from the right*, spin drift folded into windage, custom drag tables run with a BC of 1.0).
Given this skill, an assistant writes correct, idiomatic, compiling code on the first try and uses the
purpose-built helpers (e.g. `DrgDragTableFactory`) instead of hand-rolling them — faster and with fewer
round-trips than rediscovering the API from the package's XML docs.

## What it covers

- **Trajectory calculation** — the full public API: `Ammunition`, `Rifle`, `Sight`, `Rifling`,
  `ZeroingParameters`, `Atmosphere`, `Wind`, `ShotParameters`, `TrajectoryCalculator`, `TrajectoryPoint`.
- **Units** — the `Gehtsoft.Measurements` `Measurement<TUnit>` types and every unit enum, with exact
  member names.
- **Drag tables** — standard `G1..RA4`, custom in-code tables, radar `.drg` files, and multi-BC
  (BC-vs-Mach) synthesis via `DrgDragTableFactory`.
- **Serialization & persistence** — saving/loading via BXml and `System.Text.Json`, embedding library
  objects in your own file format, and decorating your own classes for the BXml serializer.
- **Reticles** — building a reticle definition in code and rendering it (e.g. to SVG), including
  bullet-drop-compensator markers.

## `reticle-designer`

Turns a plain-language brief — *"mrad hash marks every half mil, holdovers to 700 yards, wind dots"* —
into a `.reticle` file the library can load, draw and label with real trajectory data.

Where the `ballistic-calculator` skill documents the reticle **API**, this one covers the **design**
and the **file format**: the complete BXml element/attribute specification, reticle design principles
(subtension, FFP/SFP, stroke-weight hierarchy, the duplex principle, clutter budget, hash ladders,
christmas-tree wind holds, BDC anchors), and eight verified worked patterns, including a commercial grid reticle rebuilt from its manufacturer's dimensioned drawing.

It also ships `scripts/reticle.py`, which exists because the file format has no schema and **fails
silently** — a misspelled attribute is ignored and `fill="True"` reads as *false*:

- `check` reports everything the deserializer would quietly drop or misread.
- `render` writes an SVG **byte-identical** to the library's own renderer (it reproduces the same unit
  conversions, single-precision arithmetic and integer flooring), plus an ASCII raster that encodes
  stroke weight, so geometry can be reviewed without an image viewer.

No .NET and no third-party Python packages needed to check or preview a reticle.

## Structure (progressive disclosure)

```
SKILLS/ballistic-calculator/
├── SKILL.md                 # core trajectory workflow — always loaded when the skill triggers
└── references/              # specialized topics — loaded only when a task needs them
    ├── custom-drag.md       # custom / .drg / multi-BC drag curves
    ├── serialization.md     # BXml + JSON persistence, custom formats
    └── reticle.md           # building and rendering reticles

SKILLS/reticle-designer/
├── SKILL.md                 # coordinate system, workflow, gotchas
├── references/
│   ├── file-format.md       # the complete .reticle specification
│   ├── design-principles.md # how to choose the geometry
│   └── patterns.md          # worked reticle families
├── scripts/reticle.py       # check / render / preview
└── assets/*.reticle         # eight verified example reticles
```

Each `SKILL.md` stays lean so routine tasks don't pay for the specialized topics; each reference file
is pulled in on demand only when the task calls for it.

## Installation

See [INSTALL.md](INSTALL.md) for step-by-step instructions (Claude Code and Codex CLI, personal/global
and project-local).

## Requirements of the consuming project

The skill documents the API only — the app it helps you write needs the packages:

```
dotnet add package BallisticCalculator
```

which brings in `Gehtsoft.Measurements` (and, transitively, `System.Text.Json`). License: LGPL 2.1.
