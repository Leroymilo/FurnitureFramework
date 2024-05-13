# User Tutorial

If you want to use a mod (Furniture Pack) that was made for this Framework, you just need to install the latest version of the mod available on [Nexus](https://www.nexusmods.com/stardewvalley/mods/23458?tab=files).

# Author Tutorial

Here is [the documentation](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Author.md) on how to create a Furniture Pack.  
Here's a list of published mods using this Framework:
- [Lynn's Livingroom](https://www.nexusmods.com/stardewvalley/mods/23677)
- [Basic Wardrobes](https://www.nexusmods.com/stardewvalley/mods/23666)

If you are a C# mod author and need an API for this mod, you can either make it and ask for a pull request, or ping me on the SV discord server, I should be able to make it for you.

If you came here to update your Furniture Pack to the latest format version, check the [Format changelogs](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Format%20changelogs.md).

# Features

What parts of Furniture are customizable with this Framework:
- Display Name (translation not supported yet)
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
- Support for some special Furniture (Dresser, TV)

# Thanks

Huge thanks to:
- @LynnNexus for testing, feedback and bug report
- @atravita for the initial idea to implement multiple slots per Furniture
- The Stardew Valley modding community for all their help and support

# TODO

What parts of Furniture I plan to add customization to in this Framework:
- Support for mural Furniture
- Support for Bed
- Support for other special Furniture (Fish Tank, Randomized Plant (?))
- Custom light sources
- Add directionality to some fields of Particles
- Add Particle list of scales and list of scale_changes

# Known Issues

- Placing Custom Furniture on a table (vanilla or custom) is kind of broken.
