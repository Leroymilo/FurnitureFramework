
[Go to bottom](#24)

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

# 3.0 (**Work in Progress**)

**Format Update**:
- `Bed Area Pixel` was removed, `Bed Area` is now a Rectangle **in pixels**.
- `Seasonal` was removed. (You should now use a [mixed Content Pack](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Author.md#mixed-content-pack))
- The "token" markers have been changed from `{{MyToken}}` to `[[MyToken]]` to avoid conflicts with CP tokens.
- `Source Rect` was removed, now the first layer of `Layers` will be used as the base layer, this way it's possible to give it a custom depth and draw position. The `Layers` field is now required to be present and have at least one layer, but the new directional field parsing should make it manageable.
- Layers are now aligned to the bottom left corner of the bounding box by default. Use the `Draw Pos` to move them.
- **In Testing** Light sources went through a rework, see the [Light Source documentation](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Complex%20Fields/Light%20Sources.md) for more info.
- **In Testing** All positions that were relative to the top left of the base sprite are now relative to the *bottom left* of the *bounding box*.
- Nested all animation related fields into a `Animation` object.
- **In Testing** Renamed `Light Sources` to `Lights` since it includes both sources and glows.

**New Features**:
- **In Testing** Added `Draw Layers When Placing` field (bool) to draw layers when the player is placing the Furniture (to be used when there's no base layer, defaults to false to avoid ugly transparent layer stacking).
- Added `Animate When Placing` field (bool) to disable complex animation when the player is placing the Furniture (defaults to true).
- Added `Priority` to ensure your Furniture is loaded before/after other.
- **NOT TESTED** Added the possibility to put a list in `Animation Offset` to define the offset of every frame separately.
- Added automatic config options (in config.json and GMCM) for every Furniture and Included Pack of every Pack.
- Added CP compatibility: it is now possible to EditImage on the sprite-sheets used by a Furniture Pack and EditData on the content.json itself or any included files.
- Added a new command `ff_debug_print <ModId>` to dump all the data for any Furniture Pack for debug purposes. Feedback is wellcome, adding new info is possible.
- **NOT TESTED** `Fish Area` and `Screen Position` are now directional.

**Fixes**:
- Fixed animation not working in some cases where Animation Offset had negative coordinates.
- Fixed Particles not reseting their timers when restarting the game.
- **In Testing** Fixed lights sources and glows of Furniture not appearing in slots.
- Fixed compatibility with Precise Furniture pass-through feature.
- Renamed `reload_furniture_pack` command to `ff_reload` because it was too long.

**Optimizations**:
- Furniture Packs are only loaded when the game need them (lazy loading).
- Invalidating an included pack will only cause this included pack and its new children to reload. Any included pack that was already loaded will not be loaded again if not invalidated.
- Implemented layered lazy loading (packs -> types -> [collisions, layers, lightsources, particles, seats, slots, sounds]).

## 3.1 (**Work in Progress**)

**Work in Progress** API to attach any custom method to a Furniture action (right click), with access to some of the Furniture's data.
**Work in Progress** Customize StorageFurniture allowed item types.