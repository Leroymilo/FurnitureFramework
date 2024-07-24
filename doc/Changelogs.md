
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

Added an option to disable the light of Custom Fishtanks
Added an option to define the area of the bed where the player get prompted to sleep.

It is now possible to access assets from the game files or from the Furniture Framework files by adding "Content/" or "FF/" to a Source Image path.

### 2.4.1

Reworked beds to work around vanilla restrictions, added the "Bed Area Pixel" to be used for more precision than the "Bed Area" without breaking backwards compatibility.  
The functionality of "Bed Area Pixel" will be moved to the "Bed Area" field in the next Format Update.

Added a "Condition" option to restrict what can be placed in a slot with a Game State Query.

### 2.4.2

Fixed an issue with Particles that would not reset their timers when restarting the game.