# Card lessons

Preferences and corrections from making cards in this project. The create-card skill reads this before designing a card and appends to it when something new is learned.

## Category

- Custom cards use their own category via `GetModName()`. Vanilla cards stay in `UNMODDED`.
- The category name is `Bag's Mod`. Later cards reuse `LeeLuck.Category`.

## Art

- Art should match the vanilla cards: square, transparent background, bold illustrated style. The frame, title, and stat lines stay the game's.
- Vanilla face reference screenshots are `RoundsWithFriends/Cards/Reference/unmodded-frost-slam.png` and `big-bullet.png`. The colored number is the stat amount, then the label. Green helps the picker. Red is a cost. A unique effect is a short white description.
- Lee Luck has no art yet. Do not reuse the vanilla screenshots as its picture. `GetCardArt()` is abstract, so return `null` until there is art.

## Design

- Stat lines are labels. Gameplay comes from `SetupCard`, or from `OnAddCard` when the catalog says the field is not copied.
- The user wants feedback on the card itself: whether the face matches the effect, and whether the strength fits the rarity.

## Mechanics

- The field catalog and no-change values live in `CustomCards.md` at the project root. Update that file when a mechanic turns out different, and note the correction here.
- Ignoring a hit is not a copied stat. `HealthHandler.DoDamage(Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon, Player damagingPlayer, bool healthRemoval, bool lethal, bool ignoreBlock)` applies one damage instance. A prefix that returns false skips that instance. Put the timer on a component added in `OnAddCard` and destroy it in `OnRemoveCard`.
- Lee Luck ignores the first hit as soon as it is picked, then one hit every 5 seconds. Zero-damage calls do not spend the ignore. It is Uncommon, cannot be taken twice, and has no downside. Extra copies, if something adds them, shorten that wait.
