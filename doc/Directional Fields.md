# What are Directional Fields?

Directional Fields is a type of field that *can* (not must) depend on the rotation of the Furniture, so that the value read by the mod will change depending on which direction the Furniture is placed in.

When a field is directional, its value can either be itself, or a dictionary with rotation names as keys and the actual data fields as values, the rotation names being defined in the Furniture's [Rotations](Furniture.md#rotations) field.

If you have a doubt about the validity of a Directional Field you defined, you can rely on the json schema to tell you when there's a mistake. More info on how to set it up [here](Author.md#content).

Let's take the Furniture's [Layers](Complex%20Fields/Layers.md) as an example. If you choose to have a single Layer for every rotation, the field will look like this:
```json
"Layers": {
	"Source Rect": { "X": 0, "Y": 0, "Width": 32, "Height": 32 }
}
```
But if you want each rotation to look different (which you often do), you need to use the directional variant of this field.  
For this example, we'll take the "Table Test" Furniture of the Example Pack. It has `"Rotation": 2`, if you read about the Furniture `Rotations` field, you'll now that its rotation keys are "Horizontal" and "Vertical". This is how its `Layer` looks like:
```json
"Layers": {
	"Horizontal": {
		"Source Rect": { "X": 0, "Y": 0, "Width": 32, "Height": 32 }
	},
	"Vertical": {
		"Source Rect": { "X": 32, "Y": 0, "Width": 16, "Height": 48 }
	}
}
```
![Table sprite-sheet](images/directional_layers_example.png)  
You can see how each object in the `Layers` definition matches a part of the sprite-sheet.

Most directional fields can also be defined by an array instead of an object (`Collisions` cannot be an Array), this works the same way. Here what it looks like when it is non-directional:
```jsonc
"Layers": [
	// My layers applied to all directions
]
```

And what it looks like when directional:
```jsonc
"Layers": {
	"Down": [
		// My layers applied when the rotation is "Down"
	],
	"Right": [
		// My layers applied when the rotation is "Right"
	],
	"Up": [
		// My layers applied when the rotation is "Up"
	],
	"Left": [
		// My layers applied when the rotation is "Left"
	]
}
```

Some fields do not require a value for every direction. For example, if your Furniture only has Particles when it's facing Right or Left, but not when it's facing Up or Down, you can reduce the field to this:
```json
"Particles": {
	"Right": [
		// My particles applied when the rotation is "Right"
	],
	"Left": [
		// My particles applied when the rotation is "Left"
	]
}
```

Some Directional Fields have properties which are themselves directional (marked by `(directional)` in this documentation), this means that if the field is very similar in all directions but only one of its properties changes, you can define it once with a directional property. For example, here's a valid variant of the `Table Test`'s `Layers`:
```json
"Layers": {
	"Source Rect": {
		"Horizontal": {
			"X": 0,
			"Y": 0,
			"Width": 32,
			"Height": 32
		},
		"Vertical": {
			"X": 32,
			"Y": 0,
			"Width": 16,
			"Height": 48
		}
	}
}
```
Here, there is a single Layer, but its Source Rect is directional. You can ignore this feature if it's too complex for you.