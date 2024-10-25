using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
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

		static Stack<string> to_load = new();
		// Stack of data_UIDs of packs to load. It's a Stack to ensure that included packs are loaded along their root.
		static HashSet<string> UIDs = new();
		// UIDs of all Furniture Packs
		static Dictionary<string, FurniturePack> packs = new();
		// maps data_UID to pack.
		static Dictionary<string, MaxDict<HashSet<string>>> static_types = new();
		// maps type_id to a (custom) Dictionary mapping priorities
		// to the set of data_UIDs of Packs implementing this type at this priority.
		// `static_types[type_id].Max().Value.First();` to get the data_UID for a given type_id.
		// sorry
		static bool update_game_data = false;
		// if it's necessary to refresh game data,
		// must be set to true after any operation that will change Data/Furniture or Data/Shops
		// must be set to false after invalidating game data

		// Pack Properties

		readonly IContentPack content_pack;
		string path = DEFAULT_PATH;
		string UID { get => content_pack.Manifest.UniqueID; }
		string data_UID { get => $"{UID}/{path}"; }
		FurniturePack? root = null;
		bool is_included { get => root != null; }
		bool update_config = false;
		Dictionary<string, Type.FurnitureType> types = new();
		Dictionary<string, IncludedPack> included_packs = new();

		private static void invalidate_game_data()
		{
			if (!update_game_data) return;

			IGameContentHelper helper = ModEntry.get_helper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
			update_game_data = false;
		}


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