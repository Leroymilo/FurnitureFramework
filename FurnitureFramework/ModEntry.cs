using System.Runtime.Versioning;
using GenericModConfigMenu;
using GMCMOptions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;


namespace FurnitureFramework
{
    /// <summary>The mod entry point.</summary>
	[RequiresPreviewFeatures]
    internal sealed class ModEntry : Mod
    {
		static IMonitor? monitor;
		static IModHelper? helper;
		static ModConfig? config;

		public static bool print_debug = false;

		static public IModHelper get_helper()
		{
			if (helper == null) throw new NullReferenceException("Helper was not set.");
			return helper;
		}

		static public ModConfig get_config()
		{
			if (config == null) throw new NullReferenceException("Config was not set.");
			return config;
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
			helper.Events.Content.AssetsInvalidated += on_assets_invalidated;
			helper.Events.Player.Warped += on_player_warped;
			helper.Events.World.FurnitureListChanged += on_furniture_list_changed;
			helper.Events.GameLoop.SaveLoaded += on_save_loaded;
			
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
				&& !get_config().disable_AT_warning
			)
			{
				log("Furniture made with the Furniture Framework mod are not compatible with Alternative Textures.", LogLevel.Warn);
				log("You can disable this message in the config of the Furniture Framework.", LogLevel.Warn);
			}
		}

		#region Config

		private void register_config()
		{
			if (config == null) throw new NullReferenceException("Config was not set.");

			// get Generic Mod Config Menu's API (if it's installed)
			IGenericModConfigMenuApi? config_menu =
				Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (config_menu is null)
				return;

			// register mod
			config_menu.Register(
				mod: ModManifest,
				reset: () => config = new ModConfig(),
				save: () => Helper.WriteConfig(config)
			);

			config_menu.AddKeybind(
				mod: ModManifest,
				name: () => "Slot Place Keybind",
				tooltip: () => "The key to press to place an item in a slot.",
				getValue: () => config.slot_place_key,
				setValue: value => config.slot_place_key = value
			);

			config_menu.AddKeybind(
				mod: ModManifest,
				name: () => "Slot Take Keybind",
				tooltip: () => "The key to press to take an item from a slot.",
				getValue: () => config.slot_take_key,
				setValue: value => config.slot_take_key = value
			);
			
			config_menu.AddBoolOption(
				mod: ModManifest,
				name: () => "Disable AT Warning",
				tooltip: () => "Check this to disable the warning about Alternative Textures.",
				getValue: () => config.disable_AT_warning,
				setValue: value => config.disable_AT_warning = value
			);

			config_menu.AddPageLink(
				mod: ModManifest,
				pageId: $"{ModManifest.UniqueID}.slots",
				text: () => "Slots Debug Options",
				tooltip: () => "Options to draw slots areas for debugging purposes."
			);

			config_menu.AddPage(
				mod: ModManifest,
				pageId: $"{ModManifest.UniqueID}.slots",
				pageTitle: () => "Slots Debug Options"
			);

			config_menu.AddBoolOption(
				mod: ModManifest,
				name: () => "Enable slots debug",
				tooltip: () => "Check this to draw a colored rectangle over the areas of Furniture slots.",
				getValue: () => config.enable_slot_debug,
				setValue: value => config.enable_slot_debug = value
			);

			config_menu.AddNumberOption(
				mod: ModManifest,
				getValue: () => config.slot_debug_alpha,
				setValue: value => config.slot_debug_alpha = Math.Clamp(value, 0f, 1f),
				name: () => "Slot Debug Opacity",
				tooltip: () => "The opacity of rectangles drawn over the areas of Furniture slots.",
				min: 0f, max: 1f, interval: 0.01f
			);

			// get GMCM Options' API (if it's installed)
			var configMenuExt = Helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");
			if (configMenuExt is null)
				return;

			configMenuExt.AddColorOption(
				mod: ModManifest,
				getValue: () => config.slot_debug_default_color,
				setValue: value => config.slot_debug_default_color = value,
				name: () => "Default Slot Debug Color",
				tooltip: () => "The default color of the rectangles drawn over the areas of Furniture slots. It will only update on Pack reload or restart.",
				showAlpha: false,
				colorPickerStyle: (uint)IGMCMOptionsAPI.ColorPickerStyle.HSLColorWheel
			);

			Pack.FurniturePack.register_config(config_menu);
		}

		#endregion

		#region Commands

		private void register_commands()
		{
			Helper.ConsoleCommands.Add(
				"reload_furniture_pack",
				"Reloads a Furniture Pack.\n\nUsage: reload_furniture_pack <ModID>\n- ModID: the UniqueID of the Furniture Pack to reload.",
				Pack.FurniturePack.reload_pack
			);
		}

		#endregion

		#region Pack Parsing

		private void parse_furniture_packs()
		{
			foreach (IContentPack pack in Helper.ContentPacks.GetOwned())
			{
				Pack.FurniturePack.load_pack(pack);
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
			
			// if (e.Button == SButton.K)
			// {
			// 	print_debug = !print_debug;
			// 	log($"=== Debug Print {(print_debug ? "On": "Off")} ===", LogLevel.Info);
			// }

			#region Slot Interactions

			if (!Game1.player.CanMove) return;

			bool clicked_slot = false;
			
			Point pos = new(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY());

			Item? held_item = Game1.player.ActiveItem?.getOne();

			if (e.Button == get_config().slot_place_key && held_item is StardewValley.Object obj)
			{
				foreach (Furniture item in Game1.currentLocation.furniture)
				{
					Pack.FurniturePack.try_get_type(item, out Type.FurnitureType? type);
					if (type == null) continue;

					if (type.place_in_slot(item, obj, pos, Game1.player))
					{
						Helper.Input.Suppress(get_config().slot_place_key);
						clicked_slot = true;
						break;
					}
				}
			}

			if (e.Button == get_config().slot_take_key && !clicked_slot)
			{
				foreach (Furniture item in Game1.currentLocation.furniture)
				{
					Pack.FurniturePack.try_get_type(item, out Type.FurnitureType? type);
					if (type == null) continue;

					if (type.remove_from_slot(item, pos, Game1.player))
					{
						Helper.Input.Suppress(get_config().slot_take_key);
						break;
					}
				}
			}

			#endregion
        }


        /// <inheritdoc cref="IContentEvents.AssetRequested"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_asset_requested(object? sender, AssetRequestedEventArgs e)
		{
			string name = e.NameWithoutLocale.Name;

			if (name.StartsWith("Data/Furniture"))
				e.Edit(Pack.FurniturePack.edit_data_furniture);

			if (name.StartsWith("Data/Shops"))
				e.Edit(Pack.FurniturePack.edit_data_shop);

			if (Pack.FurniturePack.try_get_type(name, out Type.FurnitureType? type))
			{
				// Loading texture for menu icon
				e.LoadFrom(type.get_texture, AssetLoadPriority.Medium);
			}

			if (Pack.FurniturePack.try_get_pack_from_resource(name, out Pack.FurniturePack? f_pack))
			{
				// removing the Mod's UID and the separating character from the resource name
				string path = name[(f_pack.UID.Length + 1)..];

				if (e.DataType == typeof(JObject))
				{
					// Loading any data for this Furniture Pack
					e.LoadFrom(
						() => {return f_pack.content_pack.ModContent.Load<JObject>(path);},
						AssetLoadPriority.Low
					);
				}
				else if (e.DataType == typeof(Texture2D))
				{
					// Loading any texture for this Furniture Pack
					e.LoadFrom(
						() => {return Type.TextureManager.base_load(f_pack.content_pack.ModContent, path);},
						AssetLoadPriority.Low
					);
				}
				else
				{
					// Shouldn't happen in this mod
					log($"Unknown asset type to load for {name} : {e.DataType}.", LogLevel.Warn);
				}
			}
		}

        /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_assets_invalidated(object? sender, AssetsInvalidatedEventArgs e)
		{
			log($"invalidated assets:");
			foreach (IAssetName name in e.Names)
			{
				log($"\t{name}");
			}
		}

        /// <inheritdoc cref="IWorldEvents.FurnitureListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_furniture_list_changed(object? sender, FurnitureListChangedEventArgs e)
		{
			foreach (Furniture furniture in e.Added)
			{
				if (Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type))
				{
					type.on_placed(furniture);
				}
			}
			
			foreach (Furniture furniture in e.Removed)
			{
				if (Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type))
				{
					type.on_removed(furniture);
				}
			}
		}

        /// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_player_warped(object? sender, WarpedEventArgs e)
		{
			foreach (Furniture furniture in e.NewLocation.furniture)
			{
				if (Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type))
				{
					furniture.modData["FF.particle_timers"] = "[]";
				}
			}
		}

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_save_loaded(object? sender, SaveLoadedEventArgs e)
		{
			foreach (Furniture furniture in Game1.currentLocation.furniture)
			{
				if (Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type))
				{
					furniture.modData["FF.particle_timers"] = "[]";
				}
			}
		}
		
    }
}