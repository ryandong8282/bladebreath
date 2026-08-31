# Public repository publishing

Repository:

```text
https://github.com/ryandong8282/bladebreath
```

The repository is public and uses `main` as its default branch.

## macOS

Double-click `PUBLISH_TO_GITHUB.command`, or run:

```bash
./scripts/push_existing_repo.sh
```

## Windows

Double-click `PUBLISH_TO_GITHUB.bat`.

GitHub authentication is handled by GitHub CLI when it is already logged in, or by Git Credential Manager during `git push`.

## What the publisher does

1. Runs the project validation suite.
2. Sets the branch name to `main`.
3. Points `origin` at `ryandong8282/bladebreath`.
4. Fetches remote `main` and refuses to overwrite unmerged remote work.
5. Pushes the current branch and any local tags.

The script never uses `--force`.
