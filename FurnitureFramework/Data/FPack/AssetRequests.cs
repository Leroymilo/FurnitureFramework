using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using StardewValley.Objects;

namespace FurnitureFramework.Data.FPack
{
	partial class FPack
	{
		#region Getters

		private FType.FType GetType(string f_id)
		{
			return Furniture[f_id];
		}

		private static bool TryGetType(string f_id, [MaybeNullWhen(false)] out FType.FType type)
		{
			ModEntry.GetHelper().GameContent.Load<Dictionary<string, string>>("Data/Furniture");

			type = null;

			if (!TypesOrigin.TryGetValue(f_id, out string? UID))
				return false;

			type = PacksData[UID].GetType(f_id);
			return true;
		}

		public static bool TryGetType(Furniture furniture, [MaybeNullWhen(false)] out FType.FType type)
		{
			return TryGetType(furniture.ItemId, out type);
		}

		public static bool TryGetType(ShopMenu shop_menu, [MaybeNullWhen(false)] out FType.FType type)
		{
			type = null;
			if (!shop_menu.ShopId.StartsWith("FF/")) return false;
			return TryGetType(shop_menu.ShopId[3..], out type);
		}

		#endregion

		#region Asset Requests

		public static bool LoadResource(AssetRequestedEventArgs e)
		{
			if (!e.Name.StartsWith("FF/")) return false;

			if (!TryGetCPFromResource(e.Name, out IContentPack? c_pack))
			{
				ModEntry.Log($"Could not find a valid pack to load asset {e.Name} (I don't know how this is possible tbh).", LogLevel.Warn);
				return false;
			}

			string UID = c_pack.Manifest.UniqueID;
			string path = e.Name.Name[(UID.Length + 4)..];	// removing the "FF/{UID}/" marker
			IModContentHelper pc_helper = c_pack.ModContent;	// Pack Content Helper

			if (e.DataType == typeof(FPack))
			{	
				if (!AssetExists<FPack>(pc_helper, path)) return false;
				e.LoadFrom(
					() => {return LoadResource<FPack>(pc_helper, path);},
					AssetLoadPriority.Low
				);
			}

			else if (e.DataType == typeof(Texture2D))
			{
				if (!AssetExists<Texture2D>(pc_helper, path)) return false;
				e.LoadFrom(
					() => {return LoadResource<Texture2D>(pc_helper, path);},
					AssetLoadPriority.Low
				);
			}

			else return false;	// Neither a content file nor a texture

			if (!LoadedAssets.ContainsKey(UID))
				LoadedAssets[UID] = new();
			LoadedAssets[UID].Add(e.NameWithoutLocale.Name);

			return true;
		}

		private static bool AssetExists<Type>(IModContentHelper pc_helper, string path) where Type: notnull
		{
			if (path.StartsWith("FF/"))
				return ModEntry.GetHelper().ModContent.DoesAssetExist<Type>(path[3..]);

			else if (path.StartsWith("Content/"))
			{
				IGameContentHelper gc_helper = ModEntry.GetHelper().GameContent;
				IAssetName name = gc_helper.ParseAssetName(System.IO.Path.ChangeExtension(path[8..], null));
				return gc_helper.DoesAssetExist<Type>(name);
			}

			else return pc_helper.DoesAssetExist<Type>(path);
		}

		private static Type LoadResource<Type>(IModContentHelper helper, string path) where Type: notnull
		{
			Type result;

			if (path.StartsWith("FF/"))
			{
				result = ModEntry.GetHelper().ModContent.Load<Type>(path[3..]);
				// Load from FF content
			}

			else if (path.StartsWith("Content/"))
			{
				string fixed_path = System.IO.Path.ChangeExtension(path[8..], null);
				result = ModEntry.GetHelper().GameContent.Load<Type>(fixed_path);
				// Load from game content
			}
			
			else result = helper.Load<Type>(path);
			// Load from Pack content

			return result;
		}

		private static bool TryGetCPFromResource(IAssetName asset_name, [MaybeNullWhen(false)] out IContentPack c_pack)
		{
			c_pack = null;
			int max_key_l = 0;

			// searching content packs for which the UID is the start of the resource name
			// taking only the one with the longer matching UID in case of substring UIDs (bad)
			foreach (string key in ContentPacks.Keys)
			{
				if (asset_name.StartsWith("FF/" + key) && key.Length > max_key_l)
				{
					c_pack = ContentPacks[key];
					max_key_l = key.Length;
				}
			}

			return c_pack is not null;
		}

		#region Invalidation

		public static void InvalidateGameData()
		{
			IGameContentHelper helper = ModEntry.GetHelper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
		}

		public static bool InvalidateAsset(IAssetName name)
		{
			// Check if it's a FF asset
			if (!name.StartsWith("FF")) return false;
			// Check if it's a content file
			if (!PacksData.TryGetValue(name.Name[3..], out FPack? f_pack)) return false;
			
			if (!ToLoad.Add(f_pack.LoadData_)) return false;
			ModEntry.Log($"Queued {f_pack.DataUID} for reload.", LogLevel.Trace);
			ModEntry.GetHelper().GameContent.InvalidateCache(
				asset_info => f_pack.IncludedPacks.ContainsKey(asset_info.Name.Name[3..])
			);

			return true;
		}

		#endregion

		#region Data/Furniture

		public static void EditFurnitureData(IAssetData asset)
		{
			LoadAll();

			var editor = asset.AsDictionary<string, string>().Data;

			TypesOrigin.Clear();

			foreach (string UID in ContentPacks.Keys)
				if (PacksData.TryGetValue($"{UID}/{DEFAULT_PATH}", out FPack? f_pack))
					f_pack.AddFurnitureData(editor);
		}

		private void AddFurnitureData(IDictionary<string, string> editor)
		{
			foreach (FType.FType type in Furniture.Values)
			{
				foreach (KeyValuePair<string, string> pair in type.GetStringData())
				{
					if (!Config.IsTypeEnabled(pair.Key)) continue;

					if (TypesOrigin.ContainsKey(pair.Key))
					{
						int prev_prio = PacksData[TypesOrigin[pair.Key]].GetType(pair.Key).Priority;
						if (type.Priority <= prev_prio) continue;
					}

					TypesOrigin[pair.Key] = DataUID;
					editor[pair.Key] = pair.Value;
				}
			}

			foreach (FPack sub_pack in IncludedPacks.Values)
			{
				if (!Config.IsPackEnabled(sub_pack.DataUID)) continue;
				sub_pack.AddFurnitureData(editor);
			}
		}

		#endregion

		#region Data/Shops

		public static void EditShopData(IAssetData asset)
		{
			LoadAll();
			AddedCatalogues.Clear();

			var editor = asset.AsDictionary<string, ShopData>().Data;

			foreach (string UID in ContentPacks.Keys)
				if (PacksData.TryGetValue($"{UID}/{DEFAULT_PATH}", out FPack? f_pack))
					f_pack.AddShopData(editor);
			
			// Reloads shop extension data
			ModEntry.GetHelper().GameContent.InvalidateCache("spacechase0.SpaceCore/ShopExtensionData");
		}

		void AddShopData(IDictionary<string, ShopData> editor)
		{
			foreach (FType.FType type in Furniture.Values)
			{
				if (type.ShopId != null) AddShop(editor, type.ShopId);

				foreach (KeyValuePair<string, List<ShopItemData>> shop_items in type.GetShopItemData())
				{
					string s_id = shop_items.Key;
					AddShop(editor, s_id);
					foreach (ShopItemData shop_item in shop_items.Value)
					{
						string f_id = shop_item.Id;
						if (!Config.IsTypeEnabled(f_id)) continue;
						if (HasShopItem(editor[s_id], f_id)) continue;
						editor[s_id].Items.Add(shop_item);
					}
				}
			}

			foreach (FPack sub_pack in IncludedPacks.Values)
			{
				if (!Config.IsPackEnabled(sub_pack.DataUID)) continue;
				sub_pack.AddShopData(editor);
			}
		}

		static void AddShop(IDictionary<string, ShopData> editor, string s_id)
		{
			if (editor.ContainsKey(s_id)) return;

			// To remove portrait spot and default dialogue
			ShopOwnerData shop_owner = new() {
				Name = "AnyOrNone",
				Dialogues = new() {}
			};

			// To make a proper catalogue: everything is free
			QuantityModifier price_mod = new() {
				Id = "FreeCatalogue",
				Modification = QuantityModifier.ModificationType.Set,
				Amount = 0
			};

			ShopData catalogue_shop_data = new()
			{
				Owners = new List<ShopOwnerData>() { shop_owner },
				PriceModifiers = new() { price_mod },
				PriceModifierMode = QuantityModifier.QuantityModifierMode.Minimum,
				CustomFields = new() {
					{"HappyHomeDesigner/Catalogue", "true"}
				}
			};
			editor[s_id] = catalogue_shop_data;
			AddedCatalogues.Add(s_id);
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
	}
}