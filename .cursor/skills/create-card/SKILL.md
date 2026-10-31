---
name: create-card
description: >-
  Design, critique, and implement a ROUNDS custom card with UnboundLib
  CustomCard: card face, category, gun/player/block stats, and hooks.
  Use when the user wants to create, design, balance, review, or change a
  custom card, card category, or card stats.
---

# Create a card

Design the card, give feedback, then implement it in this project. Read [CustomCards.md](../../../CustomCards.md) for every field and its no-change value, and [lessons.md](lessons.md) for preferences from earlier cards. Do not invent fields that are not in that catalog.

## Before writing code

1. Read `CustomCards.md` and `lessons.md`.
2. Restate the card in one sentence: who it helps, and what changes in a fight.
3. Fill the card face and the gameplay together. Stat lines are labels. Only `SetupCard` and the hooks change the game.
4. Give the feedback below in the reply, then write the class. If the user only asked for a concept, stop after the feedback and wait.

Use `LeeLuck.Category` (`Bag's Mod`) for `GetModName()`. Vanilla cards stay in `UNMODDED`. The project compiles as C# 7.1.

## Required on every card

A class in `RoundsWithFriends/Cards/` under namespace `RWF.Cards`, registered once from `RWFMod.Start` with `CustomCard.BuildCard<TheCard>()`.

| Piece | Rule |
| --- | --- |
| `GetTitle()` | Short name on the card |
| `GetDescription()` | Short white sentence for a unique effect. Leave empty only when the stat lines already say the whole card |
| `GetStats()` | One `CardInfoStat` per number. The game draws the colored `amount` first, then the `stat` label. Green (`positive = true`) helps the picker. Red is a cost |
| `GetRarity()` | `Common`, `Uncommon`, or `Rare`. Rare is offered less often |
| `GetTheme()` | A `CardThemeColor.CardThemeColorType` that matches the fantasy |
| `GetModName()` | The shared category. Printed faintly on the card and used by Toggle Cards |
| `GetEnabled()` | `true` unless the card should be hidden |
| `GetCardArt()` | Required by UnboundLib, but these cards have no picture. Return `new GameObject("CardArt")` and do not load or embed a sprite |
| `GetCardBase()` | Do not override |
| `SetupCard` | Set only copied fields. Leave a field at its no-change value to leave the player alone. Set `cardInfo.allowMultiple` here |
| `OnAddCard` / `OnRemoveCard` | Behaviour the catalog does not copy. `OnRemoveCard` undoes `OnAddCard`, including destroying a component when its stack count hits 0 |

`attackSpeed` below 1 shoots faster. `damage` cannot end under `0.25`. Max ammo ends between 1 and 90. Extra `numberOfProjectiles` also splits `damage` and `knockback` when the gun does not already fire exactly one bullet. `timeToReachFullMovementMultiplier` at 0 wipes the stat; leave it at 1.

Register the card from `RWFMod.Start`, next to `Unbound.RegisterMenu`:

```csharp
using UnboundLib.Cards;
using RWF.Cards;
// ...
CustomCard.BuildCard<TheCard>();
```

`PatchUtils.ApplyPatches` already calls `Harmony.PatchAll` on this assembly. A `[HarmonyPatch]` in the card file is enough. Do not register that patch again.

## Behaviour the catalog cannot copy

`Player` and `HealthHandler` are on the same GameObject. Add a component to `player.gameObject` in `OnAddCard`. One damage instance is `HealthHandler.DoDamage(Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon, Player damagingPlayer, bool healthRemoval, bool lethal, bool ignoreBlock)`. A prefix that returns false skips that call. Name the prefix argument `damage`, or Harmony will not pass the vector in. Ignore a zero-length `damage` without spending a charge.

An upside with no cost is Uncommon or Rare, and `allowMultiple` stays false unless the user asked for copies. If copies can still stack, keep one component and shorten its timer by the stack count.

Do not add artwork to new cards. `GetCardArt()` still has to return a `GameObject` because the method is abstract; return an empty object and leave the middle of the card blank. A gameplay word, such as `MISS` when Lee Luck ignores a hit, can rise off the player and fade. That is fight feedback, not a picture on the card.

After the class compiles, build Debug:

```powershell
dotnet build RoundsWithFriends/RoundsWithFriends.csproj -c Debug
```

Tell the user to restart ROUNDS. The card shows in its category while that category is enabled, in the normal pick and the sandbox list.

## Feedback

Write this before the code, in plain sentences:

- **Face.** Title, description, each stat line (`stat`, `amount`, `positive`), rarity, theme, and category. These cards have no picture.
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
        protected override GameObject GetCardArt() => new GameObject("CardArt");
        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Common;
        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.FirepowerYellow;
        public override string GetModName() => LeeLuck.Category;
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
