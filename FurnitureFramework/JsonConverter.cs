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
		public static Vector2 extract_position(JToken token)
		{
			Vector2 result = Vector2.Zero;
			
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
					extract_position(token).ToPoint(),
					extract_size(token)
				);
			}
			catch (InvalidDataException)
			{
				throw new InvalidDataException($"Invalid Rectangle definition at {token.Path}.");
			}
		}

		public static void get_directional_rectangles(
			JToken token, List<Rectangle> rectangles, List<string> rotations
		)
		{
			rectangles.Clear();
			if (rotations.Count == 0)
			{
				rectangles.Add(extract_rect(token));
			}
			else if (token is JObject rect_dict)
			{
				foreach (string key in rotations)
				{
					JToken? rect_token = rect_dict.GetValue(key);
					if (rect_token == null || rect_token.Type == JTokenType.Null)
					{
						throw new InvalidDataException($"Missing Rectangle at {token.Path} for direction {key}.");
					}
					rectangles.Add(extract_rect(rect_token));
				}
			}
			else throw new InvalidDataException($"Directional Rectangles at {token.Path} should be a dictionary.");
		}

		public static void get_directional_sizes(
			JToken token, List<Point> sizes, List<string> rotations
		)
		{
			sizes.Clear();
			// trying to extract a single size from the token
			try
			{
				sizes.Add(extract_size(token));
				return;
			}
			catch (InvalidDataException)
			{
				if (rotations.Count == 0) throw;
				// if no rot_names, then only a single size is accepted
			}
			
			if (token is JObject size_dict)
			{
				foreach (string key in rotations)
				{
					JToken? size_token = size_dict.GetValue(key);
					if (size_token == null || size_token.Type == JTokenType.Null)
					{
						throw new InvalidDataException($"Missing Size at {token.Path} for direction {key}.");
					}
					sizes.Add(extract_size(size_token));
				}
			}
			else throw new InvalidDataException($"Directional Sizes at {token.Path} should be a dictionary.");
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