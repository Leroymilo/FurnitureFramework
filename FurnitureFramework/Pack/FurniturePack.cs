using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using FurnitureFramework.Type;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
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

		// Pack Properties

		readonly IContentPack content_pack;
		string path = DEFAULT_PATH;
		string UID { get => content_pack.Manifest.UniqueID; }
		string data_UID { get => $"{UID}/{path}"; }
		FurniturePack? root = null;
		bool is_included { get => root != null; }
		bool is_loaded = false;
		Dictionary<string, Type.FurnitureType> types = new();
		Dictionary<string, IncludedPack> included_packs = new();

		private static void invalidate_game_data()
		{
			IGameContentHelper helper = ModEntry.get_helper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
		}

		#region Getters

		private static bool try_get_cp_from_resource(string resource_name, [MaybeNullWhen(false)] out IContentPack c_pack)
		{
			c_pack = null;
			int max_key_l = 0;

			// searching content packs for which the UID is the start of the resource name
			// taking only the one with the longer matching UID in case of substring UIDs (bad)
			foreach (string key in UIDs.Keys)
			{
				if (resource_name.StartsWith(key) && key.Length > max_key_l)
				{
					c_pack = UIDs[key];
					max_key_l = key.Length;
				}
			}

			return c_pack is not null;
		}

		public static bool load_resource(AssetRequestedEventArgs e)
		{
			string name = e.NameWithoutLocale.Name;

			// Loading texture for menu icon
			if (try_get_type(name, out Type.FurnitureType? type))
			{
				e.LoadFrom(type.get_texture, AssetLoadPriority.Medium);
				return true;
			}

			if (!name.StartsWith("FF/")) return false;
			name = name[3..];	// removing the "FF/" marker

			if (!try_get_cp_from_resource(name, out IContentPack? c_pack))
			{
				ModEntry.log($"Could not find a valid pack to load asset {name}", LogLevel.Warn);
				return false;
			}

			name = name[(c_pack.Manifest.UniqueID.Length+1)..];	// removing the "{UID}/" marker

			if (e.DataType == typeof(JObject))
			{
				e.LoadFrom(
					() => {return c_pack.ModContent.Load<JObject>(name);},
					AssetLoadPriority.Low
				);
			}

			if (e.DataType == typeof(Texture2D))
			{
				e.LoadFrom(
					() => {return base_load(c_pack.ModContent, name);},
					AssetLoadPriority.Low
				);
			}

			return true;
		}

		private static Texture2D base_load(IModContentHelper pack_helper, string path)
		{
			Texture2D result;

			if (path.StartsWith("FF/"))
			{
				result = ModEntry.get_helper().ModContent.Load<Texture2D>(path[3..]);
				// Load from FF content
			}

			else if (path.StartsWith("Content/"))
			{
				string fixed_path = Path.ChangeExtension(path[8..], null);
				result = ModEntry.get_helper().GameContent.Load<Texture2D>(fixed_path);
				// Load from game content
			}
			
			else result = pack_helper.Load<Texture2D>(path);
			// Load from Pack content

			ModEntry.log($"loaded texture at {path}", LogLevel.Trace);
			return result;
		}

		private Type.FurnitureType get_type(string f_id)
		{
			return types[f_id];
		}

		private static bool try_get_type(string f_id, [MaybeNullWhen(false)] out Type.FurnitureType type)
		{
			ModEntry.get_helper().GameContent.Load<JObject>("Data/Furniture");
			// ensure that Furniture are loaded

			type = null;

			if (!static_types.TryGetValue(f_id, out string? UID))
				return false;

			type = packs[UID].get_type(f_id);
			return true;
		}

		public static bool try_get_type(Furniture furniture, [MaybeNullWhen(false)] out Type.FurnitureType type)
		{
			return try_get_type(furniture.ItemId, out type);
		}

		#endregion

		#region Asset Requests

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

		public static void edit_data_shop(IAssetData asset)
		{
			ModEntry.get_helper().GameContent.Load<JObject>("Data/Furniture");
			// ensure that Furniture are loaded

			var editor = asset.AsDictionary<string, ShopData>().Data;

			foreach (FurniturePack pack in packs.Values)
				pack.add_data_shop(editor);
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

		#region Debug Print

		public static void debug_print(string _, string[] args)
		{
			if (args.Count() == 0) reload_all();
			else reload_single(args[0]);
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

			string text;
			if (is_included)
			{
				text = $"{indent}{data_UID}";
				if (!enabled) text += " (disabled):";
				else text += ":";
			}
			else text = $"{indent}{UID}:";
			ModEntry.log(text, LogLevel.Debug);
			
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