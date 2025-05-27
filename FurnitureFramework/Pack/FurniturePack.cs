using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Shops;
using StardewValley.Objects;

namespace FurnitureFramework.Pack
{

	partial class FurniturePack
	{
		// Constants

		const int FORMAT = 3;
		const string DEFAULT_PATH = "content.json";
		const string CONFIG_PATH = "config.json";

		// Static Collections 

		static readonly Dictionary<string, IContentPack> UIDs = new();
		// UIDs of all Furniture Packs (for reload all).
		static readonly Dictionary<string, FurniturePack> packs = new();
		// maps data_UID to pack.
		static readonly Dictionary<string, string> static_types = new();
		// maps type_id to the data_UID of the pack where it's defined.
		private static Dictionary<string, HashSet<string>> loaded_assets = new();
		// a set of what asset names were loaded by each pack UID.

		static IContentPack default_pack;

		// Pack Properties

		readonly IContentPack content_pack;
		string path = DEFAULT_PATH;
		string UID { get => content_pack.Manifest.UniqueID; }
		string data_UID { get => $"{UID}/{path}"; }
		Data.FurniturePack data;
		FurniturePack? root = null;
		bool is_included { get => root != null; }
		bool is_loaded = false;
		Dictionary<string, Data.FType.FType> types = new();
		Dictionary<string, IncludedPack> included_packs = new();

		public static void invalidate_game_data()
		{
			IGameContentHelper helper = ModEntry.get_helper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
		}

		#region Getters

		private Data.FType.FType get_type(string f_id)
		{
			return types[f_id];
		}

		private static bool try_get_type(string f_id, [MaybeNullWhen(false)] out Data.FType.FType type)
		{
			ModEntry.get_helper().GameContent.Load<Dictionary<string, string>>("Data/Furniture");

			type = null;

			if (!static_types.TryGetValue(f_id, out string? UID))
				return false;

			type = packs[UID].get_type(f_id);
			return true;
		}

		public static bool try_get_type(Furniture furniture, [MaybeNullWhen(false)] out Data.FType.FType type)
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

			if (e.DataType == typeof(Data.FurniturePack))
			{	
				if (!asset_exists<Data.FurniturePack>(pc_helper, path)) return false;
				e.LoadFrom(
					() => {return load_resource<Data.FurniturePack>(pc_helper, path);},
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

		public static void EditFurnitureData(IAssetData asset)
		{
			load_all();

			var editor = asset.AsDictionary<string, string>().Data;

			static_types.Clear();

			foreach (string UID in UIDs.Keys)
				packs[$"{UID}/{DEFAULT_PATH}"].AddFurnitureData(editor);
		}

		private void AddFurnitureData(IDictionary<string, string> editor)
		{
			foreach (Data.FType.FType type in types.Values)
			{
				foreach (KeyValuePair<string, string> pair in type.GetStringData())
				{
					if (!config.is_type_enabled(pair.Key)) continue;

					if (static_types.ContainsKey(pair.Key))
					{
						int prev_prio = packs[static_types[pair.Key]].get_type(pair.Key).Priority;
						if (type.Priority <= prev_prio) continue;
					}

					static_types[pair.Key] = data_UID;
					editor[pair.Key] = pair.Value;
				}
			}

			foreach (IncludedPack i_pack in included_packs.Values)
			{
				if (!config.is_pack_enabled(i_pack.data_UID)) continue;

				i_pack.pack.AddFurnitureData(editor);
			}
		}

		#endregion

		#region Data/Shops

		public static void EditShopData(IAssetData asset)
		{
			load_all();

			var editor = asset.AsDictionary<string, ShopData>().Data;

			foreach (string UID in UIDs.Keys)
				packs[$"{UID}/{DEFAULT_PATH}"].AddShopData(editor);
			
			// All items in the Debug Catalogue are free
			if (!editor.ContainsKey("leroymilo.FF.debug_catalog")) return;    // Just in case
			QuantityModifier price_mod = new() {
				Id = "FreeCatalogue",
				Modification = QuantityModifier.ModificationType.Set,
				Amount = 0
			};
			editor["leroymilo.FF.debug_catalog"].PriceModifiers = new() { price_mod };
			editor["leroymilo.FF.debug_catalog"].PriceModifierMode = QuantityModifier.QuantityModifierMode.Minimum;
		}

		void AddShopData(IDictionary<string, ShopData> editor)
		{
			foreach (Data.FType.FType type in types.Values)
			{
				if (type.ShopId != null) AddShop(editor, type.ShopId);

				foreach (KeyValuePair<string, List<ShopItemData>> shop_items in type.GetShopItemData())
				{
					string s_id = shop_items.Key;
					AddShop(editor, s_id);
					foreach (ShopItemData shop_item in shop_items.Value)
					{
						string f_id = shop_item.Id;
						if (!config.is_type_enabled(f_id)) continue;
						if (HasShopItem(editor[s_id], f_id)) continue;
						editor[s_id].Items.Add(shop_item);
					}
				}
			}

			foreach (IncludedPack i_pack in included_packs.Values)
			{
				if (!config.is_pack_enabled(i_pack.data_UID)) continue;
				i_pack.pack.AddShopData(editor);
			}
		}

		static void AddShop(IDictionary<string, ShopData> editor, string s_id)
		{
			if (editor.ContainsKey(s_id)) return;

			ShopData catalogue_shop_data = new()
			{
				CustomFields = new Dictionary<string, string>() {
					{"HappyHomeDesigner/Catalogue", "true"}
				},
				Owners = new List<ShopOwnerData>() { 
					new() {
						Name = "AnyOrNone",
						Dialogues = new() {}	// To remove default dialogue
					}
				}
			};
			editor[s_id] = catalogue_shop_data;
		}

		static bool HasShopItem(ShopData shop_data, string f_id)
		{
			foreach (ShopItemData shop_item_data in shop_data.Items)
			{
				if (shop_item_data.ItemId == $"(F){f_id}")
					return true;
			}
			return false;
		}

		#endregion

		#endregion

		#region Debug Print

		public static void debug_print(string _, string[] args)
		{
			if (args.Length == 0) debug_print_all();
			else debug_print_single(args[0]);
		}

		private static void debug_print_all()
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
			foreach (Data.FType.FType type in data.Furniture.Values)
				ModEntry.log($"{type.FID}:\n{JObject.FromObject(type)}");

			if (included_packs.Count == 0) return;
			ModEntry.log($"{indent}\tIncluded Packs:", LogLevel.Debug);
			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.pack.debug_print(indent_count+2, config.is_pack_enabled(i_pack.data_UID));
		}

		#endregion
	}
}