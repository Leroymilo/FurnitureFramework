# How to define Custom Slots?

The Slots field of a Furniture is a (directional) list of slots objects.  
See the Example Pack for examples.

A slot object has multiple fields:

## Area (required) (directional)

This is the [Rectangle](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Structures/Rectangle.md) where the slot is located on the Furniture. This area correspond to where you should click on the Furniture to place or remove something from the slot.  
Items placed in this slot will be horizontally centered in this area, and they will be aligned to the bottom of this area (the lowest pixel of the item sprite will be on the same line as the lowest pixel of the area). This can be changed with the Offset field.  
The Area of a Slot is relative to the bottom left corner of the Bounding Box defined in [Collisions](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Complex%20Fields/Collisions.md).

Note: It is not recommended to define overlapping areas, but if you do they will be prioritized in the order they were defined (this might change to sort by depth in a Future udpate, so that an invalid slot (already used or with a condition evaluating false) can be skipped for the slot behind it).

## Offset

An offset, in pixels to change the default position of the item placed in this slot (it usually depends on the Area), it's a **decimal** [Vector](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Structures/Vector.md).  
It can be omitted, its default value is (0, 0).

## Draw Shadow

A boolean (true or false) that tells the game wether or not to draw the shadow of the placed item. Furnitures placed on Furniture have no added shadow.  
If the shadow is enabled, items will be drawn 1 (one) pixel higher, defaults to `true`.

## Shadow Offset

An offset, in pixels to change the default position of the shadow of the item placed in this slot (it usually depends on the Area), it's a **decimal** [Vector](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Structures/Vector.md).  
It can be omitted, its default value is (0, 0).

## Depth

This is the [depth](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Structures/Depth.md) at which the item in the slot should be drawn. See the "Living Room" furniture in the Example Pack to have examples of slots with depth.  

As a general rule, if you have to create a [Layer](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Furniture.md#layers) for the part of the Furniture where you'll place the spot, then the spot should have the same depth (or bigger) as the corresponding layer.

Defaults to `{"Tile": 0, "Sub": 0}`.

Note: if a Slot has the same depth as a Layer, the item in the Slot will be drawn above the Layer, so you can give the Slot the same depth as the Layer it is supposed to rest on.

## Max Size

This is an **integer** [vector](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/Structures/Vector.md) that defines how big (**in tiles**) a Furniture can be to be placed in this spot. It defaults to 1x1 if omitted.

## Debug Color

This is the name of the color of the rectangle that will be shown if the "Slots Debug" options are enabled in the config, this is just a visual help to know where the Slot's Area is located. See [here](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.color?view=net-8.0#properties) for a list of accepted color names (R, G, B and A are not accepted).

## Condition

This is a string of text that you can use to restrict who/when/where/what can be placed in the slot using a [Game State Query](https://stardewvalleywiki.com/Modding:Game_state_queries).  
The most common use is to restrict what items can be placed in the slot using the query `ITEM_CONTEXT_TAG Input <context tag>`, and replacing `<context tag>` with any context tag shared between items that you want to be able to place in this slot.

Note: the `Target` item is the Furniture itself and the `Input` item is the item held by the player when an item is being placed in the slot.

Note 2: if there's an error in the Game State Query, the game will give you an error report in the console with details. I can't really help to fix issues with it because I used the built-in function to read them, so I don't know much about it.

# Example

Here is an example of a table slot in a bigger Furniture (taken from the `living_room` Furniture of the Example Pack). It uses the Depth field to make sure the item placed in the slot displays above the correct layers.

This is where the slots are in the spritesheet (I removed some stuff for clarity):  
![slots example](https://github.com/Leroymilo/FurnitureFramework/blob/3.0.0/doc/images/slots_example.png)

This is the definition of the slots:
```json
"Slots": {
	"Down": {
		"Area": { "X": 48, "Y": -21, "Width": 32, "Height": 13 },
		"Depth": 2,
		"Max Size": { "X": 2, "Y": 1 }
	},
	"Right": {
		"Area": { "X": 64, "Y": -55, "Width": 16, "Height": 31 },
		"Depth": 2,
		"Max Size": { "X": 1, "Y": 2 }
	},
	"Up": {
		"Area": { "X": 48, "Y": -53, "Width": 32, "Height": 13 },
		"Depth": 0,
		"Max Size": { "X": 2, "Y": 1 }
	},
	"Left": {
		"Area": { "X": 0, "Y": -71, "Width": 16, "Height": 31 },
		"Depth": 1,
		"Max Size": { "X": 1, "Y": 2 }
	}
}
```