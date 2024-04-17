using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework
{
	interface IRect {

	}

	static class JC
	{
		public static T extract<T>(JObject data, string key, T def) where T : notnull
		{
			T? value = data.Value<T?>(key);
			if (value == null)
				return def;
			return value;
		}


		public static Point extract_size(JToken token)
		{
			Point result = Point.Zero;
			
			string inv_rect_msg = $"Invalid Size definition at {token.Path}.";
			if (token is JObject rect_data)
			{
				int value = rect_data.Value<int?>("Width")
					?? throw new InvalidDataException(inv_rect_msg);
				result.X = value;

				value = rect_data.Value<int?>("Height")
					?? throw new InvalidDataException(inv_rect_msg);
				result.Y = value;

				return result;
			}
			throw new InvalidDataException(inv_rect_msg);
		}
		public static Point extract_position(JToken token)
		{
			Point result = Point.Zero;
			
			string inv_rect_msg = $"Invalid Position definition at {token.Path}.";
			if (token is JObject rect_data)
			{
				int value = rect_data.Value<int?>("X")
					?? throw new InvalidDataException(inv_rect_msg);
				result.X = value;

				value = rect_data.Value<int?>("Y")
					?? throw new InvalidDataException(inv_rect_msg);
				result.Y = value;

				return result;
			}
			throw new InvalidDataException(inv_rect_msg);
		}

		public static Rectangle extract_rect(JToken token)
		{
			try
			{
				return new Rectangle(
					extract_position(token),
					extract_size(token)
				);
			}
			catch (InvalidDataException)
			{
				throw new InvalidDataException($"Invalid Rectangle definition at {token.Path}.");
			}
		}

		public static void get_list_of_rect(JToken token, List<Rectangle> list)
		{
			if (token is JArray array)
			{
				foreach (JToken sub_token in array.Children())
				{
					get_list_of_rect(sub_token, list);
				}
			}
			else
			{
				list.Add(extract_rect(token));
			}
		}

		public static void get_list_of_size(JToken token, List<Point> list)
		{
			if (token is JArray array)
			{
				foreach (JToken sub_token in array.Children())
				{
					get_list_of_size(sub_token, list);
				}
			}
			else
			{
				list.Add(extract_size(token));
			}
		}

		public static void get_list_of_string(JToken token, List<string> list)
		{
			if (token is JArray array)
			{
				foreach (JToken sub_token in array.Children())
				{
					get_list_of_string(sub_token, list);
				}
			}
			else
			{
				string? value = (string?)token;
				list.Add(
					value ?? "null"
				);
			}
		}
	}
}