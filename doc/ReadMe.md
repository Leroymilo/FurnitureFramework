# User Tutorial

If you want to use a mod (Furniture Pack) that was made for this Framework, you just need to install the latest version of the mod available on [Nexus](https://www.nexusmods.com/stardewvalley/mods/23458?tab=files).

# Author Tutorial

Here is [the documentation](Author.md) on how to create a Furniture Pack.  
Here's a list of published mods using this Framework that you can use as examples:
- [Lynn's Livingroom](https://www.nexusmods.com/stardewvalley/mods/23677)
- [Lynn's Bedroom](https://www.nexusmods.com/stardewvalley/mods/24275)
- [Basic Wardrobes](https://www.nexusmods.com/stardewvalley/mods/23666)
- [Stararmy's Museum Furniture](https://www.nexusmods.com/stardewvalley/mods/24224)

If you are a C# mod author and need an API for this mod, you can either make it and ask for a pull request, or ping me on the SV discord server, I should be able to make it for you.

[CHANGELOGS](Changelogs.md)

# Features

- New stuff that you can control with the Furniture Framework:
	- Display Name
	- Description
	- Collisions (Bounding Box and Collision Map)
	- Seats (chairs, armchairs, sofas, benches, ...)
	- Table Slots (multiple slots per Furniture!)
	- Toggleability (lamps, cauldrons, fireplaces, ...)
	- Light Sources (lamps) and Light Glows (windows)
	- Sound Effects (on click)
	- Particles

- Texture/Sprite customization:
	- Rotations (as many as you want!)
	- Source Image
	- Source Rectangles (for each rotation)
	- Source Rectangle for menu icon
	- Layers (with custom depth and Source Rectangles)
	- Variants (same data but with a different Source Image or Rectangle)
	- Animations (base Sprite and Layers)
	- Luminosity dependant Textures (like a vanilla window)

- Shop customization:
	- Default Price
	- Shops where the Furniture appears
	- Exclude from random sales
	- Attached Shop (like a Catalogue)

- Special Furniture:
	- Rugs
	- Wall-mounted Furniture
	- Dressers
	- TVs
		- Screen position and scale
	- Beds
		- Bed Type (simple, double, child bed)
		- Respawn point
		- Sleeping Area
	- Fish Tanks
		- Area where fish will swim
		- Optional vanilla Light Source

- Other Stuff:
	- Placement Rules (inside/outside/both)
	- Context Tags

- Support for included content files (to split a mods into multiple files)
- Support for Content Patcher compatibility (both image and data files patching)

- Specific Compatibility :
	- [Precise Furniture](https://www.nexusmods.com/stardewvalley/mods/23488)
	- [Market Town](https://www.nexusmods.com/stardewvalley/mods/19309)

# Thanks

Huge thanks to:
- The Stardew Valley modding community for all their help and support
- All testers (@LynnNexus, @aviroen, @tikamin, @tea, @reedtanguerra) for requesting and testing features, and reporting bugs 
- @atravita for the initial idea on how to implement multiple slots per Furniture

# TODO

What parts of Furniture I plan to add customization to in this Framework:
- See [changelogs](Changelogs.md#31-work-in-progress) for future versions

What I don't plan on adding, but I can work on if multiple people ask for it:
- Custom Randomized Plant
- Opening animation for custom Storage Furniture
- More customization for Particles (make some fields directional, list of scales and list of scale changes)

What can be added but I don't have the knowledge/courage to do it any-time soon:
- Music (sound) stop when Furniture turned off/on

# Known Issues

- Placing any StorageFurniture (Dresser, Fishtank or FFStorage) in a slot (vanilla or custom) will delete all items inside.
- Default Slots Debug Color does not apply until Pack reload or restart.
- Seats do not behave correctly for Furniture placed on Slots.
- Vanilla Furniture placed in slots doesn't show on/off state properly (fire isn't drawn on torches).
