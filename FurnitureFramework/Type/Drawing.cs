using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;

namespace FurnitureFramework.Type
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

		public void drawAtNonTileSpot(
			Furniture furniture, SpriteBatch sprite_batch,
			Vector2 position, float depth, float alpha = 1f
		)
		{
			int rot = furniture.currentRotation.Value;

			if (furniture.isTemporarilyInvisible) return;	// taken from game code, no idea what this property is

			SpriteEffects effects = SpriteEffects.None;
			Color color = Color.White * alpha;
			Rectangle source_rect = source_rects[rot].Clone();

			if (furniture.IsOn)
				source_rect.X += source_rect.Width;

			if (furniture.timeToTurnOnLights() && time_based)
				source_rect.Y += source_rect.Height;

			if (is_animated)
			{
				long time_ms = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				int frame = (int)(time_ms / frame_length) % frame_count;
				Point c_anim_offset = anim_offset * new Point(frame);
				source_rect.Location += c_anim_offset;
			}

			sprite_batch.Draw(
				texture.get(), position, source_rect,
				color, 0f, Vector2.Zero, 4f, effects, depth
			);
		}

		private void draw_fish_tank(FishTankFurniture furniture, SpriteBatch sprite_batch, float alpha)
		{
			// Code copied from FishTankFurniture.draw

			for (int i = 0; i < furniture.tankFish.Count; i++)
			{
				TankFish tankFish = furniture.tankFish[i];
				float num = Utility.Lerp(
					furniture.GetFishSortRegion().Y,
					furniture.GetFishSortRegion().X,
					tankFish.zPosition / 20f
				);
				num += 1E-07f * i;
				tankFish.Draw(sprite_batch, alpha, num);
			}

			for (int j = 0; j < furniture.floorDecorations.Count; j++)
			{
				KeyValuePair<Rectangle, Vector2>? pair = furniture.floorDecorations[j];
				if (pair is not null)
				{
					Vector2 pos = pair.Value.Value;
					Rectangle key = pair.Value.Key;
					float layerDepth = Utility.Lerp(
						furniture.GetFishSortRegion().Y,
						furniture.GetFishSortRegion().X,
						pos.Y / 20f
					) - 1E-06f;
					sprite_batch.Draw(
						furniture.GetAquariumTexture(),
						Game1.GlobalToLocal(
							new Vector2(
								furniture.GetTankBounds().Left + pos.X * 4f,
								furniture.GetTankBounds().Bottom - 4 - pos.Y * 4f
							)
						),
						key, Color.White * alpha, 0f,
						new Vector2(key.Width / 2, key.Height - 4),
						4f, SpriteEffects.None, layerDepth
					);
				}
			}

			foreach (Vector4 bubble in furniture.bubbles)
			{
				float layerDepth2 = Utility.Lerp(
					furniture.GetFishSortRegion().Y,
					furniture.GetFishSortRegion().X,
					bubble.Z / 20f
				) - 1E-06f;
				sprite_batch.Draw(
					furniture.GetAquariumTexture(),
					Game1.GlobalToLocal(
						new Vector2(
							furniture.GetTankBounds().Left + bubble.X,
							furniture.GetTankBounds().Bottom - 4 - bubble.Y - bubble.Z * 4f
						)
					),
					new Rectangle(0, 240, 16, 16),
					Color.White * alpha,
					0f, new Vector2(8f, 8f), 4f * bubble.W,
					SpriteEffects.None, layerDepth2
				);
			}
		}

		public void draw(Furniture furniture, SpriteBatch sprite_batch, int x, int y, float alpha)
		{
			DrawData draw_data = new(sprite_batch);
			draw_data.texture = texture.get();

			int rot = furniture.currentRotation.Value;

			if (furniture.isTemporarilyInvisible) return;	// taken from game code, no idea what this property is

			if (furniture is FishTankFurniture fish_tank)
				draw_fish_tank(fish_tank, sprite_batch, alpha);

			draw_data.color *= alpha;
			Rectangle bounding_box = furniture.boundingBox.Value;
			draw_data.source_rect = source_rects[rot].Clone();

			if (can_be_toggled)
				draw_data.is_on = furniture.IsOn;

			if (time_based)
				draw_data.is_dark = furniture.timeToTurnOnLights();

			draw_data.rect_offset = rect_offset;
			if (is_animated)
			{
				long time_ms = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				int frame = (int)(time_ms / frame_length) % frame_count;
				draw_data.rect_offset += anim_offset * new Point(frame);
			}

			// computing common depth
			if (p_type == PlacementType.Rug)
				draw_data.depth = 2E-09f + furniture.TileLocation.Y;
			else
			{
				draw_data.depth = bounding_box.Top;
				if (p_type == PlacementType.Mural)
					draw_data.depth -= 32;
				else draw_data.depth += 16;
			}
			draw_data.depth /= 10000f;

			// when the furniture is placed
			if (Furniture.isDrawingLocationFurniture)
			{
				draw_data.position = new(
					bounding_box.X,
					bounding_box.Y - (draw_data.source_rect.Height * 4 - bounding_box.Height)
				);
			}

			// when the furniture follows the cursor
			else
			{
				draw_data.position = new(
					64*x,
					64*y - (draw_data.source_rect.Height * 4 - bounding_box.Height)
				);
			}

			if (furniture.shakeTimer > 0) {
				draw_data.position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			draw_data.position = Game1.GlobalToLocal(Game1.viewport, draw_data.position);

			draw_data.draw();

			if (Furniture.isDrawingLocationFurniture)
			{
				layers[rot].draw(draw_data, bounding_box.Top);

				light_sources.draw_glows(
					sprite_batch,
					furniture.boundingBox.Value.Location.ToVector2(),
					rot, furniture.IsOn, furniture.timeToTurnOnLights()
				);
				// TO CHANGE

				// draw items in slots
				if (furniture.heldObject.Value is Chest chest)
				{
					slots[rot].draw(draw_data, bounding_box.Top, chest.Items);
					// draw depending on heldObject own stored bounding box
				}
				else initialize_slots(furniture, rot);
			}

			// to keep :

			if (Game1.debugMode)
			{
				Vector2 draw_pos = new(bounding_box.X, bounding_box.Y - (draw_data.source_rect.Height * 4 - bounding_box.Height));
				sprite_batch.DrawString(Game1.smallFont, furniture.QualifiedItemId, Game1.GlobalToLocal(Game1.viewport, draw_pos), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
			}
		}

		private void draw_lighting(Furniture furniture, SpriteBatch sprite_batch)
		{
			light_sources.draw_lights(
				sprite_batch,
				furniture.boundingBox.Value.Location.ToVector2(),
				furniture.currentRotation.Value,
				furniture.IsOn, furniture.timeToTurnOnLights()
			);

			// items in slots
			if (furniture.heldObject.Value is Chest chest)
			{
				foreach (Item? item in chest.Items)
				{
					if (
						item is Furniture furn_item &&
						Pack.FurniturePack.try_get_type(furn_item, out FurnitureType? type)
					)
					{
						type.draw_lighting(furn_item, sprite_batch);
					}
				}
			}
		}
	}
}