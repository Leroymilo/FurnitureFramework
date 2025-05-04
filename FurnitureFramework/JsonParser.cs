using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewValley;

namespace FurnitureFramework
{
	using SDColor = System.Drawing.Color;

	static class JsonParser
	{
		public static bool try_parse_dir<T>(JToken? token, string rot_name, ref T result)
		{
			if (try_parse(token, ref result)) return true;
			
			// Directional?
			if (token is not JObject obj) return false;

			JToken? dir_token = obj.GetValue(rot_name);
			return try_parse(dir_token, ref result);
		}

		public static List<T> parse_dir<T>(JToken? token, List<string> rot_names, T def)
		{
			List<T> result = new();

			foreach (string rot_name in rot_names)
			{
				T dir_res = def;
				try_parse_dir(token, rot_name, ref dir_res);
				result.Add(dir_res);
			}

			return result;
		}

		public static bool try_parse<T>(JToken? token, ref T result)
		{
			if (token is null || token.Type == JTokenType.Null)
				return false;
			
			if (result is Vector2 vect_result)
			{
				if (!try_parse(token, ref vect_result)) return false;
				
				result = (T)(object)vect_result;
				return true;
			}
			
			if (result is Point point_result)
			{
				if (!try_parse(token, ref point_result)) return false;
				
				result = (T)(object)point_result;
				return true;
			}
			
			if (result is Rectangle rect_result)
			{
				if (!try_parse(token, ref rect_result)) return false;
				
				result = (T)(object)rect_result;
				return true;
			}

			if (token is JArray or JObject) return false;
			
			T? n_result = token.ToObject<T?>();
			if (n_result is null) return false;

			result = n_result;
			return true;
		}

		public static T parse<T>(JToken? token, T def)
		{
			if (token is null || token.Type == JTokenType.Null)
				return def;
			
			T? n_result = token.ToObject<T?>();
			if (n_result is null) return def;

			return n_result;
		}

		private static bool is_num(JToken token)
		{
			return 
				token.Type == JTokenType.Float ||
				token.Type == JTokenType.Integer;
		}

		// Parse Enum
		public static bool try_parse_enum<TEnum>(JToken? token, ref TEnum result) where TEnum: struct, Enum
		{
			if (token is JValue && token.Type == JTokenType.String)
			{
				result = Enum.Parse<TEnum>(token.ToString());
				return Enum.IsDefined(result);
			}
			return false;
		}

		// Parse 2D Vector (float)
		public static bool try_parse(JToken? token, ref Vector2 result)
		{
			if (token is not JObject obj) return false;
			
			JToken? X_token = obj.GetValue("X");
			if (X_token == null || !is_num(X_token)) return false;
			result.X = (float)X_token;

			JToken? Y_token = obj.GetValue("Y");
			if (Y_token == null || !is_num(Y_token)) return false;
			result.Y = (float)Y_token;

			return true;
		}

		// Parse 2D Vector (integer)
		public static bool try_parse(JToken? token, ref Point result)
		{
			if (token is not JObject obj) return false;
			
			JToken? X_token = obj.GetValue("X");
			if (X_token == null || X_token.Type != JTokenType.Integer) return false;
			result.X = (int)X_token;

			JToken? Y_token = obj.GetValue("Y");
			if (Y_token == null || Y_token.Type != JTokenType.Integer) return false;
			result.Y = (int)Y_token;

			return true;
		}

		// Parse rectangle
		public static bool try_parse(JToken? token, ref Rectangle result)
		{
			if (token is not JObject obj) return false;
			
			JToken? X_token = obj.GetValue("X");
			if (X_token == null || X_token.Type != JTokenType.Integer) return false;
			result.X = (int)X_token;

			JToken? Y_token = obj.GetValue("Y");
			if (Y_token == null || Y_token.Type != JTokenType.Integer) return false;
			result.Y = (int)Y_token;
			
			JToken? W_token = obj.GetValue("Width");
			if (W_token == null || W_token.Type != JTokenType.Integer) return false;
			result.Width = (int)W_token;

			JToken? H_token = obj.GetValue("Height");
			if (H_token == null || H_token.Type != JTokenType.Integer) return false;
			result.Height = (int)H_token;

			return true;
		}

		// Parse list of strings
		public static bool try_parse(JToken? token, ref List<string> list)
		{
			if (token is JArray array)
			{
				foreach (JToken sub_token in array.Children())
				{
					if (sub_token.Type != JTokenType.String) continue;
					list.Add(sub_token.ToString());
				}

				return true;
			}

			return false;
		}

		public static Color parse_color(JToken? token, Color def)
		{
			string color_name = "";
			if (try_parse(token, ref color_name))
			{
				// From color code
				if (Utility.StringToColor(color_name) is Color color)
					return color;
				
				// From color name
				SDColor c_color = SDColor.FromName(color_name);
				return new(c_color.R, c_color.G, c_color.B);
			}
			return def;
		}
	}
}