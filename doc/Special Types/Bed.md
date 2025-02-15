# How to define a custom Bed?

You'll only need to define some new fields for a bed to work:


## Bed Spot

The Bed Spot determines where the player will appear when waking up. It is an integer [Vector](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Structures/Vector.md) mesured in **tiles**, starting from the top left of the Bounding Box.

## Bed Type

The bed type can be "Double" or "Single", a simple bed can be placed in the un-upgraded Farmhouse but will not work correctly with spouses in the upgraded Farmhouse, while a double bed cannot be placed in the un-upgraded Farmhouse.  
You can also use the the bed type "Child" to make a bed that children can sleep into.

## Bed Area

This is the area of the bed **in pixels** where the player will be asked to sleep, it is a [Rectangle](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Structures/Rectangle.md).  
The player will be asked to sleep when its hitbox is fully in this rectangle.  
By default, it will be defined as :
- Bed Area Size = (max(16, Bed Width - 32), max(16, Bed Height - 32))
- Bed Area Position = (Bed Size - Bed Area Size) / 2

So that it's a rectangle centered in the bed, leaving 16 pixels on each side, except if the bed is to small, in which case it will be a 16x16 square.

## Other Info

If you want more customization for Bed Furniture, make a post on the Nexus page or ping me on Discord (on the Stardew Valley server).