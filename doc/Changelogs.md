
[Go to bottom](#planned-future-features)

# 2.0

**Format Update**

Big internal rework, changes to the Furniture definition Format:

- Token {{variant}} changed to {{ImageVariant}}.
- Added {{ModID}} token, mod UniqueID no longer prepended to avery Furniture id.
- Merged "Indoors" & "Outdoors" into "Placement Restriction" to match vanilla Furniture format.
- Nested "X" and "Y" fields of "Seats" elements inside a "Position" field.
- Complete rework of all "Depth" fields.

## 2.1

Added support for new Furniture types.

### 2.1.1

Fixed a small bug (wrong color on placement).

### 2.1.2

Fixed a small bug with slots on beds.

### 2.1.3

Fixed bugs.

## 2.2

Added Custom Description and Configurable Files Including.

## 2.3

Added support for Fish-Tanks.  
Added field "FF" in `Furniture.modData` to help with compatibility in other mods.

### 2.3.1

Fixed an issue with included files (duplicate furniture skipped even when an included pack is disabled)

### 2.3.2

Added "Max Size" field (integer vector) for slot to define a maximum size for Furniture placed in this slot.  
Fixed an issue with Rugs rotations (changed `Furniture.updateRotation` from postfix to prefix).  
Added "Bed Type" field ("Double" or "Simple") because double beds can't be placed in un-upgraded Farmhouse and simple beds cause issues on upgraded Farmhouse (spouse and respawn).

## 2.4

Custom Light Sources.

Added an option to disable the light of Custom Fishtanks.  
Added an option to define the area of the bed where the player get prompted to sleep.  

It is now possible to access assets from the game files or from the Furniture Framework files by adding "Content/" or "FF/" to a Source Image path.

### 2.4.1

Reworked beds to work around vanilla restrictions, added the "Bed Area Pixel" to be used for more precision than the "Bed Area" without breaking backwards compatibility.  
The functionality of "Bed Area Pixel" will be moved to the "Bed Area" field in the next Format Update.

Added a "Condition" option to restrict what can be placed in a slot with a Game State Query.

# 3.0

**Format Update**:
- `Bed Area Pixel` was removed, `Bed Area` is now a Rectangle **in pixels**.
- `Seasonal` was removed. (You should now use a [mixed Content Pack](Author.md#mixed-content-pack))
- The "token" markers have been changed from `{{MyToken}}` to `[[MyToken]]` to avoid conflicts with CP tokens.
- `Source Rect` was removed, now the first layer of `Layers` will be used as the base layer, this way it's possible to give it a custom depth and draw position. The `Layers` field is now required to be present and have at least one layer, but the new directional field parsing should make it manageable.
- Layers are now aligned to the bottom left corner of the bounding box by default. Use the `Draw Pos` to move them.
- Light sources went through a rework, see the [Light Source documentation](Complex%20Fields/Light%20Sources.md) for more info.
- All positions that were relative to the top left of the base sprite are now relative to the *bottom left* of the *bounding box*.
- Nested all animation related fields into a `Animation` object.
- Renamed `Light Sources` to `Lights` since it includes both sources and glows.

**New Features**:
- Added `Draw Layers When Placing` field (bool) to draw layers when the player is placing the Furniture (to be used when there's no base layer, defaults to false to avoid ugly transparent layer stacking).
- Added `Animate When Placing` field (bool) to disable complex animation when the player is placing the Furniture (defaults to true).
- Added `Priority` to ensure your Furniture is loaded before/after other.
- **NOT TESTED** Added the possibility to put a list in the `Animation`'s `Offset` to define the offset of every frame separately.
- Added automatic config options (in config.json and GMCM) for every Furniture and Included Pack of every Pack.
- Added CP compatibility: it is now possible to EditImage on the sprite-sheets used by a Furniture Pack and EditData on the content.json itself or any included files.
- Added a new command `ff_debug_print <ModId>` to dump all the data for any Furniture Pack for debug purposes. Feedback is wellcome, adding new info is possible.
- **NOT TESTED** `Fish Area` and `Screen Position` are now directional.
- Added a new config option for a "Slot interaction key" (right click by default) to interact with Furniture in slots. It doesn't work perfectly for vanilla Furniture (missing flame on torches), but it should work fine for any custom Furniture.
- There is new logic to handle overlapping custom Slots: now the mod will search for the first valid slot when you click on a pixel where multiple Slots exist in the same Furniture (doesn't work if Slots from different Furniture overlap, but let's say that it won't happen).

**Fixes**:
- Fixed animation not working in some cases where Animation Offset had negative coordinates.
- Fixed Particles not reseting their timers when restarting the game.
- Fixed lights sources and glows of Furniture not appearing in slots.
- Fixed compatibility with Precise Furniture pass-through feature.
- Renamed `reload_furniture_pack` command to `ff_reload` because it was too long.

**Optimizations**:
- Furniture Packs are only loaded when the game need them (lazy loading).
- Invalidating an included pack will only cause this included pack and its new children to reload. Any included pack that was already loaded will not be loaded again if not invalidated.
- Implemented layered lazy loading (packs -> types -> [collisions, layers, lightsources, particles, seats, slots, sounds]).

### 3.0.1

- Fixed the `ff_reload` command because it wasn't implemented correctly (oopsy).
- Changed Bed Collision to completely overwrite the default row of empty tiles letting the player go through (use the Collision Map instead).

### 3.0.2

- Fixed custom Furniture getting toggled when placed in a Slot of a custom Furniture which is toggled on.

### 3.0.3

- Removed check preventing sitting on toggleable Furniture.

### 3.0.4

- Fixed issues with asset loading and CP integration when playing in any language that isn't English.
- Added the possibility to put a list in the `Animation`'s `Frame Duration` to define the length of every frame separately.

### 3.0.5

- Fixed `ff_reload` command crashing the game because of previous changes (oopsie).
- Added the possibility of using a Color code instead of a name in all color fields.
- Colored Light Sources should now be more intuitive: if the texture provided is white, the tint of the Light Source can be controlled by the "Color" field, which defaults to "White". This shouldn't break any existing pack, but it might change the color of Light Sources in some cases.

## 3.1

**Fixes**:
- Fixed Furniture with "Force Type" set to `table` or `long table` being impossible to pick-up.
- Fixed whatever was broken with FishTanks (broken Fish Area parsing, and bubbles and decoration not being drawn).
- Furniture data is now refreshed after a CP EditData patch (oopsie again).
- Removed the default "Have a look at my wares" text in Catalogues created with FF.
- Fixed inconsistent transparency handling when placing Furniture (visual inconsistency between vanilla and modded Furniture).

**New Features**:
- Added a "default pack" with a "debug catalog" filled with all Furniture from all Packs. It is possible to target it with CP and add Custom Furniture without creating a Furniture Pack.
- Added Screen Depth for customizing TV screen and make layers over the screen easier.
- The Santa easter-egg now checks custom Slots.
- Now, every entry in a list has an optional "ID" field.
- Added config option to load all Furniture Pack on game start (instead of when Furniture is loaded, which is often when the character selection screen is loaded, so it doesn't really impact gameplay).
- (internal) Huge rework of Furniture Pack parsing for CP patches to work.

### 3.1.1 (**testing**)

**Fixes**:
- Fixed TV's Screen Position failing parsing.
- Fixed the `ff_reload` command (it was completely broken by the 3.1 update).
- Fixed Big Craftables not blocking Furniture placement.
- Fixed previously placed Furniture with variants.
- Empty string custom Description no longer hides the item tooltip.

**New Features**:
- It is now possible to specify a Furniture ID in the `ff_debug_print` command.
- **Testing** (internal) Finished Data rework by migrating Furniture Pack functions to the Pack Data class.
- Added a config option to toggle Furniture placed in Slots when the (custom) Furniture on which they are placed is toggled.

## 3.2 (**Testing**)

**New Features**:
- Added i18n support to `Display Name` and `Description`!
- Added new Special Type: `FFStorage`. Works like a `Dresser` but with configurable tabs and item conditions.
- Added `Opening Animation` and `Closing Animation` fields for all Storage Furniture.
- Added new field `Furniture Catalogue Tab` to choose in which tab of the vanilla furniture catalogue a Furnitrure shows up.

# Planned Future Features

- Modify schema to make the requirements of special furniture clearer (with oneof and stuff)
- Add ShopData field (with a way to get catalogue tabs by using CustomFields).
- Add optional ShopItemData to ShowsInShops (`Dictionary<string, ShopItemData>`).
- Add a config option to show the collision map instead of the bounding box when placing Furniture. (Object.drawPlacementBounds)
- Make a custom config menu to enable/disable Furniture and Included packs.
- API to attach any custom method to a Furniture action (right click), with access to some of the Furniture's data (modData based per-instance settings).
- Add compatibility between custom TV screen depth and existing custom channel mods (if necessary).

- For eventual 4.0 format change:
	- Remove the fucking spaces from the fucking json keys
	- Merge Dresser, FishTank and FFStorage (rename to just Storage?)
	- Put Special Furniture types into structures -> A Furniture's Special Type is whatever is not null -> mutually exclusive
	- Split `Placement Restriction` to match 1.7 data structure.
	- Find a way to make rotation more accessible?