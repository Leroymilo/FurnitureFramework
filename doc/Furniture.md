# Custom Furniture definition

To make Furniture highly customizable, the definition of a Furniture has a lot of properties. But don't worry, everything is explained here, and there's a good chance you won't need to use everything.

Once again, this documentation uses the [Example Pack](https://github.com/Leroymilo/FurnitureFramework/tree/main/Example%20Pack) as an example, it is strongly recommeded to go back and forth between the explanation here and the example to identify what is being explained.

## Contents

* [Required Fields](#required-fields)
	* [Display Name](#display-name)
	* [Rotations](#rotations)
	* [Source Image](#source-image)
	* [Source Rect](#source-rect)
	* [Collisions](#collisions)
* [Optional Fields](#optional-fields)
	* [Price](#price)
	* [Indoor & Outdoor](#indoor--outdoor)
	* [Exclude from Random Sales](#exclude-from-random-sales)
	* [Context Tags](#exclude-from-random-sales)
	* [Shows in Shops](#shows-in-shops)
	* [Shop Id](#shop-id)
	* [Layers](#layers)
	* [Seats](#seats)

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

This field defines the collisions of your Furniture, it's what defines what part of the Furniture the player will not be able to walk through and place other Furniture on.

This field is [Directional](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Directional%20Fields.md).

It's 2 main properties give the size ***in game tiles*** of the bounding box:
- "Width"
- "Height"

They are required to properly define a the collisions of a Furniture, they must be integers.

The other propery of the Collisions field is its "Map", with it, you to decide for each tile of the bounding box if it is traversable or not. For example, this is the perfect tool if you want to make an arch that you can walk under.  
To define a custom collision Map, you first have to write it as a square of characters of the size of the bounding box. Let's take the "Living Room" Furniture from the Example Pack, it's made of Furniture from the game to give a proper reference. Here's its main sprite:  
![The main sprite for the Living Room Furniture](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/images/collision_map_example.gif)  
This gif shows that its bounding box is 8x3, and its collision Map is represented by:
```
..XXXX..
XX....XX
......XX
```
In a collision Map, the tiles where the player can walk are represented by `.` and the tiles where they can't are represented by `X`. For now (maybe forever), the precision of the collision Map is limited to tiles.

To properly put the collision Map in the Collisions model, the newlines have to be replaced by forward slashes `/`, which would look like this:
```json
{
	"Width": 8,
	"Height": 3,
	"Map": "..XXXX../XX....XX/......XX"
}
```

Note: if the topmost row or the rightmost column of your Map is fill with `.`, you can remove it and reduce the bounding box size accordingly.

Note 2: if your Furniture uses a custom collision Map, there's a good chance that you'll have to define [Layers](#layers) to avoid layering issues.

## Optional Fields

### Force Type

In this field, you can force the [type of the Furniture](https://stardewvalleywiki.com/Modding:Items#Furniture) (as a string). If you don't know how it works, don't set it, no type other than "other" has truly been tested. You might need it if you want to make a table to place other objects on, but it was not tested. Please report it if you find Furniture types that completely break the mod so that I can list them here.

### Price

This is the default price of the Furniture, it will be used if it is added to a shop's item list without specifying a price.

### Indoor & Outdoor

These fields define if the Furniture can be placed respectively indoor and outdoor. They both accept boolean values (true or false, without quotation marks), and default to true. If both are set to false, your Furniture will be indoor only because of how the game is coded.

### Exclude from Random Sales

This defines wether or not this Furniture will show-up in random sales in the vanilla Furniture Catalogue and other Furniture shops. It's a boolean value (true or false), defaulting to true.

### Context Tags

This is an array (a list) of context tags you want to add to your Furniture, it defaults to an empty list. If you want to learn more about context tags, check [the wiki](https://stardewvalleywiki.com/Modding:Items#Context_tags).

### Shows in Shops

This is an array (a list) of string Shop IDs where you want your Furniture to show-up, it defaults to an empty list. For example, having:
```json
"Shows in Shops": ["Carpenter"]
```
will add your Furniture to Robin's Shop. Here's the list of [vanilla Shop IDs](https://stardewvalleywiki.com/Modding:Shops#Vanilla_shop_IDs) on the wiki.

When used in combination to the "Shop Id" field, you can create a custom Catalogue for your custom Furniture.

### Shop Id

The Shop ID of the Shop the game should open when right-clicking on the Furniture, it's a string that defaults to `null` (no Shop attached).  
You can attach one of the [vanilla Shops](https://stardewvalleywiki.com/Modding:Shops#Vanilla_shop_IDs), or your own Shop.  
By default, if the Shop ID given doesn't match any existing shop, a default shop based on the vanilla Furniture Catalogue (no owner) will be created.  
You can then use the same Shop ID in the "Shows in Shops" field of other Furniture you created to add them to this new Catalogue. If you want to add more rules to your custom Catalogue (multipliers, owners, ...), you'll need to define it in another Content Pack using Content Patcher.

An example of this is in the [Example Pack](https://github.com/Leroymilo/FurnitureFramework/blob/main/Example%20Pack/content.json).

Note: the Shop ID is raw, your mod's UniqueID will not be prepended to it, so make sure it's unique (you can manually add your mod's ID to it for example).

### Layers

Layers are an important tool for making custom Furniture, they are necessary to properly display your Furniture when other objects are passing through it (the player, or other Furniture). Since they are quite complicated, they have their own [Layers documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Layers.md).

### Seats

Seats are what allow the Farmer to sit on your Furniture (duh), since they are quite complicated, they have their own [Seats documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Seats.md).
