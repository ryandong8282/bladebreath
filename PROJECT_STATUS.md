# Project status

## Current milestone

**M0.2 — Zhangcheng setting and playable combat validation**

The repository currently contains two coordinated prototypes:

- a dependency-free browser combat build for immediate playtesting;
- a Godot 4.7.2 project that is the source of truth for the future iOS game.

Implemented combat features include longblade attacks, guard and startup parry, directional dodge and perfect dodge, weapon clashes, posture break, execution, two styles, four skills, touch controls, HUD and a deterministic training opponent.

## Validation state

- Static GDScript/resource validation: passed.
- Browser combat logic smoke test: passed.
- Optional standalone HTML builder: included; generated output is ignored by Git.
- Godot runtime smoke test: configured in GitHub Actions; local runtime validation requires a Godot 4.7.2 Standard binary.
- iPhone device input, thermals and frame rate: not yet validated.

## Next gate

Do not expand content yet. The next milestone is to run the Godot project, repair any engine-level errors, then test the duel on an actual iPhone and tune:

1. input reliability;
2. attack readability;
3. parry and perfect-dodge windows;
4. hit stop, camera impulse and haptics;
5. whether Hearing Blade and Flowing Shadow genuinely create different decisions.
