﻿using GenericModConfigMenu;
using Microsoft.Xna.Framework;
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
		private static ModConfig config;

		private static string last_season = "spring";


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
			config = helper.ReadConfig<ModConfig>();
			HarmonyPatcher.harmony = new(ModManifest.UniqueID);
            helper.Events.Input.ButtonPressed += on_button_pressed;
			helper.Events.GameLoop.GameLaunched += on_game_launched;
			helper.Events.Content.AssetRequested += on_asset_requested;
			helper.Events.GameLoop.DayStarted += on_day_started;
			helper.Events.World.FurnitureListChanged += on_furniture_list_changed;
			
			HarmonyPatcher.patch();
        }

		#region On Game Launched

        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_game_launched(object? sender, GameLaunchedEventArgs e)
		{
			parse_furniture_packs();
			register_config();
			register_commands();

			if (
				Helper.ModRegistry.IsLoaded("PeacefulEnd.AlternativeTextures")
				&& !config.disable_AT_warning
			)
			{
				log("Furniture made with the Furniture Framework mod are not compatible with Alternative Textures.", LogLevel.Warn);
				log("You can disable this message in the config of the Furniture Framework.", LogLevel.Warn);
			}
		}

		private void register_config()
		{
			// get Generic Mod Config Menu's API (if it's installed)
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (configMenu is null)
				return;

			// register mod
			configMenu.Register(
				mod: ModManifest,
				reset: () => config = new ModConfig(),
				save: () => Helper.WriteConfig(config)
			);

			configMenu.AddKeybind(
				mod: ModManifest,
				name: () => "Slot Place Keybind",
				tooltip: () => "The key to press to place an item in a slot.",
				getValue: () => config.slot_place_key,
				setValue: value => config.slot_place_key = value
			);

			configMenu.AddKeybind(
				mod: ModManifest,
				name: () => "Slot Take Keybind",
				tooltip: () => "The key to press to take an item from a slot.",
				getValue: () => config.slot_take_key,
				setValue: value => config.slot_take_key = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Disable AT Warning",
				tooltip: () => "Check this to disable the warning about Alternative Textures.",
				getValue: () => config.disable_AT_warning,
				setValue: value => config.disable_AT_warning = value
			);


		}

		#region Commands

		private void reload_pack(string command, string[] args)
		{
			string UID = args[0];
			bool found_pack = false;

			foreach (IContentPack pack in Helper.ContentPacks.GetOwned())
			{
				if (pack.Manifest.UniqueID == UID)
				{
					load_pack(pack, true);
					found_pack = true;
					break;
				}
			}

			if (found_pack)
			{
				Helper.GameContent.InvalidateCache("Data/Furniture");
				Helper.GameContent.InvalidateCache("Data/Shops");
				log($"Finished reloading {UID}.");
			}
			else
				log($"Could not find Furniture Pack {UID}.", LogLevel.Warn);
		}

		private void register_commands()
		{
			Helper.ConsoleCommands.Add(
				"reload_furniture_pack",
				"Reloads a Furniture Pack.\n\nUsage: reload_furniture_pack <ModID>\n- ModID: the UniqueID of the Furniture Pack to reload.",
				reload_pack
			);
		}

		#endregion

		#region Furniture Pack Parsing

		private bool check_format(int format)
		{
			switch (format)
			{
				case 1: return true;
				default:
					log($"Invalid format {format}, skipping Furniture Pack.", LogLevel.Error);
					return false;
			}
		}

		private void load_pack(IContentPack pack, bool replace = false)
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
				return;
			}

			JToken? format_token = data.GetValue("Format");
			if (format_token is null || format_token.Type != JTokenType.Integer)
			{
				log("Invalid or missing Format, skipping Furniture Pack.", LogLevel.Error);
				return;
			}
			
			int format = (int)format_token;
			if(!check_format(format)) return;

			JToken? furniture_token = data.GetValue("Furniture");
			if (furniture_token == null)
			{
				log("Missing \"Furniture\" field in content.json, skipping Furniture Pack.", LogLevel.Error);
				return;
			}
			foreach((string key, JToken? f_data) in (JObject)furniture_token)
			{
				List<FurnitureType> new_furniture = new();
				if (f_data is not JObject f_obj)
				{
					log($"No data for Furniture \"{key}\", skipping entry.", LogLevel.Warn);
					continue;
				}
				try
				{
					FurnitureType.make_furniture(
						pack, key,
						f_obj,
						new_furniture
					);
				}
				catch (Exception ex)
				{
					log(ex.ToString(), LogLevel.Error);
					log($"Failed to load data for Furniture \"{key}\", skipping entry.", LogLevel.Warn);
					continue;
				}

				foreach (FurnitureType type in new_furniture)
				{
					if (!replace && furniture.ContainsKey(type.id))
					{
						log($"Duplicate Furniture: {type.id}, skipping Furniture.");
						continue;
					}

					furniture[type.id] = type;

					if (type.shop_id != null)
					{
						if (!shops.ContainsKey(type.shop_id))
						{
							shops[type.shop_id] = new();
						}
					}

					foreach (string shop_id in type.shops)
					{
						if (!shops.ContainsKey(shop_id))
						{
							shops[shop_id] = new();
						}

						shops[shop_id].Add(type.id);
					}
				}
			}
		}

		private void parse_furniture_packs()
		{
			foreach (IContentPack pack in Helper.ContentPacks.GetOwned())
			{
				load_pack(pack);
			}

			log("Finished loading Furniture Types.");
			Helper.GameContent.InvalidateCache("Data/Furniture");
			Helper.GameContent.InvalidateCache("Data/Shops");
		}

		#endregion

		#endregion

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

			bool placed = false;
			
			Point pos = new(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY());

			if (e.Button == config.slot_place_key)
			{
				foreach (Furniture item in Game1.currentLocation.furniture)
				{
					furniture.TryGetValue(item.ItemId, out FurnitureType? type);
					if (type == null) continue;
					if (!type.is_table) continue;

					if (
						Game1.player.ActiveObject is not null &&
						!Game1.player.ActiveObject.bigCraftable.Value
					)
					{
						if (type.place_in_slot(item, pos, Game1.player))
						{
							Helper.Input.Suppress(config.slot_place_key);
							placed = true;
							break;
						}
					}
				}
			}

			if (e.Button == config.slot_take_key && !placed)
			{
				foreach (Furniture item in Game1.currentLocation.furniture)
				{
					furniture.TryGetValue(item.ItemId, out FurnitureType? type);
					if (type == null) continue;
					if (!type.is_table) continue;

					if (type.remove_from_slot(item, pos, Game1.player))
					{
						Helper.Input.Suppress(config.slot_take_key);
						break;
					}
				}
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

        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_day_started(object? sender, DayStartedEventArgs e)
		{
			if (Game1.currentSeason == last_season) return;
			last_season = Game1.currentSeason;

			foreach (FurnitureType type in furniture.Values)
			{
				type.update_seasonal_texture(last_season);
			}
		}

        /// <inheritdoc cref="IWorldEvents.FurnitureListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_furniture_list_changed(object? sender, FurnitureListChangedEventArgs e)
		{
			foreach (Furniture furniture in e.Added)
			{
				ModEntry.furniture.TryGetValue(
					furniture.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.on_placed(furniture);
			}
			
			foreach (Furniture furniture in e.Removed)
			{
				ModEntry.furniture.TryGetValue(
					furniture.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.on_removed(furniture);
			}
		}
    }
}