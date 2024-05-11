using Newtonsoft.Json.Linq;

namespace FurnitureFramework
{
	class Depth
	{
		int tile;
		int sub_tile = 0;

		#region Parsing

		public Depth()
		{
			tile = 0;
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

		#endregion
	}
}