using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace FurnitureFramework.Type.Properties
{

	[RequiresPreviewFeatures]
	class SeatList: IProperty<SeatList>
	{
		private enum SeatDir
		{ Up, Right, Down, Left }

		#region Seat Subclass

		private class Seat
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;

			public readonly Vector2 position = new();
			public readonly SeatDir player_dir;

			Depth? depth;

			#region Seat Parsing

			public Seat(JObject data, string rot_name)
			{
				// Parsing Required Position

				JToken? pos_token = data.GetValue("Position");
				if (!JsonParser.try_parse(pos_token, ref position))
				{
					error_msg = "Missing or Invalid Position";

					// Directional?
					if (pos_token is not JObject pos_obj) return;

					JToken? dir_pos_token = pos_obj.GetValue(rot_name);
					if (!JsonParser.try_parse(dir_pos_token, ref position))
						return;
				}

				// Parsing Required Player Direction

				JToken? pdir_token = data.GetValue("Player Direction");
				if (!JsonParser.try_parse_enum(pdir_token, ref player_dir))
				{
					error_msg = "Missing or Invalid Player Direction";

					// Directional?
					if (pdir_token is not JObject pdir_obj) return;

					JToken? dir_pdir_token = pdir_obj.GetValue(rot_name);
					if (!JsonParser.try_parse_enum(dir_pdir_token, ref player_dir))
						return;
				}

				is_valid = true;

				parse_optional(data);
			}

			private void parse_optional(JObject data)
			{
				// Parsing optional player depth
				try { depth = new(data.GetValue("Depth")); }
				catch (InvalidDataException) { depth = null; }
			}

			#endregion

			#region Seat Methods

			public float get_player_depth(float top)
			{
				if (depth is null) return -1;	// keep default depth
				else return depth.get_value(top);
			}

			public void debug_print(int indent_count)
			{
				string indent = new('\t', indent_count);
				ModEntry.log($"{indent}Position: {position}", LogLevel.Debug);
				ModEntry.log($"{indent}Player Direction: {player_dir}", LogLevel.Debug);
				if (depth == null) ModEntry.log($"{indent}No Depth", LogLevel.Debug);
				else depth.debug_print(indent_count);
			}

			#endregion
		}

		#endregion

		#region SeatList Parsing

		public static SeatList make_default(TypeInfo info, string rot_name)
		{
			return new(info, new JArray() /*empty array -> no seats*/, rot_name);
		}

		public static SeatList make(TypeInfo info, JToken? data, string rot_name, out string? error_msg)
		{
			error_msg = null;
			
			if (data is JObject obj)
			{
				// single seat?
				Seat seat = new(obj, rot_name);
				if (seat.is_valid)
					return new(seat);

				// directional?
				JToken? dir_token = obj.GetValue(rot_name);
				if (dir_token is JObject dir_obj)
				{
					// directional single seat?
					Seat dir_seat = new(dir_obj, rot_name);
					if (dir_seat.is_valid)
						return new(dir_seat);
					
					// single seat was invalid
					ModEntry.log($"Could not parse a seat in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{seat.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Seat.", LogLevel.Warn);
				}

				else if (dir_token is JArray dir_arr)
				{
					// directional seats
					return new(info, dir_arr, rot_name);
				}

				else
				{
					// single seat was invalid
					ModEntry.log($"Could not parse a seat in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{seat.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Seat.", LogLevel.Warn);
				}
			}

			else if (data is JArray arr)
			{
				// list of seats
				return new(info, arr, rot_name);
			}

			// for all invalid cases
			return make_default(info, rot_name);
		}

		List<Seat> list = new();
		public readonly bool is_valid = false;
		public readonly string? error_msg;

		private bool has_seats {get {return list.Count > 0;}}

		private SeatList(Seat seat)
		{
			list.Add(seat);
			is_valid = true;
		}

		private SeatList(TypeInfo info, JArray array, string rot_name)
		{
			foreach (JToken token in array)
			{
				if (token is not JObject obj2) continue;	// skips comments
				add_seat(info, obj2, rot_name);
			}

			is_valid = true;
		}

		private void add_seat(TypeInfo info, JObject data, string rot_name)
		{
			Seat seat = new(data, rot_name);
			if (seat.is_valid)
				list.Add(seat);
			else
			{
				ModEntry.log($"Invalid Seat in {info.mod_id} at {data.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{seat.error_msg}", LogLevel.Warn);
				ModEntry.log($"Skipping Seat.");
			}
		}

		#endregion

		#region SeatList Methods

		public void get_seat_positions(Vector2 tile_pos, List<Vector2> result)
		{
			foreach (Seat seat in list)
			{
				result.Add(tile_pos + seat.position);
			}
		}

		public int get_sitting_direction(int seat_index)
		{
			if (!has_seats) return -1;

			return (int)list[seat_index].player_dir;
		}

		public float get_sitting_depth(int seat_index, float top)
		{
			if (!has_seats) return -1;
			
			return list[seat_index].get_player_depth(top);
		}


		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			int index = 0;
			foreach (Seat seat in list)
			{
				ModEntry.log($"{indent}Seat {index}:", LogLevel.Debug);
				seat.debug_print(indent_count + 1);
			}
		}

		#endregion
	}
}