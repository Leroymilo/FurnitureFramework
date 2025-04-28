# How to define a Custom TV?

To make a TV work, you only need to add 2 new fields:

## Screen Position (directional)

It's an integer [Vector](../Structures/Vector.md) in pixels, starting from the bottom left corner of the bounding box of the Furniture.

## Screen Scale

It might not be very intuitive at first: most vanilla TVs have a screen scale of 4, except the Plasma TV and the Tropical TV with a scale of 2, so you can choose your TV's screen scale based on these values. A decimal screen scale will probably look bad.