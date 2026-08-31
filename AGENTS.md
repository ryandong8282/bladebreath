# AGENTS.md — Wuming Zhangcheng development contract

This repository is owned by a first-time game developer and is built with AI
assistance. Agents must protect the project from scope explosion, accidental
engine complexity, and unverified “looks right” changes.

## Mission

Build a readable, responsive, iOS-first isometric action roguelite where weapons
provide the body of combat, military styles provide the combat philosophy, and
a late-Northern-Qi historical mother-body constrains the fictional world.

The source-of-truth runtime is **Unity 6000.3 LTS + C#**. The browser prototype
is a historical reference. Do not reintroduce Godot runtime files.

## Non-negotiable rules

1. Never add a major system unless the current milestone explicitly requires it.
2. Prove the player-visible combat hypothesis before enlarging content.
3. Every attack must expose startup, active, and recovery intent.
4. Mobile readability, reliable input, and 60 FPS outrank visual richness.
5. Do not add inventory, rarity, crafting, open world, networking, gacha,
   dialogue trees, or unrestricted procedural generation before the roadmap
   requires them.
6. Do not copy names, icons, animation frames, UI layouts, models, story beats,
   level layouts, or proprietary data from commercial games.
7. Avoid binary assets over 10 MB. Record every external or generated asset in
   `ASSET_LICENSE.md`.
8. One pull request should solve one player-visible problem.
9. Historical-fantasy additions must state the real anchor, fictional
   transformation, gameplay purpose, and anachronism check.
10. Never commit Unity-generated `Library`, `Temp`, `Logs`, `Obj` or exported
    Xcode build folders. Always commit relevant `.meta` files.

## Unity and C# conventions

- Types, methods and public members use PascalCase; private fields use
  `_camelCase`; locals and parameters use camelCase.
- Place project-owned code and assets under `Assets/_BladeBreath/`.
- Keep `MonoBehaviour` components narrow. Do not grow a god-class
  `PlayerController`.
- Combat resolution has one owner. UI, camera, audio and VFX observe feedback;
  they do not silently mutate combat state.
- Tunable cross-action data moves to `ScriptableObject` assets when M1 begins.
- Prefer serialized references or explicit `Configure` methods over scene-name
  lookups. Never call broad object searches every frame.
- Do not add a package until the current code genuinely needs it.
- Do not hand-edit generated `.unity`, `.prefab` or `.meta` GUID references
  unless the change is reviewed as Unity YAML.
- Keep runtime code independent from `UnityEditor`; editor-only code belongs in
  an `Editor/` directory.
- Reject silent failure: log actionable errors for required setup and assert
  invariants in development builds.

## Definition of done

Run the license-free repository check:

```bash
python3 scripts/validate_unity_project.py
```

Then open the pinned Unity version and verify:

- Console has no compile errors;
- `BladeBreath > Prototype > Create Combat Sandbox` creates and saves the scene;
- movement, attack, guard, startup parry, dodge, posture break and execution work;
- repeated `R` resets do not duplicate cameras, lights, HUD or fighters;
- Game view is readable at 16:9 and 19.5:9;
- no new warnings appear during Play Mode.

For changes touching mobile input, rendering, haptics or performance, an iPhone
test is part of done.

## Current milestone boundary

Allowed: one weapon, one training enemy, one arena, procedural primitives,
desktop controls, Unity project setup, combat feedback, and porting the two
existing styles one at a time.

Not allowed yet: second weapon, full boss production, loot economy, cinematic
narrative scenes, metagame sprawl, monetization, or network features.
