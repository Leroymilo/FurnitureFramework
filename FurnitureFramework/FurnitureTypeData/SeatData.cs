

using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework
{

	class Seats
	{
		#region SeatData

		private class SeatData
		{

		}

		#endregion

		
	}

	struct SeatData
	{
		public Vector2 position {get; private set;}
		Dictionary<int, int>? player_dir_map = null;
		int? player_dir = null;

		public SeatData(JObject seat_data, List<string> rotations)
		{
			JToken? pos_token = seat_data.GetValue("Position");
			if (pos_token == null || pos_token.Type == JTokenType.Null)
			{
				string message = $"No position given for seat at {seat_data.Path}";
				ModEntry.log(message, StardewModdingAPI.LogLevel.Trace);
				throw new InvalidDataException(message);
			}
			position = JC.extract_position(pos_token);

			JToken? dir_token = seat_data.GetValue("Player Direction");
			if (dir_token == null || dir_token.Type == JTokenType.Null)
			{
				string message = $"No player direction given for seat at {seat_data.Path}";
				ModEntry.log(message, StardewModdingAPI.LogLevel.Trace);
				throw new InvalidDataException(message);
			}

			if (rotations.Count == 0)
			{
				player_dir = parse_dir(dir_token);
			}

			else
			{
				if (dir_token is JObject dir_obj)
				{
					player_dir_map = new();
					foreach (var item in rotations.Select((value, i) => (value, i)))
					{
						JToken? token = dir_obj.GetValue(item.value);
						if (token == null || token.Type == JTokenType.Null) continue;

						player_dir_map[item.i] = parse_dir(token);
					}
				}
				else throw new InvalidDataException($"Directional directions at {dir_token.Path} should be a dictionary.");
			}
		}

		private static int parse_dir(JToken token)
		{
			int? result = null;
			string message = $"Expected 0, 1, 2 or 3 for player direction at {token.Path}.";
			if (token is JValue dir_val)
			{
				try
				{
					result = (int)dir_val;
				}
				catch
				{
					ModEntry.log(message, StardewModdingAPI.LogLevel.Trace);
					throw;
				}
			}
			if (result == null || result > 3 || result < 0)
			{
				ModEntry.log(message, StardewModdingAPI.LogLevel.Trace);
				throw new InvalidDataException(message);
			}
			return (int)result;
		}

		public readonly bool can_sit(int cur_rot)
		{
			if (player_dir != null)
			{
				return true;
			}
			if (player_dir_map != null && player_dir_map.ContainsKey(cur_rot))
			{
				return true;
			}
			return false;
		}

		public readonly int? get_dir(int cur_rot)
		{
			if (player_dir != null)
			{
				return player_dir;
			}
			if (player_dir_map != null && player_dir_map.ContainsKey(cur_rot))
			{
				return player_dir_map[cur_rot];
			}
			return null;
		}
	}
}