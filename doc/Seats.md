# How to define Custom Seats?

The Seats field of a Furniture is a (directional) list of seats objects.  
See the Example Pack for examples.

A seat object has 3 fields:
```json
{
	"X": 2.5, "Y": 0,
	"Player Direction": 2
},
```

## X & Y

The position, in tiles, of this seat. It's relative to the bounding box, the top-left tile of the bounding box being (0, 0). The values can be floats (decimals) to place a seat between 2 tiles (like for an armchair or a couch).  
For example, an Armchair facing down has a bounding box of size 2x1, and its seat is at tile (0.5, 0) in this bounding box, even though the sprite is 2x2 tiles because the bounding box is aligned with the sprite at its bottom left corner.  
If this is unclear, please check the [Couch Template](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Templates.md#couch) for a clearer example.

## Player Direction

This field indicates which direction the player will be facing when sitting on this seat. Player directions are:
- 0 for Up
- 1 for Right
- 2 for Down
- 3 for Left

This field can be directional, but only if the Seats list is not already directional.  
For example, the "Chair Test" Furniture of the Example Pack has a non-directional Seats (containing a single seat) but directional Player Directions, while the "Living Room" has directional Seats but non-directional Player Directions.  
This is usefull because some Furniture have seats that are always on the same tile but have different Player Directions depending on the Furniture's rotation (like chairs), while other Furniture have their seat position changing on every rotation (like a couch or an armchair).


This doc will probably be reworked later.