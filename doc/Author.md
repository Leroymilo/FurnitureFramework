# How to make a Furniture Pack

This is a tutorial on how to create a Content Pack for the [Furniture Framework](https://www.nexusmods.com/stardewvalley/mods/23458) mod, not to be confused with a Content Pack for [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) (shortened to "CP" in this tutorial) or [Custom Furniture](https://www.nexusmods.com/stardewvalley/mods/1254) (which is not updated for SV 1.6).

If you don't know how to format a json data file, please read a tutorial about it so that the rest of this tutorial can make sense.

This tutorial and documentation uses the [Example Pack](../Example%20Pack) as an example, sometime without a link.

## Contents

* [Manifest](#manifest)
* [Content](#content)
	* [Format](#format)
	* [Furniture](#furniture)
	* [Included](#included)
* [Commands](#commands)
* [Content Patcher Integration](#content-patcher-integration)
* [Alternative Textures Compatibility](#alternative-textures-compatibility)
* [Migration to FF 3.0](#migration-to-furniture-framework-30)

## Manifest

Like any other content pack, you will need a `manifest.json` file to make your Furniture Pack mod work with SMAPI. Here's the one provided in the Example Pack:

```json
{
	"Name": "Furniture Example Pack",
	"Author": "leroymilo",
	"Version": "3.0.1",
	"MinimumApiVersion": "4.2",
	"MinimumGameVersion": "1.6.15",
	"Description": "An example pack for the Furniture Framework",
	"UniqueID": "leroymilo.FurnitureExample.FF",
	"ContentPackFor": {
		"UniqueID": "leroymilo.FurnitureFramework",
		"MinimumVersion": "3.0.1"
	},
	"UpdateKeys": [ "Nexus:23458" ]
}
```

You need to make sure that the `UniqueID` for your Furniture Pack is unique, a good way to ensure this is too use your username as a part of it.  
The number in the `UpdateKeys` field points to the page of your mod, if you post your mod on [Nexus](https://www.nexusmods.com/stardewvalley/), this number appears in the url of your mod's page.  
I recommend keeping at least the first number of your mod's version matching the first number of FF's version because it also matches the content Format version, which is not backward-compatible. On Nexus, you can keep previous versions of your mod available for users who want to stay on a previous major version. 

## Content

This is the file where you define all your custom Furniture. Please keep in mind that all file paths that you will write in it have to be relative to your mod's directory (where `content.json` and `manifest.json` are), it is strongly recommended to put all images in the `assets` folder of your mod.

:warning: <span style="color:red">**WARNING**</span>: Unlike a CP Content Pack, field names in this file are <span style="color:red">**CASE SENSITIVE**</span>, make sure you don't forget capital letters when writting field names or ids.

It is highly recommended to use the Json Schema made for Furniture Packs (see at the very top of the content.json of the Example Pack), to setup your json validator with this Schema, you can start [here](https://json-schema.org/learn/getting-started-step-by-step#validate), but it mostly depends on what IDE or text editor you're using.

The `content.json` file is a model with only 3 fields:

### Format

The format you're using for your Furniture Pack, it matches the leftmost number in the mod's version. See the [Changelogs](Changelogs.md). The current Format is 3.

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

Since 3.0.0, every Furniture will have its own config entry automatically generated in the Generic Mod Config Menu interface.

Note: you can have as many custom Furniture as you want but their IDs must be all differents from each other.  
It is strongly recommended to add `[[ModID]].` at the start of every new Furniture ID to avoid conflicts with other mods or the game itself. For example, the first custom furniture of the Example Pack would be added to the game as `leroymilo.FurnitureExample.FF.simple_test`.  
You should also be able to modify vanilla Furniture by using their vanilla ID, doing so might be tricky for some Furniture because some properties are hardcoded (like the catalogues and the cauldron) so be carefull of what you modify (I'm pretty sure the current version of FF is able to replace 99% of vanilla Furniture).

Note 2: when using [Furniture Variants](Furniture.md#variants), the variation name or number will be automatically added to the original ID so that each variation can exist in the game.

The next part of the tutorial, with details about the Furniture definition is [here](Furniture.md).

You can also check the [Templates](Templates) for more advanced stuff, if there's something you don't understand how to do with the existing examples, you can also make a request for a new Template.

### Included

This is a dictionary where you can give the path of other json files that should be included in this Furniture Pack. Here's the structure to define an Included file:
```json
"Included": {
	"Name": {
		"Path": "my/included/file/path.json",
		"Description": "A Part of the Furniture Pack",
		"Enabled": true
	}
}
```

Each entry in the "Included" dictionary will create a config options to enable/disable this part of the Furniture Pack that will be shown in the Generic Mod Config Menu.  
An included file has the same structure as the [content.json file](#content).  
You can include files in included files, this will make config options nested in the Generic Mod Config Menu.

The "Name" can be anything you want but it must be unique in a content file, it will be used as the config option name in the Generic Mod Config Menu.  
The `Path` is the path of the included file, **relative to the mod folder**.  
The `Description` is also up to you (and optional), it will show in the tooltip of the config option.  
`Enabled` defines if this file is included by default. It is optional and its default value is `true`.

Note: if you want to edit/load this data with a Content Patcher mod, the patch would have `FF/<Furniture Pack UniqueID>/<Included Pack data path>` as a target, with `<Included Pack data path>` the path you wrote in the `Path` field.

## Commands

Here's details about console commands:

### ff_reload

Reloads a Furniture Pack, or all Packs if no id is given. Be careful when using it, reloading to apply changes to Furniture that were already placed in the world might break stuff.  
Usage: `ff_reload <ModID>` - ModID: the UniqueID of the Furniture Pack to reload

### ff_debug_print

Dumps all the data from a Furniture Pack in the console, or all Packs if no id is given.  
Usage: `ff_debug_print <ModID>` - ModID: the UniqueID of the Furniture Pack to debug print.

## Content Patcher Integration

There are multiple cases where you might want your mod to be both a CP content pack and a Furniture pack, for exemple if your mod has Furniture and other items or buildings.  
This is relatively easy: you basically have to make 2 separate mods, but since SMAPI is smart when reading mods, you can upload them together on Nexus (or other mod website) as a single file structured like this:
```
My Mod
└───[CP] My Mod
│	└───assets
│	│	└───(your assets for the CP pack)
│	└───manifest.json
│	│	{
│	│		...
│	│		"UniqueID": "MyName.MyMod.CP"
│	│		...
│	│	}
│	└───content.json
└───[FF] My Mod
	└───assets
	│	└───(your assets for the FF pack)
	└───manifest.json
	│	{
	│		...
	│		"UniqueID": "MyName.MyMod.FF"
	│		...
	│	}
	└───content.json
```

It is VERY IMPORTANT that the UniqueIDs of the 2 mods are different so that SMAPI correctly loads both.  
However, you can use the same update key for both, just make sure that both mods have the same version number to avoid SMAPI update warnings.

### How to patch FF resources

With the 3.0 FF update, it is now possible to patch textures (.png) and data (.json) requested by the Furniture Framework. This means that you can edit textures and data in your Furniture Pack with all the features of Content Patcher (config options, tokens, conditions,...).

To patch a resource from your Furniture Pack, you'll need to target `FF/<Furniture Pack UniqueID>/<path to the resource>`. Be careful, the `{{ModID}}` token from Content Patcher will point to the UniqueID of your CP mod, not your Furniture Pack! The path to the resource is relative to the root of your Furniture Pack, most of the time, it is what you wrote in your Furniture Pack's data.  
There is a simple example of this use in the [Example Pack](../Example%20Pack/%5BCP%5D/content.json): to make a bush change sprite in each season, its texture was edited with a `{{season}}` token. Another example is the [Template to make a dropdown config option](Templates/Dropdown%20Config).  
In both examples, the Content Patcher mod needs a version of the final textures to be loaded.

It is not recommended to patch a Furniture Pack's data, because it will cause FF to reload a lot of stuff. Patching stuff that will change only on every day (as opposed to every time change) is mostly fine.

## Alternative Textures Compatibility

Short answer: no.  
For technical reasons, it's practically impossible to make FF and AT truly compatible. This does not mean that installing both will break the game / burn your computer, but any Furniture made with FF cannot have Alternative Textures. I actually don't know for sure what would happen, but both mods change how the Furniture is shown in the game, so one would replace the other's method.

## Migration to Furniture Framework 3.0

Use the [migration script](../migrator), you'll need python and the PIL python library. The migrated version of you Furniture Pack will not keep its comments, so it is recommended to add them back after the conversion.  You can also do the migration by hand by looking through the doc, but good luck to not miss anything.

<span style="color:red">Please make sure to leave a version of your mod for FF 2.x available on your mod page</span>, this is because all Furniture Pack authors cannot update their mods at the same time, so many users will want to wait for all their mods to be updated before migrating, thus needing an earlier version of your mod.

How to run the python script:
- Install [python](https://www.python.org/downloads/).
- Once installed, open a terminal to run the command `pip install Pillow` and wait for it to finish.
- Download and uncompress the [migrator script](https://github.com/Leroymilo/FurnitureFramework/releases/tag/Patch-3.0.1).
- Make a copy of your Furniture Pack in case something goes wrong (might not be your fault, so make one even if you have experience with python).
- In a terminal, run the migrator script with the command `python <path to>/migrator/main.py <Furniture Pack directory> -o <Output directory>`, replace `<path to>` with the path to the newly downloaded script, `<Furniture Pack directory>` with the path to your mod (where the `manifest.json` is located), and `<Output directory>` with the path where you want your updated Furniture Pack to be written.
- The script might ask you for the directory for FF or for the game's extracted files if your Pack makes use of them, so you'll need to provide the **absolute path** to these if it asks you.
- If you had seasonal Furniture, the migrator will create a [Mixed Pack](#content-patcher-integration), because the Seasonal feature was removed from FF in favor of CP compatibility since it's much easier to setup dynamic sprites with CP than in FF's code.

Note: Seats' `Player Direction`s were changed from integers to clearer direction names (Up, Right, Down and Left), but the numbers still work, so the script won't change that.