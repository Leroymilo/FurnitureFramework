using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Pack
{

	partial class FurniturePack
	{	

		#region Load

		public static void pre_load(IModHelper helper)
		{
			foreach (IContentPack c_pack in helper.ContentPacks.GetOwned())
			{
				FurniturePack pack = new(c_pack);
				to_load.Append(pack.data_UID);
				packs[pack.data_UID] = pack;
			}
		}

		public static void load_all()
		{
			if (to_load.Count == 0) return;
			
			while (to_load.Count > 0)
			{
				string data_UID = to_load.Pop();
				packs[data_UID].load();
			}
			
			invalidate_game_data();
		}

		private FurniturePack(IContentPack c_pack)
		{
			content_pack = c_pack;
		}

		private FurniturePack(IContentPack c_pack, string data_path, FurniturePack root)
		{
			content_pack = c_pack;
			path = data_path;
			this.root = root;
		}

		private void load(bool enabled = true)
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

			if (!is_included)
				if (!check_format(data)) return;

			load_furniture(data);

			load_included(data);

			if (types.Count == 0 && included_packs.Count == 0)
			{
				ModEntry.log("This Furniture Pack is empty!", LogLevel.Warn);
				return;
			}
			
			if (update_config)
			{
				config_menu_api?.Unregister(content_pack.Manifest);
				register_config();
			}
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

			HashSet<string> added_types = new();

			foreach (Type.FurnitureType type in new_types)
			{
				string type_id = type.info.id;

				if (types.ContainsKey(type_id))
				{
					int prev_prio = types[type_id].info.priority;
					if (prev_prio != type.info.priority)
					{
						// move this pack's data_UID in static_types
						remove_conflict(type_id, prev_prio, data_UID);
					}
				}
				else
				{
					config.add_type(type_id, type.info.display_name);
					update_config = true;
				}

				added_types.Add(type_id);
				types[type_id] = type;	// replace old type anyway
				add_conflict(type_id, type.info.priority, data_UID);
			}

			foreach (string type_id in types.Keys)
			{
				if (added_types.Contains(type_id)) continue;
				types.Remove(type_id);
				remove_conflict(type_id, types[type_id].info.priority, data_UID);
				
				config.remove_type(type_id);
				update_config = true;
			}
		}

		private void load_included(JObject data)
		{
			if (data.GetValue("Included") is not JObject includes_obj) return;

			HashSet<string> added_i_packs = new();

			foreach (JProperty property in includes_obj.Properties())
			{
				IncludedPack i_pack = new(content_pack, property, root ?? this);
				string i_data_UID = i_pack.data_UID;

				if (i_pack.is_valid)
				{
					added_i_packs.Add(i_data_UID);
					if (included_packs.ContainsKey(i_data_UID))
						continue;
					
					included_packs.Add(i_data_UID, i_pack);
					i_pack.add_pack();	// Queue pack loading
					
					config.add_i_pack(i_data_UID, i_pack.name, i_pack.default_enabled);
					update_config = true;
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

				config.remove_i_pack(i_data_UID);
				update_config = true;
			}
		}

		private static void add_conflict(string type_id, int priority, string data_UID)
		{
			string? prev_result = get_type_source(type_id);

			if (!static_types.ContainsKey(type_id))
				static_types.Add(type_id, new());
			
			if (!static_types[type_id].ContainsKey(priority))
				static_types[type_id].Add(priority, new());
			
			static_types[type_id][priority].Add(data_UID);

			if (prev_result != get_type_source(type_id))
				update_game_data = true;
		}

		#endregion

		#region Unload



		private static void remove_conflict(string type_id, int priority, string data_UID)
		{
			string? prev_result = get_type_source(type_id);

			static_types[type_id][priority].Remove(data_UID);

			if (static_types[type_id][priority].Count == 0)
				static_types[type_id].Remove(priority);
			
			if (static_types[type_id].Count == 0)
				static_types.Remove(type_id);

			if (prev_result != get_type_source(type_id))
				update_game_data = true;
		}

		#endregion

		#region Reload

		public static void reload_pack(string command, string[] args)
		{
			if (args.Count() == 0) reload_all();
			else reload_single(args[0]);
			
			if (update_game_data) invalidate_game_data();
		}

		private static void reload_all()
		{
			foreach (string UID in UIDs)
				reload_single(UID);
		}

		private static void reload_single(string UID)
		{
			string data_UID = $"{UID}/{DEFAULT_PATH}";

			if (!packs.ContainsKey(data_UID))
			{
				ModEntry.log($"Pack {UID} does not exist!", LogLevel.Warn);
				return;
			}

			packs[data_UID].reload(true);
		}

		private void reload(bool cascade = false)
		{
			if (cascade) clear();

			to_load.Append(data_UID);
			load_all();
		}

		private void clear()
		{
			foreach (string type_id in types.Keys)
			{
				remove_conflict(type_id, types[type_id].info.priority, data_UID);
				config.remove_type(type_id);
			}

			types.Clear();

			foreach (IncludedPack i_pack in included_packs.Values)
				i_pack.clear();
			
			included_packs.Clear();
		}

		#endregion

	}
}