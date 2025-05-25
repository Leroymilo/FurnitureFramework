using System.Runtime.Versioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	[RequiresPreviewFeatures]
	[JsonConverter(typeof(DepthConverter))]
	public class Depth: Field
	{
		public Depth() { is_valid = true; }	// used for non-base layer default depth

		[JsonIgnore]
		public bool is_default = false;

		public int Tile = 0;
		public int Sub = 0;
		public bool Front = false;

		public float GetValue(float top)
		{
			if (Front) return 1;
			
			float min = top + 64 * Tile + 16;
			float max = top + 64 * (Tile + 1) - 2;

			float result = min + (max - min) * (Sub / 1000f);
			
			return result / 10000f;
		}
	}

	[RequiresPreviewFeatures]
	class DepthConverter : ReadOnlyConverter<Depth>
	{
		public override Depth? ReadJson(JsonReader reader, Type objectType, Depth? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			JObject new_obj = new();

			if (reader.TokenType == JsonToken.String && JToken.Load(reader).ToString() == "Front")
				new_obj.Add("Front", JToken.FromObject(true));

			else if (reader.TokenType == JsonToken.Integer)
				new_obj.Add("Tile", JToken.Load(reader).Value<int>());

			else if (reader.TokenType == JsonToken.StartObject)
				new_obj = JObject.Load(reader);

			else throw new InvalidDataException($"Could not parse Depth from {reader.Value} at {reader.Path}.");

			Depth result = new();
			serializer.Populate(new_obj.CreateReader(), result);
			return result;
		}
	}
}