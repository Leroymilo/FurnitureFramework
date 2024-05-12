using System.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
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

			public readonly Rectangle area;
			Depth depth = new();
			Vector2 offset = Vector2.Zero;
			bool draw_shadow = true;
			Vector2 shadow_offset = Vector2.Zero;

			#region SlotData Parsing

			public SlotData(JObject slot_obj)
			{
				// Parsing required layer area
				if (!JsonParser.try_parse(slot_obj.GetValue("Area"), ref area))
				{
					error_msg = "Missing or Invalid Area.";
					return;
				}

				// Parsing optional layer depth
				try { depth = new(slot_obj["Depth"]); }
				catch (InvalidDataException) {}
				
				// Parsing optional offset
				JsonParser.try_parse(slot_obj.GetValue("Offset"), ref offset);

				// Parsing optional draw shadow
				draw_shadow = JsonParser.parse(slot_obj.GetValue("Draw Shadow"), true);
				
				// Parsing optional shadow offset
				JsonParser.try_parse(slot_obj.GetValue("Shadow Offset"), ref shadow_offset);

				is_valid = true;
			}

			#endregion

			#region SlotData Methods

			public void draw_obj(
				int rot,
				SpriteBatch sprite_batch,
				StardewValley.Object obj,
				float top,
				float alpha
			)
			{
				Vector2 draw_pos = obj.TileLocation * 64;
				draw_pos.X += area.Center.X * 4 - 32;	// Horizontally centered
				draw_pos.Y += area.Bottom * 4;			// Vertically bottom aligned
				draw_pos += offset * 4;

				float draw_depth = depth.get_value(top);
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
					return;
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
			}

			#endregion

		}

		#endregion

	
		public bool has_slots {get; private set;} = false;
		List<List<SlotData>> slots = new();

		#region Slots Parsing

		public static Slots make_slots(JToken? token, List<string> rot_names)
		{
			int rot_count = 1;
			bool directional = false;
			if (rot_names.Count > 0)
			{
				rot_count = rot_names.Count;
				directional = true;
			}
			Slots result = new(rot_count);

			if (token is JArray slots_arr)
			{
				foreach (JToken slot_token in slots_arr)
				{
					if (slot_token is not JObject slot_obj) continue;
					result.add_slot(slot_obj);
				}
			}

			else if (directional && token is JObject dir_slots_obj)
			{
				foreach ((string key, int rot) in rot_names.Select((value, index) => (value, index)))
				{
					JToken? dir_slots_tok = dir_slots_obj.GetValue(key);
					if (dir_slots_tok is not JArray dir_slots_arr) continue;

					foreach (JToken slot_token in dir_slots_arr)
					{
						if (slot_token is not JObject slot_obj) continue;
						result.add_slot(slot_obj, rot);
					}
				}
			}

			return result;
		}

		private void add_slot(JObject slot_obj, int? rot = null)
		{
			SlotData slot = new(slot_obj);
			if (!slot.is_valid)
			{
				ModEntry.log($"Invalid slot at {slot_obj.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{slot.error_msg}", LogLevel.Warn);
				ModEntry.log("Skipping Slot.", LogLevel.Warn);
				return;
			}

			if (rot is null)
			{
				foreach (List<SlotData> slot_list in slots)
				{
					slot_list.Add(slot);
				}
			}
			else slots[rot.Value].Add(slot);

			has_slots = true;
		}

		private Slots(int rot_count)
		{
			for (int i = 0; i < rot_count; i++)
				slots.Add(new());
		}

		#endregion

		#region Slots Methods

		public int get_slot(Point rel_pos, int rot, out Rectangle area)
		{	
			foreach ((SlotData slot, int index) in slots[rot].Select((value, index) => (value, index)))
			{
				if (!slot.area.Contains(rel_pos)) continue;
				
				area = slot.area;
				return index;
			}

			area = Rectangle.Empty;
			return -1;
		}

		public int get_count(int rot)
		{
			return slots[rot].Count;
		}

		public void draw(
			SpriteBatch sprite_batch,
			IList<Item> items,
			int rot, float top,
			float alpha
		)
		{
			foreach ((Item item, int i) in items.Select((value, index) => (value, index)))
			{
				if (item is not StardewValley.Object obj) continue;

				slots[rot][i].draw_obj(rot, sprite_batch, obj, top, alpha);
			}
		}

		#endregion

	}
}