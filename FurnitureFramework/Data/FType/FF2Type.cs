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
		
		public Animation OpeningAnimation = new();
		public Animation ClosingAnimation = new();

		#endregion

		public override void Convert(ConversionInfo info)
		{
			
		}
	}
}