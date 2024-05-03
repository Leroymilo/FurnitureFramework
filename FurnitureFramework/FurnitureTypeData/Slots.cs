using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework
{

	class Slots
	{

		#region SlotData

		private class SlotData
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;

			List<Rectangle?> directional_areas = new();
			Rectangle single_area;
			bool is_directional = false;

			public readonly float depth = 0;

			#region SlotData Parsing

			public SlotData(JObject slot_obj, List<string>? rot_names = null)
			{
				// Parsing required layer source rectangle

				error_msg = "Missing or Invalid Area.";
				JToken? area_token = slot_obj.GetValue("Area");
				if (area_token is not JObject area_obj)
					return;

				// Case 1 : non-directional source rectangle
				bool has_rect = true;
				try
				{
					single_area = JC.extract_rect(area_token);
				}
				catch (InvalidDataException)
				{
					has_rect = false;
				}

				// Case 2 : directional source rectangle

				if (!has_rect)
				{
					if (rot_names == null)
					{
						error_msg = "No singular Player Direction given for non-directional Seat Data.";
						return;
					}

					foreach (string rot_name in rot_names)
					{
						area_token = area_obj.GetValue(rot_name);
						Rectangle? area = null;
						if (area_token is JObject)
						{
							try
							{
								area = JC.extract_rect(area_token);
								has_rect = true;
							}
							catch (InvalidDataException) {}
						}
						directional_areas.Add(area);
					}

					error_msg = "Source Rect is directional with no valid value.";
					if (!has_rect) return;
					
					is_directional = true;
					is_valid = true;
					return;	// no draw pos or depth if source rect is directional
				}

				// Parsing optional layer depth

				JToken? depth_token = slot_obj.GetValue("Depth");
				if (depth_token != null &&
					(depth_token.Type == JTokenType.Float ||
					depth_token.Type == JTokenType.Integer)
				)
				{
					depth = (float)depth_token;
				}

				is_valid = true;
			}

			#endregion

			#region SlotData Methods

			public Rectangle? get_area(Point rel_pos, int rot)
			{
				Rectangle? area;
				if (is_directional)
					area = directional_areas[rot];
				else
					area = single_area;
				
				if (area == null || !area.Value.Contains(rel_pos))
					return null;

				return area;
			}

			#endregion

		}

		#endregion

	
		public bool has_slots {get; private set;} = false;

		List<List<SlotData>> directional_slots = new();
		List<SlotData> singular_slots = new();
		bool is_directional = false;

		#region Slots Parsing

		public Slots(JToken? slots_token, List<string> rot_names)
		{
			
			if (slots_token == null || slots_token.Type == JTokenType.Null)
				return;	// No slots

			// Case 1 : non-directional slots

			if (slots_token is JArray slots_arr)
			{
				parse_slot_array(slots_arr, singular_slots, rot_names);
			}

			// Case 2 : directional slots

			else if (slots_token is JObject slots_obj)
			{
				foreach (string rot_name in rot_names)
				{
					List<SlotData> slot_list = new();

					JToken? slots_dir_token = slots_obj.GetValue(rot_name);
					if (slots_dir_token is JArray slots_dir_arr)
					{
						parse_slot_array(slots_dir_arr, slot_list, null);
					}
					directional_slots.Add(slot_list);
				}

				is_directional = true;
			}
		}

		private void parse_slot_array(
			JArray slots_arr, List<SlotData> slot_list,
			List<string>? rot_names = null
		)
		{
			foreach (JToken slot_token in slots_arr.Children())
			{
				if (slot_token is not JObject slot_obj) continue;

				SlotData slot = new(slot_obj, rot_names);
				if (!slot.is_valid)
				{
					ModEntry.log($"Invalid Slot Data at {slot_token.Path}:", StardewModdingAPI.LogLevel.Warn);
					ModEntry.log($"\t{slot.error_msg}", StardewModdingAPI.LogLevel.Warn);
					ModEntry.log("Skipping Slot.", StardewModdingAPI.LogLevel.Warn);
					continue;
				}
				has_slots = true;
				slot_list.Add(slot);
			}
		}

		#endregion

		#region Slots Methods

		public Rectangle? get_slot(Point rel_pos, int rot)
		{	
			List<SlotData> slots;

			if (is_directional)
				slots = directional_slots[rot];
			else
				slots = singular_slots;
			
			foreach (SlotData slot in slots)
			{
				Rectangle? area = slot.get_area(rel_pos, rot);
				if (area != null) return area;
			}

			return null;
		}

		public float get_depth(int rot, int slot_id = 0)
		{
			if (is_directional)
			{
				return directional_slots[rot][slot_id].depth;
			}
			else
			{
				return singular_slots[slot_id].depth;
			}
		}

		#endregion

	}
}