# Technical Documentation

This is a description of the various systems that make this mod work, you only need to read this if your intention is to contribute to its code.

# Structure

## ModEntry

Like any other C# Stardew Valley mod, the Furniture Framework (FF) starts by deriving the `Mod` class from SMAPI in the [`ModEntry` class](../FurnitureFramework/ModEntry.cs). For ease of use, some properties are made to be statically accessed (config, helper, monitor).  
The instance of this class making the Furniture Framework has a few methods that are directly used as callbacks of Events through SMAPI:

- `on_game_launched` is pretty explicit, it does the main function calls to go through the Furniture Packs installed and prepare the content pipeline.
- `on_button_pressed` deals with custom keybinds for FF. For now, it's only Slot interaction (placing, removing and acting).
- `on_asset_requested` makes the necessary function calls to the [`FurniturePack` class](#furniturepack) so that content scripts and textures can be loaded by the content pack asset loading system. Going through this event is required to let Content Patcher access these assets for compatibility.
- `on_furniture_list_changed` is , `on_player_warped` and `on_save_loaded` are mainly used for resetting Particles related data.

## FurniturePack

This class is what handles the whole process of loading all Furniture Packs installed. It manages their config, asset requests by the game, as well as their reloading. It is split between 4 files to avoid having a single file of 800 lines.

- [FurniturePack.cs](../FurnitureFramework/Pack/FurniturePack.cs) has all the general use class variables (constants, static collections and properties), methods to extract the [`FurnitureType`](#furnituretype) of a modded `Furniture`, methods handling the asset requests, and the methods handling the `ff_debug_print` command on a base level, which calls the appropriate methods for each [`FurnitureType`](#furnituretype).
- [Loading.cs](../FurnitureFramework/Pack/Loading.cs) TODO
- [Config.cs](../FurnitureFramework/Pack/Config.cs) handles all the config stuff, including registering FF and Packs to GMCM. Its first method is the one registering all config options of FF. Then it has a `PackConfig` internal class used to store the config for a Pack when the game is running. I don't know if it's better than having all these properties and method directly in the `FurniturePack` class, but it makes sense to me so I keep it like this.
- [Include.cs](../FurnitureFramework/Pack/Include.cs) TODO

## FurnitureType

TODO

## Harmony Patches

# Processes

TODO