# Installing the BallisticCalculator skill

The skill is the folder [`SKILLS/Gehtsoft.BallisticCalculator/`](SKILLS/Gehtsoft.BallisticCalculator/)
(a `SKILL.md` plus a `references/` subfolder). Installing it means copying that **whole folder** into a
skills directory that your agent scans. Both Claude Code and Codex CLI implement the same `SKILL.md`
standard, so the same folder works in either — only the destination directory differs.

Throughout, `SRC` is the skill folder in this repo:

```bash
SRC="$(pwd)/SKILLS/Gehtsoft.BallisticCalculator"   # run from the SKILL/ directory
```

> **Folder name = invocation name (Claude Code).** Claude Code derives the `/slash-command` from the
> *folder* name, so installed as `Gehtsoft.BallisticCalculator` it is `/Gehtsoft.BallisticCalculator`.
> Auto-triggering (from the description) works regardless of folder name. If you prefer a cleaner
> command, rename the destination folder to `ballistic-calculator` when copying. The examples below keep
> the original name; change the last path segment if you want the shorter one.

---

## Claude Code

Skills are discovered automatically on startup (and picked up live when added mid-session). No
registration or config edit is needed. References in the `references/` folder load on demand because
`SKILL.md` links to them.

### Personal / global (available in every project)

```bash
mkdir -p ~/.claude/skills
cp -r "$SRC" ~/.claude/skills/
# -> ~/.claude/skills/Gehtsoft.BallisticCalculator/SKILL.md
```

### Project-local (available only in one repo; check it into version control)

```bash
mkdir -p /path/to/your-app/.claude/skills
cp -r "$SRC" /path/to/your-app/.claude/skills/
# -> /path/to/your-app/.claude/skills/Gehtsoft.BallisticCalculator/SKILL.md
```

Project skills also load when Claude Code is started in a subdirectory (it searches up to the repo root).

### Verify

There is no list command. Start Claude Code, type `/` and look for the skill in the autocomplete menu,
or ask *"what skills are available?"*. To confirm it triggers, ask something like *"write C# using the
BallisticCalculator package to compute a .308 trajectory"* and check that it consults the skill.

---

## Codex CLI

Codex reads skills from `.agents/skills` directories (personal, repo, and system) and detects changes
automatically. Frontmatter (`name`, `description`) is the same standard, and `references/` is supported.

### Personal / global (available in every repo)

```bash
mkdir -p ~/.agents/skills
cp -r "$SRC" ~/.agents/skills/
# -> ~/.agents/skills/Gehtsoft.BallisticCalculator/SKILL.md
```

### Project-local (checked into a repo)

Place it under `.agents/skills` anywhere from your working directory up to the repo root:

```bash
mkdir -p /path/to/your-app/.agents/skills
cp -r "$SRC" /path/to/your-app/.agents/skills/
# -> /path/to/your-app/.agents/skills/Gehtsoft.BallisticCalculator/SKILL.md
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

On Windows, the equivalents are:

- Claude Code, global: `%USERPROFILE%\.claude\skills\Gehtsoft.BallisticCalculator\`
- Claude Code, project: `<repo>\.claude\skills\Gehtsoft.BallisticCalculator\`
- Codex, global: `%USERPROFILE%\.agents\skills\Gehtsoft.BallisticCalculator\`
- Codex, project: `<repo>\.agents\skills\Gehtsoft.BallisticCalculator\`

Copy the folder with `xcopy /E /I` or `Copy-Item -Recurse`.

---

## Updating / uninstalling

- **Update:** re-copy the folder over the installed one; both tools pick up changes automatically.
- **Uninstall:** delete the installed `Gehtsoft.BallisticCalculator/` folder from the skills directory.

---

## Notes

- The skill is documentation only. The app you build still needs `dotnet add package BallisticCalculator`.
- Keep the `references/` subfolder alongside `SKILL.md` — the specialized topics (custom drag,
  serialization, reticles) live there and are loaded on demand.

Sources: [Claude Code — Skills](https://code.claude.com/docs/en/skills),
[OpenAI Codex — Build skills](https://developers.openai.com/codex/skills).
