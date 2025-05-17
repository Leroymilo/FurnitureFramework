using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{

	[JsonConverter(typeof(VariantConverter<Point>))]
	public class SourceRectOffsets : Dictionary<string, Point> {}

	public class VariantConverter<T> : ReadOnlyConverter<Dictionary<string, T>>
	{
		public override Dictionary<string, T>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			Dictionary<string, T> result = new();

			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = JObject.Load(reader);

				foreach (JProperty property in obj.Properties())
				{
					T? value = property.Value.ToObject<T>();
					if (value != null) result.Add(property.Name, value);
				}
			}
			else if (reader.TokenType == JsonToken.StartArray)
			{
				JArray arr = JArray.Load(reader);

				for (int i = 0; i < arr.Count; i++)
				{
					T? value = arr[i].ToObject<T>();
					if (value != null) result.Add(i.ToString(), value);
				}
			}
			else throw new InvalidDataException($"Could not parse Source Image or Source Rect Offsets from {reader.Value} at {reader.Path}.");

			return result;
		}
	}

	public class ImageVariantConverter : VariantConverter<string>
	{
		public override Dictionary<string, string>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, string>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.String)
			{
				// Only if a single Image Source path is given (most cases)
				string value = JToken.Load(reader).Value<string>() ?? "FF/assets/error.png";
				return new() {{ "", value }};
			}
			else return base.ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);
		}
	}
}