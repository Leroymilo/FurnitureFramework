using FurnitureFramework.Data.FType;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Data.FPack
{
	[JsonConverter(typeof(FormatChecker))]
	public partial class FPack
	{

		// Constants

		const int FORMAT = 3;
		const string DEFAULT_PATH = "content.json";
		const string CONFIG_PATH = "config.json";

		// Static Collections 

		static readonly Dictionary<string, IContentPack> ContentPacks = new();
		// UIDs of all Furniture Packs (for reload all).
		static readonly Dictionary<string, FPack> PacksData = new();
		// maps data_UID to pack.
		static readonly Dictionary<string, string> TypesOrigin = new();
		// maps type_id to the data_UID of the pack where it's defined.
		static readonly Dictionary<string, HashSet<string>> LoadedAssets = new();
		// a set of what asset names were loaded by each pack UID.

		static IContentPack DefaultPack;

		// Pack Properties
		[JsonIgnore]
		LoadData LoadData_;
		[JsonIgnore]
		string UID { get => LoadData_.UID; }
		[JsonIgnore]
		public string DataUID { get => LoadData_.DataUID; }
		[JsonIgnore]
		readonly PackConfig Config = new();

		public int Format = 0;
		public Dictionary<string, FType.FType> Furniture = new();
		public Dictionary<string, LoadData> Included = new();

		[JsonIgnore]
		public Dictionary<string, FPack> IncludedPacks = new();

		[JsonIgnore]
		FPack? Root = null;
		[JsonIgnore]
		bool IsIncluded { get => Root != null; }

		public void SetSource(LoadData load_data)
		{
			LoadData_ = load_data;

			LoadConfig();

			foreach (string id in Furniture.Keys.ToList())
			{
				FType.FType f_type = Furniture[id];
				Furniture.Remove(id);
				f_type.SetIDs(UID, id);

				foreach (Variant var_data in f_type.Variants.Values)
				{
					Config.AddType(var_data.ID, var_data.DisplayName);
					Furniture[var_data.ID] = f_type;
				}
			}

			foreach (string name in Included.Keys)
			{
				LoadData data = Included[name];
				data.ContentPack = load_data.ContentPack;
				data.Name = name;
				data.Parent = this;
				Config.AddIPack(data);
				IncludedPacks.Add(name, data.Load());
			}

			if (!IsIncluded)
				ModEntry.Log($"Success!", LogLevel.Debug);

			if (Furniture.Count == 0 && Included.Count == 0)
				ModEntry.Log("This Furniture Pack is empty!", LogLevel.Warn);
		}

	class FormatChecker : ReadOnlyConverter<FPack>
	{
		public override FPack? ReadJson(JsonReader reader, Type objectType, FPack? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = JObject.Load(reader);

				if (!obj.TryGetValue("Format", out JToken? format_token))
				{
					ModEntry.Log("Missing Format, skipping Furniture Pack.", LogLevel.Error);
					return null;
				}

				if (!CheckFormat(format_token)) return null;

				FPack result = new();
				serializer.Populate(obj.CreateReader(), result);

				return result;
			}

			ModEntry.Log($"Furniture Pack data is Invalid.");
			throw new InvalidDataException($"Furniture Pack data is Invalid.");
		}

		static bool CheckFormat(JToken format_token)
		{
			if (format_token.Type != JTokenType.Integer)
			{
				ModEntry.Log("Invalid Format, skipping Furniture Pack.", LogLevel.Error);
				return false;
			}

			int format = format_token.Value<int>();

			switch (format)
			{
				case > FORMAT:
				case < 1:
					ModEntry.Log($"Invalid Format: {format}, skipping Furniture Pack.", LogLevel.Error);
					return false;
				case < FORMAT:
					ModEntry.Log($"Format {format} is outdated, skipping Furniture Pack.", LogLevel.Error);
					ModEntry.Log("If you are a user, wait for an update for this Furniture Pack,", LogLevel.Info);
					ModEntry.Log($"or use a version of the Furniture Framework starting with {format}.", LogLevel.Info);
					ModEntry.Log("If you are the author, check the Changelogs in the documentation to update your Pack.", LogLevel.Info);
					return false;
				case FORMAT: return true;
			}
		}
	}
	}
}