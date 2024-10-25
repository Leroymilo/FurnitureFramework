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
			public readonly bool default_enabled = true;

			public readonly FurniturePack pack;
			public string data_UID {get => pack.data_UID;}

			public readonly bool is_valid = false;
			public readonly string error_msg = "";

			#region Parsing

			public IncludedPack(IContentPack c_pack, JProperty property, FurniturePack root)
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
				
				pack = new(c_pack, path, root);

				is_valid = true;

				description = JsonParser.parse<string?>(obj.GetValue("Description"), null);
				default_enabled = JsonParser.parse(obj.GetValue("Enabled"), true);
			}

			public void add_pack()
			{
				to_load.Append(pack.data_UID);
				packs[pack.data_UID] = pack;
			}

			#endregion

			public void clear()
			{
				packs.Remove(data_UID);
				pack.clear();
			}

			#region Config

			public void register_config(IManifest manifest)
			{
				if (config_menu_api is null) return;

				config_menu_api.AddPage(
					mod: manifest,
					pageId: data_UID,
					pageTitle: () => $"{name} Config"
				);

				if (description != null)
				{
					config_menu_api.AddParagraph(
						mod: manifest,
						() => description
					);
				}

				pack.register_config();
			}

			#endregion
		}
	}
}