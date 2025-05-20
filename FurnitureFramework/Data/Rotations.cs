using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
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

	public class Field
	{
		public bool is_valid = false;
	}

	/// <summary>
	/// Holds data of Directional Fields (1 value per direction)
	/// T must be an object, not a value or an array
	/// </summary>
	public class DirectionalField<T> : Dictionary<string, T> where T : Field, new()
	{
		public T Unique = new();

		public T First
		{
			get
			{
				foreach (T value in Values)
					if (value.is_valid) return value;
				if (Unique.is_valid) return Unique;
				throw new InvalidOperationException();
			}
		}

		new public T this[string key]
		{
			get
			{
				T? result;
				if (ContainsKey(key))
				{
					result = base[key];
					if (result.is_valid) return result;
				}
				if (Unique.is_valid) return Unique;
				throw new KeyNotFoundException();
			}
			set
			{
				base[key] = value;
			}
		}
	}

	/// <summary>
	/// Removes spaces in the keys of a json
	/// </summary>
	class DirectionalConverter<T> : ReadOnlyConverter<DirectionalField<T>> where T : Field, new()
	{
		/// <inheritdoc />
		public override DirectionalField<T> ReadJson(JsonReader reader, Type objectType, DirectionalField<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				DirectionalField<T> result = new();

				JObject obj = JObject.Load(reader);
				T? instance = obj.ToObject<T>();

				// Make all directions point to the same instance
				if (instance != null && instance.is_valid) result.Unique = instance;
				// Assume directional and parse as Dictionary
				else serializer.Populate(obj.CreateReader(), result);

				return result;
			}

			throw new InvalidDataException($"Could not parse Directional Field from {reader.Value} at {reader.Path}.");
		}
	}
}