using Newtonsoft.Json;
using StardewModdingAPI;

namespace FurnitureFramework.Data.FPack
{
	partial class BasePack
	{

		protected static HashSet<LoadData> ToLoad = new();

		#region LoadData class

		public class LoadData
		{
			[JsonIgnore]
			public string Name;

			public string Path = DEFAULT_PATH;
			public string Description = "";
			public bool Enabled = true;

			[JsonIgnore]
			public IContentPack ContentPack;
			[JsonIgnore]
			public FPack? Parent;

			[JsonIgnore]
			public string UID { get => ContentPack.Manifest.UniqueID; }
			[JsonIgnore]
			public string DataUID { get => $"{UID}/{Path}"; }

			// Creates LoadData from ContentPack info, for a root Pack
			public LoadData(IContentPack? c_pack)
			{
				if (c_pack == null) {return;}
				ContentPack = c_pack;
				Name = c_pack.Manifest.Name;
			}

			public FPack? Load()
			{
				if (ContentPack == null) throw new Exception("Content Pack not set before loading Pack!");

				FPack result = ModEntry.GetHelper().GameContent.Load<FPack>("FF/"+DataUID);

				if (result is InvalidPack invalid_result)
				{
					invalid_result.Log();
					return null;
				}
				
				result.SetSource(this);

				PacksData[DataUID] = result;
				if (Parent == null) ModEntry.Log($"Success!", LogLevel.Debug);

				return result;
			}

			public bool IsAncestorQueued()
			{
				FPack? prev = Parent;
				while (prev != null)
				{
					if (ToLoad.Contains(prev.LoadData_)) return true;
					prev = prev.LoadData_.Parent;
				}
				return false;
			}
		}

		#endregion
	}

	partial class FPack
	{

		#region Load

		public static void PreLoad(IModHelper helper)
		{
			ModEntry.Log("Preloading Furniture Packs...");
			DefaultPack = helper.ContentPacks.CreateTemporary(
				helper.DirectoryPath,
				"leroymilo.FurnitureFramework.DefaultPack",
				"FF Default Pack",
				"An empty Furniture Pack coming with FF and can be edited with CP.",
				"leroymilo",
				new SemanticVersion("3.1.1")
			);

			foreach (IContentPack c_pack in helper.ContentPacks.GetOwned().Append(DefaultPack))
			{
				ToLoad.Add(new LoadData(c_pack));
				ContentPacks.Add(c_pack.Manifest.UniqueID, c_pack);
			}
			
			InvalidateGameData();
		}

		public static void LoadAll()
		{
			int count = ToLoad.Count;
			if (count == 0) return;

			Queue<LoadData> queue = new();

			// Removing recursive loads
			foreach (LoadData load_data in ToLoad)
				if (!load_data.IsAncestorQueued()) queue.Enqueue(load_data);
			ToLoad.Clear();
			
			ModEntry.Log($"Loading {queue.Count} Furniture Packs...", LogLevel.Info);

			while (queue.Count > 0)
			{
				LoadData load_data = queue.Dequeue();
				ModEntry.Log($"Loading {load_data.ContentPack.Manifest.Name} ({load_data.DataUID})...");
				FPack? data = load_data.Load();
				if (data == null)
				{
					load_data.Parent?.Included.Remove(load_data.Name);
					load_data.Parent?.IncludedPacks.Remove(load_data.DataUID);
					continue;
				}

				if (load_data.Parent != null)
					load_data.Parent.IncludedPacks[data.DataUID] = data;

				data.UnregisterConfig();
			}

			RegisterPackConfig();
		}

		#endregion

		#region Clearing

		private static void ReloadSingle(string UID)
		{
			string data_UID = $"{UID}/{DEFAULT_PATH}";

			if (PacksData.TryGetValue(data_UID, out FPack? f_pack))
				f_pack.InvalidateRelatedAssets();
			else ModEntry.Log($"Pack {UID} does not exist!", LogLevel.Warn);
		}

		private void InvalidateRelatedAssets()
		{
			// Invalidate game assets attached to this pack:
			ModEntry.GetHelper().GameContent.InvalidateCache(
				// Invalidates all associated texture files, but only the base content file
				asset_info => LoadedAssets[UID].Contains(asset_info.Name.Name) && (
					asset_info.DataType != typeof(FPack) || asset_info.Name.IsEquivalentTo($"FF/{DataUID}")	
				)
			);
			ModEntry.Log($"Invalidated assets from {UID}.", LogLevel.Trace);
		}

		#endregion
	}
}