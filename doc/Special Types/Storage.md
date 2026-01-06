# How to define a custom Storage Furniture (FFStorage)?

You can either use a preset to have your storage behave like one of the existing vanilla menus, or define your tabs and conditions yourself.
See the [Custom Storage Template](/doc/Templates/Custom%20Storage/) for a simple example.

## Storage Preset

This will copy the definition of one of the vanilla menus, including tabs with their conditions and item limitations (what types can be placed in this storage). Values are:
- `None`
- `Dresser`
- `FurnitureCatalogue`
- `Catalogue`

If this field is set, the following fields will be ignored.

Note: when setting this to Catalogue or FurnitureCatalogue, the Storage will accept Floor/Wallpaper or Furniture respectively, unlike the actual catalogues, which don't accept "selling" any items.

## Storage Condition

This is a string of text that you can use to restrict what can be placed in this Storage using a [Game State Query](https://stardewvalleywiki.com/Modding:Game_state_queries). Use the `Input` keyword to refer to the item being tested for the storage.  
The storage will accept all items if this is not set.

Note: if there's an error in the Game State Query, the game will give you an error report in the console with details. I can't really help to fix issues with it because I used the built-in function to read them, so I don't know much about it.

## Storage Tabs

This is a list of "tab properties", each entry in this list has these fields:

### ID

A simple string of text used to differentiate items when patching this data with Content Patcher. They don't have to be globally unique but have to be distincts for different items in the same list.

### Condition

This is a string of text that you can use to filter what items will be shown in that tab a [Game State Query](https://stardewvalleywiki.com/Modding:Game_state_queries). Use the `Input` keyword to refer to the item being tested for the tab.  
The tab will show all items if this is not set; it is recommended to do this in the first tab to have an "everything" tab.

Note: if there's an error in the Game State Query, the game will give you an error report in the console with details. I can't really help to fix issues with it because I used the built-in function to read them, so I don't know much about it.

### Source Image

The path (from your mod's root folder) to the image where the icon of this tab is located. You can take the vanilla tabs as a model, they are in "LooseSprites/Cursors" and "LooseSprites/Cursors2". You can even use the vanilla icons directly by setting this field to "Content/LooseSprites/Cursors" and giving the correct `Source Rect`.

### Source Rect

The part of the source image this tab should use as an icon, it's a [Rectangle](../Structures/Rectangle.md) **in pixels**.

## Opening/Closing Animation

You can define an animation that will play when the storage Furniture opens and closes by setting the "Opening Animation" and "Closing Animation" fields, their structure is the same as the Furniture's [base Animation](../Furniture.md#animation). If the opening animation is defined but the closing is not, it will just be the opening reversed.