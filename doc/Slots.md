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

This is the rectangle (in pixels) where the slot is located on the sprite for this rotation. It is defined from the top left of the sprite. This area correspond to where you should click on the Furniture to place or remove something from the slot.

This area can be directional, but if set as directional, the depth field will be ignored and default to 0.

## Depth

This is the depth at which the item in the slot should be drawn, it's mesured in tiles, starting from the bottom of the bounding box. If 0, it will be drawn over anything that is above the bottom of the bounding box of the Furniture. See the "Living Room" furniture in the Example Pack to have examples of layers with depth.

It can be a float (decimal value), but it's recommended to keep it an integer if you don't need to have layers more precise than a tile.

Defaults to `0`.


This doc will probably be reworked later.