using System.Data;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace FurnitureFramework.Type.Properties
{
	using SVObject = StardewValley.Object;

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
				// Parsing directional Slot Area
				error_msg = "Missing or Invalid Area";
				if (!JsonParser.try_parse_dir(data.GetValue("Area"), rot_name, ref area))
					return;

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

			public bool can_hold(SVObject held_obj, Furniture furniture, Farmer who)
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

			public void set_box(SVObject held_obj, Point position)
			{
				Point size = held_obj.boundingBox.Value.Size;
				position += new Point(area.Center.X, area.Bottom) * new Point(4);
				position += offset.ToPoint() * new Point(4);
				position.X -= size.X / 2;

				held_obj.boundingBox.Value = new Rectangle(position, size);
			}

			public void draw_obj(DrawData draw_data, float top, SVObject obj)
			{
				draw_data.position += new Vector2(area.Center.X, area.Bottom) * 4f;
				// Position is set to the bottom center of the slot area
				draw_data.position += offset * 4f;

				draw_data.depth = depth.get_value(top);
				draw_data.depth = MathF.BitIncrement(draw_data.depth);
				// plus epsilon to make sure it's drawn over the layer at the same depth

				if (obj is Furniture furn)
				{
					draw_data.position.X -= furn.boundingBox.Value.Size.X / 2f;
					// Moved to the bottom left of the object bounding box, centered in the slot

					if (Pack.FurniturePack.try_get_type(furn, out FurnitureType? type))
					{
						type.draw(furn, draw_data, draw_in_slot: true);
						return;
					}
					
					draw_data.position.Y -= furn.sourceRect.Value.Size.Y * draw_data.scale;
					// vanilla draw pos is on top left of Furniture source rectangle

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
					DrawData shadow_data = draw_data;	// should clone value fields like position
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

				ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId);
				draw_data.source_rect = dataOrErrorItem.GetSourceRect();
				draw_data.position -= draw_data.source_rect.Size.ToVector2() * new Vector2(2, 4);
				// Moved to the top left of the object source rect, centered in the slot

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
					draw_data.texture = dataOrErrorItem.GetTexture();
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

			public void draw_lights(DrawData draw_data, Furniture furn)
			{
				if (Pack.FurniturePack.try_get_type(furn, out FurnitureType? type))
				{
					// draw custom lights

					draw_data.position += new Vector2(area.Center.X, area.Bottom) * 4f;
					// Position is set to the bottom center of the slot area
					draw_data.position += offset * 4f;
					draw_data.position.X -= furn.boundingBox.Value.Size.X / 2f;
					// Moved to the bottom left of the object bounding box, centered in the slot

					type.draw_lights(
						draw_data, furn.currentRotation.Value,
						furn.IsOn, furn.timeToTurnOnLights()
					);
				}
				else
				{
					// draw vanilla lights (how?)
					// maybe call addLights? (fix bounding box before calling it)
				}
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

		public bool can_hold(int index, SVObject obj, Furniture furniture, Farmer who)
		{
			return list[index].can_hold(obj, furniture, who);
		}

		public void set_box(int index, SVObject obj, Point position)
		{
			list[index].set_box(obj, position);
		}

		public void draw(DrawData draw_data, float top, IList<Item> items)
		{
			foreach ((Item item, int i) in items.Select((value, index) => (value, index)))
			{
				if (ModEntry.get_config().enable_slot_debug)
					list[i].draw_debug(draw_data);

				if (item is not SVObject obj) continue;
				list[i].draw_obj(draw_data, top, obj);
			}
		}

		public void draw_lights(DrawData draw_data, IList<Item> items)
		{
			foreach ((Item item, int i) in items.Select((value, index) => (value, index)))
			{
				if (item is not Furniture furn) continue;
				list[i].draw_lights(draw_data, furn);
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