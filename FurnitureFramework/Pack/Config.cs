using GenericModConfigMenu;
using GMCMOptions;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Pack
{
	partial class FurniturePack
	{

		public static HashSet<string> to_register = new();

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

			config_menu_api.AddKeybind(
				mod: manifest,
				name: () => "Slot Interact Keybind",
				tooltip: () => "The key to press to interact with a Furniture placed in a slot.",
				getValue: () => config.slot_interact_key,
				setValue: value => config.slot_interact_key = value
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
			JObject data_f = new();
			JObject data_p = new();

			Dictionary<string, bool> types = new();
			Dictionary<string, string> type_names = new();
			Dictionary<string, bool> i_packs = new();
			Dictionary<string, Data.IncludedPack> i_pack_info = new();
			Dictionary<string, string> i_pack_names = new();

			public void set_data(JObject data)
			{
				JToken? f_tok = data.GetValue("Furniture");
				if (f_tok is JObject f_obj) data_f = f_obj;
				else data_f = new();

				JToken? p_tok = data.GetValue("Included");
				if (p_tok is JObject p_obj) data_p = p_obj;
				else data_p = new();
			}

			public void add_type(string type_id, string type_name)
			{
				type_names[type_id] = type_name;
				JToken? token = data_f.GetValue(type_id);
				if (token == null) types[type_id] = true;
				else types[type_id] = token.Value<bool>();
			}

			public bool is_type_enabled(string type_id)
			{
				return types[type_id];
			}

			public void add_i_pack(string i_data_UID, string name, Data.IncludedPack info)
			{
				i_pack_info[i_data_UID] = info;
				i_pack_names[i_data_UID] = name;
				JToken? token = data_p.GetValue(i_data_UID);
				if (token == null) i_packs[i_data_UID] = info.Enabled;
				else i_packs[i_data_UID] = token.Value<bool>();
			}

			public bool is_pack_enabled(string i_data_UID)
			{
				return i_packs[i_data_UID];
			}

			public JObject save()
			{
				return new()
				{
					{ "Furniture", JObject.FromObject(types) },
					{ "Included", JObject.FromObject(i_packs) }
				};
			}

			public void clear()
			{
				data_f = new();
				data_p = new();

				types.Clear();
				type_names.Clear();
				i_packs.Clear();
				i_pack_info.Clear();
				i_pack_names.Clear();
			}

			public void reset()
			{
				foreach (string type_id in types.Keys)
					types[type_id] = true;

				foreach(string i_data_UID in i_packs.Keys)
					i_packs[i_data_UID] = i_pack_info[i_data_UID].Enabled;
			}

			public void register(IGenericModConfigMenuApi api, IManifest manifest)
			{
				if (types.Count > 0)
				{
					api.AddSectionTitle(manifest, () => "Furniture", null);
					foreach (string type_id in types.Keys)
					{
						if (type_id == "leroymilo.FF.debug_catalog") continue;
						api.AddBoolOption(
							manifest,
							() => types[type_id],
							(bool value) => {
								if (types[type_id] != value)
									invalidate_game_data();
								types[type_id] = value;
							},
							() => type_names[type_id],
							() => type_id,
							type_id
						);
					}
				}

				if (i_packs.Count > 0)
				{
					api.AddSectionTitle(manifest, () => "Included Packs", null);
					foreach (string i_data_UID in i_packs.Keys)
					{
						api.AddBoolOption(
							manifest,
							() => i_packs[i_data_UID],
							(bool value) => {
								if (i_packs[i_data_UID] != value)
									invalidate_game_data();
								i_packs[i_data_UID] = value;
							},
							() => i_pack_names[i_data_UID],
							() => i_pack_info[i_data_UID].Description,
							i_data_UID
						);

						api.AddPageLink(
							mod: manifest,
							pageId: i_data_UID,
							text: () => $"{i_pack_names[i_data_UID]} Config"
						);
					}
				}
			}
		}

		#endregion

		PackConfig config = new();

		private void load_config()
		{
			JObject? config_data = content_pack.ReadJsonFile<JObject>(CONFIG_PATH);
			if (config_data == null) return;

			JToken? config_token = config_data.GetValue(data_UID);
			if (config_token is JObject config_obj)
				config.set_data(config_obj);
		}

		private void save_config()
		{
			if (root != null) 
			{
				// only the root pack should save the config.
				root.save_config();
				return;
			}


			JObject? config_data = content_pack.ReadJsonFile<JObject>(CONFIG_PATH);
			if (config_data == null) config_data = new();

			save_config(config_data);

			string path = Path.Combine(content_pack.DirectoryPath, CONFIG_PATH);
			File.WriteAllText(path, config_data.ToString());
		}

		private void save_config(JObject config_data)
		{
			config_data[data_UID] = config.save();

			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.pack.save_config(config_data);
		}

		private void reset_config()
		{
			config.reset();
			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.pack.reset_config();
		}

		private void unregister_config()
		{
			if (config_menu_api is null) return;

			config_menu_api.Unregister(content_pack.Manifest);
		}

		public static void register_pack_config()
		{
			if (config_menu_api is null) return;

			foreach (string UID in to_register)
				packs[$"{UID}/{DEFAULT_PATH}"].register_config();
			
			to_register.Clear();
		}

		private void register_config()
		{
			if (config_menu_api is null) return;

			IManifest manifest = content_pack.Manifest;

			if (!is_included)
			{
				config_menu_api.Register(
					manifest,
					() => {
						reset_config();
						invalidate_game_data();
					},
					save_config
				);
				save_config();
			}

			config.register(config_menu_api, manifest);

			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.register_config(manifest);
		}
	}
}