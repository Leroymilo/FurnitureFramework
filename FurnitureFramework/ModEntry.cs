using System.Reflection.Metadata.Ecma335;
using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Objects;


namespace FurnitureFramework
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
		private static IMonitor? monitor;
		private static IModHelper? helper;


		public static bool print_debug = false;


		public static readonly Dictionary<string, FurnitureType> furniture = new();
		public static readonly Dictionary<string, List<string>> shops = new();

		static public IModHelper get_helper()
		{
			if (helper == null) throw new NullReferenceException("Helper was not set.");
			return helper;
		}

		static public void log(string message, LogLevel log_level = LogLevel.Debug)
		{
			if (monitor == null) throw new NullReferenceException("Monitor was not set.");
			monitor.Log(message, log_level);
		}

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
			// Harmony.DEBUG = true;

			monitor = Monitor;
			ModEntry.helper = Helper;
			HarmonyPatcher.harmony = new(ModManifest.UniqueID);
            helper.Events.Input.ButtonPressed += on_button_pressed;
			helper.Events.GameLoop.GameLaunched += on_game_launched;
			helper.Events.Content.AssetRequested += on_asset_requested;
			
			HarmonyPatcher.patch();
        }

        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_game_launched(object? sender, GameLaunchedEventArgs e)
		{
			foreach (IContentPack pack in Helper.ContentPacks.GetOwned())
			{
				string UID = pack.Manifest.UniqueID;

				log($"Reading Furniture Types of {UID}...");
				JObject data;
				try
				{
					data = pack.ModContent.Load<JObject>("content.json");
				}
				catch (ContentLoadException ex)
				{
					log($"Could not load content.json for {UID}:\n{ex}", LogLevel.Error);
					continue;
				}

				int? format;
				try
				{
					format = data.Value<int?>("Format");
					if (format == null) throw new NullReferenceException("Value is null or missing");
				}
				catch (Exception ex)
				{
					log("No valid format given (should be a number), trying to read as latest format.", LogLevel.Warn);
					log($"{ex}", LogLevel.Trace);
					format = 1;
				}
				// for backwards compatibility


				JToken? furniture_token = data.GetValue("Furniture");
				if (furniture_token == null)
				{
					log("Missing \"Furniture\" field in content.json, skipping Content Pack.", LogLevel.Error);
					continue;
				}
				foreach((string key, JToken? f_data) in (JObject)furniture_token)
				{
					FurnitureType new_furniture;
					if (f_data == null)
					{
						log($"No data for Furniture \"{key}\", skipping entry.", LogLevel.Warn);
						continue;
					}
					try
					{
						new_furniture = new(pack, key, (JObject)f_data);
						furniture[new_furniture.id] = new_furniture;
					}
					catch (Exception ex)
					{
						log(ex.ToString(), LogLevel.Error);
						log($"Failed to load data for Furniture \"{key}\", skipping entry.", LogLevel.Warn);
						continue;
					}

					if (new_furniture.shop_id != null)
					{
						if (!shops.ContainsKey(new_furniture.shop_id))
						{
							shops[new_furniture.shop_id] = new();
						}
					}

					foreach (string shop_id in new_furniture.shops)
					{
						if (!shops.ContainsKey(shop_id))
						{
							shops[shop_id] = new();
						}

						shops[shop_id].Add(new_furniture.id);
					}
				}
			}

			log("Finished loading Furniture Types.");
			Helper.GameContent.InvalidateCache("Data/Furniture");
		}


        /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void on_button_pressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
			
			if (e.Button == SButton.K)
			{
				print_debug = !print_debug;
				log($"=== Debug Print {(print_debug ? "On": "Off")} ===", LogLevel.Info);
			}
        }


        /// <inheritdoc cref="IContentEvents.AssetRequested"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_asset_requested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.StartsWith("Data/Furniture"))
			{
				e.Edit(asset => {
					var editor = asset.AsDictionary<string, string>().Data;
					foreach ((string id, FurnitureType f) in furniture)
					{
						editor[id] = f.get_string_data();
					}
				});
			}

			if (e.NameWithoutLocale.StartsWith("Data/Shops"))
			{
				e.Edit(asset => {
					var editor = asset.AsDictionary<string, ShopData>().Data;
					foreach ((string shop_id, List<string> f_ids) in shops)
					{
						log($"Adding shop: {shop_id}.");
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

						log($"Has {f_ids.Count} items.");
						foreach (string f_id in f_ids)
						{
							if (!has_shop_item(editor[shop_id], f_id))
							{
								log($"Adding shop item: {f_id}");
								ShopItemData shop_item_data = new()
								{
									Id = f_id,
									ItemId = $"(F){f_id}",
									Price = furniture[f_id].price
								};

								editor[shop_id].Items.Add(shop_item_data);
							}
						}
					}
				});
			}

			if (furniture.ContainsKey(e.Name.Name))
			{
				e.LoadFrom(furniture[e.Name.Name].get_icon_texture, AssetLoadPriority.Medium);
			}
		}

		private bool has_shop_item(ShopData shop_data, string f_id)
		{
			foreach (ShopItemData shop_item_data in shop_data.Items)
			{
				if (shop_item_data.ItemId == $"(F){f_id}")
					return true;
			}
			return false;
		}

    }
}