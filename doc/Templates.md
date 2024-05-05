# Templates

This is a file with templates reproducing Vanilla Furniture, so that you can have a strating point when making your own Furniture.  
All images referenced are in the assets of the Example Pack.

## Contents

- [Basic Furniture](#basic-furniture)
- [Seasonal Furniture](#seasonal-furniture)
- [Sittable Furniture](#sittable-furniture)
	- [Chair](#chair)
	- [Couch](#couch)
- [Table Furniture](#table-furniture)
- [Catalogue](#catalogue)
- [Complex Furniture](#complex-furniture)
	- [Living Room](#living-room)

## Basic Furniture

This can be adapted for any Furniture having no rotations.

```json
{
	"Display Name": "Basic Furniture",

	"Rotations": 1,
	"Collisions": {
		"Width": 1,
		"Height": 1
	},	// in tiles

	"Indoors": false,
	"Outdoors": true,

	"Source Image": "assets/simple.png",
	"Source Rect": {"X": 0, "Y": 0, "Width": 16, "Height": 48},	// in pixels
}
```

## Seasonal Furniture

This can be adapted for any Seasonal Furniture having no rotations.

```json
{
	"Display Name": "Seasonal Bush",
	"Rotations": 1,
	"Collisions": {
		"Width": 2,
		"Height": 1
	},

	"Indoors": false,
	"Outdoors": true,

	"Seasonal": true,
	"Source Image": "assets/bush.png",
	"Source Rect": {"X": 0, "Y": 0, "Width": 32, "Height": 32},	// in pixels
}
```
Make sure that all the seasonal tile-sheets are located where the `Source Image` path points to:
- `assets/bush_spring.png`
- `assets/bush_summer.png`
- `assets/bush_fall.png`
- `assets/bush_winter.png`

## Sittable Furniture

### Chair

This can be adapted for any chair-like Furniture with 4 rotations and a simple front layer.

```json
{

	"Display Name": "Chair Furniture",
	"Price": 0,

	"Rotations": 4,
	"Collisions": {
		"Width": 1,
		"Height": 1
		// in tiles
	},

	"Indoors": true,
	"Outdoors": true,

	"Source Image": "assets/chair.png",
	"Source Rect": {
		"Down":		{"X": 0,  "Y": 0, "Width": 16, "Height": 32},
		"Right":	{"X": 16, "Y": 0, "Width": 16, "Height": 32},
		"Up":		{"X": 32, "Y": 0, "Width": 16, "Height": 32},
		"Left":		{"X": 48, "Y": 0, "Width": 16, "Height": 32}
		// in pixels
		// must have all directions
	},
	"Layers": {
		"Up": [
			{
				"Source Rect": {"X": 32, "Y": 32, "Width": 16, "Height": 32}
				// in pixels
			}
		]
		// Only the "Up" rotation needs to have LayerData
		// because it's the only direction where the chair
		// is in front of the Player sitting in it.
	},

	"Seats": [
		// positions are from the top left of the Bounding Box (not the texture!)
		{
			"X": 0, "Y": 0,		// in tiles, can be decimal
			"Player Direction": {
				"Up": 0,
				"Right": 1,
				"Down": 2,
				"Left": 3
			}
			// The direction the player is facing when sitting on this seat:
			// 0 means Up, 1 means Right, 2 means Down and 3 means Left.
		}
	]
}
```

### Couch

This can be adapted for more complex Sittable Furniture, like a couch.

```json
{

	"Display Name": "Couch Test",

	"Rotations": 4,
	"Collisions": {
		"Down": 	{"Width": 3, "Height": 1},
		"Right": 	{"Width": 2, "Height": 2},
		"Up": 		{"Width": 3, "Height": 1},
		"Left": 	{"Width": 2, "Height": 2}
		// in tiles
		// Each rotation has its own Bounding Box Size.
	},

	"Indoors": true,
	"Outdoors": true,

	"Source Image": "assets/couch.png",
	"Source Rect": {
		"Down":		{"X": 0,	"Y": 0, "Width": 48, "Height": 32},
		"Right":	{"X": 48,	"Y": 0, "Width": 32, "Height": 48},
		"Up":		{"X": 80,	"Y": 0, "Width": 48, "Height": 32},
		"Left":		{"X": 128,	"Y": 0, "Width": 32, "Height": 48}
		// in pixels
		// must have all directions
	},

	"Layers": [
		{
			"Source Rect": {
				// Layer for Up is transparent, can be omitted
				"Right":	{"X": 48, "Y": 48, "Width": 32, "Height": 48},
				"Up":		{"X": 80, "Y": 48, "Width": 48, "Height": 32},
				"Left":		{"X": 128, "Y": 48, "Width": 32, "Height": 48}
			}
		}
	],

	"Seats": [
		// positions are from the top left of the Bounding Box (not the texture!)
		{
			// Left seat when facing Up or Down
			"X": 0.5, "Y": 0,
			"Player Direction": {
				"Up": 0,
				"Down": 2
			}
		},
		{
			// Right seat when facing Up or Down
			"X": 1.5, "Y": 0,
			"Player Direction": {
				"Up": 0,
				"Down": 2
			}
		},
		{
			// Top seat when facing Right
			"X": 1, "Y": 0,
			"Player Direction": {
				"Right": 1
			}
		},
		{
			// Bottom seat when facing Right
			"X": 1, "Y": 1,
			"Player Direction": {
				"Right": 1
			}
		},
		{
			// Top seat when facing Left
			"X": 0, "Y": 0,
			"Player Direction": {
				"Left": 3
			}
		},
		{
			// Bottom seat when facing Left
			"X": 0, "Y": 1,
			"Player Direction": {
				"Left": 3
			}
		}
		// About the Player Direction values:
		// 0 means Up, 1 means Right, 2 means Down and 3 means Left.
	]
}
```

## Table Furniture

This can be adapted for any Furniture with slots to place objects on it. Keep in mind that it's limited to a single slot per Furniture for now.

```json
{
	"Display Name": "Table Furniture",

	"Rotations": 2,
	"Collisions": {
		"Horizontal": 	{"Width": 2, "Height": 1},
		"Vertical": 	{"Width": 1, "Height": 2}
		// in tiles
	},

	"Indoors": true,
	"Outdoors": true,

	"Source Image": "assets/table.png",
	"Source Rect": {
		"Horizontal":	{"X": 0,	"Y": 0, "Width": 32, "Height": 32},
		"Vertical":		{"X": 32,	"Y": 0, "Width": 16, "Height": 48}
		// in pixels
	},

	"Slots": {
		"Horizontal": [
			{
				"Area": {"X": 0, "Y": 11, "Width": 32, "Height": 13}
			}
		],
		"Vertical": [
			{
				"Area": {"X": 0, "Y": 9, "Width": 16, "Height": 31}
			}
		]
		// The Area rectangle is in pixels, and is relative to the sprite for each rotation.
	}
}
```
Be carefull, the Area of a Slot is relative to the sprite for this rotation, not to the whole spritesheet. In this example, since the Vertical Source Rect starts at (32, 0), the Vertical Area starting at (0, 9) is actually starting at (32, 9) on the spritesheet.

## Catalogue

This is an example of how to make a custom catalogue throught the Furniture Framework.

```json
{
	"Display Name": "Custom Catalogue",

	"Rotations": 1,
	"Collisions": {
		"Width": 1,
		"Height": 1
	},	// in tiles

	"Indoors": true,
	"Outdoors": true,

	"Source Image": "assets/catalogue.png",
	"Source Rect": {"X": 0, "Y": 0, "Width": 16, "Height": 32},	// in pixels

	"Shop Id": "leroymilo.FurnitureExample.FF.custom_catalogue",
	// To create a Shop. It is strongly recommended to include your mod's UniqueID
	"Shows in Shops": ["Carpenter"]
	// Adding the Custom Catalogue to Robin's Shop
}
```

If you want other Furniture to show up in this Catalogue, you have to add the field `"Shows in Shops": ["leroymilo.FurnitureExample.FF.custom_catalogue"]` to their definition.  
You can also define a Shop in more details by using Content Patcher to patch Data/Shops, see [the wiki](https://stardewvalleywiki.com/Modding:Shops) for more info about custom Shops. Shops with an ID that already exists should be attached to the Furniture without having their definition modified. The same way goes for Shop Items: if you define a Shop Item in a CP Patch, it won't be overwritten by the Furniture Framework. If you have any issue with this feature, ping me in the [modding channel of the Stardew Valley Discord server](https://discord.com/channels/137344473976799233/156109690059751424) so that I can help you.

## Complex Furniture

Before trying to use one of these templates, it is strongly recommended to read about all of the features they use in the [documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Furniture.md).

### Living Room

This is an example of a Furniture with:
- 4 Rotations
- Collision Maps
- Seats
- Layers
- Slots

```json
{

	"Display Name": "Living Room",

	"Rotations": 4,

	"Collisions": {
		"Down": 	{
			"Width": 8, "Height": 3,
			"Map": "..XXXX../XX....XX/...XX.XX"
		},
		"Right": 	{
			"Width": 5, "Height": 5,
			"Map": "..XXX/XX.../XX..X/XX..X/..XX."
		},
		"Up": 		{
			"Width": 8, "Height": 3,
			"Map": "XX.XX.../XX....XX/..XXXX.."
		},
		"Left": 	{
			"Width": 5, "Height": 5,
			"Map": ".XX../X..XX/X..XX/...XX/XXX.."
		}
	},

	"Indoors": true,
	"Outdoors": true,

	"Source Image": "assets/living_room.png",
	"Source Rect": {
		"Down":		{"X": 0, "Y": 0, "Width": 128, "Height": 64},
		"Right":	{"X": 0, "Y": 128, "Width": 80, "Height": 96},
		"Up":		{"X": 0, "Y": 64, "Width": 128, "Height": 64},
		"Left":		{"X": 80, "Y": 128, "Width": 80, "Height": 96}
	},

	"Layers": {
		"Up": [
			{
				// Back of Long Couch, facing Up
				"Source Rect": {"X": 80, "Y": 224, "Width": 64, "Height": 32},
				"Draw Pos": {"X": 32, "Y": 32},
				"Depth": 0
			},
			{
				// Lower Arm of Couch facing Right
				"Source Rect": {"X": 160, "Y": 128, "Width": 32, "Height": 48},
				"Draw Pos": {"X": 0, "Y": 0},
				"Depth": 1
			},
			{
				// Lower Arm of Armchair facing Left
				"Source Rect": {"X": 192, "Y": 64, "Width": 32, "Height": 32},
				"Draw Pos": {"X": 96, "Y": 16},
				"Depth": 1
			},
			{
				// Upper Arm of Armchair facing Left
				"Source Rect": {"X": 192, "Y": 96, "Width": 32, "Height": 32},
				"Draw Pos": {"X": 96, "Y": 16},
				"Depth": 2
			}
			// Upper Arm of Couch facing Right & Table are already drawn by the base sprite 
		],
		"Right": [
			{
				// Back of Armchair facing Up
				"Source Rect": {"X": 0, "Y": 224, "Width": 32, "Height": 32},
				"Draw Pos": {"X": 32, "Y": 64},
				"Depth": 0
			},
			{
				// Lower Arm of Long Couch facing Right
				"Source Rect": {"X": 128, "Y": 0, "Width": 32, "Height": 64},
				"Draw Pos": {"X": 0, "Y": 16},
				"Depth": 1
			},
			{
				// Table
				"Source Rect": {"X": 64, "Y": 160, "Width": 16, "Height": 48},
				"Draw Pos": {"X": 64, "Y": 32},
				"Depth": 3
			},
			{
				// Upper Arm of Long Couch facing Right
				"Source Rect": {"X": 128, "Y": 64, "Width": 32, "Height": 64},
				// in pixels
				"Draw Pos": {"X": 0, "Y": 16},
				"Depth": 4
			}
		],
		"Down": [
			{
				// Lower Arm of Couch facing Left
				"Source Rect": {"X": 192, "Y": 128, "Width": 32, "Height": 48},
				"Draw Pos": {"X": 96, "Y": 16},
				"Depth": 0
			},
			{
				// Lower Arm of Armchair facing Right
				"Source Rect": {"X": 192, "Y": 0, "Width": 32, "Height": 32},
				"Draw Pos": {"X": 0, "Y": 16},
				"Depth": 1
			},
			{
				// Table
				"Source Rect": {"X": 48, "Y": 32, "Width": 32, "Height": 32},
				"Draw Pos": {"X": 48, "Y": 32},
				"Depth": 1
			},
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
			}
		],
		"Left": [
			{
				// Back of Couch facing Up
				"Source Rect": {"X": 32, "Y": 224, "Width": 48, "Height": 32},
				"Draw Pos": {"X": 0, "Y": 64},
				"Depth": 0
			},
			{
				// Lower Arm of Long Couch facing Left
				"Source Rect": {"X": 160, "Y": 0, "Width": 32, "Height": 64},
				"Draw Pos": {"X": 48, "Y": 16},
				"Depth": 1
			},
			{
				// Table
				"Source Rect": {"X": 80, "Y": 144, "Width": 16, "Height": 48},
				"Draw Pos": {"X": 0, "Y": 16},
				"Depth": 4
			},
			{
				// Upper Arm of Long Couch facing Left
				"Source Rect": {"X": 160, "Y": 64, "Width": 32, "Height": 64},
				"Draw Pos": {"X": 48, "Y": 16},
				"Depth": 4
			}
		]
	},

	"Seats": {
		"Down": [
			{
				// Left seat of Long Couch
				"X": 2.5, "Y": 0,
				"Player Direction": 2
			},
			{
				// Center seat of Long Couch
				"X": 3.5, "Y": 0,
				"Player Direction": 2
			},
			{
				// Right seat of Long Couch
				"X": 4.5, "Y": 0,
				"Player Direction": 2
			},
			{
				// Seat of Armchair
				"X": 1, "Y": 1,
				"Player Direction": 1
			},
			{
				// Top seat of Couch
				"X": 6, "Y": 1,
				"Player Direction": 3
			},
			{
				// Bottom seat of Couch
				"X": 6, "Y": 2,
				"Player Direction": 3
			}
		],
		"Right": [
			{
				// Left seat of Couch
				"X": 2.5, "Y": 0,
				"Player Direction": 2
			},
			{
				// Right seat of Couch
				"X": 3.5, "Y": 0,
				"Player Direction": 2
			},
			{
				// Top seat of Long Couch
				"X": 1, "Y": 1,
				"Player Direction": 1
			},
			{
				// Center seat of Long Couch
				"X": 1, "Y": 2,
				"Player Direction": 1
			},
			{
				// Bottom seat of Long Couch
				"X": 1, "Y": 3,
				"Player Direction": 1
			},
			{
				// Seat of Armchair
				"X": 2.5, "Y": 4,
				"Player Direction": 0
			}
		],
		"Up": [
			{
				// Top seat of Couch
				"X": 1, "Y": 0,
				"Player Direction": 1
			},
			{
				// Bottom seat of Couch
				"X": 1, "Y": 1,
				"Player Direction": 1
			},
			{
				// Seat of Armchair
				"X": 6, "Y": 1,
				"Player Direction": 3
			},
			{
				// Left seat of Long Couch
				"X": 2.5, "Y": 2,
				"Player Direction": 0
			},
			{
				// Center seat of Long Couch
				"X": 3.5, "Y": 2,
				"Player Direction": 0
			},
			{
				// Right seat of Long Couch
				"X": 4.5, "Y": 2,
				"Player Direction": 0
			}
		],
		"Left": [
			{
				// Seat of Armchair
				"X": 1.5, "Y": 0,
				"Player Direction": 2
			},
			{
				// Top seat of Long Couch
				"X": 3, "Y": 1,
				"Player Direction": 3
			},
			{
				// Center seat of Long Couch
				"X": 3, "Y": 2,
				"Player Direction": 3
			},
			{
				// Bottom seat of Long Couch
				"X": 3, "Y": 3,
				"Player Direction": 3
			},
			{
				// Left seat of Couch
				"X": 0.5, "Y": 4,
				"Player Direction": 0
			},
			{
				// Right seat of Couch
				"X": 1.5, "Y": 4,
				"Player Direction": 0
			}
		]
	},

	"Slots": {
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
		// The Area rectangle is in pixels, and is relative to the sprite for each rotation.
	}
}
```