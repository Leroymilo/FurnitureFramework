# How to define Custom Layers?

The Layers field of a Furniture is a (directional) list of layer objects.  
See the Example Pack for examples.

A layer object has 3 fields:

## Source Rect

The part of the source image this layer should draw on the screen. It's a rectangle, see the Furniture's [Source Rect](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Furniture.md#source-rect) for more info on how to define a Rectangle.  
This field can be directional if the Layers list is not already directional. However the Draw Pos and Depth will be ignored if the Source Rect is directional.

## Draw Pos

This is the position, in pixels, relative to the top left of the base sprite (for the current rotation), where the layer should be drawn. Its structure is:
```json
"Draw Pos": {"X": 32, "Y": 16}
```
Both X and Y are integers.

Be carefull, the Draw Pos is relative to the sprite for this rotation, not to the whole spritesheet.  
In the `living_room` Furniture of the Example Pack for example, the Layer "Back of Couch facing Up" for the Left Rotation has (0, 64) as Draw Pos, but since the Source Rect for the Left Rotation starts at (80, 128) the layer mentionned would actually be drawn at (80, 192) on the spritesheet.

Defaults to `{"X": 0, "Y": 0}`

## Depth

This is the depth at which the layer should be drawn, it's mesured in tiles, starting from the bottom of the bounding box. If 0, it will be drawn over anything that is above the bottom of the bounding box of the Furniture. See the [Example](#example) to have examples of layers with depth.

It can be a float (decimal value), but it's recommended to keep it an integer if you don't need to have layers more precise than a tile.

Using a depth equal to the Bounding Box Height of the Furniture will place the Layer on the same depth as the base sprite and will cause [Z-fighting](https://en.wikipedia.org/wiki/Z-fighting).

Using a depth bigger than the Bounding Box Height of the Furniture will place the Layer behind the base sprite of the Furniture.

Defaults to `0`

## Example

Here's an example on how to use layers with the down-facing `living_room` Furniture of the Example Pack. This is basically as complicated as it gets, I'd recommend checking some other Furniture from the Example Pack or the [Templates](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Templates.md) for simpler layers fitting your needs.

In this example, we will go through the layers from back to front, so from the highest depth to the lowest. In this case, the highest depth is the base sprite (at depth = Collisions.Height = 3).

This gif shows how the layers are drawn from back to front, with a Farmer to show how it would be drawn in-between the layers:

![layers example gif](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/images/layers_example.gif)

Here's the list of Layers for this Furniture facing down:
```json
[
	{
		// Upper Arm of Armchair facing Right
		"Source Rect": {"X": 192, "Y": 32, "Width": 32, "Height": 32},
		"Draw Pos": {"X": 0, "Y": 16},
		"Depth": 2
	},
	{
		// Upper Arm of Couch facing Left
		"Source Rect": {"X": 192, "Y": 176, "Width": 32, "Height": 48},
		"Draw Pos": {"X": 96, "Y": 16},
		"Depth": 2
	},
	{
		// Lower Arm of Armchair facing Right
		"Source Rect": {"X": 192, "Y": 0, "Width": 32, "Height": 32},
		"Draw Pos": {"X": 0, "Y": 16},
		"Depth": 1
	},
	{
		// Table
		"Source Rect": {"X": 144, "Y": 224, "Width": 32, "Height": 32},
		"Draw Pos": {"X": 48, "Y": 32},
		"Depth": 1
	},
	{
		// Lower Arm of Couch facing Left
		"Source Rect": {"X": 192, "Y": 128, "Width": 32, "Height": 48},
		"Draw Pos": {"X": 96, "Y": 16},
		"Depth": 0
	}	
]
```

Keep in mind that the Source Rect is defined from [this spritesheet](https://github.com/Leroymilo/FurnitureFramework/tree/main/%5BFF%5D%20Example%20Pack/assets/living_room.png).