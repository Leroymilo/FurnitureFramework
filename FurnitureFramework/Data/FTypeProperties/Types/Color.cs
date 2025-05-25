



using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley;

namespace FurnitureFramework.Data
{
	using SDColor = System.Drawing.Color;
	
	class ColorConverter : ReadOnlyConverter<Color>
	{
		public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.String)
			{
				string color_name = JToken.Load(reader).ToString();

				// From color code
				if (Utility.StringToColor(color_name) is Color color)
					return color;

				// From color name
				SDColor c_color = SDColor.FromName(color_name);
				return new(c_color.R, c_color.G, c_color.B);
			}
			return existingValue;
		}
	}
}