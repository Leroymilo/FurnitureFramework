# Depth Format

Every depth is split into 2 values:

## Tile

The "Tile" depth is required, it represents the full depth of a tile.  
Tile Depth = 0 is the tile at the top of the bounding box, bigger Tile Depth will push the depth down (south) of this. Negative Tile Depth will put the depth behind the base Sprite, Tile Depth bigger than Collisions.Height will put the depth in front of the Furniture (will overlap with stuff placed in front of the Furniture).

## Sub

The "Sub" depth is optional, it goes from 0 to 1000 (included) and allows for fine tuning in a tile. 0 will be at the top of the tile, and 1000 at the bottom.  
Because of a quirk in the game's code, there's a gap between `"Depth": {"Tile": N, "Sub": 1000}` and `"Depth": {"Tile": N+1, "Sub": 0}` to avoid layering issues with other objects placed behind and in front of the Furniture.

# Visual explanation

// TODO