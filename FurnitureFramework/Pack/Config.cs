using GenericModConfigMenu;
using GMCMOptions;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Pack
{
	partial class FurniturePack
	{
		public static IGenericModConfigMenuApi? config_menu_api;
		public static IGMCMOptionsAPI? config_options_api;

		public static void register_FF_config(IManifest manifest)
		{
			if (config_menu_api is null) return;
			
			ModConfig config = ModEntry.get_config();
			IModHelper helper = ModEntry.get_helper();

			// register mod
			config_menu_api.Register(
				mod: manifest,
				reset: () => config = new ModConfig(),
				save: () => helper.WriteConfig(config)
			);

			config_menu_api.AddKeybind(
				mod: manifest,
				name: () => "Slot Place Keybind",
				tooltip: () => "The key to press to place an furniture in a slot.",
				getValue: () => config.slot_place_key,
				setValue: value => config.slot_place_key = value
			);

			config_menu_api.AddKeybind(
				mod: manifest,
				name: () => "Slot Take Keybind",
				tooltip: () => "The key to press to take an furniture from a slot.",
				getValue: () => config.slot_take_key,
				setValue: value => config.slot_take_key = value
			);

			config_menu_api.AddBoolOption(
				mod: manifest,
				name: () => "Load all Furniture Packs on game start",
				tooltip: () => "If this is not checked, the game will load packs only when it needs the data.",
				getValue: () => config.load_packs_on_start,
				setValue: value => {
					config.load_packs_on_start = value;
					load_all();
				}
			);

			config_menu_api.AddBoolOption(
				mod: manifest,
				name: () => "Disable AT Warning",
				tooltip: () => "Check this to disable the warning about Alternative Textures.",
				getValue: () => config.disable_AT_warning,
				setValue: value => config.disable_AT_warning = value
			);

			config_menu_api.AddPageLink(
				mod: manifest,
				pageId: $"{manifest.UniqueID}.slots",
				text: () => "Slots Debug Options",
				tooltip: () => "Options to draw slots areas for debugging purposes."
			);

			config_menu_api.AddPage(
				mod: manifest,
				pageId: $"{manifest.UniqueID}.slots",
				pageTitle: () => "Slots Debug Options"
			);

			config_menu_api.AddBoolOption(
				mod: manifest,
				name: () => "Enable slots debug",
				tooltip: () => "Check this to draw a colored rectangle over the areas of Furniture slots.",
				getValue: () => config.enable_slot_debug,
				setValue: value => config.enable_slot_debug = value
			);

			config_menu_api.AddNumberOption(
				mod: manifest,
				getValue: () => config.slot_debug_alpha,
				setValue: value => config.slot_debug_alpha = Math.Clamp(value, 0f, 1f),
				name: () => "Slot Debug Opacity",
				tooltip: () => "The opacity of rectangles drawn over the areas of Furniture slots.",
				min: 0f, max: 1f, interval: 0.01f
			);

			if (config_options_api is null) return;

			config_options_api.AddColorOption(
				mod: manifest,
				getValue: () => config.slot_debug_default_color,
				setValue: value => config.slot_debug_default_color = value,
				name: () => "Default Slot Debug Color",
				tooltip: () => "The default color of the rectangles drawn over the areas of Furniture slots. It will only update on Pack reload or restart.",
				showAlpha: false,
				colorPickerStyle: (uint)IGMCMOptionsAPI.ColorPickerStyle.HSLColorWheel
			);
		}
	
		#region PackConfig class

		private class PackConfig
		{
			Dictionary<string, bool> types = new();
			Dictionary<string, string> type_names = new();
			Dictionary<string, bool> i_packs = new();
			Dictionary<string, bool> i_pack_defaults = new();
			Dictionary<string, string> i_pack_names = new();

			#region Add/Remove

			public void add_type(string type_id, string type_name)
			{
				types[type_id] = true;
				type_names[type_id] = type_name;
			}

			public void remove_type(string type_id)
			{
				types.Remove(type_id);
				type_names.Remove(type_id);
			}

			public void add_i_pack(string i_pack_UID, string name, bool default_)
			{
				i_packs[i_pack_UID] = default_;
				i_pack_defaults[i_pack_UID] = default_;
				i_pack_names[i_pack_UID] = name;
			}

			public void remove_i_pack(string i_pack_UID)
			{
				i_packs.Remove(i_pack_UID);
				i_pack_defaults.Remove(i_pack_UID);
				i_pack_names.Remove(i_pack_UID);
			}

			#endregion

			public void load(JToken? data)
			{
				if (data is not JObject data_obj) return;

				if (data_obj.TryGetValue("Furniture", out JToken? f_tok) && f_tok is JObject f_obj)
				{
					foreach (JProperty f_prop in f_obj.Properties())
					{
						if (types.ContainsKey(f_prop.Name) && f_prop.Value.Type == JTokenType.Boolean)
							types[f_prop.Name] = (bool)f_prop.Value;
					}
				}

				if (data_obj.TryGetValue("Included", out JToken? i_tok) && i_tok is JObject i_obj)
				{
					foreach (JProperty i_prop in i_obj.Properties())
					{
						if (i_packs.ContainsKey(i_prop.Name) && i_prop.Value.Type == JTokenType.Boolean)
							i_packs[i_prop.Name] = (bool)i_prop.Value;
					}
				}
			}

			public JObject save()
			{
				return new()
				{
					{ "Furniture", JObject.FromObject(types) },
					{ "Included", JObject.FromObject(i_packs) }
				};
			}

			public void reset()
			{
				foreach (string type_id in types.Keys)
				{
					types[type_id] = true;
				}

				foreach(string i_pack_UID in i_packs.Keys)
				{
					i_packs[i_pack_UID] = i_pack_defaults[i_pack_UID];
				}
			}

			public void register(IGenericModConfigMenuApi api, IManifest manifest)
			{
				api.AddSectionTitle(manifest, () => "Furniture", null);
				foreach (string type_id in types.Keys)
				{
					api.AddBoolOption(
						manifest,
						() => types[type_id],
						(bool value) => {
							types[type_id] = value;
							invalidate_game_data();
						},
						() => type_names[type_id],
						() => type_id,
						type_id
					);
				}

				api.AddSectionTitle(manifest, () => "Included Paks", null);
				foreach (string i_pack_UID in i_packs.Keys)
				{
					api.AddBoolOption(
						manifest,
						() => i_packs[i_pack_UID],
						(bool value) => {
							i_packs[i_pack_UID] = value;
							invalidate_game_data();
						},
						() => i_pack_names[i_pack_UID],
						() => i_pack_UID,
						i_pack_UID
					);

					api.AddPageLink(
						mod: manifest,
						pageId: i_pack_UID,
						text: () => $"{i_pack_names[i_pack_UID]} Config"
					);
				}
			}
		}

		#endregion

		PackConfig config = new();

		public static void register_pack_config()
		{
			foreach (string UID in UIDs)
				packs[$"{UID}/{DEFAULT_PATH}"].register_config();
		}

		private void register_config()
		{
			if (config_menu_api is null) return;

			IManifest manifest = content_pack.Manifest;

			if (!is_included)
			{
				config_menu_api.Register(
					manifest,
					() => {reset_config(); invalidate_game_data();},
					save_config);
			}

			config.register(config_menu_api, manifest);

			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.register_config(manifest);
			
			update_config = false;
		}

		private void load_config()
		{
			JObject? config_data = content_pack.ReadJsonFile<JObject>(CONFIG_PATH);
			if (config_data == null) config_data = new();

			load_config(config_data);
		}

		private void load_config(JObject config_data)
		{
			config.load(config_data.GetValue(data_UID));

			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.pack.load_config(config_data);
		}

		private void save_config()
		{
			if (!is_included) return;	// only the root pack should save the config.

			JObject? config_data = content_pack.ReadJsonFile<JObject>(CONFIG_PATH);
			if (config_data == null) config_data = new();

			save_config(config_data);

			string path = Path.Combine(content_pack.DirectoryPath, CONFIG_PATH);
			File.WriteAllText(path, config_data.ToString());
		}

		private void save_config(JObject config_data)
		{
			config_data.Add(data_UID, config.save());

			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.pack.save_config(config_data);
		}

		private void reset_config()
		{
			config.reset();
			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.pack.reset_config();
		}
	}
}