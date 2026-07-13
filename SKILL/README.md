# BallisticCalculator — Agent Skill

A self-contained **Agent Skill** that teaches an AI coding assistant (Claude Code, Codex CLI, or any
tool that supports the `SKILL.md` standard) how to use the **`BallisticCalculator`** .NET NuGet package
correctly — without reading the library source or decompiling the package to rediscover its API.

The skill lives in [`SKILLS/Gehtsoft.BallisticCalculator/`](SKILLS/Gehtsoft.BallisticCalculator/).

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

## Structure (progressive disclosure)

```
SKILLS/Gehtsoft.BallisticCalculator/
├── SKILL.md                 # core trajectory workflow — always loaded when the skill triggers
└── references/              # specialized topics — loaded only when a task needs them
    ├── custom-drag.md       # custom / .drg / multi-BC drag curves
    ├── serialization.md     # BXml + JSON persistence, custom formats
    └── reticle.md           # building and rendering reticles
```

`SKILL.md` stays lean (core workflow) so routine trajectory tasks don't pay for the specialized
topics; each reference file is pulled in on demand only when the task calls for it.

## Installation

See [INSTALL.md](INSTALL.md) for step-by-step instructions (Claude Code and Codex CLI, personal/global
and project-local).

## Requirements of the consuming project

The skill documents the API only — the app it helps you write needs the packages:

```
dotnet add package BallisticCalculator
```

which brings in `Gehtsoft.Measurements` (and, transitively, `System.Text.Json`). License: LGPL 2.1.
