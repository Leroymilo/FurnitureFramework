using System.Runtime.Versioning;
using Microsoft.Xna.Framework.Graphics;

namespace FurnitureFramework.Type
{
	[RequiresPreviewFeatures]
	struct DynaTexture
	{
		string path;

		[RequiresPreviewFeatures]
		public DynaTexture(TypeInfo info, string path)
		{
			this.path = $"FF/{info.mod_id}/{path}";
		}

		public Texture2D get()
		{
			return ModEntry.get_helper().GameContent.Load<Texture2D>(path);
		}
	}
}