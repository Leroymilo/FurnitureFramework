

using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{
	class SeasonalTexture
	{
		Dictionary<Season, Texture2D> textures = new();

		public SeasonalTexture(IContentPack pack, string path, bool is_seasonal)
		{
			if (is_seasonal)
			{
				string extension = Path.GetExtension(path);
				string radical = path.Replace(extension, "");

				foreach (Season season in Enum.GetValues(typeof(Season)))
				{
					string? season_name = Enum.GetName(season);
					if (season_name is null) continue; // shouldn't happen
					string seasonal_path = $"{radical}_{season_name.ToLower()}{extension}";
					textures[season] = TextureManager.load(pack.ModContent, seasonal_path);
				}
			}
			else
			{
				Texture2D texture = TextureManager.load(pack.ModContent, path);
				foreach (Season season in Enum.GetValues(typeof(Season)))
				{
					textures[season] = texture;
				}
			}
		}

		public Texture2D get_texture()
		{
			return textures[Game1.season];
		}
	}
}