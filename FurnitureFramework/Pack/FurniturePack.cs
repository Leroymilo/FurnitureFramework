using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using GenericModConfigMenu;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
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

		static HashSet<string> to_load = new();
		// HashSet of data_UIDs of packs to load.
		static Dictionary<string, FurniturePack> packs = new();
		// maps data_UID (UID/json_path) to pack.
		static Dictionary<string, HashSet<string>> data_UIDs = new();
		// maps UID to data_UIDs of base and included packs of this UID.
		static Dictionary<string, string> static_types = new();
		// maps Furniture ID to the data_UID it's from.
		static Dictionary<string, HashSet<string>> conflicts = new();
		// maps type_id to HashSet of all data_UID trying to patch it.

		// Pack Properties

		IContentPack content_pack;
		string UID;
		string path;
		string data_UID { get => $"{UID}/{path}"; }
		Dictionary<string, Type.FurnitureType> types = new();
		Dictionary<string, IncludedPack> included_packs = new();
		bool is_included = false;


		#region Getters

		public static bool try_get_type_from_data_file(string data_file_name, [MaybeNullWhen(false)] out FurniturePack pack)
		{
			pack = null;

			foreach (FurniturePack f_pack in packs.Values)
			{
				// f_pack.
			}

			return false;
		}

		public static bool try_get_pack_from_resource(string resource_name, [MaybeNullWhen(false)] out FurniturePack pack)
		{
			pack = null;
			int max_key_l = 0;

			// searching packs for which the UID is the start of the resource name
			// taking only the one with the longer matching UID in case of substring UIDs (bad)
			foreach (string key in packs.Keys)
			{
				if (resource_name.StartsWith(key) && key.Length > max_key_l)
				{
					pack = packs[key];
					max_key_l = key.Length;
				}
			}

			return pack is not null;
		}

		private bool try_get_type_pack(string f_id, [MaybeNullWhen(false)] ref Type.FurnitureType? type)
		{
			bool found = false;

			// prioritize included files to overload definition
			foreach (IncludedPack sub_pack in included_packs)
			{
				if (!sub_pack.enabled) continue;
				found |= sub_pack.pack.try_get_type_pack(f_id, ref type);
				if (found) break;
			}

			if (!found)
			{
				found |= types.TryGetValue(f_id, out type);
			}

			return found;
		}

		public static bool try_get_type(string f_id, [MaybeNullWhen(false)] out Type.FurnitureType type)
		{
			type = null;

			if (!static_types.TryGetValue(f_id, out string? UID))
				return false;

			return packs[UID].try_get_type_pack(f_id, ref type);
		}

		public static bool try_get_type(Furniture furniture, [MaybeNullWhen(false)] out Type.FurnitureType type)
		{
			return try_get_type(furniture.ItemId, out type);
		}

		#endregion

		#region Asset Requests

		private void add_data_furniture(IDictionary<string, string> editor)
		{
			foreach ((string id, Type.FurnitureType f) in types)
			{
				editor[id] = f.get_string_data();
			}

			foreach (IncludedPack sub_pack in included_packs)
			{
				if (sub_pack.enabled) sub_pack.pack.add_data_furniture(editor);
			}
		}

		public static void edit_data_furniture(IAssetData asset)
		{
			var editor = asset.AsDictionary<string, string>().Data;

			foreach (FurniturePack pack in packs.Values)
				pack.add_data_furniture(editor);
		}

		private static bool has_shop_item(ShopData shop_data, string f_id)
		{
			foreach (ShopItemData shop_item_data in shop_data.Items)
			{
				if (shop_item_data.ItemId == $"(F){f_id}")
					return true;
			}
			return false;
		}

		private void add_data_shop(IDictionary<string, ShopData> editor)
		{
			foreach ((string shop_id, List<string> f_ids) in shops)
			{
				if (!editor.ContainsKey(shop_id))
				{
					ShopData catalogue_shop_data = new()
					{
						CustomFields = new Dictionary<string, string>() {
							{"HappyHomeDesigner/Catalogue", "true"}
						},
						Owners = new List<ShopOwnerData>() { 
							new() { Name = "AnyOrNone" }
						}
					};
					editor[shop_id] = catalogue_shop_data;
				}

				foreach (string f_id in f_ids)
				{
					if (!has_shop_item(editor[shop_id], f_id))
					{
						ShopItemData shop_item_data = new()
						{
							Id = f_id,
							ItemId = $"(F){f_id}",
							// Price = types[f_id].price
						};

						editor[shop_id].Items.Add(shop_item_data);
					}
				}
			}

			foreach (IncludedPack sub_pack in included_packs)
			{
				if (sub_pack.enabled) sub_pack.pack.add_data_shop(editor);
			}
		}

		public static void edit_data_shop(IAssetData asset)
		{
			var editor = asset.AsDictionary<string, ShopData>().Data;

			foreach (FurniturePack pack in packs.Values)
				pack.add_data_shop(editor);
		}

		#endregion
	
		#region Config

		private bool has_config()
		{
			return included_packs.Count > 0;
		}

		private void reset_config()
		{
			foreach (IncludedPack sub_pack in included_packs)
			{
				sub_pack.enabled = sub_pack.default_enabled;
				sub_pack.pack.reset_config();
			}
		}

		private void read_config(JObject config_data, string? prefix = null)
		{
			prefix ??= "";

			foreach (IncludedPack sub_pack in included_packs)
			{
				sub_pack.read_config(config_data, prefix);
			}
		}

		private void save_config()
		{
			JObject data = new();

			save_config(data, "");

			string path = Path.Combine(content_pack.DirectoryPath, CONFIG_PATH);
			File.WriteAllText(path, data.ToString());
		}

		private void save_config(JObject data, string prefix)
		{
			foreach (IncludedPack sub_pack in included_packs)
			{
				sub_pack.save_config(data, prefix);
			}
		}

		private void register_pack_config(IGenericModConfigMenuApi config_menu)
		{
			if (!has_config()) return;

			config_menu.Register(
				mod: content_pack.Manifest,
				reset: reset_config,
				save: () => save_config()
			);

			add_config(config_menu);
		}

		private void add_config(IGenericModConfigMenuApi config_menu)
		{
			if (!has_config()) return;

			foreach (IncludedPack sub_pack in included_packs)
			{
				sub_pack.add_config(config_menu);
			}

			foreach (IncludedPack sub_pack in included_packs)
			{
				sub_pack.add_config_page(config_menu);
			}
		}

		public static void register_config(IGenericModConfigMenuApi config_menu)
		{
			foreach (FurniturePack pack in packs.Values)
			{
				pack.register_pack_config(config_menu);
			}
		}

		private static void update_after_config_change()
		{
			IGameContentHelper helper = ModEntry.get_helper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
		}

		#endregion

		public static void debug_print(string command, string[] args)
		{
			if (args.Count() == 0)
			{
				ModEntry.log("No ModID given.", LogLevel.Warn);
				return;
			}
			string UID = args[0];
		}
	}
}