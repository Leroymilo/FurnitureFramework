using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	/// <summary>
	/// Holds the rotations names
	/// </summary
	public class Rotations
	{
		public static List<string> current = new();
		public List<string> rot_names = new();

		public Rotations(List<string> rot_names)
		{
			this.rot_names = rot_names;
			current = rot_names;
		}
	}

	class RotationConverter : JsonReadOnlyConv<Rotations>
	{
		public override Rotations? ReadJson(JsonReader reader, Type objectType, Rotations? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Integer)
			{
				int? rot_count = reader.ReadAsInt32();

				switch (rot_count)
				{
					case 1:
						return new(new() { "NoRot" });
					case 2:
						return new(new() {
							"Horizontal", "Vertical"
						});
					case 4:
						return new(new() {
							"Up", "Right", "Down", "Left"
						});
				}
			}
			else if (reader.TokenType == JsonToken.StartArray)
			{
				JObject obj = new() {
					{ "rot_names", JArray.Load(reader) }
				};
				return obj.ToObject<Rotations>();
			}

			throw new InvalidDataException($"Could not parse Furniture from {reader.Value} at {reader.Path}.");
		}
	}

	/// <summary>
	/// Holds data of Directional Fields (1 value per direction)
	/// T must be an object, not a value or an array
	/// </summary>
	class DirectionalField<T> where T : new()
	{
		public List<T> values;
	}

	/// <summary>
	/// Removes spaces in the keys of a json
	/// </summary>
	class DirectionalConverter<T> : JsonReadOnlyConv<DirectionalField<T>> where T : new()
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

					result.values = Enumerable.Repeat(instance, Rotations.current.Count).ToList();
				}
				else
				{
					// Parse directions separately

					result.values = new();
					foreach (string rot_name in Rotations.current)
					{
						JToken? token = obj.GetValue(rot_name);
						if (token == null) instance = new();
						else instance = token.ToObject<T>() ?? new();
						result.values.Add(instance);
					}
				}

				return result;
			}

			throw new InvalidDataException($"Could not parse Furniture from {reader.Value} at {reader.Path}.");
		}
	}
}