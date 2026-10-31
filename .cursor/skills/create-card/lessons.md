# Card lessons

Preferences and corrections from making cards in this project. The create-card skill reads this before designing a card and appends to it when something new is learned.

## Category

- Custom cards use their own category via `GetModName()`. Vanilla cards stay in `UNMODDED`.
- The category name is `Bag's Mod`. Later cards reuse `LeeLuck.Category`.

## Art

- Do not put artwork on custom cards. `GetCardArt()` is still required; return an empty `GameObject` and leave the art window blank.
- Vanilla face reference screenshots are `RoundsWithFriends/Cards/Reference/unmodded-frost-slam.png` and `big-bullet.png`. Match that layout for title, description, and stat lines.
- When Lee Luck ignores a hit, the word `MISS` pops up from the player and fades. That feedback is separate from the card face.

## Design

- Stat lines are labels. Gameplay comes from `SetupCard`, or from `OnAddCard` when no catalog field does the job.
- The user wants feedback on the card itself: whether the face matches the effect, and whether the strength fits the rarity.
- A unique effect is a short description plus a colored stat whose amount is the number (`Every 10s`) and whose label is the effect (`Ignore a hit`).

## Mechanics

- The field catalog and no-change values live in `CustomCards.md` at the project root. Update that file when a mechanic turns out different, and note the correction here.
- Ignoring a hit is not a copied stat. `HealthHandler.DoDamage(Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon, Player damagingPlayer, bool healthRemoval, bool lethal, bool ignoreBlock)` applies one damage instance. A prefix that returns false skips that instance. Put the timer on a component added in `OnAddCard` and destroy it in `OnRemoveCard`.
- Lee Luck ignores the first hit as soon as it is picked, then one hit every 10 seconds. Zero-damage calls do not spend the ignore. It is Uncommon, cannot be taken twice, and has no downside. Extra copies, if something adds them, shorten that wait.
