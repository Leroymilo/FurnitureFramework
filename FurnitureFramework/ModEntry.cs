using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
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
			monitor = Monitor;
			ModEntry.helper = Helper;
			HarmonyPatcher.harmony = new(ModManifest.UniqueID);
            helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.GameLoop.GameLaunched += on_game_launched;
			helper.Events.Content.AssetRequested += on_asset_requested;
			
			HarmonyPatcher.patch();

			// for quick access to decompiled code
			Furniture test = new();
			// Object test;
			// GameLocation location = new();
			// Farmer farmer = new();
			return;
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
					if (f_data == null)
					{
						log($"No data for Furniture \"{key}\", skipping entry.", LogLevel.Warn);
						continue;
					}
					try
					{
						FurnitureType new_furniture = new(pack, key, (JObject)f_data);
						furniture[new_furniture.id] = new_furniture;
					}
					catch (Exception ex)
					{
						log(ex.ToString(), LogLevel.Error);
						log($"Failed to load data for Furniture \"{key}\", skipping entry.", LogLevel.Warn);
					}
				}
			}

			log("Finished loading Furniture Types.");
			Helper.GameContent.InvalidateCache("Data/Furniture");
		}


        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
			
			if (e.Button == SButton.K)
				print_debug = true;
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

			if (furniture.ContainsKey(e.Name.Name))
			{
				e.LoadFrom(furniture[e.Name.Name].get_icon_texture, AssetLoadPriority.Medium);
			}
		}

    }
}