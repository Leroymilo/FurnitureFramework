# Custom Furniture definition

To make Furniture highly customizable, the definition of a Furniture has a lot of properties. But don't worry, everything is explained here, and there's a good chance you won't need to use everything.

Once again, this documentation uses the [Example Pack](https://github.com/Leroymilo/FurnitureFramework/tree/main/%5BFF%5D%20Example%20Pack) as an example, it is strongly recommeded to go back and forth between the explanation here and the example to identify what is being explained.

## Contents

* [Required Fields](#required-fields)
	* [Display Name](#display-name)
	* [Rotations](#rotations)
	* [Source Image](#source-image)
	* [Source Rect](#source-rect)
	* [Collisions](#collisions)
* [Optional Fields](#optional-fields)
	* [Vanilla Fields](#vanilla-fields)
		* [Force Type](#force-type)
		* [Price](#price)
		* [Indoor & Outdoor](#indoor--outdoor)
		* [Context Tags](#exclude-from-random-sales)
		* [Exclude from Random Sales](#exclude-from-random-sales)
	* [Custom Catalogue Shop](#custom-catalogue-shop)
		* [Shows in Shops](#shows-in-shops)
		* [Shop Id](#shop-id)
	* [Animation](#animation)
	* [Special Type](#special-type)
	* [Icon Rect](#icon-rect)
	* [Seasonal](#seasonal)
	* [Layers](#layers)
	* [Seats](#seats)
	* [Slots](#slots)
	* [Toggle](#toggle)
	* [Sounds](#sounds)

## Required Fields

### Display Name

This is the name of the Furniture as it will be displayed in game, it has basically no restriction, except that it must be a string (text between quotation marks `"`).  
This field is not actually required, but it doesn't really make sense to ommit it, it will default to "No Name".

### Rotations

This field allow you to define rotations. It can be of 2 types: an integer (a whole number), or a list of rotation names.  
If it is set to a list of rotation names, the number of names given corresponds to the number of rotations. The names given will be used as "rotation keys" in [Directional Fields](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Directional%20Fields.md) (you'll see about this later).  
If it is set to an integer, it simply gives the number of rotations of the Furniture. For example, a table has 2 rotations (horizontal and vertical), and a chair has 4 rotations (up, right, down and left).  
When using a number of rotations instead of a list of names, the resulting "rotation keys" are hardcoded:
- `"Rotations": 1` -> no need for rotations keys
- `"Rotations": 2` -> `"Rotations": ["Horizontal", "Vertical"]`
- `"Rotations": 4` -> `"Rotations": ["Down", "Right", "Up", "Left"]`

Any other number will result in an error because rotation keys are essential to parse Directional Fields. If you don't need to use Directional Fields, then the Rotations should be set to 1 (the Furniture cannot be rotated).

Note: the order of the rotation names will only define in which order they cycle when using right-click when placing a Furniture in game.

Note 2: you can set as many rotations as you want! Just make sure that they have distinct names. Here's an example with 6 rotations: 
```json
"Rotations": ["r1", "r2", "r3", "r4", "r5", "r6"]
```
You'll just have to remember to use these names as keys when defining directional fields.

### Source Image

This is the path, **relative to your mod's directory**, to the spritesheet to use for this Furniture. All sprites used in drawing your Furniture in the game (all rotations and layers) have to be in the same spritesheet. It is possible to use the same spritesheet for multiple Furniture.  
It is **strongly** recommended to align all sprites on a 16x16 pixel grid, because every game tile is 16x16 pixels large.

This field now supports variants: instead of giving a single path, you can give a dictionary of paths:
```json
"Source Image": {
	"Brown": "assets/armchair.png",
	"Yellow": "assets/armchair_yellow.png",
	"Blue": "assets/armchair_blue.png"
},
```
This example is taken from the `armchair_test` Furniture of the Example Pack.

Please note that this will create as many separate Furniture as source images are given, but all their properties (asside from Source Image) will be identical, including their Display Name. However, you can use the token `{{variant}}` in the Display Name field so that it will be replaced with the variant key (see the `armchair_test` Furniture in the Example Pack).  
This is kind of a replacement for a compatibility with Alternative Textures because making this mod truly compatible with Alternative Textures will be hard to do.

Note: it is possible to use both a dictionary of Source Images and [Seasonal](#seasonal) sprites, but all of the variants path given must have seasonal suffixes.

### Source Rect

This defines what part of the provided image will be used as your Furniture sprite.

This field is a Rectangle, it is important to understand how it works because it will be used later in other fields.
If you are familiar with CP, it is the same data model used in the fields `ToArea` and `FromArea` of an `EditImage` patch.  
A rectangle has 4 properties, all the mesurements are ***in image pixels*** and must be integers:
- "X": the horizontal position of the top-left of the rectangle
- "Y": the vertical position of the top-right of the rectangle
- "Width": the width of the rectangle
- "Height": the height of the rectangle

A proper rectangle would look like this:
```json
{"X": 64, "Y": 32, "Width": 32, "Height": 32}
```
or, if spread on multiple lines, like this:
```json
{
	"X": 64,
	"Y": 32,
	"Width": 32,
	"Height": 32
}
```

This field is also the first [Directional Field](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Directional%20Fields.md) you'll encounter, so make sure to check how they work.

### Collisions

This field defines the collisions of your Furniture, it's what defines what part of the Furniture the player will not be able to walk through and place other Furniture on. Since they are quite complicated, they have their own [Collisions documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Collisions.md).

## Optional Fields

### Vanilla Fields

These fields are basically what you'll find in a Furniture defined in [`Data/Furniture`](https://stardewvalleywiki.com/Modding:Items#Furniture).

#### Force Type

In this field, you can force the vanilla type of the Furniture (as a string). If you don't know how it works, don't set it, most types have not been tested and are replaced with other fields in the Furniture Framework. Please report it if you find Furniture types that completely break the mod so that I can list them here.

Do not use these types:
- chair
- bench
- couch
- armchair
- long table
- table
- dresser

#### Price

This is the default price of the Furniture, it will be used if it is added to a shop's item list without specifying a price.

#### Indoor & Outdoor

These fields define if the Furniture can be placed respectively indoor and outdoor. They both accept boolean values (true or false, without quotation marks), and default to true. If both are set to false, your Furniture will be indoor only because of how the game is coded.

#### Context Tags

This is an array (a list) of context tags you want to add to your Furniture, it defaults to an empty list. If you want to learn more about context tags, check [the wiki](https://stardewvalleywiki.com/Modding:Items#Context_tags).

#### Exclude from Random Sales

This defines wether or not this Furniture will show-up in random sales in the vanilla Furniture Catalogue and other Furniture shops. It's a boolean value (true or false), defaulting to true.

### Custom Catalogue Shop

#### Shows in Shops

This is an array (a list) of string Shop IDs where you want your Furniture to show-up, it defaults to an empty list.  For example, having:
```json
"Shows in Shops": ["Carpenter"]
```
will add your Furniture to Robin's Shop. Here's the list of [vanilla Shop IDs](https://stardewvalleywiki.com/Modding:Shops#Vanilla_shop_IDs) on the wiki. Be carefull, some shops have some weird quirks when their owner is not around.

When used in combination to the "Shop Id" field, you can create a custom Catalogue for your custom Furniture.

#### Shop Id

The Shop ID of the Shop the game should open when right-clicking on the Furniture, it's a string that defaults to `null` (no Shop attached).  
You can attach one of the [vanilla Shops](https://stardewvalleywiki.com/Modding:Shops#Vanilla_shop_IDs), or your own Shop.  
By default, if the Shop ID given doesn't match any existing shop, a default shop based on the vanilla Furniture Catalogue (no owner) will be created.  
You can then use the same Shop ID in the "Shows in Shops" field of other Furniture you created to add them to this new Catalogue. If you want to add more rules to your custom Catalogue (multipliers, owners, ...), you'll need to define it in another Content Pack using Content Patcher.

An example of this is in the [Example Pack](https://github.com/Leroymilo/FurnitureFramework/blob/main/Example%20Pack/content.json).

Note: the Shop ID is raw, your mod's UniqueID will not be prepended to it, so make sure it's unique (you can manually add your mod's ID to it for example).

### Animation

You can define animations for your Furniture, but you'll need to fill a few fields for it to work:

####

####

####

####

### Special Type

This kind of replace the "Type" field in the vanilla Furniture data. It's a string that can take one if these values:
- None (no special type)
- Dresser
- [TV](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/types/TV.md)

Some Special Types have their own documentation linked in this list for extra info.

Other types that I need to add:
- Bed
- FishTank

### Icon Rect

This field is another Rectangle, like a [Source Rect](#source-rect). This rectangle will tell the game which part of the texture to use to display the Furniture in the menu.

### Seasonal

This field will allow you to create Furniture with different Sprites depending on the season, it's a boolean (true or false).  
If false, the [Source Image](#source-image) will be read as is and the mod will try to read the image from this path.   If true, the mod will try to read an image for each season, based on the given Source Image path. For example, if Source Image is `assets/bush.png`, the mod will try to read these 4 images:
- `assets/bush_spring.png`
- `assets/bush_summer.png`
- `assets/bush_fall.png`
- `assets/bush_winter.png`

:warning: If any of these images is missing, the Furniture won't be created.

The `seasonal_bush_test` in the Example Pack uses this feature.

### Layers

Layers are an important tool for making custom Furniture, they are necessary to properly display your Furniture when other objects are passing through it (the player, or other Furniture). Since they are quite complicated, they have their own [Layers documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Layers.md).

### Seats

Seats are what allow the Farmer to sit on your Furniture (duh), since they are quite complicated, they have their own [Seats documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Seats.md).

### Slots

Slots are where you can place items or other Furniture on a table Furniture. Since they are quite complicated, they have their own [Slots documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Slots.md).

### Toggle

This field is boolean (true or false) and will make a Furniture toggleable. "Toggleable" means that it can be turned on and off with right-click.  
When a Furniture can be toggled, every sprite in its spritesheet needs to be duplicated: for every "Source Rect" you defined (for the [base sprite](#source-rect) or for [Layers](#layers)), the origin of the Width of the Rectangle will be added to its horizontal position when the Furniture is turned on. This way, your Furniture can change how it looks when it's toggled.

A good example of this is the `Custom Cauldron` Furniture in the Example Pack: you can see in its spritesheet that it has its base sprite in the top left corner, and a Layer in the bottom left corner, while the "On" variants of these sprites are on the left.  
![Custom Cauldron spritesheet](https://raw.githubusercontent.com/Leroymilo/FurnitureFramework/main/%5BFF%5D%20Example%20Pack/assets/cauldron.png)

### Sounds

With sounds, you can make your Furniture play custom sound effects when you click on it! Since they are quite complicated, they have their own [Sounds documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Sounds.md).

### Particles

Particles have so many settings, you have to read the [Custom Particles Documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Particles.md).
