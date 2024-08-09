using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace FurnitureFramework
{
	class DynaTexture
	{
		string radical;
		string extension;
		bool seasonal;
		bool weather_based;

		public string base_name
		{
			get => $"{radical}{extension}";
		}

		public DynaTexture(FurnitureType type, string path, bool seasonal, bool weather_based)
		{
			path = $"{type.mod_id}/{path}";
			extension = Path.GetExtension(path);
			radical = path.Replace(extension, "");
			this.seasonal = seasonal;
			this.weather_based = weather_based;
		}

		public Texture2D get()
		{
			Texture2D? result = null;

			string? season = Enum.GetName(Game1.season);
			string? weather = Game1.currentLocation?.GetWeather()?.weather.Value;

			if (seasonal && season is not null)
			{
				if (weather_based && weather is not null)
				{
					try_load($"{radical}_{season}_{weather}{extension}", ref result);
				}
				if (result is null)	// else or load failure
				{
					try_load($"{radical}_{season}{extension}", ref result);
				}
			}
			if (result is null)	// else or load failure
			{
				if (weather_based && weather is not null)
				{
					try_load($"{radical}_{weather}{extension}", ref result);
				}
				if (result is null)	// else or load failure
				{
					try_load(base_name, ref result);
				}
			}
			
			if (result is null)
				throw new NullReferenceException($"Could not find texture {base_name} or any valid variation.");
			return result;
		}

		private bool try_load(string name, [MaybeNullWhen(false)] ref Texture2D? texture)
		{
			try
			{
				texture = ModEntry.get_helper().GameContent.Load<Texture2D>(name);
				return true;
			}
			catch (Microsoft.Xna.Framework.Content.ContentLoadException)
			{
				texture = null;
				return false;
			}
		}
	}

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