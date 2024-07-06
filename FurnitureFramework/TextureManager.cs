using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace FurnitureFramework
{
	static class TextureManager
	{
		public static readonly Dictionary<string, Texture2D> textures = new();

		public static Texture2D load(IModContentHelper pack_helper, string path)
		{
			if (textures.ContainsKey(path)) return textures[path];
			
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
			textures[path] = result;
			return result;
		}
	}
}