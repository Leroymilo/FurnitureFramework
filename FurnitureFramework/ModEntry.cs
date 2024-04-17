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
		static IMonitor? monitor;
		public static IModHelper helper {
			get {
				if (helper == null)
					throw new NullReferenceException("Helper was not set");
				return helper;
			}
			private set {helper = value;}
		}

		static public void log(string message, LogLevel log_level = LogLevel.Debug)
		{
			if (monitor == null) throw new NullReferenceException("Monitor was not set.");
			monitor.Log(message, log_level);
		}

		static readonly List<CustomFurniture> furnitures = new();

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
			monitor = Monitor;
			ModEntry.helper = Helper;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.GameLoop.GameLaunched += on_game_launched;
			
			// for quick access to decompiled code
			Furniture test = new();
			GameLocation location = new();
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

				log($"Reading Custom Furniture of {UID}...");
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
						log($"No data for furniture {key}, skipping entry.", LogLevel.Warn);
						continue;
					}
					furnitures.Add(new CustomFurniture(pack, key, (JObject)f_data));
				}
			}
		}


        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
        }
    }
}