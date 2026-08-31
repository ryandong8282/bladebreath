# Project status

## Current milestone

**M0.3 — Unity migration and combat-baseline recovery**

The source-of-truth runtime has moved from Godot 4.7.2 / GDScript to Unity
6000.3.23f1 / C#. The previous Godot implementation remains available through
Git history; `prototype-web/` remains in the repository as a behavior reference.

## Implemented in the Unity migration baseline

- Unity Hub-recognizable project structure;
- URP 17.3.0 and Input System 1.16.0 package declarations;
- runtime-generated greybox arena and fixed isometric camera;
- camera-relative movement;
- attack, guard, startup parry, dodge invulnerability;
- health, posture, recovery, posture break and contextual execution;
- deterministic enemy with readable normal and unblockable delayed attacks;
- runtime HUD and clean reset loop;
- editor menu that creates `CombatSandbox.unity` and applies iOS defaults;
- license-free repository validation in GitHub Actions.

## Validation state

- Repository structure and basic C# source checks: passed locally before commit.
- Git tree boundary: Godot runtime paths removed from the migration branch.
- Unity Package Manager restore: not yet verified in a real Unity editor.
- Unity C# compiler and Play Mode: not yet verified in a real Unity editor.
- iPhone touch input, safe area, haptics, thermals and frame rate: not yet tested.

The Python validator deliberately does not pretend to be a Unity compiler.

## Functionality still to recover from M0.2

1. longblade three-hit chain and input buffer;
2. active-window weapon clashes and clash levels;
3. Hearing Blade / 听刃 and Flowing Shadow / 流影;
4. four prototype skills and Edge / 锋意;
5. initiative / 势权 decision;
6. touch joystick and action buttons;
7. hit stop, camera impulse, audio, haptics and VFX.

## Next gate

Open the project with the pinned Unity version, fix any real editor/compiler
errors, generate the combat sandbox, and play the duel before adding roguelite
rooms or more content. The migration is successful only when the Unity version
recovers the old prototype’s useful decisions without inheriting unnecessary
complexity.
