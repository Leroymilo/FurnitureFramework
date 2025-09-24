using GenericModConfigMenu;
using GMCMOptions;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Data.FPack
{
	partial class FPack
	{

		public static HashSet<string> ToRegister = new();

		public static IGenericModConfigMenuApi? ConfigMenuAPI;
		public static IGMCMOptionsAPI? ConfigOptionAPI;
	
		#region PackConfig class

		private class PackConfig
		{
			JObject data_f = new();
			JObject data_p = new();

			Dictionary<string, bool> types = new();
			Dictionary<string, string> type_names = new();
			Dictionary<string, bool> i_packs = new();
			Dictionary<string, LoadData> i_pack_info = new();
			Dictionary<string, string> i_pack_names = new();

			public void SetData(JObject data)
			{
				JToken? f_tok = data.GetValue("Furniture");
				if (f_tok is JObject f_obj) data_f = f_obj;
				else data_f = new();

				JToken? p_tok = data.GetValue("Included");
				if (p_tok is JObject p_obj) data_p = p_obj;
				else data_p = new();
			}

			public void AddType(string type_id, string type_name)
			{
				type_names[type_id] = type_name;
				JToken? token = data_f.GetValue(type_id);
				if (token == null) types[type_id] = true;
				else types[type_id] = token.Value<bool>();
			}

			public bool IsTypeEnabled(string type_id)
			{
				return types[type_id];
			}

			public void AddIPack(LoadData info)
			{
				i_pack_info[info.DataUID] = info;
				i_pack_names[info.DataUID] = info.Name;
				JToken? token = data_p.GetValue(info.DataUID);
				if (token == null) i_packs[info.DataUID] = info.Enabled;
				else i_packs[info.DataUID] = token.Value<bool>();
			}

			public bool IsPackEnabled(string i_data_UID)
			{
				return i_packs[i_data_UID];
			}

			public JObject Save()
			{
				return new()
				{
					{ "Furniture", JObject.FromObject(types) },
					{ "Included", JObject.FromObject(i_packs) }
				};
			}

			public void Reset()
			{
				foreach (string type_id in types.Keys)
					types[type_id] = true;

				foreach(string i_data_UID in i_packs.Keys)
					i_packs[i_data_UID] = i_pack_info[i_data_UID].Enabled;
			}

			public void Register(IGenericModConfigMenuApi api, IManifest manifest)
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
							value => {
								if (types[type_id] != value)
									InvalidateGameData();
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
							value => {
								if (i_packs[i_data_UID] != value)
									InvalidateGameData();
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

		private void LoadConfig()
		{
			JObject? config_data = LoadData_.ContentPack.ReadJsonFile<JObject>(CONFIG_PATH);
			if (config_data == null) return;

			JToken? config_token = config_data.GetValue(DataUID);
			if (config_token is JObject config_obj)
				Config.SetData(config_obj);
		}

		private void SaveConfig()
		{
			if (Root != null) 
			{
				// only the root pack should save the config.
				Root.SaveConfig();
				return;
			}

			JObject? config_data = LoadData_.ContentPack.ReadJsonFile<JObject>(CONFIG_PATH);
			config_data ??= new();

			SaveConfig(config_data);

			string path = Path.Combine(LoadData_.ContentPack.DirectoryPath, CONFIG_PATH);
			File.WriteAllText(path, config_data.ToString());
		}

		private void SaveConfig(JObject config_data)
		{
			config_data[DataUID] = Config.Save();

			foreach (FPack sub_pack in IncludedPacks.Values)
				sub_pack.SaveConfig(config_data);
		}

		private void ResetConfig()
		{
			Config.Reset();
			foreach (FPack sub_pack in IncludedPacks.Values)
				sub_pack.ResetConfig();
		}

		private void UnregisterConfig()
		{
			if (ConfigMenuAPI is null) return;

			ConfigMenuAPI.Unregister(LoadData_.ContentPack.Manifest);
			ToRegister.Add(UID);
		}

		public static void RegisterPackConfig()
		{
			if (ConfigMenuAPI is null) return;

			foreach (string UID in ToRegister)
				PacksData[$"{UID}/{DEFAULT_PATH}"].RegisterConfig();
			
			ToRegister.Clear();
		}

		public void RegisterSubConfig()
		{
			if (ConfigMenuAPI is null) return;

			ConfigMenuAPI.AddPage(
				mod: LoadData_.ContentPack.Manifest,
				pageId: DataUID,
				pageTitle: () => $"{LoadData_.Name} Config"
			);

			if (LoadData_.Description != null && LoadData_.Description.Length > 0)
			{
				ConfigMenuAPI.AddParagraph(
					mod: LoadData_.ContentPack.Manifest,
					() => LoadData_.Description
				);
			}
		}

		private void RegisterConfig()
		{
			if (ConfigMenuAPI is null) return;

			IManifest manifest = LoadData_.ContentPack.Manifest;

			if (!IsIncluded)
			{
				ConfigMenuAPI.Register(
					manifest,
					() => {
						ResetConfig();
						InvalidateGameData();
					},
					SaveConfig
				);
				SaveConfig();
			}

			Config.Register(ConfigMenuAPI, manifest);

			foreach (FPack sub_pack in IncludedPacks.Values)
			{
				sub_pack.RegisterSubConfig();
				sub_pack.RegisterConfig();
			}
		}
	}
}