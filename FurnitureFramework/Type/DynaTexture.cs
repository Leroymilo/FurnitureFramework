using System.Runtime.Versioning;
using Microsoft.Xna.Framework.Graphics;

namespace FurnitureFramework.Type
{
	[RequiresPreviewFeatures]
	struct DynaTexture
	{
		private string mod_id;
		private string path;
		private bool logged_error = false;

		public string asset_name {
			get { return $"FF/{mod_id}/{path}"; }
		}

		[RequiresPreviewFeatures]
		public DynaTexture(TypeInfo info, string path)
		{
			mod_id = info.mod_id;
			this.path = path;
		}

		public Texture2D get()
		{
			try
			{
				return ModEntry.get_helper().GameContent.Load<Texture2D>(asset_name);
			}
			catch (Microsoft.Xna.Framework.Content.ContentLoadException)
			{
				if (!logged_error)
					ModEntry.log(
						$"Missing texture for {mod_id} at {path}, replacing with error texture",
						StardewModdingAPI.LogLevel.Error
					);
				logged_error = true;
				return ModEntry.get_helper().ModContent.Load<Texture2D>("assets/error.png");
			}
		}
	}
}