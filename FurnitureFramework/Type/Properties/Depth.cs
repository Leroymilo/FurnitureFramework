using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Type.Properties
{
	class Depth
	{
		int tile;
		int sub_tile = 0;

		#region Parsing

		public Depth(int depth = 0)
		{
			tile = depth;
		}

		public Depth(int depth, int sub)
		{
			tile = depth;
			sub_tile = sub;
		}

		public Depth(JToken? token)
		{
			if (token is JObject obj)
			{
				JToken? tile_token = obj.GetValue("Tile");
				if (tile_token is null || tile_token.Type != JTokenType.Integer)
					throw new InvalidDataException("Invalid depth");
				tile = (int)tile_token;

				JToken? sub_token = obj.GetValue("Sub");
				if (sub_token is not null && sub_token.Type == JTokenType.Integer)
					sub_tile = (int)sub_token;
				sub_tile = Math.Clamp(sub_tile, 0, 1000);
			}

			else if (token is not null && token.Type == JTokenType.Integer)
			{
				tile = (int)token;
			}
			
			else
				throw new InvalidDataException("Invalid depth");
		}

		#endregion

		#region Methods
		
		public float get_value(float top)
		{
			float min = top + 64 * tile + 16;
			float max = top + 64 * (tile + 1) - 2;

			float result = min + (max - min) * (sub_tile / 1000f);
			
			return result / 10000f;
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			ModEntry.log($"{indent}Depth: Tile: {tile}, Sub: {sub_tile}", StardewModdingAPI.LogLevel.Debug);
		}

		#endregion
	}
}