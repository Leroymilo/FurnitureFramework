using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
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
		string type;

		List<Point> bb_sizes = new();

		int rotations;
		int price;
		int placement_rules;
		
		Texture2D texture;
		List<Rectangle> source_rects = new();

		bool random_sales;
		List<string> context_tags = new();

		List<LightSource> light_sources = new();


		public CustomFurniture(IContentPack pack, string id, JObject data)
		{
			mod_id = pack.Manifest.UniqueID;
			this.id = id;
			display_name = JC.extract(data, "Display Name", "No Name");
			type = JC.extract(data, "Force Type", "other");
			rotations = JC.extract(data, "Rotations", 1);
			price = JC.extract(data, "Price", 0);
			random_sales = JC.extract(data, "Show in Catalogue", false);

			placement_rules =
				+ 1 * JC.extract(data, "Indoors", 1)
				+ 2 * JC.extract(data, "Outdoors", 1)
				- 1;

			string text_path = data.Value<string>("Texture")
				?? throw new InvalidDataException($"Missing Texture for Furniture {id}");
			texture = TextureManager.load(pack.ModContent, text_path);

			JToken? rect_token = data.GetValue("SourceRect");
			if (rect_token != null) JC.get_list_of_rect(rect_token, source_rects);

			JToken size_token = data.GetValue("Bounding Box Size")
				?? throw new InvalidDataException($"Missing Bounding Box Size for Furniture {id}.");
			JC.get_list_of_size(size_token, bb_sizes);
			if (bb_sizes.Count < 1)
				throw new InvalidDataException($"At least one Bounding Box Size required for Furniture {id}");

			JToken? tag_token = data.GetValue("Context Tags");
			if (tag_token != null) JC.get_list_of_string(tag_token, context_tags);
		}

		
	}

}