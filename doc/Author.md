# How to make a Furniture Pack

This is a tutorial on how to create a Content Pack for the [Furniture Framework](https://www.nexusmods.com/stardewvalley/mods/23458) mod, not to be confused with a Content Pack for [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) (shortened to "CP" in this tutorial) or [Custom Furniture](https://www.nexusmods.com/stardewvalley/mods/1254) (which is not updated for SV 1.6).

If you don't know how to format a json data file, please read a tutorial about it so that the rest of this tutorial makes sense.

This tutorial and documentation uses the [Example Pack](https://github.com/Leroymilo/FurnitureFramework/tree/main/Example%20Pack) as an example, sometimes without a link.

## Contents

* [Manifest](#manifest)
* [Content](#content)
	* [Format](#format)
	* [Furniture](#furniture)

## Manifest

Like any other content pack, you will need a `manifest.json` file to make your Furniture Pack mod work with SMAPI. Here's the one provided in the Example Pack:

```json
{
	"Name": "Furniture Example Pack",
	"Author": "leroymilo",
	"Version": "1.0",
	"MinimumApiVersion": "4.0",
	"MinimumGameVersion": "1.6",
	"Description": "An example pack for the Furniture Framework",
	"UniqueID": "leroymilo.FurnitureExample.FF",
	"ContentPackFor": {
		"UniqueID": "leroymilo.FurnitureFramework"
	},
	"UpdateKeys": [ "Nexus:23458" ]
}
```

You need to make sure that the `UniqueID` for your Furniture Pack is unique, a good way to ensure this is too use your username as a part of it.  
The number in the `UpdateKeys` field points to the page of your mod, if you post your mod on [Nexus](https://www.nexusmods.com/stardewvalley/), this number appears in the url of your mod's page.

If your Furniture Pack is coupled with a CP Content Pack, they have to have distincts `UniqueID`s (usually prefixing them with `FF` and `CP` is enough).

## Content

This is the file where you define all your custom Fruniture. Please keep in mind that all file paths that you will write in it have to be relative to your mod's directory (where `content.json` and `manifest.json` are), it is strongly recommended to put all images in the `assets` folder of your mod.

:warning: <span style="color:red">**WARNING**</span>: Unlike a CP Content Pack, names in this field are <span style="color:red">**CASE SENSITIVE**</span>, make sure you don't forget capital letters when writting field names or ids.

The `content.json` file is a model with only 2 fields:

### Format

The format you're using for your Furniture Pack, for now, only the `"Format": 1` is supported, this number will get larger with future versions.

### Furniture

This is a dictionary where you'll define your custom Furniture.</br>
Each entry in this dictionary has the Id of the Furniture for key and the Furniture definition for value like this:
```json
"Furniture": {
	"my_furniture_id": {
		// My furniture definition
	},
	"my_other_furniture_id": {
		// My other furniture definition
	}
}
```

Note that you can have as many custom Furniture as you want but their ids must be all differents from each other.  
The `UniqueID` (followed by an dot `.`) of your mod will be automatically added to the id you give when the Furniture is added to the game. For example, the first custom furniture of the Example Pack would be added to the game as `leroymilo.FurnitureExample.FF.simple_test`. You only need to worry about this if you want another mod (like a CP Pack): you'll need to use the id of the Furniture Pack.

The next part of the tutorial, with details about the Furniture definition is [here](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Furniture.md).

You can also check the [Templates](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Templates.md) to make relatively simple Furniture.
