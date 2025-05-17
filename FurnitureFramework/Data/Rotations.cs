using System.Runtime.Versioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	class RotationConverter : ReadOnlyConverter<List<string>>
	{
		public static List<string> last = new();

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

			last = result;
			return result;
		}
	}

	/// <summary>
	/// Holds data of Directional Fields (1 value per direction)
	/// T must be an object, not a value or an array
	/// </summary>
	[RequiresPreviewFeatures]
	[JsonConverter(typeof(DirectionalConverter<>))]
	class DirectionalField<T> where T : new()
	{
		public List<T> values;
	}

	/// <summary>
	/// Removes spaces in the keys of a json
	/// </summary>
	[RequiresPreviewFeatures]
	class DirectionalConverter<T> : ReadOnlyConverter<DirectionalField<T>> where T : new()
	{
		/// <inheritdoc />
		public override DirectionalField<T> ReadJson(JsonReader reader, Type objectType, DirectionalField<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				DirectionalField<T> result = new();

				JObject obj = JObject.Load(reader);
				T? instance = obj.ToObject<T>();
				if (instance != null)
				{
					// Make all directions point to the same instance

					result.values = Enumerable.Repeat(instance, RotationConverter.last.Count).ToList();
				}
				else
				{
					// Parse directions separately

					result.values = new();
					foreach (string rot_name in RotationConverter.last)
					{
						JToken? token = obj.GetValue(rot_name);
						if (token == null) instance = new();
						else instance = token.ToObject<T>() ?? new();
						result.values.Add(instance);
					}
				}

				return result;
			}

			throw new InvalidDataException($"Could not parse Directional Field from {reader.Value} at {reader.Path}.");
		}
	}
}