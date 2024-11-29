# How to define Custom Layers?

![Layers](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/images/layers.png)

The Layers field of a Furniture is a directional list of layer objects.

This is required for a Furniture to work: it needs at least 1 layer (refered as the "base layer"). By default, the [Depth](#depth) of the base layer is `{"Tile": 0, "Sub": 0}` instead of `{"Tile": 0, "Sub": 1000}` to match what it was before version 3.0.0 (also it makes more sense).

A layer object has 3 fields:

## Source Rect (required) (directional)

The part of the source image this layer should draw on the screen, it's a [Rectangle](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Structures/Rectangle.md).

## Draw Pos (directional)

This is the position, in pixels, relative to the bottom left of the furniture (for the current rotation), where the layer should be drawn. It is an **integer** [Vector](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Structures/Vector.md).

It defaults to `{"X": 0, "Y": 0}`, which means that the bottom left corner of the sprite is at the bottom left corner of the furniture. Keep in mind that the positive Y axis is down, so you have to use negative Y coordinate to move the layer up.

## Depth

This is the [depth](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Structures/Depth.md) at which the layer should be drawn. See the [Example](#example) to have examples of layers with depth.

Defaults to `{"Tile": 0, "Sub": 1000}` (so the bottom of the top-most tile of the Furniture's bounding box).

## Example of complex Layers

Here's an example on how to use layers with the down-facing `living_room` Furniture of the Example Pack. This is basically as complicated as it gets, I'd recommend checking some other Furniture from the Example Pack or the [Templates](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Templates.md) for simpler layers fitting your needs.

In this example, we will go through the layers from back to front. In this case, the lowest depth is the base layer (at `"Depth": {"Tile": 0, "Sub": 0}`).

This gif shows how the layers are drawn from back to front, with a Farmer to show how it would be drawn in-between the layers:

![layers example gif](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/images/layers_example.gif)

Here's the list of Layers for this Furniture facing down, from back to front:
```json
[
	{
		// Upper Arm of Armchair facing Right
		"Source Rect": {"X": 192, "Y": 32, "Width": 32, "Height": 32},
		"Draw Pos": {"X": 0, "Y": -48},
		"Depth": 1
	},
	{
		// Upper Arm of Couch facing Left
		"Source Rect": {"X": 192, "Y": 176, "Width": 32, "Height": 48},
		"Draw Pos": {"X": 96, "Y": -48},
		"Depth": 1
	},
	{
		// Lower Arm of Armchair facing Right
		"Source Rect": {"X": 192, "Y": 0, "Width": 32, "Height": 32},
		"Draw Pos": {"X": 0, "Y": -48},
		"Depth": {"Tile": 1, "Sub": 1000}
	},
	{
		// Table
		"Source Rect": {"X": 144, "Y": 224, "Width": 32, "Height": 32},
		"Draw Pos": {"X": 48, "Y": -32},
		"Depth": 2
	},
	{
		// Lower Arm of Couch facing Left
		"Source Rect": {"X": 192, "Y": 128, "Width": 32, "Height": 48},
		"Draw Pos": {"X": 96, "Y": -48},
		"Depth": {"Tile": 2, "Sub": 1000}
	}
]
```

Keep in mind that the Source Rect is defined from [this spritesheet](https://github.com/Leroymilo/FurnitureFramework/tree/main/%5BFF%5D%20Example%20Pack/assets/living_room.png).