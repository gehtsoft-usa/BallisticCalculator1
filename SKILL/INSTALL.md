# Installing the BallisticCalculator skills

There are two independent skills, each a self-contained folder:

- [`SKILLS/ballistic-calculator/`](SKILLS/ballistic-calculator/) — using the library
- [`SKILLS/reticle-designer/`](SKILLS/reticle-designer/) — designing reticles and `.reticle` files

Installing one means copying that **whole folder** into a skills directory that your agent scans. Both
Claude Code and Codex CLI implement the same `SKILL.md` standard, so the same folder works in either —
only the destination directory differs.

Throughout, `SRC` is the skill folder in this repo. Substitute whichever skill you are installing, or
copy both:

```bash
SRC="$(pwd)/SKILLS/ballistic-calculator"   # run from the SKILL/ directory
SRC="$(pwd)/SKILLS/reticle-designer"       # ...or this one
```

> **Keep the folder names as they are.** Claude Code derives a skill's identity and its
> `/slash-command` from the *folder* name (the frontmatter `name:` is not used for this), and each
> folder is kept in sync with its `name:` — install `ballistic-calculator` and you get
> `/ballistic-calculator`; install `reticle-designer` and you get `/reticle-designer`. Auto-triggering
> (from the description) works regardless, but matching the names keeps the identity consistent across
> Claude Code and Codex.

---

## Claude Code

Skills are discovered automatically on startup (and picked up live when added mid-session). No
registration or config edit is needed. References in the `references/` folder load on demand because
`SKILL.md` links to them.

### Personal / global (available in every project)

```bash
mkdir -p ~/.claude/skills
cp -r "$SRC" ~/.claude/skills/
# -> ~/.claude/skills/ballistic-calculator/SKILL.md
```

### Project-local (available only in one repo; check it into version control)

```bash
mkdir -p /path/to/your-app/.claude/skills
cp -r "$SRC" /path/to/your-app/.claude/skills/
# -> /path/to/your-app/.claude/skills/ballistic-calculator/SKILL.md
```

Project skills also load when Claude Code is started in a subdirectory (it searches up to the repo root).

### Verify

There is no list command. Start Claude Code, type `/` and look for the skill in the autocomplete menu,
or ask *"what skills are available?"*. To confirm a skill triggers, ask something like:

- `ballistic-calculator` — *"write C# using the BallisticCalculator package to compute a .308
  trajectory"*
- `reticle-designer` — *"make me an mrad reticle with hash marks every half mil and holdovers out to
  10 mils"*

and check that it consults the skill.

---

## Codex CLI

Codex reads skills from `.agents/skills` directories (personal, repo, and system) and detects changes
automatically. Frontmatter (`name`, `description`) is the same standard, and `references/` is supported.

### Personal / global (available in every repo)

```bash
mkdir -p ~/.agents/skills
cp -r "$SRC" ~/.agents/skills/
# -> ~/.agents/skills/ballistic-calculator/SKILL.md
```

### Project-local (checked into a repo)

Place it under `.agents/skills` anywhere from your working directory up to the repo root:

```bash
mkdir -p /path/to/your-app/.agents/skills
cp -r "$SRC" /path/to/your-app/.agents/skills/
# -> /path/to/your-app/.agents/skills/ballistic-calculator/SKILL.md
```

### System-wide (shared machine/container image)

```bash
sudo mkdir -p /etc/codex/skills
sudo cp -r "$SRC" /etc/codex/skills/
```

### Verify

Start Codex in a project and ask it to write code against the BallisticCalculator package; confirm it
picks up the skill. (Codex also exposes a `skill-installer` helper and per-skill config under
`~/.codex/config.toml` if you prefer to manage skills that way.)

---

## Windows paths

On Windows, the equivalents are — with `<skill>` being `ballistic-calculator` or `reticle-designer`:

- Claude Code, global: `%USERPROFILE%\.claude\skills\<skill>\`
- Claude Code, project: `<repo>\.claude\skills\<skill>\`
- Codex, global: `%USERPROFILE%\.agents\skills\<skill>\`
- Codex, project: `<repo>\.agents\skills\<skill>\`

Copy the folder with `xcopy /E /I` or `Copy-Item -Recurse`.

---

## Updating / uninstalling

- **Update:** re-copy the folder over the installed one; both tools pick up changes automatically.
- **Uninstall:** delete the installed skill folder from the skills directory.

---

## Notes

- Both skills are documentation only. The app you build still needs
  `dotnet add package BallisticCalculator`.
- Keep the subfolders alongside `SKILL.md`. `references/` holds the specialized topics, loaded on
  demand. For `reticle-designer`, `scripts/reticle.py` and `assets/` must come along too — `SKILL.md`
  invokes the script by relative path and points at the example reticles.
- `reticle-designer`'s tooling needs Python 3.8+ and nothing else: no .NET and no third-party
  packages are required to check or preview a reticle.

Sources: [Claude Code — Skills](https://code.claude.com/docs/en/skills),
[OpenAI Codex — Build skills](https://developers.openai.com/codex/skills).
