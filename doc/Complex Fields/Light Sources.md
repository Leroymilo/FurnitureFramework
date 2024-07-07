# How to define Custom Light Sources?

The `Light Sources` field of a Furniture is a list of Light Sources objects.  
See the Example Pack for examples.

A Light Source object has up to 5 fields:

## Position

This is the position of the center of the light source on the Furniture **in pixels**. This field can be directional.

## Source Image

This is the path of the image that will be used for the light, you can provide it yourself in your assets or use one from the game.  
For example, "Content/LooseSprites/Lighting/indoorWindowLight.png" is used for lamp lights, and "FF/assets/light_glows/window.png" is used for the window glow (it is in the Furniture Framework's assets because I extracted it from the cursors tilesheet).

## Mode

This defines when the light or glow will be applied, it can be one of 5:
- `always_on`
- `when_on`
- `when_off`
- `when_dark_out`
- `when_bright_out`

"when_on" and "when_off" refer to toggleable Furniture (see [here](https://github.com/Leroymilo/FurnitureFramework/blob/main/doc/Furniture.md#Toggle)), and "when_dark_out" and "when_bright_out" will make your light turn on and or off based on the weather and time ("dark out" means night or rainy weather).

## Radius

This is a number defining the radius of your light source, it simply scales your light texture (this only applies to light sources, not glows).

## Is Glow

This is a boolean field (true or false) to deferentiate between light sources and light glows.  
Light sources will prevent the screen to darken around them at night time or on rainy days, while glows will directly draw their texture in the world. For example, lamps use light sources while windows use light glows.  
Light glows are mostly used for Furniture supposed to glow during the day, when there's no dark filter to cancel. They also have a nice feature with obstruction: place a table in front of a window, and you'll see the light glow spill over it, place a column in front of the window and the glow will be cut off.

## Other Info

For better examples and references for Position coordiantes, check the Lamp and Window Tests in the Example Pack.