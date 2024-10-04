using System.Runtime.Versioning;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace FurnitureFramework.Type
{
	[RequiresPreviewFeatures]
	class DynaTexture
	{
		string path;

		[RequiresPreviewFeatures]
		public DynaTexture(TypeInfo info, string path)
		{
			this.path = $"{info.mod_id}/{path}";
		}

		public Texture2D get()
		{
			try
			{
				return ModEntry.get_helper().GameContent.Load<Texture2D>(path);
			}
			catch (Microsoft.Xna.Framework.Content.ContentLoadException)
			{
				throw new NullReferenceException($"Could not find texture {path}.");
			}
		}
	}

	[RequiresPreviewFeatures]
	static class TextureManager
	{

		public static Texture2D base_load(IModContentHelper pack_helper, string path)
		{
			Texture2D result;

			if (path.StartsWith("FF/"))
			{
				result = ModEntry.get_helper().ModContent.Load<Texture2D>(path[3..]);
			}
			else if (path.StartsWith("Content/"))
			{
				string fixed_path = Path.ChangeExtension(path[8..], null);
				result = ModEntry.get_helper().GameContent.Load<Texture2D>(fixed_path);
			}
			else result = pack_helper.Load<Texture2D>(path);

			ModEntry.log($"loaded texture at {path}", LogLevel.Trace);
			return result;
		}
	}
}