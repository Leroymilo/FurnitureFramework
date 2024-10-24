using GenericModConfigMenu;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Pack
{
	partial class FurniturePack
	{

		private class IncludedPack
		{
			public readonly string name;
			string? description = null;
			bool default_enabled = true;

			readonly FurniturePack pack;
			public string data_UID {get => pack.data_UID;}
			bool enabled = true;

			public readonly bool is_valid = false;
			public readonly string error_msg = "";

			string page_id;

			#region Parsing

			public IncludedPack(IContentPack c_pack, JProperty property)
			{
				name = property.Name;

				if (property.Value is not JObject obj)
				{
					error_msg = "Data should be an object.";
					return;
				}

				string path = "";
				if (!JsonParser.try_parse(obj.GetValue("Path"), ref path))
				{
					error_msg = "Invalid or missing Path.";
					return;
				}
				
				pack = new(c_pack, path);

				is_valid = true;

				description = JsonParser.parse<string?>(obj.GetValue("Description"), null);
				default_enabled = JsonParser.parse(obj.GetValue("Enabled"), true);
				
				enabled = default_enabled;

				page_id = $"{c_pack.Manifest}.{name}";
			}

			public void add_pack()
			{
				to_load.Add(pack.data_UID);
				packs[pack.data_UID] = pack;
			}

			#endregion

			public void clear(bool cascade = true)
			{
				packs.Remove(data_UID);
				data_UIDs[pack.UID].Remove(data_UID);
				pack.clear(cascade);
			}

			#region Config

			public void add_config(IGenericModConfigMenuApi config_menu)
			{
				Func<string>? tooltip = null;
				if (description is not null) tooltip = () => description;

				config_menu.AddBoolOption(
					mod: pack.content_pack.Manifest,
					getValue: () => enabled,
					setValue: (value) => {
						enabled = value;
						update_after_config_change();
					},
					name: () => name,
					tooltip: tooltip
				);

				if (!pack.has_config()) return;
				
				config_menu.AddPageLink(
					mod: pack.content_pack.Manifest,
					pageId: page_id,
					text: () => $"{name} Config",
					tooltip: () => $"Additional config options for the {name} part of this Furniture Pack."
				);
			}

			public void add_config_page(IGenericModConfigMenuApi config_menu)
			{
				if (!pack.has_config()) return;

				config_menu.AddPage(
					mod: pack.content_pack.Manifest,
					pageId: page_id,
					pageTitle: () => $"{name} Config"
				);

				pack.add_config(config_menu);
			}

			public void read_config(JObject config_data, string prefix)
			{
				string key = $"{prefix}.{name}";

				JToken? config_token = config_data.GetValue(key);
				if (config_token is not null && config_token.Type == JTokenType.Boolean)
					enabled = (bool)config_token;
				
				pack.read_config(config_data, key);
			}

			public void save_config(JObject data, string prefix)
			{
				string key = $"{prefix}.{name}";

				data.Add(key, new JValue(enabled));
				
				pack.save_config(data, key);
			}

			#endregion
		}
	}
}