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

			public IncludedPack(IContentPack c_pack, string name, Data.IncludedPack data, FurniturePack root)
			{
				this.name = name;

				string path = data.Path;
				pack = new(c_pack, path, root);

				if (packs.ContainsKey(data_UID))
					pack = packs[data_UID];

				is_valid = true;

				description = data.Description;
				default_enabled = data.Enabled;
			}

			#endregion

			public void clear()
			{
				pack.clear(cascade : true);
				packs.Remove(data_UID);
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