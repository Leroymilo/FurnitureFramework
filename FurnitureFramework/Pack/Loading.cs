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
				to_load.Enqueue(
					data_UID,
					get_priority(c_pack.Manifest)
				);
				packs[data_UID] = new(c_pack);
			}
		}

		private static int get_priority(IManifest manifest)
		{
			manifest.ExtraFields.TryGetValue("Priority", out object? prio_obj);
			if (prio_obj == null) return DEFAULT_PRIO;	// no need to log error if no priority

			if (prio_obj is int prio_int)
			{
				return prio_int;
			}
			else
			{
				ModEntry.log($"Invalid value for Priority in manifest of {manifest.UniqueID}, defaulting to {DEFAULT_PRIO}.", LogLevel.Warn);
				return DEFAULT_PRIO;
			}
		}

		public static void load_all()
		{
			while (to_load.Count > 0)
			{
				FurniturePack pack = packs[to_load.Dequeue()];
				pack.load();
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
				ModEntry.log($"Could not load {data_UID}:\n{ex}", LogLevel.Error);
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

			// Adding the valid Pack to the map of data_UID (can't remember why)
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

			List<Type.FurnitureType> read_types = new();
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
						read_types
					);
				}
				catch (Exception ex)
				{
					ModEntry.log(ex.ToString(), LogLevel.Error);
					ModEntry.log($"Failed to load data for Furniture \"{f_prop.Name}\" in {data_UID}, skipping entry.", LogLevel.Warn);
					continue;
				}
			}

			types.Clear();
			shops.Clear();
			foreach (Type.FurnitureType type in read_types)
			{
				types[type.info.id] = type;
				type_ids[type.info.id] = data_UID;

				if (type.shop_id != null)
				{
					if (!shops.ContainsKey(type.shop_id))
						shops[type.shop_id] = new();
				}

				foreach (string shop_id in type.shops)
				{
					if (!shops.ContainsKey(shop_id))
						shops[shop_id] = new();
					shops[shop_id].Add(type.info.id);
				}
			}
		}

		private void load_included(JObject data)
		{
			if (data.GetValue("Included") is not JObject includes_obj) return;
			
			included_packs.Clear();

			foreach (JProperty property in includes_obj.Properties())
			{
				IncludedPack included_pack = new(content_pack, property);
				if (included_pack.is_valid) included_packs.Add(included_pack);
				else
				{
					ModEntry.log($"Issue parsing included pack {included_pack.name} in {data_UID}:", LogLevel.Warn);
					ModEntry.log($"\t{included_pack.error_msg}", LogLevel.Warn);
				}
			}
		}
	}

	#endregion

	#region Re-Load

	// TO DO

	#endregion

}