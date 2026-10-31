---
name: create-card
description: >-
  Design, critique, and implement a ROUNDS custom card with UnboundLib
  CustomCard: card face, category, art, gun/player/block stats, and hooks.
  Use when the user wants to create, design, balance, review, or change a
  custom card, card art, card category, or card stats.
---

# Create a card

Design the card, give feedback, then implement it in this project. Read [CustomCards.md](../../../CustomCards.md) for every field and its no-change value, and [lessons.md](lessons.md) for preferences from earlier cards. Do not invent fields that are not in that catalog.

## Before writing code

1. Read `CustomCards.md` and `lessons.md`.
2. Restate the card in one sentence: who it helps, and what changes in a fight.
3. Fill the card face and the gameplay together. Stat lines are labels. Only `SetupCard` and the hooks change the game.
4. Give the feedback below in the reply, then write the class. If the user only asked for a concept, stop after the feedback and wait.

Reuse the category string already used by cards in `RoundsWithFriends/Cards/`. If none exist yet, pick one short category name, use it, and record it in `lessons.md`. Vanilla cards stay in `UNMODDED`.

## Required on every card

A class in `RoundsWithFriends/Cards/` under namespace `RWF.Cards`, registered once from `RWFMod.Start` with `CustomCard.BuildCard<TheCard>()`.

| Piece | Rule |
| --- | --- |
| `GetTitle()` | Short name on the card |
| `GetDescription()` | What it feels like. Leave empty if the stat lines already say it |
| `GetStats()` | One `CardInfoStat` per visible change. `positive` matches whether that row helps the picker |
| `GetRarity()` | `Common`, `Uncommon`, or `Rare`. Rare is offered less often |
| `GetTheme()` | A `CardThemeColor.CardThemeColorType` that matches the fantasy |
| `GetModName()` | The shared category. Printed faintly on the card and used by Toggle Cards |
| `GetEnabled()` | `true` unless the card should be hidden |
| `GetCardArt()` | Required. A `GameObject` with a `SpriteRenderer`, or `null` when there is no art yet. Square PNG, transparent background, same bold illustrated style as vanilla art. Say when it is null |
| `GetCardBase()` | Do not override |
| `SetupCard` | Set only copied fields. Leave a field at its no-change value to leave the player alone. Set `cardInfo.allowMultiple` here |
| `OnAddCard` / `OnRemoveCard` | Only for fields `CustomCards.md` says are not copied. `OnRemoveCard` undoes `OnAddCard` |

`attackSpeed` below 1 shoots faster. `damage` cannot end under `0.25`. Max ammo ends between 1 and 90. Extra `numberOfProjectiles` also splits `damage` and `knockback` when the gun does not already fire exactly one bullet. `timeToReachFullMovementMultiplier` at 0 wipes the stat; leave it at 1.

After the class compiles, build Debug:

```powershell
dotnet build RoundsWithFriends/RoundsWithFriends.csproj -c Debug
```

Tell the user to restart ROUNDS. The card shows in its category while that category is enabled, in the normal pick and the sandbox list.

## Feedback

Write this before the code, in plain sentences:

- **Face.** Title, description, each stat line (`stat`, `amount`, `positive`), rarity, theme, category, and whether art is included.
- **Play.** The fields you will set, the value, and the no-change value you are departing from. Name any hook and what `OnRemoveCard` restores.
- **Read.** Whether a player can tell the effect from the card alone. Call out a stat line that does not match the field, or a field that will not copy from `SetupCard`.
- **Strength.** What the card gives, what it costs, and how it feels after two copies if `allowMultiple` is true. Prefer a real downside or a higher rarity over a card that is only upside.
- **Fit.** How it sits next to vanilla cards of that rarity. Say if it is too broad, too similar to a vanilla card, or fine.

Change the design when the feedback finds a mismatch. Do not implement a card whose stat lines describe a different effect from the fields.

## Skeleton

```csharp
using UnityEngine;

namespace RWF.Cards
{
    public class CardName : UnboundLib.Cards.CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            cardInfo.allowMultiple = true;
        }

        protected override string GetTitle() => "Card Name";
        protected override string GetDescription() => "";
        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat { stat = "Damage", amount = "+50%", simepleAmount = CardInfoStat.SimpleAmount.notAssigned, positive = true }
        };
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Common;
        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.FirepowerYellow;
        public override string GetModName() => "Category";
        public override bool GetEnabled() => true;
    }
}
```

Confirm method signatures against `UnboundLib.Cards.CustomCard` if a build error says an override does not match. `simepleAmount` is the game's spelling.

## Learn

At the end of a card task, update [lessons.md](lessons.md) when any of these happened:

- The user corrected a fact, rejected a design, or stated a preference (category name, power, naming, art, rarity).
- A field behaved differently from `CustomCards.md`. Fix `CustomCards.md` too, then add one line in `lessons.md` pointing at that correction.
- A new card established a default worth reusing.

Add a short bullet under the matching heading. Do not repeat a lesson that is already there. Change this `SKILL.md` only when the workflow itself was wrong.
