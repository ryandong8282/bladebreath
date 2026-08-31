# Changelog

## 0.3.1 — Unity source-control hardening

- added stable `.meta` files for every tracked Unity asset and folder;
- removed the obsolete `com.unity.modules.input` manifest entry while retaining the Input System package;
- expanded repository validation to reject missing, duplicate or orphan Unity metadata;
- reject a lowercase root `assets/` directory because it conflicts with Unity's `Assets/` directory on case-insensitive macOS file systems.

## 0.3.0 — Unity migration baseline

- changed the source-of-truth runtime from Godot 4.7.2 / GDScript to Unity
  6000.3.23f1 / C#;
- added URP 17.3.0 and Input System 1.16.0 project dependencies;
- removed Godot runtime scenes, resources and scripts from the current tree while
  preserving them in Git history;
- ported a first playable subset: movement, attack, guard, startup parry, dodge,
  health, posture, stagger, execution, deterministic enemy, HUD and reset;
- added runtime greybox generation and an editor command to create
  `CombatSandbox.unity`;
- added iOS landscape / IL2CPP project defaults;
- replaced Godot CI with license-free Unity repository validation;
- retained `prototype-web/` as a historical behavior reference;
- documented the feature gap that remains before Unity reaches M0.2 parity.

## 0.2.0 — Zhangcheng setting and browser combat preview

- renamed the product direction to **无铭：漳城夜 / Wuming: Night of Zhangcheng** while keeping BladeBreath as the internal codename;
- established a late-Northern-Qi historical mother-body with fictional dynasty, geography and institutions;
- added the Great Heng, Zhangjing, the Statue Bureau, the Nameless One and the fall-of-the-capital narrative framework;
- replaced the generic jade-fantasy art direction with limestone sculpture, residual pigment, kiln fire, tomb-procession and bronze-red visual rules;
- added a historical bible, anachronism redlines and research references;
- added a dependency-free HTML Canvas combat prototype with keyboard, mouse and touch controls;
- added the experimental initiative / 势权 display, combat feedback and particle presentation;
- added automated browser-combat smoke validation and an optional local single-file builder;
- updated roadmap, backlog, wish pool, asset pipeline and owner playtest guide.

## 0.1.0 — M0 combat greybox

- procedural isometric 3D arena and placeholder combatants;
- longblade three-hit attack chain;
- guard, startup parry window and posture pressure;
- directional dodge with invulnerability and perfect-dodge window;
- active weapon clashes with win, even and loss outcomes;
- posture break and contextual execution;
- Hearing Blade and Flowing Shadow styles with two skills each;
- deterministic training enemy with normal, delayed, fast and unblockable attacks;
- keyboard, mouse and multi-touch input;
- runtime HUD, telegraphs, logs and restart loop;
- static validation, engine smoke-test workflow and publishing script.
