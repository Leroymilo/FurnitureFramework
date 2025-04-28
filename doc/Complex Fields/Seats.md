# How to define Custom Seats?

The Seats field of a Furniture is a (directional) list of seats objects.  
See the Example Pack for examples.

A seat object has 3 fields:

## Position (required) (directional)

It's a **decimal** [Vector](../Structures/Vector.md), in tiles, of this seat. It's relative to the bounding box, so sitting on the top-left tile of the bounding box would be `{"X": 0, "Y": 0}`.  
For example, an Armchair facing down has a bounding box of size 2x1, and its seat is at tile (0.5, 0) in this bounding box, even though the sprite is 2x2 tiles because the bounding box is aligned with the sprite at its bottom left corner.  
If this is unclear, please check the "Couch Test" in the example pack [here](../../Example%20Pack/[FF]/assets/seats/seats.json).

## Player Direction (required) (directional)

This field indicates which direction the player will be facing when sitting on this seat. Player directions are "Up", "Right", "Down", and "Left".  
This field can be directional, but its only useful if the `Seats` list is not already directional.  
For example, the "Chair Test" Furniture of the Example Pack has a non-directional Seats (containing a single seat) but directional Player Directions, while the "Living Room" has directional Seats but non-directional Player Directions. Both can be found [here](../../Example%20Pack/[FF]/assets/seats/seats.json)  
This is useful because some Furniture have seats that are always on the same tile but have different Player Directions depending on the Furniture's rotation (like chairs), while other Furniture have their seat position changing on every rotation (like a couch or an armchair).

## Depth

This is the [depth](../Structures/Depth.md) at which the player will be drawn when sitting in this seat.  
If this field is set to null or omitted, the player depth will be computed by the game, it often works pretty well so you should only use this field if the natural player depth is wrong.