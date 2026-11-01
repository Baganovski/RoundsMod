# Custom card reference

This is the list of things you can change on a card made with UnboundLib (`CustomCard`), which this project already references. The card uses the same frame, title, description, stat lines, and color theme as a vanilla card. `GetModName()` is its category in the Toggle Cards menu. Vanilla cards stay in the category `UNMODDED`.

A card class has two different jobs:

- **What the card says.** Title, description, stat lines, rarity, and theme. The stat lines are labels. They do not change gameplay by themselves. The art window shows the shared green pixel `BAG'S MOD`.
- **What the card does.** Set fields in `SetupCard`. When the card is picked, the game copies those fields onto the player. Use `OnAddCard` only for behaviour the copy does not cover, and undo that in `OnRemoveCard`.

Register a card once at startup with `CustomCard.BuildCard<YourCard>()`. While its category is enabled, it joins the normal pick and the sandbox card list.

Leave a field at the value in the **No change** column and that part of the player stays as it was.

## Card face

| What | Where | Values |
| --- | --- | --- |
| Title | `GetTitle()` | ALL CAPS, like the vanilla titles in `VanillaCards.md` |
| Description | `GetDescription()` | Smaller white text under the title. Sentence case, one line, about 22 characters, like `Bigger bullets`. Longer text wraps onto the stat lines. A one-off effect that is not a stock stat goes here, and the stat list stays empty. Leave the description empty when the stat lines already say the card |
| Stat lines | `GetStats()` | The numbers on the card. See below |
| Rarity | `GetRarity()` | `Common`, `Uncommon`, `Rare`. Rare is offered less often |
| Theme | `GetTheme()` | Frame color. See below |
| Art | `GetCardArt()` | `BagCardFace.Art()`. See below |
| Frame | `GetCardBase()` | Leave this. It returns the vanilla card frame |
| Category | `GetModName()` | Group name in Toggle Cards. Cards that return the same string share a category. The name is also printed faintly in the bottom-left corner of the card. Leave that corner label in place. If you do not override it, the category is `Modded` |
| In the pool | `GetEnabled()` | `true` puts it in the pick and the toggle menu. `false` hides it |
| Can be taken again | `cardInfo.allowMultiple` in `SetupCard` | `true` lets the same player find it again. `false` removes it from later picks for that player |

### Stat lines (`CardInfoStat`)

Each entry is one row on the card. The game draws the colored `amount` first, then the `stat` label, right-aligned. Keep `amount` and `stat` together on one line, about 22 characters. Put the number in `amount`. A sentence in `stat` with an empty `amount` wraps and covers the description.

| Field | What it does |
| --- | --- |
| `stat` | The label. Use the vanilla word: `DMG`, `HP`, `ATKSPD`, `AMMO`, `Reload time`, `Reload speed`, `Block cooldown`, `Bullet speed`, `Bullet bounces`, `Life steal`, `Bullets` |
| `amount` | The value, written like a vanilla card. See the wording below |
| `positive` | `true` uses the good color. `false` uses the bad color |
| `simepleAmount` | A stock phrase instead of `amount`. Use `notAssigned` to show `amount`. Other values: `aLittleBitOf`, `Some`, `aLotOf`, `aHugeAmountOf`, `slightlyLower`, `lower`, `aLotLower`, `slightlySmaller`, `smaller` |

### Wording

Match `VanillaCards.md`. The number on the card is not the field value from `SetupCard`.

| Kind | On the card | Not this |
| --- | --- | --- |
| Multiplier | `+50%`, `-25%`, `+100%` | `1.5`, `0.75`, `0.25` |
| Time | `+0.25s`, `+0.5s`, `3s`, `10s` | `0.25`, `25%` of a second, `10 seconds` on a stat |
| Count | `+3`, `+4`, `-2`, `-1` | `3 ammo` inside the amount |
| Distance | `+7m` | `7` |

A quarter second of reload is `+0.25s Reload time`. A quarter less damage is `-25% DMG`. `0.25` is the time form. `25%` is the multiplier form.

An effect that is not one of those labels is a description sentence, and `GetStats()` returns an empty array. PHOENIX says `Respawn once on death`. BRAWLER says `+200% HP 3s after dealing DMG`. LEE LUCK says `Ignore a hit per 10s`.

### Themes (`CardThemeColor.CardThemeColorType`)

`DestructiveRed`, `FirepowerYellow`, `DefensiveBlue`, `TechWhite`, `EvilPurple`, `PoisonGreen`, `NatureBrown`, `ColdBlue`, `MagicPink`.

### Art

`GetCardArt()` is abstract, so every card still returns a `GameObject`. Return `BagCardFace.Art()`. That object is the shared green pixel `BAG'S MOD`, centered in the art window. Do not assign a sprite. The faint bottom-left category name stays. The theme colors the frame. A word that pops off the player during a fight, such as `MISS`, is gameplay feedback, not a picture on the card.

## Gun stats

Set these on the `gun` argument in `SetupCard`. The game copies them onto the player's gun when the card is picked.

### Multiplied (1 = no change)

| Field | Effect |
| --- | --- |
| `damage` | Bullet damage. `1.5` is +50%, `0.5` is half. After each card, damage will not go below `0.25` |
| `attackSpeed` | Time between shots. `0.5` shoots twice as fast. `2` shoots half as fast |
| `projectileSpeed` | Bullet speed |
| `projectielSimulatonSpeed` | How fast the bullet's own simulation runs |
| `gravity` | Bullet gravity. Below 1 floats. Above 1 drops faster |
| `knockback` | Knockback dealt by a hit. Also scaled when the card adds extra bullets. See bullets below |
| `recoilMuiltiplier` | Multiplies the player's body recoil |
| `reloadTime` | Multiplies reload time. Below 1 reloads faster |
| `multiplySpread` | Multiplies bullet spread |
| `bulletDamageMultiplier` | Extra multiplier on bullet damage |
| `damageAfterDistanceMultiplier` | Damage change over the bullet's travel |
| `speedMOnBounce` | Speed kept after a bounce |
| `dmgMOnBounce` | Damage kept after a bounce |
| `chargeDamageMultiplier` | Damage multiplier while a charge shot is held. Scaled the same way as `damage` when `chargeNumberOfProjectilesTo` is not 0 |
| `timeToReachFullMovementMultiplier` | Multiplies how quickly the player reaches full move speed. Leave this at 1. A value of 0 wipes it |

### Added (0 = no change)

| Field | Effect |
| --- | --- |
| `ammo` | Added to max ammo. After the card, max ammo is clamped between 1 and 90 |
| `ammoReg` | Added to ammo regenerated over time |
| `reloadTimeAdd` | Added to reload time, in seconds |
| `numberOfProjectiles` | Extra bullets per shot. `0` adds none. `1` adds one bullet. If the player does not already fire exactly one bullet, extra bullets also scale `damage` and `knockback` so the shots share that stat |
| `bursts` | Extra shots in a burst |
| `reflects` | Extra bounces |
| `smartBounce` | Extra bounces that aim toward a target |
| `randomBounces` | Extra random bounces |
| `bulletPortal` | Extra portal hops |
| `spread` | Added spread, in degrees |
| `evenSpread` | Added even spread between bullets |
| `projectileSize` | Added bullet size |
| `size` | Added gun size |
| `timeBetweenBullets` | Added delay between bullets in a shot |
| `drag` | Added bullet drag |
| `dragMinSpeed` | Added minimum speed after drag |
| `slow` | Added slow on hit |
| `percentageDamage` | Added damage as a fraction of the target's health |
| `cos` | Added damage-over-time amount |
| `overheatMultiplier` | Added overheat |
| `chargeSpreadTo` | Spread added as a charge shot fills |
| `chargeEvenSpreadTo` | Even spread added as a charge shot fills |
| `chargeSpeedTo` | Speed added as a charge shot fills |
| `chargeRecoilTo` | Recoil added as a charge shot fills |
| `chargeNumberOfProjectilesTo` | Extra bullets added as a charge shot fills |

### Replaced only when you set them

| Field | No change | Effect |
| --- | --- | --- |
| `unblockable` | `false` | `true` makes the player's bullets unblockable |
| `ignoreWalls` | `false` | `true` makes bullets pass through walls |
| `dontAllowAutoFire` | `false` | `true` turns off holding the trigger to keep firing |
| `useCharge` | `false` | `true` turns the gun into a charge shot |
| `destroyBulletAfter` | `0` | Any other value replaces how long a bullet lives |
| `lockGunToDefault` | `false` | `true` locks attack speed. `forceSpecificAttackSpeed` becomes the fixed cooldown |
| `projectileColor` | Black | Black leaves the yellow bullet. Any other color is forced to full saturation and full brightness, so a grey becomes a pure hue |
| `projectiles` | Empty | If this has an entry, `projectiles[0].objectToSpawn` replaces the bullet prefab |
| `objectsToSpawn` | Empty | Effects attached to the bullet. See below |

### On the gun, but not copied when the card is picked

Setting these in `SetupCard` does nothing. Change them on the player's gun inside `OnAddCard`, and put the old value back in `OnRemoveCard`.

`recoil`, `shake`, `shakeM`, `forceSpecificShake`, `explodeNearEnemyRange`, `explodeNearEnemyDamage`, `waveMovement`, `teleport`, `spawnSkelletonSquare`, `hitMovementMultiplier`, `attackSpeedMultiplier`, `isProjectileGun`, `currentCharge`.

## Player stats

Set these on `statModifiers` in `SetupCard`.

### Multiplied (1 = no change)

| Field | Effect |
| --- | --- |
| `health` | Max health. `1.5` is +50% health |
| `movementSpeed` | Move speed |
| `jump` | Jump strength |
| `gravity` | Player gravity. Below 1 is floatier |
| `sizeMultiplier` | Player size |

### Added (0 = no change)

| Field | Effect |
| --- | --- |
| `numberOfJumps` | Extra jumps |
| `regen` | Added to health regeneration |
| `lifeSteal` | Added lifesteal |
| `respawns` | Extra lives |
| `secondsToTakeDamageOver` | Added time over which incoming damage is dealt |

### Flags

| Field | No change | Effect |
| --- | --- | --- |
| `refreshOnDamage` | `false` | `true` enables the player's refresh-on-damage behaviour |
| `automaticReload` | `true` | `false` turns off automatic reload. Leaving it `true` keeps reload as it was |
| `AddObjectToPlayer` | Empty | A prefab spawned on the player, such as an aura or an orbiting object |

### On the player, but not copied when the card is picked

`slow`, `slowSlow`, `fastSlow`, `tasteOfBloodSpeed`, `rageSpeed`, `attackSpeedMultiplier`.

`tasteOfBloodSpeed` and `rageSpeed` are driven by the separate `TasteOfBlood` and `Rage` behaviours, not by writing the number on the card. Attack speed for a normal card is `gun.attackSpeed`.

## Block stats

Set these on `block` in `SetupCard`.

| Field | No change | Effect |
| --- | --- | --- |
| `cdMultiplier` | `1` | Multiplies block cooldown. Below 1 blocks more often |
| `cdAdd` | `0` | Seconds added to block cooldown |
| `additionalBlocks` | `0` | Extra block charges |
| `healing` | `0` | Health restored when a block succeeds |
| `forceToAdd` | `0` | Knockback added to a block |
| `forceToAddUp` | `0` | Upward knockback added to a block |
| `autoBlock` | `false` | `true` turns on automatic blocking |
| `objectsToSpawn` | Empty | Effects spawned when a block happens |

## Bullet effects (`gun.objectsToSpawn`)

Each entry is an `ObjectsToSpawn`. The game adds it to the player's bullet. Matching effects stack instead of being added twice when `scaleStacks` is set.

| Field | Effect |
| --- | --- |
| `effect` | The effect prefab |
| `AddToProjectile` | An object parented to the bullet |
| `direction` | `forward`, `normal`, or `identity` |
| `spawnOn` | `all`, `player`, or `notPlayer` |
| `numberOfSpawns` | How many to spawn |
| `spawnAsChild` | Parent the spawn to the target |
| `normalOffset` | Offset along the hit normal |
| `stickToBigTargets` | Stick to large targets |
| `stickToAllTargets` | Stick to every target |
| `zeroZ` | Flatten the spawn onto the play plane |
| `removeScriptsFromProjectileObject` | Strip scripts from the added projectile object |
| `scaleStacks` | Stack with an effect of the same name |
| `scaleStackM` | How much each stack scales it |
| `scaleFromDamage` | Scale the effect from the bullet's damage |
| `stacks` | Starting stack count |

## Code hooks

| Method | When it runs |
| --- | --- |
| `SetupCard` | Once, when the card is built. Put stat changes here |
| `OnAddCard` | When a player receives the card. Use this for behaviour the tables above do not copy. `Player` and `HealthHandler` are on the same object |
| `OnRemoveCard` | When the card is taken off that player. Undo whatever `OnAddCard` changed, including destroying a component |

One damage instance is `HealthHandler.DoDamage(Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon, Player damagingPlayer, bool healthRemoval, bool lethal, bool ignoreBlock)`. Nothing in the tables above ignores a hit. A Harmony prefix on `DoDamage` that returns false skips that instance. Name the damage argument `damage`. A zero-length damage vector is not a hit. `PatchUtils.ApplyPatches` already applies `[HarmonyPatch]` types in this assembly.
| `OnReassignCard` | When the card is moved to another player |
| `Callback` | Each time a visible copy of the card is created |

`OnAddCard` and `OnRemoveCard` receive the live player, gun, ammo, character data, health, gravity, block, and stat modifiers.
