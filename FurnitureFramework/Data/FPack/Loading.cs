using Newtonsoft.Json;
using StardewModdingAPI;

namespace FurnitureFramework.Data.FPack
{
	partial class FPack
	{

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

			public FPack Load()
			{
				if (ContentPack == null) throw new Exception("Content Pack not set before loading Pack!");

				FPack result = ModEntry.GetHelper().GameContent.Load<FPack>("FF/"+DataUID);
				result.SetSource(this);

				PacksData[DataUID] = result;
				if (Parent == null) ModEntry.Log($"Success!", LogLevel.Debug);

				return result;
			}
		}

		#endregion

		#region Load

		static Stack<LoadData> ToLoad = new();

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
				ToLoad.Push(new LoadData(c_pack));
				ContentPacks.Add(c_pack.Manifest.UniqueID, c_pack);
			}
			
			InvalidateGameData();
		}

		public static void LoadAll()
		{
			if (ToLoad.Count == 0) return;
			
			ModEntry.Log($"Loading {ToLoad.Count} Furniture Packs...", LogLevel.Info);

			while (ToLoad.Count > 0)
			{
				LoadData load_data = ToLoad.Pop();
				ModEntry.Log($"Loading {load_data.DataUID}...");
				FPack? data;
				try { data = load_data.Load(); }
				catch (Exception e)
				{
					ModEntry.Log($"Failed, skipping pack.\n{e}", LogLevel.Error);
					data = null;
				}
				if (data == null)
				{
					load_data.Parent?.Included.Remove(load_data.Name);
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