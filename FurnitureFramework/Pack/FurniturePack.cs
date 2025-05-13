using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using FurnitureFramework.Type;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Shops;
using StardewValley.Objects;

namespace FurnitureFramework.Pack
{

	[RequiresPreviewFeatures]
	partial class FurniturePack
	{
		// Constants

		const int FORMAT = 3;
		const string DEFAULT_PATH = "content.json";
		const int DEFAULT_PRIO = 1000;
		const string CONFIG_PATH = "config.json";

		// Static Collections 

		static Dictionary<string, IContentPack> UIDs = new();
		// UIDs of all Furniture Packs (for reload all).
		static Dictionary<string, FurniturePack> packs = new();
		// maps data_UID to pack.
		static Dictionary<string, string> static_types = new();
		// maps type_id to the data_UID of the pack where it's defined.
		static Dictionary<string, HashSet<string>> loaded_assets = new();
		// a set of what asset names were loaded by each pack UID.

		static IContentPack default_pack;

		// Pack Properties

		readonly IContentPack content_pack;
		string path = DEFAULT_PATH;
		string UID { get => content_pack.Manifest.UniqueID; }
		string data_UID { get => $"{UID}/{path}"; }
		FurniturePack? root = null;
		bool is_included { get => root != null; }
		bool is_loaded = false;
		Dictionary<string, FurnitureType> types = new();
		Dictionary<string, IncludedPack> included_packs = new();

		private static void invalidate_game_data()
		{
			IGameContentHelper helper = ModEntry.get_helper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
		}

		#region Getters

		private FurnitureType get_type(string f_id)
		{
			return types[f_id];
		}

		private static bool try_get_type(string f_id, [MaybeNullWhen(false)] out FurnitureType type)
		{
			ModEntry.get_helper().GameContent.Load<Dictionary<string, string>>("Data/Furniture");

			type = null;

			if (!static_types.TryGetValue(f_id, out string? UID))
				return false;

			type = packs[UID].get_type(f_id);
			return true;
		}

		public static bool try_get_type(Furniture furniture, [MaybeNullWhen(false)] out FurnitureType type)
		{
			return try_get_type(furniture.ItemId, out type);
		}

		#endregion

		#region Asset Requests

		public static bool load_resource(AssetRequestedEventArgs e)
		{
			if (!e.Name.StartsWith("FF/")) return false;

			if (!try_get_cp_from_resource(e.Name, out IContentPack? c_pack))
			{
				ModEntry.log($"Could not find a valid pack to load asset {e.Name} (I don't know how this is possible tbh).", LogLevel.Warn);
				return false;
			}

			string UID = c_pack.Manifest.UniqueID;
			string path = e.Name.Name[(UID.Length + 4)..];	// removing the "FF/{UID}/" marker
			IModContentHelper pc_helper = c_pack.ModContent;	// Pack Content Helper

			if (e.DataType == typeof(JObject))
			{	
				if (!asset_exists<JObject>(pc_helper, path)) return false;
				e.LoadFrom(
					() => {return load_resource<JObject>(pc_helper, path);},
					AssetLoadPriority.Low
				);
			}

			else if (e.DataType == typeof(Texture2D))
			{
				if (!asset_exists<Texture2D>(pc_helper, path)) return false;
				e.LoadFrom(
					() => {return load_resource<Texture2D>(pc_helper, path);},
					AssetLoadPriority.Low
				);
			}

			else return false;	// Neither a content file nor a texture

			if (!loaded_assets.ContainsKey(UID))
				loaded_assets[UID] = new();
			loaded_assets[UID].Add(e.NameWithoutLocale.Name);

			return true;
		}

		private static bool asset_exists<Type>(IModContentHelper pc_helper, string path) where Type: notnull
		{
			if (path.StartsWith("FF/"))
				return ModEntry.get_helper().ModContent.DoesAssetExist<Type>(path[3..]);

			else if (path.StartsWith("Content/"))
			{
				IGameContentHelper gc_helper = ModEntry.get_helper().GameContent;
				IAssetName name = gc_helper.ParseAssetName(Path.ChangeExtension(path[8..], null));
				return gc_helper.DoesAssetExist<Type>(name);
			}

			else return pc_helper.DoesAssetExist<Type>(path);
		}

		private static Type load_resource<Type>(IModContentHelper helper, string path) where Type: notnull
		{
			Type result;

			if (path.StartsWith("FF/"))
			{
				result = ModEntry.get_helper().ModContent.Load<Type>(path[3..]);
				// Load from FF content
			}

			else if (path.StartsWith("Content/"))
			{
				string fixed_path = Path.ChangeExtension(path[8..], null);
				result = ModEntry.get_helper().GameContent.Load<Type>(fixed_path);
				// Load from game content
			}
			
			else result = helper.Load<Type>(path);
			// Load from Pack content

			ModEntry.log($"loaded asset at {path}", LogLevel.Trace);
			return result;
			
		}

		private static bool try_get_cp_from_resource(IAssetName asset_name, [MaybeNullWhen(false)] out IContentPack c_pack)
		{
			c_pack = null;
			int max_key_l = 0;

			// searching content packs for which the UID is the start of the resource name
			// taking only the one with the longer matching UID in case of substring UIDs (bad)
			foreach (string key in UIDs.Keys)
			{
				if (asset_name.StartsWith("FF/" + key) && key.Length > max_key_l)
				{
					c_pack = UIDs[key];
					max_key_l = key.Length;
				}
			}

			return c_pack is not null;
		}

		#region Data/Furniture

		public static void edit_data_furniture(IAssetData asset)
		{
			load_all();

			var editor = asset.AsDictionary<string, string>().Data;

			static_types.Clear();

			foreach (string UID in UIDs.Keys)
				packs[$"{UID}/{DEFAULT_PATH}"].add_data_furniture(editor);
		}

		private void add_data_furniture(IDictionary<string, string> editor)
		{
			foreach (FurnitureType type in types.Values)
			{
				if (!config.is_type_enabled(type.info.id)) continue;

				if (static_types.ContainsKey(type.info.id))
				{
					int prev_prio = packs[static_types[type.info.id]].get_type(type.info.id).info.priority;
					if (type.info.priority <= prev_prio) continue;
				}

				static_types[type.info.id] = data_UID;
				editor[type.info.id] = type.get_string_data();
			}

			foreach (IncludedPack i_pack in included_packs.Values)
			{
				if (!config.is_pack_enabled(i_pack.data_UID)) continue;

				i_pack.pack.add_data_furniture(editor);
			}
		}

		#endregion

		#region Data/Shops

		public static void edit_data_shop(IAssetData asset)
		{
			load_all();

			var editor = asset.AsDictionary<string, ShopData>().Data;

			foreach (string UID in UIDs.Keys)
				packs[$"{UID}/{DEFAULT_PATH}"].add_data_shop(editor);
			
			// All items in the Debug Catalogue are free
			if (!editor.ContainsKey("FF.debug_catalog")) return;    // Just in case
			QuantityModifier price_mod = new() { Id = "FreeCatalogue",
				Modification = QuantityModifier.ModificationType.Set,
				Amount = 0
			};
			editor["FF.debug_catalog"].PriceModifiers = new() { price_mod };
			editor["FF.debug_catalog"].PriceModifierMode = QuantityModifier.QuantityModifierMode.Minimum;
		}

		private void add_data_shop(IDictionary<string, ShopData> editor)
		{
			foreach (FurnitureType type in types.Values)
			{
				if (!config.is_type_enabled(type.info.id)) continue;

				type.add_data_shop(editor);
			}

			foreach (IncludedPack i_pack in included_packs.Values)
			{
				if (!config.is_pack_enabled(i_pack.data_UID)) continue;

				i_pack.pack.add_data_shop(editor);
			}
		}

		#endregion

		#endregion

		#region Debug Print

		public static void debug_print(string _, string[] args)
		{
			if (args.Count() == 0) debut_print_all();
			else debug_print_single(args[0]);
		}

		private static void debut_print_all()
		{
			foreach (string UID in UIDs.Keys)
				debug_print_single(UID);
		}

		private static void debug_print_single(string UID)
		{
			string data_UID = $"{UID}/{DEFAULT_PATH}";

			if (!packs.ContainsKey(data_UID))
			{
				ModEntry.log($"Pack {UID} does not exist!", LogLevel.Warn);
				return;
			}

			packs[data_UID].debug_print(0);
		}

		private void debug_print(int indent_count, bool enabled = true)
		{
			string indent = new('\t', indent_count);

			if (is_included)
			{
				string text = $"{indent}{data_UID}";
				if (!enabled)
				{
					ModEntry.log(text + " (disabled)", LogLevel.Debug);
					return;
				}
				ModEntry.log(text + ':', LogLevel.Debug);
			}
			else ModEntry.log($"{indent}{UID}:", LogLevel.Debug);
			
			ModEntry.log($"{indent}\tFurniture:", LogLevel.Debug);
			foreach (FurnitureType type in types.Values)
				type.debug_print(indent_count+2, config.is_type_enabled(type.info.id));
			
			ModEntry.log($"{indent}\tIncluded Packs:", LogLevel.Debug);
			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.pack.debug_print(indent_count+2, config.is_pack_enabled(i_pack.data_UID));
		}

		#endregion
	}
}