using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework
{

	class Seats
	{
		#region SeatData

		private class SeatData
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;

			public readonly Vector2 position = new();

			List<int?> directional_player_dir = new();
			int single_player_dir;
			bool is_directional = false;

			#region SeatData Parsing

			public SeatData(JObject seat_obj, List<string>? rot_names = null)
			{
				// If rot_names is null, this seat cannot have directional player direction

				// Parsing required seat position

				error_msg = "Missing X coordinate in Seat Data.";
				JToken? x_token = seat_obj.GetValue("X");
				if (x_token == null ||
					(x_token.Type != JTokenType.Float &&
					x_token.Type != JTokenType.Integer)
				) return;
				position.X = (float)x_token;

				error_msg = "Missing Y coordinate in Seat Data.";
				JToken? y_token = seat_obj.GetValue("Y");
				if (y_token == null ||
					(y_token.Type != JTokenType.Float &&
					y_token.Type != JTokenType.Integer)
				) return;
				position.Y = (float)y_token;

				// Parsing player direction
				error_msg = "Missing Player Direction in Seat Data.";
				JToken? pd_token = seat_obj.GetValue("Player Direction");
				if (pd_token == null ||
					(pd_token.Type != JTokenType.Integer &&
					pd_token.Type != JTokenType.Object)
				) return;

				// Case 1 : non-directional player direction
				if (pd_token.Type == JTokenType.Integer)
				{
					single_player_dir = (int)pd_token;
				}
				else if (rot_names == null)
				{
					error_msg = "No singular Player Direction given for non-directional Seat Data.";
					return;
				}

				// Case 2 : directional player direction
				else if (pd_token is JObject pd_obj)
				{
					bool has_p_dir = false;
					foreach (string rot_name in rot_names)
					{
						pd_token = pd_obj.GetValue(rot_name);
						int? player_dir = null;
						if (pd_token != null && pd_token.Type == JTokenType.Integer)
						{
							player_dir = (int)pd_token;
							has_p_dir = true;
						}
						directional_player_dir.Add(player_dir);
					}

					error_msg = "Player Direction is directional with no valid value.";
					if (!has_p_dir) return;
					
					is_directional = true;
				}

				is_valid = true;
			}

			#endregion

			#region SeatData Methods

			public bool is_valid_seat(int rot)
			{
				if (is_directional)
				{
					return directional_player_dir[rot] != null;
				}
				return true;
			}

			public int? get_sitting_dir(int rot)
			{
				if (is_directional)
				{
					return directional_player_dir[rot];
				}
				return single_player_dir;
			}

			#endregion
		}

		#endregion

		public bool has_seats {get; private set;} = false;

		List<List<SeatData>> directional_seats = new();
		List<SeatData> singular_seats = new();
		bool is_directional = false;

		#region Seats Parsing

		public Seats(JToken? seats_token, List<string> rot_names)
		{
			if (seats_token == null || seats_token.Type == JTokenType.Null)
				return;	// No seats

			// Case 1 : non-directional seats

			if (seats_token is JArray seats_arr)
			{
				parse_seat_array(seats_arr, singular_seats, rot_names);
			}

			// Case 2 : directional seats

			else if (seats_token is JObject seats_obj)
			{
				foreach (string rot_name in rot_names)
				{
					List<SeatData> seat_list = new();

					JToken? seats_dir_token = seats_obj.GetValue(rot_name);
					if (seats_dir_token is JArray seats_dir_arr)
					{
						parse_seat_array(seats_dir_arr, seat_list, null);
					}
					directional_seats.Add(seat_list);
				}

				is_directional = true;
			}
		}

		private void parse_seat_array(
			JArray seats_arr, List<SeatData> seat_list,
			List<string>? rot_names = null
		)
		{
			foreach (JToken seat_token in seats_arr.Children())
			{
				if (seat_token is not JObject seat_obj) continue;

				SeatData seat = new(seat_obj, rot_names);
				if (!seat.is_valid)
				{
					ModEntry.log($"Invalid Seat Data at {seat_token.Path}:", StardewModdingAPI.LogLevel.Warn);
					ModEntry.log($"\t{seat.error_msg}", StardewModdingAPI.LogLevel.Warn);
					ModEntry.log("Skipping Seat.", StardewModdingAPI.LogLevel.Warn);
					continue;
				}
				has_seats = true;
				seat_list.Add(seat);
			}
		}

		#endregion
	
		#region Seats Methods

		public void get_seat_positions(int rot, Vector2 tile_pos, List<Vector2> list)
		{
			if (!has_seats) return;

			// Case 1 : non-directional seats

			if (!is_directional)
			{
				foreach (SeatData seat in singular_seats)
				{
					if (seat.is_valid_seat(rot))
						list.Add(tile_pos + seat.position);
				}
			}

			// Case 2 : directional seats

			else
			{
				foreach (SeatData seat in directional_seats[rot])
				{
					list.Add(tile_pos + seat.position);
					// seats are always valid because the player dir is non-directional
				}
			}
		}

		public int? get_sitting_direction(int rot, int seat_index)
		{
			if (!has_seats) return null;

			// Case 1 : non-directional seats

			if (!is_directional)
			{
				foreach (SeatData seat in singular_seats)
				{
					int? sit_dir = seat.get_sitting_dir(rot);
					if (sit_dir != null)
					{
						if (seat_index == 0)
							return sit_dir;
						seat_index--;
					}
				}
			}

			// Case 2 : directional seats

			else
			{
				foreach (SeatData seat in directional_seats[rot])
				{
					if (seat_index == 0)
						return seat.get_sitting_dir(rot);
					seat_index--;
				}
			}

			return null;
		}

		#endregion
	}
}