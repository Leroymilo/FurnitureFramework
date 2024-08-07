# User Tutorial

If you want to use a mod (Furniture Pack) that was made for this Framework, you just need to install the latest version of the mod available on [Nexus](https://www.nexusmods.com/stardewvalley/mods/23458?tab=files).

# Author Tutorial

Here is [the documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Author.md) on how to create a Furniture Pack.  
Here's a list of published mods using this Framework that you can use as examples:
- [Lynn's Livingroom](https://www.nexusmods.com/stardewvalley/mods/23677)
- [Lynn's Bedroom](https://www.nexusmods.com/stardewvalley/mods/24275)
- [Basic Wardrobes](https://www.nexusmods.com/stardewvalley/mods/23666)
- [Stararmy's Museum Furniture](https://www.nexusmods.com/stardewvalley/mods/24224)

If you are a C# mod author and need an API for this mod, you can either make it and ask for a pull request, or ping me on the SV discord server, I should be able to make it for you.

[CHANGELOGS](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Changelogs.md)

# Features

What parts of Furniture are customizable with this Framework:
- Display Name (translation not supported yet)
- Custom Description
- Default Price
- Custom Rotations
- Custom Bounding Box Size
- Custom Collision Map
- Placement Rules
- Source Image
- Custom Source Rect
- Custom Source Rect for Icon
- Custom Layers
- Custom Seats
- Custom Attached Shop (Catalogue like)
- Shop it appears in
- Exclude from random sales
- Context Tags
- Custom table slots
- Image & Source Rect Variants
- Seasonal Textures
- Toggleable Furniture
- Custom Sound Effects
- Custom Particles
- Animated base Sprite and Layers
- Support for special Furniture (Dresser, TV, Bed, FishTank)
- Support for Rugs and Wall-mounted Furniture
- Support for configurable includes
- Custom Light Sources and Glows (lamps and windows)

# Thanks

Huge thanks to:
- The Stardew Valley modding community for all their help and support
- @LynnNexus for testing, feedback and bug report
- @atravita for the initial idea on how to implement multiple slots per Furniture

# TODO

What parts of Furniture I plan to add customization to in this Framework:
- CP compatibility to edit loaded sprites & data.	@hualianlian
- Add better titles and descriptions for the json schema.
- Support for i18n translations.
- Customize StorageFurniture allowed item types.	@B
- Weather dependant textures

What I don't plan on adding, but I can work on if someone asks for it:
- Custom Randomized Plant
- More customization for Particles (make some fields directionality, list of scales and list of scale changes)

What can be added but I don't have the knowledge/courage to do it any-time soon:
- Music (sound) stop when Furniture turned off/on

# Known Issues

- Default Slots Debug Color does not apply until Pack reload or restart.
