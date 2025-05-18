using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	[JsonConverter(typeof(SpaceRemover<FType>))]
	public class FType
	{
		#region fields

		public int Priority = 1000;
		public string DisplayName = "No Name";
		public string? Description;

		// The method to use parsed rotations to parse directional field isn't reliable: it's dependant on json order
		// Fix: parse directional fields into a dict, then use actual rotations keys when fetching the values
		[JsonConverter(typeof(RotationConverter))]
		public List<string> Rotations;

		[JsonConverter(typeof(ImageVariantConverter))]
		public Dictionary<string, string> SourceImage = new();

		public DirectionalCollisions Collisions = new();
		public string ForceType = "other";
		public int Price = 0;
		public int PlacementRestriction = 2;
		public List<string> ContextTags = new();
		public bool ExcludefromRandomSales = true;
		public List<string> ShowsinShop = new();
		public string? ShopId;

		[JsonConverter(typeof(VariantConverter<Point>))]
		public Dictionary<string, Point> SourceRectOffsets = new() { { "", Point.Zero } };

		public JObject? Animation;

		public bool AnimateWhenPlacing = true;
		public string SpecialType = "None";
		public string PlacementType = "Normal";
		public Rectangle? IconRect;
		public bool Toggle = false;
		public bool TimeBased = false;
		public JToken? Sounds;
		public JToken? Layers;
		public bool DrawLayersWhenPlacing = false;
		public JToken? Seats;
		public JToken? Slots;
		public JToken? Lights;
		public JToken? Particles;

		// TV
		public JToken? ScreenPosition;
		public float ScreenScale = 2f;

		// Bed
		public string BedType = "Double";
		public JToken? BedSpot;
		public JToken? BedArea;

		// Fishtank
		public JToken? FishArea;
		public bool DisableFishtankLight = false;

		#endregion

		[OnDeserialized]
		private void validate(StreamingContext context)
		{
			foreach (string rot_name in Rotations)
			{
				bool valid = false;
				try { valid = Collisions[rot_name].is_valid; }
				catch { }
				if (!valid) throw new InvalidDataException($"Missing Collisions for rotation {rot_name}.");
			}
		}
	}

	/// <summary>
	/// Removes spaces in the keys of a json
	/// </summary>
	class SpaceRemover<T> : ReadOnlyConverter<T> where T : new()
	{
		/// <inheritdoc />
		public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = JObject.Load(reader);
				JObject new_obj = new();
				foreach (JProperty property in obj.Properties())
				{
					new_obj.Add(property.Name.Replace(" ", null), property.Value);
				}

				T result = new();
				serializer.Populate(new_obj.CreateReader(), result);
				return result;
			}

			throw new InvalidDataException($"Could not parse Furniture from {reader.Value} at {reader.Path}.");
		}
	}
}