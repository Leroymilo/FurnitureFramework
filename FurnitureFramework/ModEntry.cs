using System.Collections;
using GenericModConfigMenu;
using GMCMOptions;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;


namespace FurnitureFramework
{
	using SVObject = StardewValley.Object;

    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
		static IMonitor? monitor;
		static IModHelper? helper;
		static ModConfig? config;

		public static bool print_debug = false;

		#region getters

		static public IModHelper GetHelper()
		{
			if (helper == null) throw new NullReferenceException("Helper was not set.");
			return helper;
		}

		static public ModConfig GetConfig()
		{
			if (config == null) throw new NullReferenceException("Config was not set.");
			return config;
		}

		static public void Log(string message, LogLevel log_level = LogLevel.Debug)
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

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Content.AssetRequested += OnAssetRequested;
			helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
			helper.Events.Player.Warped += OnPlayerWarped;
			helper.Events.World.FurnitureListChanged += OnFurnitureListChanged;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			
			FFHarmony.HarmonyPatcher.Patch(new(ModManifest.UniqueID));
        }

		#region On Game Launched

		/// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event data.</param>
		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			Data.FPack.FPack.PreLoad(GetHelper());
			RegisterConfig();
			RegisterCommands();

			if (
				Helper.ModRegistry.IsLoaded("PeacefulEnd.AlternativeTextures")
				&& !GetConfig().disable_AT_warning
			)
			{
				Log("Furniture made with the Furniture Framework mod are not compatible with Alternative Textures.", LogLevel.Warn);
				Log("You can disable this message in the config of the Furniture Framework.", LogLevel.Warn);
			}

			if (config?.load_packs_on_game_start ?? false) Data.FPack.FPack.LoadAll();
		}

		#region config

		private void RegisterConfig()
		{
			if (config == null) throw new NullReferenceException("Config was not set.");

			// get Generic Mod Config Menu's API (if it's installed)
			IGenericModConfigMenuApi? config_menu =
				Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			Data.FPack.FPack.ConfigMenuAPI = config_menu;

			// get GMCM Options' API (if it's installed)
			IGMCMOptionsAPI? config_options = Helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");
			Data.FPack.FPack.ConfigOptionAPI = config_options;
			
			RegisterFFConfig(ModManifest, config_menu, config_options);
		}
		public static void RegisterFFConfig(IManifest manifest,
			IGenericModConfigMenuApi? ConfigMenuAPI,
			IGMCMOptionsAPI? ConfigOptionAPI
		)
		{
			if (ConfigMenuAPI is null) return;
			
			ModConfig config_ = GetConfig();
			IModHelper helper_ = GetHelper();

			// register mod
			ConfigMenuAPI.Register(
				mod: manifest,
				reset: () => config_ = new ModConfig(),
				save: () => helper_.WriteConfig(config_)
			);

			ConfigMenuAPI.AddKeybind(
				mod: manifest,
				name: () => "Slot Place Keybind",
				tooltip: () => "The key to press to place an furniture in a slot.",
				getValue: () => config_.slot_place_key,
				setValue: value => config_.slot_place_key = value
			);

			ConfigMenuAPI.AddKeybind(
				mod: manifest,
				name: () => "Slot Take Keybind",
				tooltip: () => "The key to press to take an furniture from a slot.",
				getValue: () => config_.slot_take_key,
				setValue: value => config_.slot_take_key = value
			);

			ConfigMenuAPI.AddKeybind(
				mod: manifest,
				name: () => "Slot Interact Keybind",
				tooltip: () => "The key to press to interact with a Furniture placed in a slot.\nCan be weird with vanilla Furniture placed in Slots.",
				getValue: () => config_.slot_interact_key,
				setValue: value => config_.slot_interact_key = value
			);

			ConfigMenuAPI.AddBoolOption(
				mod: manifest,
				name: () => "Enable toggle carry to slot",
				tooltip: () => "If this is enabled, toggling a Furniture will also toggle the Furniture in its slots.",
				getValue: () => config_.toggle_carry_to_slot,
				setValue: value => config_.toggle_carry_to_slot = value
			);

			ConfigMenuAPI.AddBoolOption(
				mod: manifest,
				name: () => "Disable AT Warning",
				tooltip: () => "Check this to disable the warning about Alternative Textures.",
				getValue: () => config_.disable_AT_warning,
				setValue: value => config_.disable_AT_warning = value
			);

			ConfigMenuAPI.AddBoolOption(
				mod: manifest,
				name: () => "Load Packs on game start",
				tooltip: () => "Check this to force the game to load all the Furniture Packs when the game starts.",
				getValue: () => config_.load_packs_on_game_start,
				setValue: value => config_.load_packs_on_game_start = value
			);

			ConfigMenuAPI.AddPageLink(
				mod: manifest,
				pageId: $"{manifest.UniqueID}.slots",
				text: () => "Slots Debug Options",
				tooltip: () => "Options to draw slots areas for debugging purposes."
			);

			ConfigMenuAPI.AddPage(
				mod: manifest,
				pageId: $"{manifest.UniqueID}.slots",
				pageTitle: () => "Slots Debug Options"
			);

			ConfigMenuAPI.AddBoolOption(
				mod: manifest,
				name: () => "Enable slots debug",
				tooltip: () => "Check this to draw a colored rectangle over the areas of Furniture slots.",
				getValue: () => config_.enable_slot_debug,
				setValue: value => config_.enable_slot_debug = value
			);

			ConfigMenuAPI.AddNumberOption(
				mod: manifest,
				getValue: () => config_.slot_debug_alpha,
				setValue: value => config_.slot_debug_alpha = Math.Clamp(value, 0f, 1f),
				name: () => "Slot Debug Opacity",
				tooltip: () => "The opacity of rectangles drawn over the areas of Furniture slots.",
				min: 0f, max: 1f, interval: 0.01f
			);

			if (ConfigOptionAPI is null) return;

			ConfigOptionAPI.AddColorOption(
				mod: manifest,
				getValue: () => config_.slot_debug_default_color,
				setValue: value => config_.slot_debug_default_color = value,
				name: () => "Default Slot Debug Color",
				tooltip: () => "The default color of the rectangles drawn over the areas of Furniture slots. It will only update on Pack reload or restart.",
				showAlpha: false,
				colorPickerStyle: (uint)IGMCMOptionsAPI.ColorPickerStyle.HSLColorWheel
			);
		}

		#endregion

		private void RegisterCommands()
		{
			string desc = "Reloads a Furniture Pack, or all Packs if no id is given.\n\n";
			desc += "Usage: ff_reload <ModID>\n- ModID: the UniqueID of the Furniture Pack to reload.";
			Helper.ConsoleCommands.Add("ff_reload", desc, Data.FPack.FPack.Reload);

			desc = "Dumps all the data from a Furniture Pack, or all Packs if no id is given.\n\n";
			desc += "Usage: ff_debug_print <ModID>\n- ModID: the UniqueID of the Furniture Pack to debug print.";
			Helper.ConsoleCommands.Add("ff_debug_print", desc, Data.FPack.FPack.DebugPrint);
		}

		#endregion

		#region On Button Pressed

        /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
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
			if (Game1.activeClickableMenu != null) return;

			Point screen_pos = e.Cursor.GetScaledScreenPixels().ToPoint();

			// Block click if mouse is over any main HUD component
			foreach (StardewValley.Menus.IClickableMenu menu in Game1.onScreenMenus)
			{
				Rectangle bounding_box = new(menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height);
				if (bounding_box.Contains(screen_pos)) return;
			}

			Point pos = e.Cursor.AbsolutePixels.ToPoint();
			Item? item = Game1.player.ActiveItem?.getOne();
			if (e.Button == GetConfig().slot_place_key && item is SVObject obj)
			{
				foreach (Furniture furniture in Game1.currentLocation.furniture)
				{
					Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type);
					if (type == null) continue;

					if (type.PlaceInSlot(furniture, pos, Game1.player, obj))
					{
						Helper.Input.Suppress(GetConfig().slot_place_key);
						return;
					}
				}
			}

			if (e.Button == GetConfig().slot_take_key)
			{
				foreach (Furniture furniture in Game1.currentLocation.furniture)
				{
					Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type);
					if (type == null) continue;

					if (type.RemoveFromSlot(furniture, pos, Game1.player))
					{
						Helper.Input.Suppress(GetConfig().slot_take_key);
						return;
					}
				}
			}

			if (e.Button == GetConfig().slot_interact_key)
			{
				// Checking distance between player and click
				if ((Game1.player.StandingPixel - pos).ToVector2().Length() <= 128)
				{
					foreach (Furniture furniture in Game1.currentLocation.furniture)
					{
						Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type);
						if (type == null) continue;

						if (type.ActionInSlot(furniture, pos, Game1.player))
						{
							Helper.Input.Suppress(GetConfig().slot_interact_key);
							return;
						}
					}
				}
			}

			#endregion
        }

		#endregion

		#region Asset handling

        /// <inheritdoc cref="IContentEvents.AssetRequested"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture") || e.Name.IsEquivalentTo("Data/Furniture_international"))
				e.Edit(Data.FPack.FPack.EditFurnitureData);

			else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops") || e.Name.IsEquivalentTo("Data/Shops_international"))
				e.Edit(Data.FPack.FPack.EditShopData, AssetEditPriority.Default + 100);
			
			else if (e.NameWithoutLocale.IsEquivalentTo("spacechase0.SpaceCore/ShopExtensionData"))
			// adding catalogue tabs with spacecore
				e.Edit(asset => {
					string json_string = "{\"Tabs\": \"FurnitureCatalogue\"}";
					Type? type = Type.GetType("SpaceCore.VanillaAssetExpansion.ShopExtensionData,SpaceCore");
					if (type == null) return;
					var obj = JsonConvert.DeserializeObject(json_string, type);

					var editor = asset.GetData<IDictionary>();
					foreach (string shop_id in Data.FPack.FPack.AddedCatalogues)
						editor.Add(shop_id, obj);
				});

			// Loading any Furniture Pack data or texture (including menu icons)
			else Data.FPack.FPack.LoadResource(e);
		}

		private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
		{
			bool did_stuff = false;
			foreach (IAssetName name in e.Names)
				did_stuff |= Data.FPack.FPack.InvalidateAsset(name);
			
			if (did_stuff) Data.FPack.FPack.InvalidateGameData();
		}

		#endregion

		#region Placed Furniture Updates

        /// <inheritdoc cref="IWorldEvents.FurnitureListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void OnFurnitureListChanged(object? sender, FurnitureListChangedEventArgs e)
		{
			foreach (Furniture furniture in e.Added)
			{
				if (Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type))
				{
					type.OnPlaced(furniture);
				}
			}
			
			foreach (Furniture furniture in e.Removed)
			{
				if (Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type))
				{
					Data.FType.FType.OnRemoved(furniture);
				}
			}
		}

        /// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void OnPlayerWarped(object? sender, WarpedEventArgs e)
		{
			foreach (Furniture furniture in e.NewLocation.furniture)
			{
				Data.FType.FType.SetModData(furniture);
			}
		}

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
		{
			foreach (Furniture furniture in Game1.currentLocation.furniture)
			{
				Data.FType.FType.SetModData(furniture);
			}
		}
		
		#endregion
    }
}