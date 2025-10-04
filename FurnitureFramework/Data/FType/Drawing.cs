using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;
using FurnitureFramework.Data.FType.Properties;
using HarmonyLib;
using System.Text.Json.Serialization;

namespace FurnitureFramework.Data.FType
{
	public struct DrawData
	{
		[JsonIgnore]
		public readonly SpriteBatch sprite_batch;
		public string mod_id;
		[JsonIgnore]
		public Texture2D? texture;
		public string texture_path;
		public Vector2 position;
		public Rectangle source_rect;
		public Color color = Color.White;
		public float rotation = 0f;
		public Vector2 origin = Vector2.Zero;
		public float scale = 4f;
		[JsonIgnore]
		public SpriteEffects effects = SpriteEffects.None;
		public float depth;

		public bool is_on = false;
		public bool is_dark = false;
		public Point rect_offset = Point.Zero;

		public DrawData(SpriteBatch sprite_batch, string mod_id, string texture_path)
		{
			this.sprite_batch = sprite_batch;
			this.mod_id = mod_id;
			this.texture_path = texture_path;
		}

		public void Draw()
		{
			Rectangle rect = source_rect.Clone();
			if (is_on)
				rect.X += rect.Width;
			if (is_dark)
				rect.Y += rect.Height;
			rect.Location += rect_offset;

			texture ??= ModEntry.GetHelper().GameContent.Load<Texture2D>($"FF/{mod_id}/{texture_path}");

			sprite_batch.Draw(
				texture, position, rect,
				color, rotation, origin, scale,
				effects, depth
			);
		}
	}
	
	public partial class FType
	{
		Rectangle GetIconSourceRect()
		{
			if (IconRect != null)
				return (Rectangle)IconRect;
			return Layers[Rotations.First()].First().SourceRect;
		}

		public string GetSourceImage(Furniture furniture)
		{
			if (SourceImage.Count == 1)
				return SourceImage[""];

			return furniture.modData["FF.SourceImage"];
		}

		Point GetOffset(Furniture furniture)
		{
			if (SourceRectOffsets.Count == 0)
				return Point.Zero;

			return SourceRectOffsets[furniture.modData["FF.RectVariant"]];
		}

		// for drawInMenu transpiler
		private static Rectangle GetIconSourceRect(Furniture furniture)
		{
			if (FPack.FPack.TryGetType(furniture, out FType? type))
			{
				Rectangle result = type.GetIconSourceRect();
				result.Location += type.GetOffset(furniture);
				return result;
			}
			return ItemRegistry.GetDataOrErrorItem(furniture.QualifiedItemId).GetSourceRect();
		}

		public void drawAtNonTileSpot(
			Furniture furniture, SpriteBatch sprite_batch,
			Vector2 position, float depth, float alpha = 1f
		)
		{
			// Used for FF Furniture placed in vanilla slot
			DrawData draw_data = new(sprite_batch, ModID, GetSourceImage(furniture))
			{
				position = position,
				color = Color.White * alpha,
				depth = depth
			};

			draw_data.position.Y += furniture.sourceRect.Height * 4;

			draw(furniture, draw_data, draw_in_slot: true);
		}

		public void draw(Furniture furniture, SpriteBatch sprite_batch, int x, int y, float alpha)
		{
			DrawData draw_data = new(sprite_batch, ModID, GetSourceImage(furniture))
			{
				color = Color.White * alpha
			};

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
			// draw_data.texture_path = texture.get();

			if (furniture.isTemporarilyInvisible) return;   // taken from game code, no idea what this property is

			if (furniture is FishTankFurniture fish_tank)
				DrawFishTank(fish_tank, draw_data);

			draw_data.is_on = Toggle && furniture.IsOn;
			draw_data.is_dark = TimeBased && furniture.timeToTurnOnLights();

			draw_data.rect_offset = Animation.GetOffsetLoop() + GetOffset(furniture);

			if (furniture is StorageFurniture && furniture.modData.ContainsKey("FF.storage_open_state"))
			{
				bool is_open = bool.Parse(furniture.modData["FF.storage_open_state"]);
				long start_time = long.Parse(furniture.modData["FF.storage_anim_start"]);
				if (is_open) draw_data.rect_offset += OpeningAnimation.GetOffsetOnce(start_time);
				else draw_data.rect_offset += ClosingAnimation.GetOffsetOnce(start_time);
			}

			if (furniture.shakeTimer > 0)
			{
				draw_data.position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}

			float top = furniture.boundingBox.Top;
			string rot = GetRot(furniture);

			if (Furniture.isDrawingLocationFurniture)
			{
				// when placed

				if (draw_in_slot)
					Layers[rot][0].Draw(draw_data, top, ignore_depth: true);
				else Layers[rot].DrawAll(draw_data, top);

				Lights[rot].DrawGlows(draw_data, furniture.IsOn, furniture.timeToTurnOnLights());

				if (!draw_in_slot)
				{
					// draw items in slots
					if (furniture.heldObject.Value is Chest chest)
						Slots[rot].Draw(draw_data, top, chest.Items);
					else InitializeSlots(furniture, rot);
				}
			}

			else
			{
				// while placing

				if (!AnimateWhenPlacing)
					draw_data.rect_offset -= Animation.GetOffsetLoop();

				if (DrawLayersWhenPlacing) Layers[rot].DrawAll(draw_data, top);
				else Layers[rot][0].Draw(draw_data, top);
			}
		}

		private void DrawFishTank(FishTankFurniture furniture, DrawData draw_data)
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

			draw_data.texture = ModEntry.GetHelper().GameContent.Load<Texture2D>("LooseSprites/AquariumFish");
			// From FishTankFurniture.GetAquariumTexture
			// Going through FF's content's pipeline causes a "Disposed object" error,
			// maybe missing some invalidation somewhere.

			// Floor decorations
			foreach (KeyValuePair<Rectangle, Vector2>? pair in furniture.floorDecorations)
			{
				if (pair == null) continue;

				Vector2 pos = pair.Value.Value;
				Rectangle key = pair.Value.Key;

				DrawData fish_data = draw_data;

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

				fish_data.Draw();
			}

			// Bubbles
			foreach (Vector4 bubble in furniture.bubbles)
			{
				DrawData bubble_data = draw_data;

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

				bubble_data.Draw();
			}
		}

		private void DrawLights(Furniture furniture, SpriteBatch sprite_batch)
		{
			DrawData draw_data = new(sprite_batch, ModID, GetSourceImage(furniture));

			Rectangle bounding_box = furniture.boundingBox.Value;
			draw_data.position = new(bounding_box.X, bounding_box.Bottom);
			draw_data.position = Game1.GlobalToLocal(Game1.viewport, draw_data.position);

			string rot = Rotations[furniture.currentRotation.Value];

			DrawLights(furniture, draw_data);

			// items in slots
			if (furniture.heldObject.Value is Chest chest)
				Slots[rot].DrawLights(draw_data, chest.Items);
			else InitializeSlots(furniture, rot);
		}

		public void DrawLights(Furniture furniture, DrawData draw_data)
		{
			string rot = Rotations[furniture.currentRotation.Value];
			draw_data.texture_path = GetSourceImage(furniture);
			draw_data.mod_id = ModID;
			Lights[rot].DrawSources(draw_data, furniture.IsOn, furniture.timeToTurnOnLights());
		}

		public static void addLights(Furniture furniture)
		{
			if (furniture.heldObject.Value is Chest chest)
			{
				foreach (Item item in chest.Items)
				{
					if (item is Furniture held_furn)
						held_furn.addLights();
				}
			}
		}
	}
}