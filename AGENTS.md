# AGENTS.md — Wuming Zhangcheng development contract

This repository is developed by a non-game-developer owner with AI assistance.
Agents must protect the project from scope explosion and hidden complexity.

## Mission

Build a readable, responsive, iOS-first isometric action game where weapons
provide the body of combat, military styles provide the combat philosophy, and
a late-Northern-Qi historical mother-body constrains the fictional world.

## Non-negotiable rules

1. Never add a major system unless the current milestone explicitly requires it.
2. Combat logic must remain data-oriented; do not scatter weapon/style checks
   through unrelated UI or movement code.
3. Every attack must expose startup, active, and recovery intent even when the
   greybox animation is procedural.
4. Mobile readability and 60 FPS are higher priority than visual richness.
5. Do not add inventory, rarity, crafting, open world, networking, gacha,
   dialogue trees, or procedural level generation before M2.
6. Do not copy names, icons, animation frames, UI layouts, models, story beats,
   or proprietary data from commercial games. Mechanical inspiration must be
   expressed through original implementation and presentation.
7. Avoid binary assets over 10 MB. Record every external asset license in
   `ASSET_LICENSE.md`.
8. One pull request should solve one player-visible problem.
9. Historical fantasy is not permission for dynasty soup: record the real anchor, fictional transformation, and gameplay purpose for important visual or narrative additions.
10. Do not add downloaded reference images to the repository without explicit license verification and attribution.

## Code conventions

- GDScript identifiers and file names: English snake_case.
- Player-facing text and design documents may be Chinese.
- Prefer small components and signals over a giant player script.
- Avoid hard-coded NodePath chains across scenes.
- Put tunable combat values in resources or clearly grouped constants.
- Every state transition must have one owner.
- Reject silent failures; use assertions for required references in debug builds.

## Definition of done

Before a PR is ready:

```bash
GODOT_BIN=/path/to/godot scripts/validate.sh
```

Then manually verify:

- keyboard movement and attacks;
- touch overlay layout at 16:9 and 19.5:9;
- parry, guard, dodge, perfect dodge, clash;
- both styles can gain and spend Edge;
- player death and enemy death can restart cleanly;
- no new warnings in the Godot debugger.

## First milestone boundaries

Allowed: one weapon, two styles, one enemy, one arena, procedural primitives, short setting text, and a browser reference implementation.
Not allowed: second weapon, full boss production, loot economy, cinematic narrative scene, metagame, or save system.
