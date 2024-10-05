using System.Data;
using System.Runtime.Versioning;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace FurnitureFramework.Type.Properties
{
	[RequiresPreviewFeatures]
	class SlotList: IProperty<SlotList>
	{
		#region Slot Subclass

		private class Slot
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;

			public readonly Rectangle area;
			Depth depth = new(0, 1000);
			Vector2 offset = Vector2.Zero;
			bool draw_shadow = true;
			Vector2 shadow_offset = Vector2.Zero;

			Point max_size = new Point(1);
			string? item_query = null;

			Color debug_color;	// Optional, default defined in config.
			static Texture2D debug_texture;

			static Slot()
			{
				debug_texture = new(Game1.graphics.GraphicsDevice, 1, 1);
				debug_texture.SetData(new[] { Color.White });
			}

			#region Slot Parsing

			public Slot(JObject data, string rot_name)
			{
				// Parsing Required Slot Area
				JToken? area_token = data.GetValue("Area");
				if (!JsonParser.try_parse(area_token, out area))
				{
					error_msg = "Missing or Invalid Area";

					// Directional?
					if (area_token is not JObject area_obj) return;

					JToken? dir_area_token = area_obj.GetValue(rot_name);
					if (!JsonParser.try_parse(dir_area_token, out area))
						return;
				}

				is_valid = true;

				parse_optional(data);
			}

			private void parse_optional(JObject data)
			{
				// Parsing optional layer depth
				try { depth = new(data["Depth"]); }
				catch (InvalidDataException) {}
				
				// Parsing optional offset
				JsonParser.try_parse(data.GetValue("Offset"), ref offset);

				// Parsing optional draw shadow
				draw_shadow = JsonParser.parse(data.GetValue("Draw Shadow"), true);
				
				// Parsing optional shadow offset
				JsonParser.try_parse(data.GetValue("Shadow Offset"), ref shadow_offset);

				// Parsing optional max size
				JsonParser.try_parse(data.GetValue("Max Size"), ref max_size);

				// Parsing optional condition
				JToken? token = data.GetValue("Condition");
				if (token is not null && token.Type == JTokenType.String)
				{ item_query = token.ToString(); }

				// Parsing optional debug color
				debug_color = JsonParser.parse_color(
					data.GetValue("Debug Color"),
					ModEntry.get_config().slot_debug_default_color
				);
			}

			#endregion

			#region Slot Methods

			public bool can_hold(StardewValley.Object held_obj, Furniture furniture, Farmer who)
			{
				bool result = true;

				if (item_query != null)
				{
					result &= GameStateQuery.CheckConditions(
						item_query,
						location: furniture.Location,
						player: who,
						targetItem: furniture,
						inputItem: held_obj
					);
				}

				if (held_obj is Furniture held_furn)
				{
					Point size = held_furn.boundingBox.Value.Size / new Point(64);
					result &= size.X <= max_size.X && size.Y <= max_size.Y;
				}

				return result;
			}

			public void draw_obj(DrawData draw_data, float top, StardewValley.Object obj)
			{
				draw_data.position.X += area.Center.X * 4;	// Horizontally centered
				draw_data.position.Y += area.Bottom * 4;	// Vertically bottom aligned
				draw_data.position += offset * 4;

				draw_data.depth = depth.get_value(top);
				draw_data.depth = MathF.BitIncrement((float)draw_data.depth);
				// plus epsilon to make sure it's drawn over the layer at the same depth

				if (obj is Furniture furn)
				{
					Point source_rect_size;
					if (Pack.FurniturePack.try_get_type(furn, out FurnitureType? type))
						source_rect_size = type.get_source_rect_size(furn.currentRotation.Value);
					else source_rect_size = furn.sourceRect.Value.Size;
					
					draw_data.position -= source_rect_size.ToVector2() * new Vector2(2, 4);
					// draw pos is on top left of Furniture Sprite

					furn.drawAtNonTileSpot(
						draw_data.sprite_batch,
						draw_data.position,
						draw_data.depth,
						draw_data.color.A
					);
					return;
				}
				
				
				if (draw_shadow)
				{
					DrawData shadow_data = draw_data.DeepClone();
					shadow_data.texture = Game1.shadowTexture;
					shadow_data.source_rect = Game1.shadowTexture.Bounds;
					shadow_data.position += shadow_offset * 4;
					shadow_data.position -= shadow_data.source_rect.Size.ToVector2() * new Vector2(2, 4);
					// draw pos is on top left of Shadow texture
					
					shadow_data.draw();

					draw_data.depth = MathF.BitIncrement(draw_data.depth);
					// plus epsilon to make sure it's drawn over the shadow
					draw_data.position.Y -= 4;
					// 1 pixel higher to leave space to see the shadow under the item
				}
				
				draw_data.position -= new Vector2(32, 64);
				// draw pos is on top left of Item Sprite

				if (obj is ColoredObject)
				{
					obj.drawInMenu(
						draw_data.sprite_batch,
						draw_data.position, 1f, 1f,
						draw_data.depth,
						StackDrawType.Hide, Color.White, drawShadow: false
					);
				}
				else
				{
					ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId);
					draw_data.texture = dataOrErrorItem.GetTexture();
					draw_data.source_rect = dataOrErrorItem.GetSourceRect();

					draw_data.draw();
				}
			}

			public void draw_debug(DrawData draw_data)
			{
				draw_data.texture = debug_texture;
				draw_data.position += area.Location.ToVector2() * 4f;
				draw_data.source_rect = area;
				draw_data.color = debug_color * ModEntry.get_config().slot_debug_alpha;
				draw_data.depth = float.MaxValue;
				
				draw_data.draw();
			}

			public void debug_print(int indent_count)
			{
				string indent = new('\t', indent_count);
				ModEntry.log($"{indent}Area: {area.Location}, {area.Size}", LogLevel.Debug);
				ModEntry.log($"{indent}Offset: {offset}", LogLevel.Debug);
				depth.debug_print(indent_count);
				ModEntry.log($"{indent}Draw Shadow: {draw_shadow}", LogLevel.Debug);
				if (draw_shadow) ModEntry.log($"{indent}Shadow Offset: {shadow_offset}", LogLevel.Debug);
				if (max_size != new Point(1)) ModEntry.log($"{indent}Max Size: {max_size}", LogLevel.Debug);
				if (item_query != null) ModEntry.log($"{indent}Condition: {item_query}", LogLevel.Debug);
				ModEntry.log($"{indent}Debug Color: {debug_color}", LogLevel.Debug);
			}

			#endregion
		}

		#endregion

		#region SlotList Parsing

		public static SlotList make_default(TypeInfo info, string rot_name)
		{
			return new(info, new JArray() /*empty array -> no slots*/, rot_name);
		}

		public static SlotList make(TypeInfo info, JToken? data, string rot_name, out string? error_msg)
		{
			error_msg = null;

			if (data == null || data.Type == JTokenType.None)
				return make_default(info, rot_name);
			
			if (data is JObject obj)
			{
				// single slot?
				Slot slot = new(obj, rot_name);
				if (slot.is_valid)
					return new(slot);

				// directional?
				JToken? dir_token = obj.GetValue(rot_name);
				if (dir_token is JObject dir_obj)
				{
					// directional single slot?
					Slot dir_slot = new(dir_obj, rot_name);
					if (dir_slot.is_valid)
						return new(dir_slot);
					
					// single slot was invalid
					ModEntry.log($"Could not parse a slot in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{slot.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Slot.", LogLevel.Warn);
				}

				else if (dir_token is JArray dir_arr)
				{
					// directional slots
					return new(info, dir_arr, rot_name);
				}

				else
				{
					// single slot was invalid
					ModEntry.log($"Could not parse a slot in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{slot.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Slot.", LogLevel.Warn);
				}
			}

			else if (data is JArray arr)
			{
				// list of slots
				return new(info, arr, rot_name);
			}

			// for all invalid cases
			error_msg = "Invalid Slot List definition, fallback to no Slots.";
			return make_default(info, rot_name);
		}

		List<Slot> list = new();

		public int count {get => list.Count;}

		private SlotList(Slot slot)
		{
			list.Add(slot);
		}

		private SlotList(TypeInfo info, JArray array, string rot_name)
		{
			foreach (JToken token in array)
			{
				if (token is not JObject obj2) continue;	// skips comments
				add_slot(info, obj2, rot_name);
			}
		}

		private void add_slot(TypeInfo info, JObject data, string rot_name)
		{
			Slot slot = new(data, rot_name);
			if (slot.is_valid)
				list.Add(slot);
			else
			{
				ModEntry.log($"Invalid Slot in {info.mod_id} at {data.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{slot.error_msg}", LogLevel.Warn);
				ModEntry.log($"Skipping Slot.");
			}
		}

		#endregion

		#region SlotList Methods

		public int get_slot(Point rel_pos)
		{	
			foreach ((Slot slot, int index) in list.Select((value, index) => (value, index)))
			{
				if (!slot.area.Contains(rel_pos)) continue;
				return index;
			}
			
			return -1;
		}

		public bool can_hold(int index, StardewValley.Object obj, Furniture furniture, Farmer who)
		{
			return list[index].can_hold(obj, furniture, who);
		}

		public void draw(
			DrawData draw_data, float top,
			IList<Item> items
		)
		{
			foreach ((Item item, int i) in items.Select((value, index) => (value, index)))
			{
				if (ModEntry.get_config().enable_slot_debug)
					list[i].draw_debug(draw_data);

				if (item is not StardewValley.Object obj) continue;
				list[i].draw_obj(draw_data, top, obj);
			}
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			int index = 0;
			foreach (Slot slot in list)
			{
				ModEntry.log($"{indent}Slot {index}:", LogLevel.Debug);
				slot.debug_print(indent_count + 1);
				index ++;
			}
		}

		#endregion
	}
}