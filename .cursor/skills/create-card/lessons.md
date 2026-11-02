# Card lessons

Preferences and corrections from making cards in this project. The create-card skill reads this before designing a card and appends to it when something new is learned.

## Category

- Custom cards use their own category via `GetModName()`. Vanilla cards stay in `UNMODDED`.
- The category name is `Bag's Mod`. Later cards reuse `LeeLuck.Category`.

## Art

- Do not put a picture on custom cards. `GetCardArt()` returns `BagCardFace.Art()`, the shared green pixel `BAG'S MOD` centered in the art window. The faint bottom-left category name stays.
- Match title, description, and stat lines to `VanillaCards.md`. The reference screenshots are no longer in the repo.
- When Lee Luck ignores a hit, the word `MISS` pops up from the player and fades. That feedback is separate from the card face.
- While Lee Luck is charged, the player has a soft green glow. It goes out when the ignore is spent and comes back when the wait ends.

## Design

- Stat lines are labels. Gameplay comes from `SetupCard`, or from `OnAddCard` when no catalog field does the job.
- The user wants feedback on the card itself: whether the face matches the effect, and whether the strength fits the rarity.
- Card text wraps in a narrow box and a wrapped description sits on top of a wrapped stat. A description stays on one line at about 22 characters. A stat line is `amount` plus `stat` on one line, about 22 characters together. Do not put a sentence in `stat` with an empty `amount`.
- Write the face like `VanillaCards.md`. Titles are ALL CAPS. A multiplier is a signed percent (`-25%`, `+50%`), never the field value (`0.75`) and never a bare decimal. A time is seconds with `s` on the number (`+0.25s`, `+0.5s`, `10s`, `3s`). A count is a signed integer (`+3`, `-2`). Labels are the short vanilla words: `DMG`, `HP`, `ATKSPD`, `AMMO`, `Reload time`, `Block cooldown`. An effect that is not one of those stats is a sentence in the description, with no stat line, as on PHOENIX and BRAWLER.

## Mechanics

- The field catalog and no-change values live in `CustomCards.md` at the project root. Update that file when a mechanic turns out different, and note the correction here.
- Do not `Object.Destroy` a card effect component in `OnRemoveCard`. Sandbox calls `OnRemoveCard` and then `OnAddCard` for the same card in one frame. `Destroy` waits until the end of the frame, `GetComponent` still finds the dying component, and the effect disappears after the stats have already been copied. Lower `stacks` and skip the effect while `stacks` is 0. Hide anything visual when the last stack comes off.
- Ignoring a hit is not a copied stat. `HealthHandler.DoDamage(Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon, Player damagingPlayer, bool healthRemoval, bool lethal, bool ignoreBlock)` applies one damage instance. A prefix that returns false skips that instance. Put the timer on a component added in `OnAddCard` and set its stacks to 0 in `OnRemoveCard`.
- Lee Luck ignores the first bullet as soon as it is picked, then one bullet every 10 seconds. That includes another player's bullet and a bullet you fired that comes back and hits you. The ignore runs in a prefix on `ProjectileHit.RPCA_DoHit` and returns false, so the bullet passes through and parasite or poison never starts. It is not spent while blocking, dead, or in cease-fire, and it is not spent on explosions, saws, or other non-bullet damage. A bullet with no damage does not spend it. It is Uncommon, cannot be taken twice, and has no downside. Extra copies, if something adds them, shorten that wait. The green glow only sorts itself when it turns on.
- `gun.ammo` in `SetupCard` adds a flat amount. Do not double `gun.ammo` or `GunAmmo.maxAmmo` in `OnAddCard`. An absolute magazine write fights cards that add bullets, such as BARRAGE, and can leave one ammo that still fires the spread.
- `projectileColor` black leaves the yellow bullet. Any other color is averaged, then forced to full saturation and full brightness, so a near-grey with a slightly higher blue channel becomes pure blue for every player. Paint the spawned bullet in a postfix on `Gun.ApplyProjectileStats` when the color has to stay grey.
- Surface-only drops on bullets: register in a postfix on `Gun.ApplyProjectileStats` with `ProjectileHit.AddHitActionWithData`. `RPCA_DoHit` invokes that after it builds `HitInfo` and after `Block.DoBlock` returns on blocked hits, same as vanilla `TracerRound` and `TrickShot`. Lee Luck's prefix on `RPCA_DoHit` only returns false when `FindHealth(viewID)` finds a player; on floors, walls, and boxes `FindHealth` is null and the full method runs, so the hit action still fires with Lee Luck or 3D Printed Bullets. Skip player bodies with `GetComponentInParent<HealthHandler>()` on the hit transform or collider transform. Do not rely on Harmony postfixes on `Hit` or `RPCA_DoHit` with manual `BuildHitInfo` or `view.IsMine` gates for the drop. Do not add a custom `RayHitEffect` and append it in a postfix on `ProjectileHit.Start` with ascending `effects.Sort`; ROUNDS sorts `effects` descending in `ResortHitEffects`. Anchor to the hit `Collider2D` and each `FixedUpdate` destroy the mine if that collider is missing, disabled, or inactive; otherwise move the kinematic mine with `MovePosition` / `MoveRotation` from local offset on that collider's transform. Do not parent a trigger collider onto the block.
