# How to define a custom Bed?

You'll only need to define some new fields for a bed to work:


## Bed Spot

The Bed Spot determines where the player will appear when waking up. It is an integer [Vector](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Structures/Vector.md) mesured in **tiles**, starting from the top left of the Bounding Box.

## Bed Type

The bed type can be "Double" or "Single", a simple bed can be placed in the un-upgraded Farmhouse but will not work correctly with spouses in the upgraded Farmhouse, while a double bed cannot be placed in the un-upgraded Farmhouse.  
You can also use the the bed type "Child" to make a bed that children can sleep into.

## Bed Area

<span style="color:red">**DEPRECATED** The data in this field will be used as `Bed Area Pixel` in the next Format version</span>

This is the area of the bed **in tiles** where the player will be asked to sleep, it is a [Rectangle](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Structures/Rectangle.md).  
The player will be asked to sleep when its hitbox is fully in this rectangle.

By default, this area is the rectangle inside the bed, one tile removed from the actual bounding box. For example, a 6x4 Bed will have a 4x2 centered area where the game will ask if you want to sleep.

## Bed Area Pixel

<span style="color:red">This will be moved to `Bed Area` in the next Format version</span>

This is the area of the bed **in pixels** where the player will be asked to sleep, it is a [Rectangle](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Structures/Rectangle.md).  
The player will be asked to sleep when its hitbox is fully in this rectangle.

## Other Info

If you want more customization for Bed Furniture, make a post on the Nexus page or ping me on Discord (on the Stardew Valley server).