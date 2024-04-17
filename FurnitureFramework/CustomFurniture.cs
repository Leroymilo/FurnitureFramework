using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;


namespace FurnitureFramework
{

	class CustomFurniture
	{
		string mod_id;

		string id;
		string display_name;
		string description;
		
		// Texture2D texture;

		List<LightSource> light_sources = new();


		public CustomFurniture(IContentPack pack, string id, JObject data)
		{
			mod_id = pack.Manifest.UniqueID;
			this.id = id;
			display_name = "";
			description = "";
		}
	}

}