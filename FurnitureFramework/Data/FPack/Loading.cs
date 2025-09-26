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
				if (Parent != null)
				{
					if (Parent.IsIncluded)
						result.Root = Parent.Root;
					else
						result.Root = Parent;
				}
				else ModEntry.Log($"Success!", LogLevel.Debug);

				return result;
			}
		}

		#endregion

		#region Load

		static Stack<LoadData> ToLoad = new();

		public static void PreLoad(IModHelper helper)
		{
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

				ToRegister.Add(data.UID);
			}

			RegisterPackConfig();
		}

		#endregion

		#region Clearing

		public static bool InvalidateAsset(IAssetName name)
		{
			if (!name.StartsWith("FF")) return false;
			if (!TryGetCPFromResource(name, out IContentPack? c_pack))return false;
			return ReloadSingle(c_pack.Manifest.UniqueID);
		}

		private static bool ReloadSingle(string UID)
		{
			string data_UID = $"{UID}/{DEFAULT_PATH}";

			if (!PacksData.ContainsKey(data_UID))
			{
				ModEntry.Log($"Pack {UID} does not exist!", LogLevel.Warn);
				return false;
			}

			return PacksData[data_UID].QueueReload();
		}

		private bool QueueReload()
		{
			if (ToLoad.Contains(LoadData_))
			{
				ModEntry.Log("Already queued!");
				return false;
			}
			ToLoad.Push(LoadData_);
			UnregisterConfig();
			InvalidateRelatedAssets();
			return true;
		}

		private void InvalidateRelatedAssets()
		{
			if (IsIncluded) return;

			// Invalidate game assets attached to this pack:
			foreach (string asset_name in LoadedAssets[UID])
				ModEntry.GetHelper().GameContent.InvalidateCache(asset_name);

			ModEntry.Log($"Invalidated assets from {UID}.", LogLevel.Trace);
		}

		#endregion
	}
}