using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data.FType.Properties
{
	class RotationConverter : ReadOnlyConverter<List<string>>
	{
		public override List<string>? ReadJson(JsonReader reader, Type objectType, List<string>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			List<string> result = new();

			if (reader.TokenType == JsonToken.Integer)
			{
				int? rot_count = JToken.Load(reader).Value<int>();

				switch (rot_count)
				{
					case 1:
						result.Add("NoRot"); break;
					case 2:
						result.AddRange(new List<string>() {
							"Horizontal", "Vertical"
						}); break;
					case 4:
						result.AddRange(new List<string>() {
							"Down", "Right", "Up", "Left"
						}); break;
					default:
						throw new InvalidDataException($"Could not parse Rotations from {reader.Value} at {reader.Path}.");
				}
			}
			else if (reader.TokenType == JsonToken.StartArray)
			{
				result = JArray.Load(reader).ToObject<List<string>>() ?? new() { "NoRot" };
			}
			else throw new InvalidDataException($"Could not parse Rotations from {reader.Value} at {reader.Path}.");

			return result;
		}
	}
}