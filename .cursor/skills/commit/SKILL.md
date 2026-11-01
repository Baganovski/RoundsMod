---
name: commit
description: >-
  Updates the Bag's Mod credits from staged player-facing changes, suggests
  rule or skill updates learned from that work, then commits and pushes. Use
  only when the user invokes /commit.
disable-model-invocation: true
---

# /commit

Commit and push the staged changes. Before the commit, update the Bag's Mod credits when the staged diff includes a player-facing change in this local build, and suggest any rule or skill update the staged work justifies.

## Steps

1. Run `git status`, `git diff --cached`, and `git log -8 --oneline` in parallel.
2. If nothing is staged, stop and say so. Do not stage other files to invent a commit.
3. Read the staged diff. If it changes something a player sees in ROUNDS, and the Bag's Mod credits do not already say it, add one sentence to the `BagsCreditsName` description array in `RoundsWithFriends/RoundsWithFriends.cs`.
   - Credit only a change a player sees in ROUNDS: a card, menu, match rule, or other in-game behavior.
   - Do not credit work on this project: skills, rules, docs, build scripts, or credits wording.
   - Name the thing and what the player now sees or can do. Include the concrete detail (what it does, where it shows, the number or feedback), and stop there. Match these lines:
     - The main menu shows a green pixel BAG'S MOD under the ROUNDS logo, and hides it on other pages.
     - The corner Discord and website links are hidden.
     - Matches can be set to 3 or 5.
     - Lee Luck ignores one hit every 10 seconds and shows MISS when it blocks.
   - Do not duplicate an existing line, and do not add a name back to the Rounds With Friends credits.
4. If the credits text changed, run `dotnet build RoundsWithFriends/RoundsWithFriends.csproj -c Debug`, then stage only `RoundsWithFriends/RoundsWithFriends.cs`. Do not stage unrelated unstaged work or build output.
5. Before committing, learn from the staged diff. Read the project rules in `.cursor/rules` and the project skills in `.cursor/skills` that the changed files touch, including a `lessons.md` a skill says to keep. Suggest an update only when the work shows one of these and the file does not already say it:
   - The user corrected a fact, rejected an approach, or stated a preference that a later task would need.
   - A rule or skill was wrong, incomplete, or skipped a step that caused rework.
   - The change established a reusable default for this project.
   Name the file and the short change. Prefer the skill that owns the work, or its `lessons.md`, over a new rule. Skip one-off code, credits, and docs. If nothing qualifies, say there is nothing to add. Do not edit or stage rules or skills in this step.
6. Commit only what is staged. Follow the git commit protocol. On PowerShell, pass the message with a here-string:

```powershell
git commit -m @"
Short message about why.

"@
```

7. Push the current branch with `git push`. Do not force-push. If the branch has no upstream, use `git push -u origin HEAD`.
8. Run `git status` and tell the user the commit and push result. Include the rule or skill suggestions from step 5, or say there is nothing to add. If the credits text changed, say to restart ROUNDS.
