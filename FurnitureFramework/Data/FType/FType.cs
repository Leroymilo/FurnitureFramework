using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FurnitureFramework.Data.FType.Properties;
using StardewModdingAPI;

namespace FurnitureFramework.Data.FType
{
	using BedType = StardewValley.Objects.BedFurniture.BedType;

	public enum SpecialType {
		None,
		Dresser,
		FFStorage,
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

	public enum CatalogueTab {
		None,
		Table,	// Table, LongTable, Dresser
		Seat,	// Chair, Bench, Couch, Armchair
		Wall,	// Painting, Window
		Floor,	// Rug
		Decor	// Lamp, Sconce, Bookcase, Decor, Other, Fireplace
	}

	public enum StoragePreset {
		None,
		Dresser,
		FurnitureCatalogue,
		Catalogue
	}

	public struct Variant
	{
		public string ID;
		public string ImageVariant;
		public string RectVariant;
		public string SourceImage;
		public Point Offset;

		public string GetVariantString(string value, IContentPack pack)
		{
			value = ReplaceTokens(value);
			if (value.StartsWith("i18n:", true, null))
			{
				value = pack.Translation.Get(value[5..]);
				value = ReplaceTokens(value);
			}
			return value;
		}

		private string ReplaceTokens(string value)
		{
			return value
				.Replace("[[ImageVariant]]", ImageVariant, true, null)
				.Replace("[[RectVariant]]", RectVariant, true, null);
		}
	}

	[JsonConverter(typeof(SpaceRemover<FType>))]
	public partial class FType
	{
		#region fields

		[JsonIgnore]
		public string ModID;
		[JsonIgnore]
		public string FID;
		[JsonIgnore]
		public readonly Dictionary<string, Variant> Variants = new();

		public int Priority = 1000;
		public string DisplayName = "No Name";
		public string? Description;

		[JsonConverter(typeof(RotationConverter))]
		public List<string> Rotations;

		[JsonConverter(typeof(ImageVariantConverter))]
		public Dictionary<string, string> SourceImage = new();

		[JsonConverter(typeof(FieldListDictConverter<LayerList, Layer>))]
		public FieldListDict<LayerList, Layer> Layers = new();
		public bool DrawLayersWhenPlacing = false;

		[JsonConverter(typeof(FieldDictConverter<Collisions>))]
		public FieldDict<Collisions> Collisions = new();

		public string ForceType = "other";
		public int Price = 0;
		public int PlacementRestriction = 2;
		public List<string> ContextTags = new();
		public bool ExcludefromRandomSales = true;
		public CatalogueTab FurnitureCatalogueTab = CatalogueTab.None;
		public List<string> ShowsinShops = new();
		public string? ShopId;

		[JsonConverter(typeof(VariantConverter<Point>))]
		public Dictionary<string, Point> SourceRectOffsets = new();

		public Animation Animation = new();
		public bool AnimateWhenPlacing = true;
		public SpecialType SpecialType = SpecialType.None;
		public PlacementType PlacementType = PlacementType.Normal;
		public Rectangle? IconRect;
		public bool Toggle = false;
		public bool TimeBased = false;

		[JsonConverter(typeof(FieldListConverter<SoundList, Sound>))]
		public SoundList Sounds = new();

		[JsonConverter(typeof(FieldListDictConverter<SeatList, Seat>))]
		public FieldListDict<SeatList, Seat> Seats = new();

		[JsonConverter(typeof(FieldListDictConverter<SlotList, Slot>))]
		public FieldListDict<SlotList, Slot> Slots = new();

		[JsonConverter(typeof(FieldListDictConverter<LightList, Light>))]
		public FieldListDict<LightList, Light> Lights = new();

		[JsonConverter(typeof(FieldListDictConverter<ParticleList, Particles>))]
		public FieldListDict<ParticleList, Particles> Particles = new();

		// TV
		[JsonConverter(typeof(DirStructConverter<Point>))]
		public DirStruct<Point> ScreenPosition = new();
		public float ScreenScale = 2f;

		[JsonConverter(typeof(FieldDictConverter<Depth>))]
		public FieldDict<Depth> ScreenDepth = new() { { Utils.NOROT, new() { Tile = 0, Sub = 1000 } } };

		// Bed
		public BedType BedType = BedType.Double;
		public Point BedSpot = new(1);
		public Rectangle? BedArea;

		// Fishtank
		[JsonConverter(typeof(DirStructConverter<Rectangle>))]
		public DirStruct<Rectangle> FishArea = new();
		public bool DisableFishtankLight = false;

		// FFStorage
		public StoragePreset StoragePreset = StoragePreset.None;
		public string? StorageCondition;
		public List<TabProperty> StorageTabs = new();

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
					ModEntry.Log($"Missing Collisions for rotation {rot_name}.", LogLevel.Error);
					throw new InvalidDataException($"Missing Collisions for rotation {rot_name}.");
				}

				valid = false;
				try { valid = Layers[rot_name][0].is_valid; }
				catch { }
				if (!valid)
				{
					ModEntry.Log($"Missing Layer for rotation {rot_name}.", LogLevel.Error);
					throw new InvalidDataException($"Missing Layer for rotation {rot_name}.");
				}

				// The default depth of non-base layers is 
				foreach (Layer layer in Layers[rot_name].Skip(1))
					if (layer.Depth.is_default) layer.Depth.Sub = 1000;

				Lights[rot_name].Validate(context);
			}

			ShowsinShops.Add("leroymilo.FF.debug_catalog");

			switch (PlacementType)
			{
				case PlacementType.Rug:
					ForceType = "rug";
					break;
				case PlacementType.Mural:
					ForceType = "painting";
					break;
			}
		}

		public void SetIDs(string mod_id, string f_id)
		{
			ModID = mod_id;
			FID = f_id.Replace("[[ModID]]", mod_id, true, null);
			ShopId = ShopId?.Replace("[[ModID]]", mod_id, true, null);
			for (int i = 0; i < ShowsinShops.Count; i++)
				ShowsinShops[i] = ShowsinShops[i].Replace("[[ModID]]", mod_id, true, null);
			for (int i = 0; i < ContextTags.Count; i++)
				ContextTags[i] = ContextTags[i].Replace("[[ModID]]", mod_id, true, null);
			FillVariants();
		}

		void FillVariants()
		{
			if (SourceRectOffsets.Count == 0)
				FillVariants(FID, "", Point.Zero);

			foreach (KeyValuePair<string, Point> pair in SourceRectOffsets)
			{
				FillVariants(
					$"{FID}_{pair.Key}",
					pair.Key, pair.Value
				);
			}
		}

		void FillVariants(string id, string rect_variant, Point offset)
		{
			foreach (KeyValuePair<string, string> pair in SourceImage)
			{
				string full_id = id;
				if (pair.Key != "") full_id = $"{id}_{pair.Key}";

				Variants.Add(full_id, new()
				{
					ID = full_id,
					ImageVariant = pair.Key,
					SourceImage = pair.Value,
					RectVariant = rect_variant,
					Offset = offset
				});
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
				JObject obj = Utils.RemoveSpaces(JObject.Load(reader));
				T result = new();
				serializer.Populate(obj.CreateReader(), result);
				return result;
			}

			throw new InvalidDataException($"Could not parse Furniture from {reader.Value} at {reader.Path}.");
		}
	}
}