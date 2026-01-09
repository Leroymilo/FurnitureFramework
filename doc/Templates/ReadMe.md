# Templates

This folder holds Furniture Packs made to be used as examples for specific features. If you think there's something in FF that would be easier to understand with a template, feel free to suggest it.

## Seasonal Furniture

This is a very simple example of a Mixed Pack (CP + FF) to create a Furniture with a sprite that depends on the in-game season. This method works with a lot of CP tokens like `season` and `weather`. This was originally in the Example Pack but was moved here for practical purposes.

## Dropdown Config

This is an example of how to create a Mixed Pack (CP + FF) which create a config option to switch between variants of a Furniture with a dropdown option button in the Generic Mod Config Menu (GMCM).

## Time Based Animation

This is an example of how to make a Furniture with different animations depending on the time of day.  
It works by having a Furniture both with an `Animation` field and `Time Based` set to `true`. To make it animated only during the day/night, simply make all the frames for the night/day identical.

## Full CP Pack

This is an example of how to make a Furniture Pack by putting it entirely in FF's Default Pack with Content Patcher. Note that this type of pack still requires FF to work, but it allows the use of CP tokens and operations, which can be quite usefull.

## i18n Example

This is an example of how to incorporate i18n translations in your Furniture's names and descriptions, specifically how the i18n implementation interacts with [Variants](/doc/Furniture.md#variants). Please make sure to read the [documentation on i18n](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation#i18n_folder) to understand how its files work (the "Reading translations" is not important here).

## Custom Storage

This is an example of a custom `FFStorage` Furniture with custom tabs and conditions.

## Layered TV

Just an example of a TV with a custom screen depth and a layer that is drawn over the screen to test compatibility with custom TV channel mods.