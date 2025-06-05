# How to define a Custom TV?

To make a TV work, you only need to add 2 new fields:

## Screen Position (directional)

It's an integer [Vector](../Structures/Vector.md) in pixels, starting from the bottom left corner of the bounding box of the Furniture.

## Screen Scale

It might not be very intuitive at first: most vanilla TVs have a screen scale of 2, except the Plasma TV and the Tropical TV with a scale of 4, so you can choose your TV's screen scale based on these values. A decimal screen scale will probably look bad.

## Screen Depth (directional) (optional)

Defines the Depth of the screen of the TV, it will default to a Depth of `{"Tile": 0, "Sub": 1000}`, so it'll be in front of any layer drawn on a depth with `Tile` set to 0.  
This is usefull if you want to put something that would cover the screen (like a border or a filter).