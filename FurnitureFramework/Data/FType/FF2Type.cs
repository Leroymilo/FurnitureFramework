using FurnitureFramework.Data.FPack;
using FurnitureFramework.Data.FType.Properties;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;


// Parses a type made with Format 2 to convert it to the latest format

namespace FurnitureFramework.Data.FType
{
	using BedType = StardewValley.Objects.BedFurniture.BedType;

	class FF2Type : OldType
	{
		#region fields

		public string DisplayName = "No Name";
		public string? Description;

		[JsonConverter(typeof(RotationConverter))]
		public List<string> Rotations;
		

		[JsonConverter(typeof(ImageVariantConverter))]
		public Dictionary<string, string> SourceImage = new();
		[JsonConverter(typeof(DirStructConverter<Rectangle>))]
		public DirStruct<Rectangle> SourceRect;

		[JsonConverter(typeof(FieldListDictConverter<LayerList, Layer>))]
		public FieldListDict<LayerList, Layer> Layers = new();

		[JsonConverter(typeof(FieldDictConverter<Collisions>))]
		public FieldDict<Collisions> Collisions = new();

		public string ForceType = "other";
		public int Price = 0;
		public int PlacementRestriction = 2;
		public List<string> ContextTags = new();
		public bool ExcludefromRandomSales = true;
		public List<string> ShowsinShops = new();
		public string? ShopId;

		[JsonConverter(typeof(VariantConverter<Point>))]
		public Dictionary<string, Point> SourceRectOffsets = new();

		// To migrate to animation class
		public int FrameCount = 0;
		public int FrameLength = 0;
		public Point AnimationOffset = Point.Zero;

		public SpecialType SpecialType = SpecialType.None;
		public PlacementType PlacementType = PlacementType.Normal;
		public Rectangle? IconRect;
		public bool Seasonal;

		public bool Toggle = false;
		public bool TimeBased = false;

		[JsonConverter(typeof(FieldListConverter<SoundList, Sound>))]
		public SoundList Sounds = new();

		[JsonConverter(typeof(FieldListDictConverter<SeatList, Seat>))]
		public FieldListDict<SeatList, Seat> Seats = new();

		[JsonConverter(typeof(FieldListDictConverter<SlotList, Slot>))]
		public FieldListDict<SlotList, Slot> Slots = new();

		[JsonConverter(typeof(FieldListDictConverter<LightList, Light>))]
		public FieldListDict<LightList, Light> LightSources = new();

		[JsonConverter(typeof(FieldListDictConverter<ParticleList, Particles>))]
		public FieldListDict<ParticleList, Particles> Particles = new();

		// TV
		[JsonConverter(typeof(DirStructConverter<Point>))]
		public DirStruct<Point> ScreenPosition = new();
		public float ScreenScale = 2f;

		// Bed
		public BedType BedType = BedType.Double;
		public Point BedSpot = new(1);
		public Rectangle? BedArea;
		public Rectangle? BedAreaPixel;

		// Fishtank
		[JsonConverter(typeof(DirStructConverter<Rectangle>))]
		public DirStruct<Rectangle> FishArea = new();
		public bool DisableFishtankLight = false;

		#endregion

		public static string? ReplaceNullTokens(string? value)
		{
			if (value == null) return value;
			return value.Replace("{{", "[[").Replace("}}", "]]");
		}
		public static string ReplaceTokens(string value)
		{
			return value.Replace("{{", "[[").Replace("}}", "]]");
		}

		public override FF3Type Convert(ConversionInfo info)
		{
			FF3Type result = new();

			#region field matching

			result.DisplayName = ReplaceTokens(DisplayName);
			result.Description = ReplaceNullTokens(Description);
			result.Rotations = Rotations;
			result.SourceImage = SourceImage;
			result.Layers = Layers;
			result.Collisions = Collisions;
			
			result.ForceType = ForceType;
			result.Price = Price;
			result.PlacementRestriction = PlacementRestriction;
			result.ContextTags = ContextTags;
			result.ExcludefromRandomSales = ExcludefromRandomSales;
			for (int i = 0; i < ShowsinShops.Count; i++)
			{
				result.ShowsinShops.Add(ReplaceTokens(ShowsinShops[i]));
			}
			result.ShopId = ReplaceNullTokens(ShopId);

			result.SourceRectOffsets = SourceRectOffsets;
			result.Animation = new(){
				FrameCount=FrameCount,
				FrameDuration=new(){FrameLength},
				Offset=new(){AnimationOffset}
			};
			result.Animation.Validate();

			result.SpecialType = SpecialType;
			result.PlacementType = PlacementType;
			result.IconRect = IconRect;
			result.Toggle = Toggle;
			result.TimeBased = TimeBased;

			result.Sounds = Sounds;
			result.Seats = Seats;
			result.Slots = Slots;
			result.Lights = LightSources;
			result.Particles = Particles;

			result.ScreenPosition = ScreenPosition;
			result.ScreenScale = ScreenScale;

			result.BedType = BedType;
			result.BedSpot = BedSpot;
			if (BedArea.HasValue)
				result.BedArea = new(BedArea.Value.Location * new Point(16), BedArea.Value.Size * new Point(16));
			else if (BedAreaPixel.HasValue)
				result.BedArea = BedAreaPixel;
			
			result.FishArea = FishArea;
			result.DisableFishtankLight = DisableFishtankLight;

			#endregion

			#region positional rework

			foreach (string dir_key in Rotations)
			{
				// Creating new layer from SourceRect field

				// Computing new coordinates for all fields
			}

			#endregion

			// Seasonal stuff

			return result;
		}
	}
}