using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace FurnitureFramework
{
	static class TextureManager
	{
		public static readonly Dictionary<string, Texture2D> textures = new();

		public static Texture2D load(IModContentHelper content_helper, string path)
		{

			string true_path;
			if (path.StartsWith("FF."))
			{
				content_helper = ModEntry.get_helper().ModContent;
				true_path = path[3..];
			}
			else
			{
				true_path = path;
			}

			if (!textures.ContainsKey(path))
			{
				textures[path] = content_helper.Load<Texture2D>(true_path);
				ModEntry.log($"loading texture at {true_path}", LogLevel.Trace);
			}
			return textures[path];
		}
	}
}