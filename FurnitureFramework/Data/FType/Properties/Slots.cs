using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace FurnitureFramework.Data.FType.Properties
{
	using SVObject = StardewValley.Object;

	[JsonConverter(typeof(FieldConverter<Slot>))]
	public class Slot : Field
	{
		[Required]
		[Directional]
		public Rectangle Area;
		public Point Offset = Point.Zero;
		public bool DrawShadow = true;
		public Point ShadowOffset = Point.Zero;
		public Depth Depth = new();
		public Point MaxSize = new(1, 1);

		[JsonConverter(typeof(ColorConverter))]
		public Color? DebugColor;
		public string? Condition;

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			is_valid = true;
		}

		static Texture2D debug_texture;

		static Slot()
		{
			debug_texture = new(Game1.graphics.GraphicsDevice, 1, 1);
			debug_texture.SetData(new[] { Color.White });
		}

		#region methods

		public bool CanHold(Farmer? who, Furniture furniture, Item held_item)
		{
			bool result = true;

			if (Condition is not null)
			{
				result &= GameStateQuery.CheckConditions(
					Condition,
					location: furniture.Location,
					player: who,
					targetItem: furniture,
					inputItem: held_item
				);
			}

			if (held_item is Furniture held_furn)
			{
				Point size = held_furn.boundingBox.Value.Size / new Point(64);
				result &= size.X <= MaxSize.X && size.Y <= MaxSize.Y;
			}

			return result;
		}

		public Point GetLocalPos(Item item)
		{
			Point pos = new Point(Area.Center.X, Area.Bottom) * new Point(4);
			pos += Offset * new Point(4);
			if (item is not Furniture && DrawShadow) pos.Y -= 4;
			return pos;
		}

		public void SetBox(SVObject held_obj, Point position)
		{
			Point size = held_obj.boundingBox.Value.Size;
			position += GetLocalPos(held_obj);
			// position is in the top left corner
			position.X -= size.X / 2;
			position.Y -= size.Y;

			held_obj.boundingBox.Value = new Rectangle(position, size);
		}

		public void DrawObj(DrawData draw_data, float top, Item item)
		{
			draw_data.position += GetLocalPos(item).ToVector2();
			// Position is set to the bottom center of the slot area
			draw_data.rect_offset = Point.Zero;

			draw_data.depth = Depth.GetValue(top);
			draw_data.depth = MathF.BitIncrement(draw_data.depth);
			// plus epsilon to make sure it's drawn over the layer at the same depth

			if (item is Furniture furn)
			{
				draw_data.position.X -= furn.boundingBox.Value.Size.X / 2f;
				// Moved to the bottom left of the furniture bounding box, centered in the slot

				if (FPack.FPack.TryGetType(furn, out FType? type))
				{
					draw_data.mod_id = type.ModID;
					draw_data.texture_path = type.GetSourceImage(furn);
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
			
			draw_data.is_on = false;
			draw_data.is_dark = false;
			
			if (DrawShadow)
			{
				DrawData shadow_data = draw_data;	// should clone value fields like position
				shadow_data.texture = Game1.shadowTexture;
				shadow_data.source_rect = Game1.shadowTexture.Bounds;
				shadow_data.position.Y += 4;
				shadow_data.position += ShadowOffset.ToVector2() * 4;
				shadow_data.position -= shadow_data.source_rect.Size.ToVector2() * new Vector2(2, 4);
				// draw pos is on top left of Shadow texture
				
				shadow_data.Draw();

				draw_data.depth = MathF.BitIncrement(draw_data.depth);
				// plus epsilon to make sure it's drawn over the shadow
			}

			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
			draw_data.source_rect = dataOrErrorItem.GetSourceRect();
			draw_data.position -= draw_data.source_rect.Size.ToVector2() * new Vector2(2, 4);
			// Moved to the top left of the item's source rect, centered in the slot

			if (item is ColoredObject)
			{
				item.drawInMenu(
					draw_data.sprite_batch,
					draw_data.position, 1f, 1f,
					draw_data.depth,
					StackDrawType.Hide, Color.White, drawShadow: false
				);
			}
			else
			{
				draw_data.texture = dataOrErrorItem.GetTexture();
				draw_data.Draw();
			}
		}

		public void DrawDebug(DrawData draw_data)
		{
			draw_data.texture = debug_texture;
			draw_data.position += Area.Location.ToVector2() * 4f;
			draw_data.source_rect = Area;
			if (DebugColor is null)
				draw_data.color = ModEntry.GetConfig().slot_debug_default_color;
			else draw_data.color = (Color)DebugColor;
			draw_data.color *= ModEntry.GetConfig().slot_debug_alpha;
			draw_data.depth = float.MaxValue;
			
			draw_data.Draw();
		}

		public void DrawLights(DrawData draw_data, Furniture furn)
		{
			if (FPack.FPack.TryGetType(furn, out FType? type))
			{
				// draw custom lights

				draw_data.position += GetLocalPos(furn).ToVector2();
				// Position is set to the bottom center of the slot area
				draw_data.position.X -= furn.boundingBox.Value.Size.X / 2f;
				// Moved to the bottom left of the object bounding box, centered in the slot

				type.DrawLights(furn, draw_data);
			}
		}

		#endregion
	}

	public class SlotList : List<Slot>
	{

		public int GetSlot(Point? rel_pos, Chest chest, Farmer? who, Furniture furn, [NotNull] ref Item? item)
		{	
			// Searches for filled slot if obj is null, else for empty slot.
			// Finds first valid slot in either condition, checks cursor position if rel_pos is not null.

			bool skipped_invalid = false;
			foreach ((Slot slot, int index) in this.Select((value, index) => (value, index)))
			{
				if (rel_pos is not null && !slot.Area.Contains(rel_pos.Value)) continue;
				if ((item is null) == (chest.Items[index] is null)) continue;

				if (item is null)
				{
					if (chest.Items[index] is Item item_)
					{
						item = item_;
						return index;
					}
				}
				else
				{
					if (slot.CanHold(who, furn, item)) return index;
					skipped_invalid = true;
				}
			}
			
			if (skipped_invalid) Game1.showRedMessage("This item cannot be placed here.");
			// held item doesn't match condition
			// or held furniture is too big

			item ??= new SVObject();
			return -1;
		}

		public void Draw(DrawData draw_data, float top, IList<Item> items)
		{
			foreach ((Item item, int i) in items.Select((value, index) => (value, index)))
			{
				if (ModEntry.GetConfig().enable_slot_debug)
					this[i].DrawDebug(draw_data);

				if (item is null) continue;
				this[i].DrawObj(draw_data, top, item);
			}
		}

		public void DrawLights(DrawData draw_data, IList<Item> items)
		{
			foreach ((Item item, int i) in items.Select((value, index) => (value, index)))
			{
				if (item is not Furniture furn) continue;
				this[i].DrawLights(draw_data, furn);
			}
		}

	}
}