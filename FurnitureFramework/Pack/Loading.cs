using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Pack
{

	partial class FurniturePack
	{	

		#region Load

		static Stack<string> to_load = new();
		// Stack of data_UIDs of packs to load. It's a Stack to ensure that included packs are loaded along their root.

		public static void pre_load(IModHelper helper)
		{
			foreach (IContentPack c_pack in helper.ContentPacks.GetOwned())
			{
				FurniturePack pack = new(c_pack);
				to_load.Append(pack.data_UID);
				packs[pack.data_UID] = pack;
				UIDs.Add(pack.UID, c_pack);
			}
			
			invalidate_game_data();
		}

		public static void load_all()
		{
			if (to_load.Count == 0) return;
			
			while (to_load.Count > 0)
			{
				string data_UID = to_load.Pop();
				packs[data_UID].load();
			}

			register_pack_config();
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
			if (is_loaded) return;

			ModEntry.log($"Loading {data_UID}...");

			JObject data;
			try
			{
				data = ModEntry.get_helper().GameContent.Load<JObject>($"FF/{data_UID}");
			}
			catch (ContentLoadException ex)
			{
				ModEntry.log($"Could not load {data_UID}, skipping Furniture Pack:\n{ex}", LogLevel.Error);
				return;
			}

			if (!is_included)
				if (!check_format(data)) return;

			load_config();

			load_furniture(data);

			load_included(data);

			to_register.Add(UID);

			is_loaded = true;

			if (types.Count == 0 && included_packs.Count == 0)
			{
				ModEntry.log("This Furniture Pack is empty!", LogLevel.Warn);
				return;
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
						f_obj, new_types
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
				config.add_type(type.info.id, type.info.display_name);
				types[type.info.id] = type;
			}
		}

		private void load_included(JObject data)
		{
			if (data.GetValue("Included") is not JObject includes_obj) return;

			foreach (JProperty property in includes_obj.Properties())
			{
				IncludedPack i_pack = new(content_pack, property, root ?? this);
				string i_data_UID = i_pack.data_UID;

				if (i_pack.is_valid)
				{	
					included_packs.Add(i_data_UID, i_pack);
					packs[i_pack.data_UID] = i_pack.pack;
					
					config.add_i_pack(i_data_UID, i_pack.name, i_pack.default_enabled);
				}
				else
				{
					ModEntry.log($"Issue parsing included pack {i_pack.name} in {data_UID}:", LogLevel.Warn);
					ModEntry.log($"\t{i_pack.error_msg}", LogLevel.Warn);
				}
			}
		}

		#endregion

		#region Invalidate

		public static void invalidate(string name)
		{
			if (!name.StartsWith("FF/")) return;
			name = name[3..];

			if (packs.ContainsKey(name))
				packs[name].invalidate();
		}

		private void invalidate()
		{
			if (!is_loaded) return;

			clear(cascade: false);
			unregister_config();

			ModEntry.log($"Invalidated {data_UID}", LogLevel.Trace);
			is_loaded = false;
		}

		private void clear(bool cascade)
		{
			(root ?? this).save_config();

			if (cascade)
			{
				foreach (IncludedPack i_pack in included_packs.Values)
					i_pack.clear();
			}

			types.Clear();
			included_packs.Clear();
			config.clear();

			to_load.Append(data_UID);

			invalidate_game_data();
		}

		#endregion

		#region Reload

		public static void reload_pack(string command, string[] args)
		{
			if (args.Count() == 0) reload_all();
			else reload_single(args[0]);
			
			invalidate_game_data();

			load_all();
		}

		private static void reload_all()
		{
			foreach (string UID in UIDs.Keys)
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

			packs[data_UID].reload();
		}

		private void reload()
		{
			if (!is_loaded) return;

			clear(cascade: true);
			unregister_config();

			is_loaded = false;

			to_load.Append(data_UID);
		}

		#endregion

	}
}