using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Pack
{

	partial class FurniturePack
	{	

		#region Initial Load

		public static void pre_load(IModHelper helper)
		{
			foreach (IContentPack c_pack in helper.ContentPacks.GetOwned())
			{
				string data_UID = $"{c_pack.Manifest.UniqueID}/{DEFAULT_PATH}";
				to_load.Add(data_UID);
				packs[data_UID] = new(c_pack);
			}
		}

		public static void load_all()
		{
			while (to_load.Count > 0)
			{
				string data_UID = to_load.First();
				to_load.Remove(data_UID);
				packs[data_UID].load();
			}
			
			ModEntry.log("Finished loading Furniture Types.");
			IGameContentHelper helper = ModEntry.get_helper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
		}

		private FurniturePack(IContentPack c_pack, string path = DEFAULT_PATH)
		{
			content_pack = c_pack;
			UID = content_pack.Manifest.UniqueID;
			this.path = path;

			is_included = path != DEFAULT_PATH;
		}

		private void load()
		{
			ModEntry.log($"Loading {data_UID}...");

			JObject data;
			try
			{
				data = ModEntry.get_helper().GameContent.Load<JObject>(data_UID);
			}
			catch (ContentLoadException ex)
			{
				ModEntry.log($"Could not load {data_UID}, skipping Furniture Pack:\n{ex}", LogLevel.Error);
				return;
			}

			#region Read Content

			if (!is_included) { if (!check_format(data)) return; }

			load_furniture(data);

			load_included(data);

			#endregion

			if (types.Count == 0 && included_packs.Count == 0)
			{
				ModEntry.log("This Furniture Pack is empty!", LogLevel.Warn);
				return;
			}

			#region Config

			if (!is_included)
			{
				JObject? config_data = null;
				try
				{
					config_data = content_pack.ModContent.Load<JObject>(CONFIG_PATH);
				}
				catch (ContentLoadException)
				{
					save_config();
				}

				if (config_data is not null)
					read_config(config_data);
			}

			#endregion

			// Adding the valid Pack to the map of data_UID for reloading purposes
			if (!data_UIDs.ContainsKey(UID))
				data_UIDs.Add(UID, new());
			data_UIDs[UID].Add(data_UID);
			
		}

		private bool check_format(JObject data)
		{
			JToken? format_token = data.GetValue("Format");
			if (format_token is null || format_token.Type != JTokenType.Integer)
			{
				ModEntry.log("Missing Format, skipping Furniture Pack.", LogLevel.Error);
				return false;
			}

			int format = -1;
			if (!JsonParser.try_parse(data.GetValue("Format"), ref format))
			{
				ModEntry.log("Missing or invalid Format, skipping Furniture Pack.", LogLevel.Error);
				return false;
			}

			switch (format)
			{
				case > FORMAT:
				case < 1:
					ModEntry.log($"Invalid Format: {format}, skipping Furniture Pack.", LogLevel.Error);
					return false;
				case < FORMAT:
					ModEntry.log($"Format {format} is outdated, skipping Furniture Pack.", LogLevel.Error);
					ModEntry.log("If you are a user, wait for an update for this Furniture Pack,", LogLevel.Info);
					ModEntry.log($"or use a version of the Furniture Framework starting with {format}.", LogLevel.Info);
					ModEntry.log("If you are the author, check the Changelogs in the documentation to update your Pack.", LogLevel.Info);
					return false;
				case FORMAT: return true;
			}
		}

		private void load_furniture(JObject data)
		{
			if (data.GetValue("Furniture") is not JObject furn_obj) return;

			List<Type.FurnitureType> new_types = new();
			foreach (JProperty f_prop in furn_obj.Properties())
			{
				if (f_prop.Value is not JObject f_obj)
				{
					ModEntry.log($"No data for Furniture \"{f_prop.Name}\" in {data_UID}, skipping entry.", LogLevel.Warn);
					continue;
				}

				try
				{
					Type.FurnitureType.make_furniture(
						content_pack, f_prop.Name,
						f_obj,
						new_types
					);
				}
				catch (Exception ex)
				{
					ModEntry.log(ex.ToString(), LogLevel.Error);
					ModEntry.log($"Failed to load data for Furniture \"{f_prop.Name}\" in {data_UID}, skipping entry.", LogLevel.Warn);
					continue;
				}
			}

			foreach (Type.FurnitureType type in new_types)
			{
				string type_id = type.info.id;
				types[type_id] = type;

				int prev_prio = get_current_priority(type_id);
				if (prev_prio > type.info.priority)
					continue;
				else if (prev_prio == type.info.priority)
				{
					if (!conflicts.ContainsKey(type_id))
						conflicts[type_id] = new() {static_types[type_id]};
					conflicts[type_id].Add(data_UID);
					continue;
				}
				else // prev_prio < type.info.priority
				{
					conflicts.Remove(type_id);
					static_types[type_id] = data_UID;
				}

			}
		}

		private void load_included(JObject data)
		{
			if (data.GetValue("Included") is not JObject includes_obj) return;

			HashSet<string> added_i_packs = new();

			foreach (JProperty property in includes_obj.Properties())
			{
				IncludedPack i_pack = new(content_pack, property);
				string i_data_UID = i_pack.data_UID;

				if (i_pack.is_valid)
				{
					added_i_packs.Add(i_data_UID);
					if (included_packs.ContainsKey(i_data_UID))
						continue;
					
					included_packs.Add(i_data_UID, i_pack);
					i_pack.add_pack();	// Queue pack loading
				}
				else
				{
					ModEntry.log($"Issue parsing included pack {i_pack.name} in {data_UID}:", LogLevel.Warn);
					ModEntry.log($"\t{i_pack.error_msg}", LogLevel.Warn);
				}
			}

			foreach (string i_data_UID in included_packs.Keys)
			{
				if (added_i_packs.Contains(i_data_UID)) continue;
				included_packs[i_data_UID].clear();
				included_packs.Remove(i_data_UID);
			}
		}
	
		#endregion

		#region Reload

		public static void reload_pack(string command, string[] args)
		{
			if (args.Count() == 0) reload_all();
			else reload_single(args[0]);
			
			IGameContentHelper helper = ModEntry.get_helper().GameContent;
			helper.InvalidateCache("Data/Furniture");
			helper.InvalidateCache("Data/Shops");
		}

		private static void reload_all()
		{
			foreach (string UID in data_UIDs.Keys)
				reload_single(UID);
		}

		private static void reload_single(string UID)
		{
			packs[$"{UID}/{DEFAULT_PATH}"].reload(true);
		}

		private void reload(bool cascade = false)
		{
			clear(cascade);

			to_load.Add(data_UID);
			load_all();
		}

		private void clear(bool cascade = false)
		{
			foreach (string type_id in types.Keys)
			{
				if (static_types[type_id] == data_UID)
					static_types.Remove(type_id);
				
				if (conflicts.ContainsKey(type_id))
				{
					conflicts[type_id].Remove(data_UID);
					if (conflicts[type_id].Count == 0)
						conflicts.Remove(type_id);
				}
			}

			types.Clear();

			if (cascade)
			{
				foreach (IncludedPack i_pack in included_packs.Values)
					i_pack.clear();
			}
		}

		#endregion

	}
}