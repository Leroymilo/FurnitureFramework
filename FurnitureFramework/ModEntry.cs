using System.Runtime.Versioning;
using GenericModConfigMenu;
using GMCMOptions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;


namespace FurnitureFramework
{
	using SVObject = StardewValley.Object;

    /// <summary>The mod entry point.</summary>
	[RequiresPreviewFeatures]
    internal sealed class ModEntry : Mod
    {
		static IMonitor? monitor;
		static IModHelper? helper;
		static ModConfig? config;

		public static bool print_debug = false;

		#region getters

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

		#endregion

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper _)
        {
			// Harmony.DEBUG = true;

			monitor = Monitor;
			helper = Helper;
			config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += on_game_launched;
            helper.Events.Input.ButtonPressed += on_button_pressed;
			helper.Events.Content.AssetRequested += on_asset_requested;
			helper.Events.Player.Warped += on_player_warped;
			helper.Events.World.FurnitureListChanged += on_furniture_list_changed;
			helper.Events.GameLoop.SaveLoaded += on_save_loaded;
			
			FFHarmony.HarmonyPatcher.harmony = new(ModManifest.UniqueID);
			FFHarmony.HarmonyPatcher.patch();
        }

		#region On Game Launched

        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_game_launched(object? sender, GameLaunchedEventArgs e)
		{
			Pack.FurniturePack.pre_load(get_helper());
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

		private void register_config()
		{
			if (config == null) throw new NullReferenceException("Config was not set.");

			// get Generic Mod Config Menu's API (if it's installed)
			IGenericModConfigMenuApi? config_menu =
				Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			Pack.FurniturePack.config_menu_api = config_menu;

			// get GMCM Options' API (if it's installed)
			IGMCMOptionsAPI? config_options = Helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");
			Pack.FurniturePack.config_options_api = config_options;
			
			Pack.FurniturePack.register_FF_config(ModManifest);
		}

		private void register_commands()
		{
			string desc = "Reloads a Furniture Pack, or all Packs if no id is given.\n\n";
			desc += "Usage: ff_reload <ModID>\n- ModID: the UniqueID of the Furniture Pack to reload.";
			Helper.ConsoleCommands.Add("ff_reload", desc, Pack.FurniturePack.reload_pack);

			desc = "Dumps all the data from a Furniture Pack, or all Packs if no id is given.\n\n";
			desc += "Usage: ff_debug_print <ModID>\n- ModID: the UniqueID of the Furniture Pack to debug print.";
			Helper.ConsoleCommands.Add("ff_debug_print", desc, Pack.FurniturePack.debug_print);
		}

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
			
			Point pos = new(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY());

			Item? item = Game1.player.ActiveItem?.getOne();
			if (e.Button == get_config().slot_place_key && item is SVObject obj)
			{
				foreach (Furniture furniture in Game1.currentLocation.furniture)
				{
					Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type);
					if (type == null) continue;

					if (type.place_in_slot(furniture, pos, Game1.player, obj))
					{
						Helper.Input.Suppress(get_config().slot_place_key);
						return;
					}
				}
			}

			if (e.Button == get_config().slot_take_key)
			{
				foreach (Furniture furniture in Game1.currentLocation.furniture)
				{
					Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type);
					if (type == null) continue;

					if (type.remove_from_slot(furniture, pos, Game1.player))
					{
						Helper.Input.Suppress(get_config().slot_take_key);
						return;
					}
				}
			}

			if (e.Button == get_config().slot_interact_key)
			{
				// Checking distance between player and click
				if ((Game1.player.StandingPixel - pos).ToVector2().Length() <= 128)
				{
					foreach (Furniture furniture in Game1.currentLocation.furniture)
					{
						Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type);
						if (type == null) continue;

						if (type.action_in_slot(furniture, pos, Game1.player))
						{
							Helper.Input.Suppress(get_config().slot_interact_key);
							return;
						}
					}
				}
			}

			#endregion
        }

		#region Asset handling

        /// <inheritdoc cref="IContentEvents.AssetRequested"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_asset_requested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture") || e.Name.IsEquivalentTo("Data/Furniture_international"))
				e.Edit(Pack.FurniturePack.edit_data_furniture, priority: AssetEditPriority.Early);

			else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops") || e.Name.IsEquivalentTo("Data/Shops_international"))
				e.Edit(Pack.FurniturePack.edit_data_shop, priority: AssetEditPriority.Early);

			// Loading any Furniture Pack data or texture (including menu icons)
			else Pack.FurniturePack.load_resource(e);
		}

		#endregion

		#region Placed Furniture Updates

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
		
		#endregion
    }
}