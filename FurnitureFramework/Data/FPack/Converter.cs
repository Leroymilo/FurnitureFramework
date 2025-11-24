using FurnitureFramework.Data.FType;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewModdingAPI;


// Converts packs from older versions
namespace FurnitureFramework.Data.FPack
{
	public struct ConversionInfo
	{
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
		[JsonIgnore]
		public new Dictionary<string, OldType> Furniture = new();
		[JsonIgnore]
		public new Dictionary<string, OldPack> IncludedPacks = new();

		public void Convert(ConversionInfo? info = null)
		{
			info ??= new(LoadData_.ContentPack);

			foreach (string key in Furniture.Keys.ToList())
			{
				
			}

			// Recursive included pack conversion?
			// Or convert them when loaded in FPack.SetSource?
		}
	}
}