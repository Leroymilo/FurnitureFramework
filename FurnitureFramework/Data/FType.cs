using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	using BedType = StardewValley.Objects.BedFurniture.BedType;

	public enum SpecialType {
		None,
		Dresser,
		TV,
		Bed,
		FishTank,
		// RandomizedPlant
	}

	public enum PlacementType {
		Normal,
		Rug,
		Mural
	}

	[RequiresPreviewFeatures]
	[JsonConverter(typeof(SpaceRemover<FType>))]
	public class FType
	{
		#region fields

		public int Priority = 1000;
		public string DisplayName = "No Name";
		public string? Description;

		[JsonConverter(typeof(RotationConverter))]
		public List<string> Rotations;

		[JsonConverter(typeof(ImageVariantConverter))]
		public Dictionary<string, string> SourceImage = new();

		[JsonConverter(typeof(DirListDictConverter<LayerList, Layer>))]
		public DirListDict<LayerList, Layer> Layers = new();
		public bool DrawLayersWhenPlacing = false;

		[JsonConverter(typeof(DirFieldDictConverter<Collisions>))]
		public DirFieldDict<Collisions> Collisions = new();
		public string ForceType = "other";
		public int Price = 0;
		public int PlacementRestriction = 2;
		public List<string> ContextTags = new();
		public bool ExcludefromRandomSales = true;
		public List<string> ShowsinShop = new();
		public string? ShopId;

		[JsonConverter(typeof(VariantConverter<Point>))]
		public Dictionary<string, Point> SourceRectOffsets = new() { { "", Point.Zero } };

		public Animation Animation = new();
		public bool AnimateWhenPlacing = true;
		public SpecialType SpecialType = SpecialType.None;
		public PlacementType PlacementType = PlacementType.Normal;
		public Rectangle? IconRect;
		public bool Toggle = false;
		public bool TimeBased = false;
		public JToken? Sounds;
		public JToken? Seats;
		public JToken? Slots;
		public JToken? Lights;
		public JToken? Particles;

		// TV
		public JToken? ScreenPosition;
		public float ScreenScale = 2f;

		// Bed
		public BedType BedType = BedType.Double;
		public JToken? BedSpot;
		public JToken? BedArea;

		// Fishtank
		public JToken? FishArea;
		public bool DisableFishtankLight = false;

		#endregion

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			foreach (string rot_name in Rotations)
			{
				bool valid = false;
				try { valid = Collisions[rot_name].is_valid; }
				catch { }
				if (!valid)
				{
					ModEntry.log($"Missing Collisions for rotation {rot_name}.");
					throw new InvalidDataException($"Missing Collisions for rotation {rot_name}.");
				}

				valid = false;
				try { valid = Layers[rot_name][0].is_valid; }
				catch { }
				if (!valid)
				{
					ModEntry.log($"Missing Layer for rotation {rot_name}.");
					throw new InvalidDataException($"Missing Layer for rotation {rot_name}.");
				}
			}
		}
	}

	/// <summary>
	/// Removes spaces in the keys of a json
	/// </summary>
	[RequiresPreviewFeatures]
	class SpaceRemover<T> : ReadOnlyConverter<T> where T : new()
	{

		/// <inheritdoc />
		public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = Utils.RemoveSpaces(JObject.Load(reader));
				T result = new();
				serializer.Populate(obj.CreateReader(), result);
				return result;
			}

			throw new InvalidDataException($"Could not parse Furniture from {reader.Value} at {reader.Path}.");
		}
	}
}