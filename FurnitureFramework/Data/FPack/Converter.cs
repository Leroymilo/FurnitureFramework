using FurnitureFramework.Data.FType;
using Newtonsoft.Json;
using StardewModdingAPI;


// Converts packs from older versions
namespace FurnitureFramework.Data.FPack
{
	public struct ConversionInfo
	{
		static Dictionary<string, string> NewPaths = new();
		// Store the target path for packs that have been converted so that
		// included packs are saved in the same place when they are converted.
		static Dictionary<string, string> NewCPPaths = new();
		// Same thing but for CP Packs that were generated when necessary.

		string PackPath;
		IManifest Manifest;
		List<string> SeasonalTextures = new();
		
		public ConversionInfo(IContentPack c_pack)
		{
			PackPath = c_pack.DirectoryPath;
			Manifest = c_pack.Manifest;
		}
	}

	public class OldPack : BasePack
	{
		public new Dictionary<string, OldType> Furniture = new();
		[JsonIgnore]
		public new Dictionary<string, OldPack> IncludedPacks = new();

		public void Convert(string path, ConversionInfo? info = null)
		{
			return;
			info ??= new();

			FPack result = new();

			foreach (string key in Furniture.Keys.ToList())
			{
				OldType f_type = Furniture[key];
				string new_key = key;
				if (f_type is FF2Type)
					new_key = FF2Type.ReplaceTokens(key);

				result.Furniture[new_key] = f_type.Convert(info.Value);
			}
		}
	}
}