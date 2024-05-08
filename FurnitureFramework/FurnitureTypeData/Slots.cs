using System.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

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

			public readonly float depth = 0.1f;

			Vector2 offset = Vector2.Zero;
			bool draw_shadow = true;
			Vector2 shadow_offset = Vector2.Zero;

			#region SlotData Parsing

			public SlotData(JObject slot_obj, List<string>? rot_names = null)
			{
				// Parsing required layer area

				error_msg = "Missing or Invalid Area.";
				JToken? area_token = slot_obj.GetValue("Area");
				if (area_token is not JObject area_obj)
					return;

				// Case 1 : non-directional area
				bool has_rect = true;
				try
				{
					single_area = JC.extract_rect(area_token);
				}
				catch (InvalidDataException)
				{
					has_rect = false;
				}

				// Case 2 : directional area

				if (!has_rect)
				{
					if (rot_names == null)
					{
						error_msg = "No singular Area given for non-directional Seat Data.";
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

					error_msg = "Area is directional with no valid value.";
					if (!has_rect) return;
					
					is_directional = true;
				}

				// Parsing optional layer depth
				
				depth = JC.extract(slot_obj, "Depth", 0.1f);

				// Parsing optional offset

				JToken? offset_token = slot_obj.GetValue("Offset");
				if (offset_token != null)
				{
					offset = JC.extract_position(offset_token);
				}

				// Parsing optional draw shadow

				draw_shadow = JC.extract(slot_obj, "Draw Shadow", true);
				

				// Parsing optional shadow offset
				if (draw_shadow)
				{
					JToken? s_offset_token = slot_obj.GetValue("Shadow Offset");
					if (s_offset_token != null)
					{
						shadow_offset = JC.extract_position(s_offset_token);
					}
				}

				is_valid = true;
			}

			#endregion

			#region SlotData Methods

			public Rectangle? get_area(int rot)
			{
				if (is_directional)
					return directional_areas[rot];
				return single_area;
			}

			public bool draw_obj(
				int rot,
				SpriteBatch sprite_batch,
				Item item,
				int bottom,
				float alpha
			)
			{
				Rectangle area;
				if (is_directional)
				{
					Rectangle? area_ = directional_areas[rot];
					if (area_ == null) return false;
					area = area_.Value;
				}
				else
					area = single_area;

				if (item is not StardewValley.Object obj) return true;

				Vector2 draw_pos = obj.TileLocation * 64;
				draw_pos.X += area.Center.X * 4 - 32;	// Horizontally centered
				draw_pos.Y += area.Bottom * 4;			// Vertically bottom aligned
				draw_pos += offset * 4;

				float draw_depth = bottom - depth * 64;
				draw_depth = draw_depth / 10000f;
				draw_depth = MathF.BitIncrement(draw_depth);
				draw_depth = MathF.BitIncrement(draw_depth);
				// plus epsilon to make sure it's drawn over the layer at the same depth

				if (obj is Furniture furn)
				{
					draw_pos.Y -= furn.sourceRect.Height * 4;
					draw_pos = Game1.GlobalToLocal(Game1.viewport, draw_pos);

					furn.drawAtNonTileSpot(
						sprite_batch, draw_pos,
						draw_depth,
						alpha
					);
					return true;
				}
				
				draw_pos.Y -= 64;
				draw_pos = Game1.GlobalToLocal(Game1.viewport, draw_pos);
				
				if (draw_shadow)
				{
					sprite_batch.Draw(
						Game1.shadowTexture,
						draw_pos + new Vector2(32f, 48f) + shadow_offset * 4,
						Game1.shadowTexture.Bounds,
						Color.White * alpha, 0f,
						Game1.shadowTexture.Bounds.Center.ToVector2(),
						4f, SpriteEffects.None,
						draw_depth
					);

					draw_depth = MathF.BitIncrement(draw_depth);
					// plus epsilon to make sure it's drawn over the shadow

					draw_pos.Y -= 4; // to leave space to show the shadow
				}


				if (obj is ColoredObject)
				{
					obj.drawInMenu(
						sprite_batch,
						draw_pos, 1f, 1f,
						draw_depth,
						StackDrawType.Hide, Color.White, drawShadow: false
					);
				}
				else
				{
					ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId);
				
					sprite_batch.Draw(
						dataOrErrorItem2.GetTexture(),
						draw_pos,
						dataOrErrorItem2.GetSourceRect(),
						Color.White * alpha,
						0f, Vector2.Zero, 4f,
						SpriteEffects.None,
						draw_depth
					);
				}

				return true;
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
			foreach (JToken slot_token in slots_arr)
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

		public int get_slot(Point rel_pos, int rot, out Rectangle area)
		{	
			List<SlotData> slots;
			area = Rectangle.Empty;

			if (is_directional)
				slots = directional_slots[rot];
			else
				slots = singular_slots;
			
			foreach ((SlotData slot, int index) in slots.Select((value, index) => (value, index)))
			{
				Rectangle? area_ = slot.get_area(rot);
				if (area_ is null) continue;

				if (!area_.Value.Contains(rel_pos)) continue;
				
				area = area_.Value;
				return index;
			}

			return -1;
		}

		public int get_count(int rot)
		{
			if (is_directional)
			{
				return directional_slots[rot].Count;
			}
			else
			{
				// to change to count slots without the current rotation
				return singular_slots.Count;
			}
		}

		public void draw(
			SpriteBatch sprite_batch,
			IList<Item> items,
			int rot, int bottom,
			float alpha
		)
		{
			int index = 0;

			List<SlotData> slots;
			if (is_directional)
				slots = directional_slots[rot];
			else
				slots = singular_slots;

			foreach (Item item in items)
			{
				while(!slots[index].draw_obj(
					rot, sprite_batch, item, bottom, alpha
				))
				{
					index++;
				}
				index++;
			}
		}

		#endregion

	}
}