# How to define Custom Slots?

The Slots field of a Furniture is a (directional) list of slots objects.  
See the Example Pack for examples.

A slot object has 2 fields:
```json
{
	"Area": {"X": 64, "Y": 41, "Width": 16, "Height": 31},
	"Depth": 1
},
```

## Area

This is the rectangle (in pixels) where the slot is located on the sprite for this rotation. This area correspond to where you should click on the Furniture to place or remove something from the slot.  
Be carefull, the Area of a Slot is relative to the sprite for this rotation, not to the whole spritesheet. For example, in the Vertical Slot of the `table_test` Furniture of the Example Pack, since the Vertical Source Rect starts at (32, 0), the Vertical Area starting at (0, 9) is actually starting at (32, 9) on the spritesheet.

This area can be directional, but if set as directional, the depth field will be ignored and default to 0.

## Depth

This is the depth at which the item in the slot should be drawn, it's mesured in tiles, starting from the bottom of the bounding box. If 0, it will be drawn over anything that is above the bottom of the bounding box of the Furniture. See the "Living Room" furniture in the Example Pack to have examples of layers with depth.

It can be a float (decimal value), but it's recommended to keep it an integer if you don't need to have layers more precise than a tile.

As a general rule, if you have to create a [Layer](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Furniture.md#layers) for the part of the Furniture where you'll place the spot, then the spot should have the same depth (or lower) as the corresponding layer.

Defaults to `0`.

## Example

Here is an example of a table slot in a bigger Furniture (taken from the `living_room` Furniture of the Example Pack). I uses the Depth field.

This is where the slots are in the spritesheet (I removed some stuff for clarity):  
![slots example](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/images/slots_example.png)

This is the definition of the slots:
```json
{
	"Down": [
		{
			"Area": {"X": 48, "Y": 43, "Width": 32, "Height": 13}
		}
	],
	"Right": [
		{
			"Area": {"X": 64, "Y": 41, "Width": 16, "Height": 31},
			"Depth": 1
		}
	],
	"Up": [
		{
			"Area": {"X": 48, "Y": 11, "Width": 32, "Height": 13},
			"Depth": 2
		}
	],
	"Left": [
		{
			"Area": {"X": 0, "Y": 25, "Width": 16, "Height": 31},
			"Depth": 2
		}
	]
}
```