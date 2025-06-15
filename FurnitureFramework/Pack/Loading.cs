using FurnitureFramework.Data.FType;
using Microsoft.Xna.Framework.Content;
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
			default_pack = helper.ContentPacks.CreateTemporary(
				helper.DirectoryPath,
				"leroymilo.FurnitureFramework.DefaultPack",
				"FF Default Pack",
				"An empty Furniture Pack coming with FF and can be edited with CP.",
				"leroymilo",
				new SemanticVersion("3.1")
			);

			foreach (IContentPack c_pack in helper.ContentPacks.GetOwned().Append(default_pack))
			{
				FurniturePack pack = new(c_pack);
				to_load.Push(pack.DataUID);
				packs[pack.DataUID] = pack;
				UIDs.Add(pack.UID, c_pack);
			}
			
			invalidate_game_data();
		}

		public static void load_all()
		{
			if (to_load.Count == 0) return;
			
			ModEntry.log($"Loading {to_load.Count} Furniture Packs...", LogLevel.Info);

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

			try
			{
				data = ModEntry.get_helper().GameContent.Load<Data.FurniturePack>($"FF/{DataUID}");
			}
			catch (ContentLoadException ex)
			{
				ModEntry.log($"Could not load {DataUID}, skipping Furniture Pack:\n{ex}", LogLevel.Error);
				return;
			}

			if (!is_included)
				if (!check_format(data.Format)) return;
			
			load_config();

			load_furniture(data.Furniture);

			load_included(data.Included);

			to_register.Add(UID);

			ModEntry.log($"Loaded {DataUID}!", LogLevel.Info);

			is_loaded = true;

			if (types.Count == 0 && included_packs.Count == 0)
			{
				ModEntry.log("This Furniture Pack is empty!", LogLevel.Warn);
				return;
			}
		}

		private bool check_format(int format)
		{
			switch (format)
			{
				case 0:
					ModEntry.log("Missing Format, skipping Furniture Pack.", LogLevel.Error);
					return false;
				case > FORMAT:
				case < 1:
					ModEntry.log($"Invalid Format for {DataUID}: {format}, skipping Furniture Pack.", LogLevel.Error);
					return false;
				case < FORMAT:
					ModEntry.log($"Format {format} for {DataUID} is outdated, skipping Furniture Pack.", LogLevel.Error);
					ModEntry.log("If you are a user, wait for an update for this Furniture Pack,", LogLevel.Info);
					ModEntry.log($"or use a version of the Furniture Framework starting with {format}.", LogLevel.Info);
					ModEntry.log("If you are the author, check the Changelogs in the documentation to update your Pack.", LogLevel.Info);
					return false;
				case FORMAT: return true;
			}
		}

		private void load_furniture(Dictionary<string, Data.FType.FType> furniture)
		{
			foreach (KeyValuePair<string, Data.FType.FType> pair in furniture)
			{
				pair.Value.SetIDs(UID, pair.Key);

				foreach (Variant variant in pair.Value.Variants.Values)
				{
					config.add_type(variant.ID, variant.DisplayName);
					types[variant.ID] = pair.Value;
				}
			}
		}

		private void load_included(Dictionary<string, Data.IncludedPack> included)
		{
			foreach (KeyValuePair<string, Data.IncludedPack> data in included)
			{
				IncludedPack i_pack = new(content_pack, data.Key, data.Value, root ?? this);
				string i_data_UID = i_pack.data_UID;

				if (i_pack.is_valid)
				{	
					included_packs.Add(i_data_UID, i_pack);
					packs[i_pack.data_UID] = i_pack.pack;
					
					config.add_i_pack(i_data_UID, i_pack.name, data.Value);

					to_load.Push(i_data_UID);
				}
				else
				{
					ModEntry.log($"Issue parsing included pack {i_pack.name} in {DataUID}:", LogLevel.Warn);
					ModEntry.log($"\t{i_pack.error_msg}", LogLevel.Warn);
				}
			}
		}

		#endregion

		#region Reload

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
		}

		public static bool invalidate_asset(IAssetName name)
		{
			if (!name.StartsWith("FF")) return false;
			if (!try_get_cp_from_resource(name, out IContentPack? c_pack)) return false;
			return reload_single(c_pack.Manifest.UniqueID);
		}

		public static void reload_pack(string command, string[] args)
		{
			if (args.Count() == 0) reload_all();
			else reload_single(args[0]);
		}

		private static void reload_all()
		{
			bool reloaded = false;
			foreach (string UID in UIDs.Keys)
				reloaded |= reload_single(UID);
			
			if (reloaded) invalidate_game_data();
		}

		private static bool reload_single(string UID)
		{
			string data_UID = $"{UID}/{DEFAULT_PATH}";

			if (!packs.ContainsKey(data_UID))
			{
				ModEntry.log($"Pack {UID} does not exist!", LogLevel.Warn);
				return false;
			}

			return packs[data_UID].reload();
		}

		private bool reload()
		{
			if (!is_loaded) return false;

			ModEntry.log($"Reloading {DataUID}...");

			clear(cascade: true);
			unregister_config();

			is_loaded = false;

			// Invalidate game assets attached to this pack:
			foreach (string asset_name in loaded_assets[UID])
				ModEntry.get_helper().GameContent.InvalidateCache(asset_name);

			// Log looks like this for each <asset_name>, not sure it's good:
			// [16:53:42 TRACE Furniture Framework] Requested cache invalidation for '<asset_name>'.
			// [16:53:42 TRACE SMAPI] Furniture Framework loaded asset '<asset_name>'.
			// [16:53:42 TRACE SMAPI] Invalidated 1 asset names (<asset_name>).
			// Propagated 1 core assets (<asset_name>).

			ModEntry.log($"Invalidated assets from {UID}.");

			to_load.Push(DataUID);
			return true;
		}

		#endregion

	}
}