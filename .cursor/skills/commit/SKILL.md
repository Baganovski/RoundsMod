---
name: commit
description: >-
  Updates the Bag's Mod credits from staged player-facing changes, then commits
  and pushes. Use only when the user invokes /commit.
disable-model-invocation: true
---

# /commit

Commit and push the staged changes. Before the commit, update the Bag's Mod credits when the staged diff includes a player-facing change in this local build.

## Steps

1. Run `git status`, `git diff --cached`, and `git log -8 --oneline` in parallel.
2. If nothing is staged, stop and say so. Do not stage other files to invent a commit.
3. Read the staged diff. If it changes something a player would notice, and the Bag's Mod credits do not already say it, add one short line to the `BagsCreditsName` description array in `RoundsWithFriends/RoundsWithFriends.cs`.
   - Skip docs, rules, this skill, and wording-only credits edits.
   - Do not add a name back to the Rounds With Friends credits.
   - Keep the line brief and player-facing. Do not duplicate an existing line.
4. If the credits text changed, run `dotnet build RoundsWithFriends/RoundsWithFriends.csproj -c Debug`, then stage only `RoundsWithFriends/RoundsWithFriends.cs`. Do not stage unrelated unstaged work or build output.
5. Commit only what is staged. Follow the git commit protocol. On PowerShell, pass the message with a here-string:

```powershell
git commit -m @"
Short message about why.

"@
```

6. Push the current branch with `git push`. Do not force-push. If the branch has no upstream, use `git push -u origin HEAD`.
7. Run `git status` and tell the user the commit and push result. If the credits text changed, say to restart ROUNDS.
