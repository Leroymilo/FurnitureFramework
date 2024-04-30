# How to define Custom Layers?

The Layers field of a Furniture is a (directional) list of layer objects.  
See the Example Pack for examples.

A layer object has 3 fields:

## Source Rect

The part of the source image this layer should draw on the screen. It's a rectangle, see the Furniture's [Source Rect](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Furniture.md#source-rect) for more info on how to define a Rectangle. Be careful, this one is not directional since the Layers list is already directional. This field is required.

## Draw Pos

This is the position, in pixels, relative to the top left of the base sprite (for the current rotation), where the layer should be drawn. Its structure is:
```json
"Draw Pos": {"X": 32, "Y": 16}
```
Both X and Y are integers.

Defaults to `{"X": 0, "Y": 0}`

## Depth

This is the depth at which the layer should be drawn, it's mesured in tiles, starting from the bottom of the bounding box. If 0, it will be drawn over anything that is above the bottom of the bounding box of the Furniture. See the "Living Room" furniture in the Example Pack to have examples of layers with depth.

It can be a float (decimal value), but it's recommended to keep it an integer if you don't need to have layers more precise than a tile.

Defaults to `0`


This doc will be 