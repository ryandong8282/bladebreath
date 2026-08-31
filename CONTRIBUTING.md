# Contributing

This is a prototype-first Unity repository. Contributions should improve a
measured combat problem rather than enlarge the feature list.

1. Open or reference one issue.
2. State the player-visible hypothesis.
3. Keep the diff focused.
4. Put project code and assets under `Assets/_BladeBreath/`.
5. Include reproduction and validation steps.
6. Do not add unlicensed assets.
7. Update a design document when behavior changes.
8. For historical-fantasy additions, state the real anchor, fictional
   transformation, gameplay purpose, and anachronism check.
9. Commit Unity `.meta` files, but never commit `Library/`, `Temp/`, `Logs/`,
   `Obj/` or exported Xcode build folders.

Before opening a PR:

```bash
python3 scripts/validate_unity_project.py
```

Also compile and run the affected scene in the pinned Unity editor. Static
validation is not a substitute for Play Mode or iPhone testing.

PR titles use `type(scope): summary`, for example:

- `feat(combat): add perfect-dodge Edge reward`
- `fix(input): preserve guard while moving on touch`
- `tune(enemy): lengthen delayed-slash tell`
