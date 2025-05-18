using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;

namespace FurnitureFramework.FType
{
	struct DrawData
	{
		public readonly SpriteBatch sprite_batch;
		public Texture2D texture;
		public Vector2 position;
		public Rectangle source_rect;
		public Color color = Color.White;
		public float rotation = 0f;
		public Vector2 origin = Vector2.Zero;
		public float scale = 4f;
		public SpriteEffects effects = SpriteEffects.None;
		public float depth;
		
		public bool is_on = false;
		public bool is_dark = false;
		public Point rect_offset = Point.Zero;

		public DrawData(SpriteBatch sprite_batch) { this.sprite_batch = sprite_batch; }

		public void draw()
		{
			Rectangle rect = source_rect.Clone();
			if (is_on)
				rect.X += rect.Width;
			if (is_dark)
				rect.Y += rect.Height;
			rect.Location += rect_offset;

			sprite_batch.Draw(
				texture, position, rect,
				color, rotation, origin, scale,
				effects, depth
			);
		}
	}

	partial class FurnitureType
	{

		// for drawInMenu transpiler
		private static Rectangle get_icon_source_rect(Furniture furniture)
		{
			if (Pack.FurniturePack.try_get_type(furniture, out FurnitureType? type))
				return type.icon_rect;

			return ItemRegistry.GetDataOrErrorItem(furniture.QualifiedItemId).GetSourceRect();
		}

		public void drawAtNonTileSpot(
			Furniture furniture, SpriteBatch sprite_batch,
			Vector2 position, float depth, float alpha = 1f
		)
		{
			// Used for FF Furniture placed in vanilla slot
			DrawData draw_data = new(sprite_batch);

			draw_data.position = position;
			draw_data.position.Y += furniture.sourceRect.Height * 4;
			draw_data.color = Color.White * alpha;
			draw_data.depth = depth;

			draw(furniture, draw_data, draw_in_slot: true);
		}

		public void draw(Furniture furniture, SpriteBatch sprite_batch, int x, int y, float alpha)
		{
			DrawData draw_data = new(sprite_batch);

			draw_data.color = Color.White * alpha;
			Rectangle bounding_box = furniture.boundingBox.Value;

			// when the furniture is placed
			if (Furniture.isDrawingLocationFurniture)
			{
				draw_data.position = new(
					bounding_box.X,
					bounding_box.Bottom
				);
			}

			// when the furniture follows the cursor
			else
			{
				draw_data.position = new Vector2(x, y) * 64f;
				draw_data.position.Y += furniture.boundingBox.Height;
			}

			draw_data.position = Game1.GlobalToLocal(Game1.viewport, draw_data.position);

			draw(furniture, draw_data, false);

			// to keep (?) :

			if (Game1.debugMode)
			{
				Vector2 draw_pos = new(bounding_box.X, bounding_box.Y - (draw_data.source_rect.Height * 4 - bounding_box.Height));
				sprite_batch.DrawString(Game1.smallFont, furniture.QualifiedItemId, Game1.GlobalToLocal(Game1.viewport, draw_pos), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
			}
		}

		public void draw(Furniture furniture, DrawData draw_data, bool draw_in_slot = true)
		{
			draw_data.texture = texture.get();

			if (furniture.isTemporarilyInvisible) return;	// taken from game code, no idea what this property is

			if (furniture is FishTankFurniture fish_tank)
				draw_fish_tank(fish_tank, draw_data);

			if (can_be_toggled)
				draw_data.is_on = furniture.IsOn;
			else draw_data.is_on = false;

			if (time_based)
				draw_data.is_dark = furniture.timeToTurnOnLights();
			else draw_data.is_dark = false;

			draw_data.rect_offset = rect_offset + animation.get_offset();

			if (furniture.shakeTimer > 0) {
				draw_data.position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}

			float top = furniture.boundingBox.Top;
			int rot = furniture.currentRotation.Value;

			// Just in case there is a corrupted furniture.
			if (rot >= rotations.Count)
			{
				rot = 0;
				furniture.currentRotation.Set(0);
			}

			if (Furniture.isDrawingLocationFurniture)
			{
				// when placed

				if (draw_in_slot)
					layers[rot].draw_one(draw_data, top, ignore_depth: true);
				else layers[rot].draw_all(draw_data, top);

				light_sources[rot].draw_glows(draw_data, furniture.IsOn, furniture.timeToTurnOnLights());

				if (!draw_in_slot)
				{
					// draw items in slots
					if (furniture.heldObject.Value is Chest chest)
						slots[rot].draw(draw_data, top, chest.Items);
					else initialize_slots(furniture, rot);
				}
			}

			else
			{
				// while placing
				
				if (!placing_animate)
					draw_data.rect_offset -= animation.get_offset();

				if (placing_layers) layers[rot].draw_all(draw_data, top);
				else layers[rot].draw_one(draw_data, top);
			}
		}

		private void draw_fish_tank(FishTankFurniture furniture, DrawData draw_data)
		{
			// Code copied from FishTankFurniture.draw

			Vector2 fish_sort_region = furniture.GetFishSortRegion();
			Rectangle tank_bounds = furniture.GetTankBounds();

			// Fishes
			for (int i = 0; i < furniture.tankFish.Count; i++)
			{
				TankFish tankFish = furniture.tankFish[i];
				float num = Utility.Lerp(
					fish_sort_region.Y,
					fish_sort_region.X,
					tankFish.zPosition / 20f
				);
				num += 1E-07f * i;
				tankFish.Draw(draw_data.sprite_batch, draw_data.color.A, num);
			}

			// Floor decorations
			foreach (KeyValuePair<Rectangle, Vector2>? pair in furniture.floorDecorations)
			{
				if (pair == null) continue;

				Vector2 pos = pair.Value.Value;
				Rectangle key = pair.Value.Key;
				
				DrawData fish_data = draw_data;

				fish_data.texture = furniture.GetAquariumTexture();

				fish_data.position = new Vector2(
					tank_bounds.Left,
					tank_bounds.Bottom
				) + new Vector2(1, -1) * pos * 4f - new Vector2(0, 4);
				fish_data.position = Game1.GlobalToLocal(fish_data.position);

				fish_data.source_rect = key;
				fish_data.origin = new Vector2(key.Width / 2, key.Height - 4);
				fish_data.depth = Utility.Lerp(
					fish_sort_region.Y,
					fish_sort_region.X,
					pos.Y / 20f
				) - 1E-06f;

				fish_data.draw();
			}

			// Bubbles
			foreach (Vector4 bubble in furniture.bubbles)
			{
				DrawData bubble_data = draw_data;

				bubble_data.texture = furniture.GetAquariumTexture();

				bubble_data.position = new Vector2(
					tank_bounds.Left + bubble.X,
					tank_bounds.Bottom - bubble.Y - bubble.Z * 4f - 4
				);
				bubble_data.position = Game1.GlobalToLocal(bubble_data.position);

				bubble_data.source_rect = new Rectangle(0, 240, 16, 16);
				bubble_data.origin = new Vector2(8f, 8f);
				bubble_data.scale = 4f * bubble.W;
				bubble_data.depth = Utility.Lerp(
					fish_sort_region.Y,
					fish_sort_region.X,
					bubble.Z / 20f
				) - 1E-06f;

				bubble_data.draw();
			}
		}

		private void draw_lights(Furniture furniture, SpriteBatch sprite_batch)
		{
			DrawData draw_data = new(sprite_batch);

			Rectangle bounding_box = furniture.boundingBox.Value;
			draw_data.position = new(bounding_box.X, bounding_box.Bottom);
			draw_data.position = Game1.GlobalToLocal(Game1.viewport, draw_data.position);

			int rot = furniture.currentRotation.Value;

			draw_lights(draw_data, rot, furniture.IsOn, furniture.timeToTurnOnLights());

			// items in slots
			if (furniture.heldObject.Value is Chest chest)
				slots[rot].draw_lights(draw_data, chest.Items);
			else initialize_slots(furniture, rot);
		}

		public void draw_lights(DrawData draw_data, int rot, bool is_on, bool is_dark)
		{
			draw_data.texture = texture.get();
			light_sources[rot].draw_sources(draw_data, is_on, is_dark);
		}
	}
}