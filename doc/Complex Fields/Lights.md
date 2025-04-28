# How to define Custom Light Sources?

The `Lights` field of a Furniture is a directional field containing one or multiple Light Sources and Light Glows.  
See the Example Pack for examples.

Here are the fields of a Light Source or Light Glow:

## Light Type (required)

The light type defines if this light is a source (like a vanilla lamp) or a glow (like a vanilla window). Note that light sources will only be visible when it's dark outside (night or rain) because they remove the darkness of the screen.  
Light Glows also have a nice feature with obstruction: place a table in front of a window, and you'll see the light glow spill over it, place a column in front of the window and the glow will be cut off (obstructed) by the column.  
The possible values are `Source` and `Glow`.

## Source Rect (required) (directional)

The Source Rect is a [Rectangle](../Structures/Rectangle.md), it is the part of the Source Image that will be used to draw this light.

## Position (required) (directional)

This is the position of the center of the light source on the Furniture **in pixels**, from the bottom left corner of the bounding box.

## Source Image

The path to the image that will be used for the light, you can provide it yourself in your assets or use one from the game.  
For example, "Content/LooseSprites/Lighting/indoorWindowLight.png" is used for lamp lights, and "FF/assets/light_glows/window.png" is used for the window glow (it is in the Furniture Framework's assets because I extracted it from the game's cursors tilesheet).  
If you omit this path, the main texture of your Furniture will be used.

## Toggle

This is a boolean value (true or false) that dictates if this light can be turned on and off by clicking on the Furniture.  
If this is set to `true`, the Source Rect of this light will be shifted to the right (by the width of the base source rect) when the Furniture is "on".  
If you omit this value, the equivalent from the main Furniture will be applied.

## Time Based

This is a boolean value (true or false) that dictates if this light will change its state depending on the outside light.  
If this is set to `true`, the Source Rect of this light will be shifted down (by the height of the base source rect) when it's dark out (because of night-time or rainy weather).  
If you omit this value, the equivalent from the main Furniture will be applied.

## Radius

This is a number defining the radius of your light source, it simply scales your light texture (this only applies to light sources because it looks bad on glows). The default value is `2`.

## Color

A way to give a tint to your light source, if you don't want to modify the texture itself. The `Color` is the _Name_ of a color, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.color?view=net-8.0#properties) for a list of accepted color names (RGB values are not accepted). Defaults to `White` (which will not affect the color of the texture).

# Examples

For better examples and references for Position coordiantes, check the Lamp and Window Tests in the Example Pack.