using System.Data;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;


namespace FurnitureFramework
{

	class FurnitureType
	{
		private static string[] forbidden_rotation_names = {"Width", "Height"};

		string mod_id;

		public readonly string id;
		string display_name;
		string type;

		int rotations;
		List<string> rot_names = new();

		Collisions collisions;

		public readonly int price;
		int placement_rules;
		
		Texture2D texture;
		List<Rectangle> source_rects = new();
		
		Layers layers;

		bool exclude_from_random_sales;
		List<string> context_tags = new();

		Seats seats;

		Slots slots;

		// TO ADD : torch fire positions, seats, placement spots

		bool is_rug = false;
		// bool can_be_toggled = false;
		// bool can_be_placed_on = false;
		bool is_mural = false;
		public readonly bool is_table = false;

		public readonly string? shop_id = null;
		public readonly HashSet<string> shops = new();

		public FurnitureType(IContentPack pack, string id, JObject data)
		{
			#region base attributes

			mod_id = pack.Manifest.UniqueID;
			this.id = $"{mod_id}.{id}";
			display_name = JC.extract(data, "Display Name", "No Name");
			type = JC.extract(data, "Force Type", "other");
			price = JC.extract(data, "Price", 0);
			exclude_from_random_sales = JC.extract(data, "Exclude from Random Sales", false);

			placement_rules =
				+ 1 * JC.extract(data, "Indoors", 1)
				+ 2 * JC.extract(data, "Outdoors", 1)
				- 1;

			parse_rotations(data);

			#endregion

			#region textures & source rects

			string text_path = data.Value<string>("Source Image")
				?? throw new InvalidDataException($"Missing Texture for Furniture {id}");
			texture = TextureManager.load(pack.ModContent, text_path);

			JToken? rect_token = data.GetValue("Source Rect");
			if (rect_token != null && rect_token.Type != JTokenType.Null)
				JC.get_directional_rectangles(rect_token, source_rects, rot_names);
			else if (rotations == 1)
				source_rects.Add(texture.Bounds);
			else
				throw new InvalidDataException($"Missing Source Rectangles for Furniture {id}.");

			layers = new(data.GetValue("Layers"), rot_names, texture);

			#endregion

			collisions = new(data.GetValue("Collisions"), rot_names, this.id);

			seats = new(data.GetValue("Seats"), rot_names);

			slots = new(data.GetValue("Slots"), rot_names);
			is_table = slots.has_slots;

			JToken? tag_token = data.GetValue("Context Tags");
			if (tag_token != null) JC.get_list_of_string(tag_token, context_tags);

			JToken? shop_id_token = data.GetValue("Shop Id");
			if (shop_id_token != null && shop_id_token.Type == JTokenType.String)
			{
				shop_id = (string?)shop_id_token;
			}

			JToken? shops_token = data.GetValue("Shows in Shops");
			if (shops_token is JArray shops_array)
			{
				foreach (JToken shop_token in shops_array.Children())
				{
					if (shop_token.Type != JTokenType.String) continue;
					string? shop_str = (string?)shop_token;
					if (shop_str == null) continue;
					shops.Add(shop_str);
				}
			}
		}

		public void parse_rotations(JObject data)
		{
			JToken? rot_token = data.GetValue("Rotations");
			if (rot_token == null || rot_token.Type == JTokenType.Null)
				throw new InvalidDataException($"Missing Rotations for Furniture {id}.");
			
			if (rot_token is JValue rot_value)
			{
				try
				{
					rotations = (int)rot_value;
				}
				catch
				{
					throw new InvalidDataException($"Invalid Rotations for Furniture {id}, should be a number or a list of names.");
				}

				switch (rotations)
				{
					case 1:
						return;
					case 2:
						rot_names.AddRange(new string[2]{"Horizontal", "Vertical"});
						return;
					case 4:
						rot_names.AddRange(new string[4]{"Down", "Right", "Up", "Left"});
						return;
				}

				throw new InvalidDataException($"Invalid Rotations for Furniture {id}, number can be 1, 2 or 4.");
			}

			if (rot_token is JArray rot_arr)
			{
				foreach (JToken key_token in rot_arr.Children())
				{
					if (key_token.Type != JTokenType.String) continue;
					string? key = (string?)key_token;
					if (key == null) continue;

					if (rot_names.Contains(key))
					{
						ModEntry.log($"Furniture {id} has duplicate rotation key {key}", LogLevel.Warn);
						continue;
					}

					rot_names.Add(key);
				}

				rotations = rot_names.Count;

				if (rotations == 0)
				{
					rotations = 1;
					ModEntry.log($"Furniture {id} has no valid rotation key, fallback to \"Rotations\": 1", LogLevel.Warn);
				}

				return;
			}

			throw new InvalidDataException($"Invalid Rotations for Furniture {id}, should be a number or a list of names.");
		}

		public string get_string_data()
		{
			string result = display_name;
			result += $"/{type}";
			result += $"/{source_rects[0].Width/16} {source_rects[0].Height/16}";
			result += $"/1 1";	// overwritten by updateRotation
			result += $"/{rotations}";
			result += $"/{price}";
			result += $"/{placement_rules}";
			result += $"/{display_name}";
			result += $"/0";
			result += $"/{id}";	// texture path
			result += $"/{exclude_from_random_sales}";
			if (context_tags.Count > 0)
				result += $"/" + context_tags.Join(delimiter: " ");

			// ModEntry.log(result);

			return result;
		}

		public Texture2D get_icon_texture()
		{
			return texture;
		}

		public void draw(Furniture furniture, SpriteBatch sprite_batch, int x, int y, float alpha)
		{
			int rot = furniture.currentRotation.Value;
			Point pos = furniture.TileLocation.ToPoint() * Collisions.tile_game_size;

			if (furniture.isTemporarilyInvisible) return;	// taken from game code, no idea what's this property

			SpriteEffects effects = SpriteEffects.None;
			Color color = Color.White * alpha;
			float depth;
			Vector2 position;
			Rectangle bounding_box = collisions.get_bounding_box(pos, rot);

			// computing common depth :
			if (is_rug) depth = 2E-09f + furniture.TileLocation.Y;
			else
			{
				depth = bounding_box.Top + 16;
				if (is_mural) depth -= 48;
				else depth -= 8;
			}
			depth /= 10000f;

			// when the furniture is placed
			if (Furniture.isDrawingLocationFurniture)
			{
				position = new(
					bounding_box.X,
					bounding_box.Y - (source_rects[rot].Height * 4 - bounding_box.Height)
				);
			}

			// when the furniture follows the cursor
			else
			{
				position = new(
					64*x,
					64*y - (source_rects[rot].Height * 4 - bounding_box.Height)
				);
			}

			if (furniture.shakeTimer > 0) {
				position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			position = Game1.GlobalToLocal(Game1.viewport, position);

			sprite_batch.Draw(
				texture, position, source_rects[rot],
				color, 0f, Vector2.Zero, 4f, effects, depth
			);

			if (Furniture.isDrawingLocationFurniture)
			{
				layers.draw(
					sprite_batch, color,
					position, bounding_box.Bottom,
					rot
				);
			}

			// draw held object
			if (furniture.heldObject.Value is StardewValley.Object obj)
			{
				Rectangle bb = obj.boundingBox.Value;

				Vector2 draw_pos = bb.Center.ToVector2() - new Vector2(32);

				if (furniture.heldObject.Value is Furniture item)
				{
					draw_pos.Y = bb.Y + bb.Height - item.sourceRect.Height * 4;
					draw_pos = Game1.GlobalToLocal(Game1.viewport, draw_pos);

					item.drawAtNonTileSpot(
						sprite_batch, draw_pos,
						(furniture.boundingBox.Bottom - 7) / 10000f,
						alpha
					);
				}
				else
				{
					draw_pos.Y -= 16;
					draw_pos = Game1.GlobalToLocal(Game1.viewport, draw_pos);
					
					sprite_batch.Draw(
						Game1.shadowTexture,
						draw_pos + new Vector2(32f, 53f),
						Game1.shadowTexture.Bounds,
						Color.White * alpha, 0f,
						Game1.shadowTexture.Bounds.Center.ToVector2(),
						4f, SpriteEffects.None,
						bb.Bottom / 10000f
					);

					if (furniture.heldObject.Value is ColoredObject)
					{
						furniture.heldObject.Value.drawInMenu(
							sprite_batch,
							draw_pos, 1f, 1f,
							(bb.Bottom + 1) / 10000f,
							StackDrawType.Hide, Color.White, drawShadow: false
						);
					}
					else
					{
						ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(furniture.heldObject.Value.QualifiedItemId);
					
						sprite_batch.Draw(
							dataOrErrorItem2.GetTexture(),
							draw_pos,
							dataOrErrorItem2.GetSourceRect(),
							Color.White * alpha,
							0f, Vector2.Zero, 4f,
							SpriteEffects.None,
							(bb.Bottom + 1) / 10000f
						);
					}
				}
				// draw depending on heldObject own stored bounding box
			}

			// // CODE FOR ITEM ON TABLE
			// if (heldObject.Value != null)
			// {
			// 	if (heldObject.Value is Furniture furniture)
			// 	{
			// 		furniture.drawAtNonTileSpot(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - furniture.sourceRect.Height * 4 - (drawHeldObjectLow ? (-16) : 16))), (float)(boundingBox.Bottom - 7) / 10000f, alpha);
			// 	}
			// 	else
			// 	{
			// 		ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
			// 		spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow ? 32 : 85))) + new Vector2(32f, 53f), Game1.shadowTexture.Bounds, Color.White * alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)boundingBox.Bottom / 10000f);
			// 		if (heldObject.Value is ColoredObject)
			// 		{
			// 			heldObject.Value.drawInMenu(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow ? 32 : 85))), 1f, 1f, (float)(boundingBox.Bottom + 1) / 10000f, StackDrawType.Hide, Color.White, drawShadow: false);
			// 		}
			// 		else
			// 		{
			// 			spriteBatch.Draw(dataOrErrorItem2.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow ? 32 : 85))), dataOrErrorItem2.GetSourceRect(), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBox.Bottom + 1) / 10000f);
			// 		}
			// 	}
			// }

			// if ((bool)isOn && (int)furniture_type == 14)
			// {
			// 	// FIREPLACE FIRE
			// 	Rectangle boundingBoxAt = GetBoundingBoxAt(x, y);
			// 	spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 12, boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt.Bottom - 2) / 10000f);
				
			// 	spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32 - 4, boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 2047) + (double)(y * 98)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt.Bottom - 1) / 10000f);
			// }

			// else if ((bool)isOn && (int)furniture_type == 16)
			// {
			// 	// TORCHES FIRE
			// 	Rectangle boundingBoxAt2 = GetBoundingBoxAt(x, y);
			// 	spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 20, (float)boundingBox.Center.Y - 105.6f)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt2.Bottom - 2) / 10000f);
			// }

			// to keep :

			if (Game1.debugMode)
			{
				Vector2 draw_pos = new(bounding_box.X, bounding_box.Y - (source_rects[rot].Height * 4 - bounding_box.Height));
				sprite_batch.DrawString(Game1.smallFont, furniture.QualifiedItemId, Game1.GlobalToLocal(Game1.viewport, draw_pos), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
			}

			// ModEntry.print_debug = false;
		}

		public void GetSeatPositions(Furniture furniture, ref List<Vector2> list)
		{
			// This is a postfix, it keeps the original seat positions.

			int cur_rot = furniture.currentRotation.Value;
			Vector2 tile_pos = furniture.TileLocation;
			
			seats.get_seat_positions(cur_rot, tile_pos, list);
		}

		public void GetSittingDirection(Furniture furniture, Farmer who, ref int sit_dir)
		{
			int seat_index = furniture.sittingFarmers[who.UniqueMultiplayerID];
			int rot = furniture.currentRotation.Value;

			int? new_sit_dir = seats.get_sitting_direction(rot, seat_index);
			if (new_sit_dir != null) sit_dir = (int)new_sit_dir;
		}
		
		public void checkForAction(Furniture furniture, Farmer who, bool justCheckingForActivity, ref bool had_action)
		{
			if (justCheckingForActivity) return;
			// had_action is already true from original method

			// Shop
			if (shop_id != null)
			{
				if (Utility.TryOpenShopMenu(shop_id, Game1.currentLocation))
					had_action = true;
			}

			// Play Sound

			// Toggle

			// Seats
			if (seats.has_seats && !had_action)
			{
				int sit_count = furniture.GetSittingFarmerCount();
				who.BeginSitting(furniture);
				if (furniture.GetSittingFarmerCount() > sit_count)
					had_action = true;
			}

			// // Take held object (commented to avoid place-pick in same click)
			// if (furniture.heldObject.Value != null)
			// {
			// 	ModEntry.log("Taking held object through checkForAction");
			// 	StardewValley.Object value = furniture.heldObject.Value;
			// 	furniture.heldObject.Value = null;
			// 	if (who.addItemToInventoryBool(value))
			// 	{
			// 		value.performRemoveAction();
			// 		Game1.playSound("coin");
			// 		had_action =  true;
			// 	}
			// 	else
			// 	{
			// 		furniture.heldObject.Value = value;
			// 	}
			// }
		}

		public void rotate(Furniture furniture)
		{
			int rot = furniture.currentRotation.Value;
			rot = (rot + 1) % rotations;

			if (rot < 0) rot = 0;

			furniture.currentRotation.Value = rot;
			furniture.updateRotation();
		}

		public void updateRotation(Furniture furniture)
		{
			int rot = furniture.currentRotation.Value;
			Point pos = furniture.TileLocation.ToPoint() * Collisions.tile_game_size;

			furniture.boundingBox.Value = collisions.get_bounding_box(pos, rot);
		}

		public void IntersectsForCollision(Furniture furniture, Rectangle rect, ref bool collides)
		{
			int rot = furniture.currentRotation.Value;
			Point pos = furniture.TileLocation.ToPoint() * Collisions.tile_game_size;
			collides = collisions.is_colliding(rect, pos, rot);
			return;
		}

		public bool can_hold(Point tile)
		{
			// returns true if it has a table slot extending on tile at "tile"
			return false;
		}

		public void canBePlacedHere(
			Furniture furniture, GameLocation loc, Vector2 tile,
			CollisionMask collisionMask, ref bool result
		)
		{
			if (result) return;	// no need to check, it already checked the bounding box.

			// don't change this part

			if (!loc.CanPlaceThisFurnitureHere(furniture))
			{
				// false
				return;
			}

			if (!furniture.isGroundFurniture())
			{
				tile.Y = furniture.GetModifiedWallTilePosition(loc, (int)tile.X, (int)tile.Y);
			}

			CollisionMask passable_ignored = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;
			bool is_passable = furniture.isPassable();
			if (is_passable)
			{
				passable_ignored |= CollisionMask.Characters | CollisionMask.Farmers;
			}

			collisionMask &= ~(CollisionMask.Furniture | CollisionMask.Objects);

			// Actual collision detection made by collisions

			int rot = furniture.currentRotation.Value;
			if (!collisions.can_be_placed_here(rot, loc, tile.ToPoint(), collisionMask, passable_ignored))
			{
				result = false;
				return;
			}

			if (furniture.GetAdditionalFurniturePlacementStatus(loc, (int)tile.X * 64, (int)tile.Y * 64) != 0)
			{
				result = false;
				return;
			}

			result = true;
			return;
		}

		public void AllowPlacementOnThisTile(Furniture furniture, int x, int y, ref bool allow)
		{
			Rectangle tile_rect = new(
				new Point(x, y) * Collisions.tile_game_size,
				Collisions.tile_game_size
			);

			bool collides = true;
			IntersectsForCollision(furniture, tile_rect, ref collides);
			allow = !collides;
		}

		public bool place_in_slot(Furniture furniture, Point pos, Farmer who)
		{
			if (who.ActiveItem is not StardewValley.Object)
				return false;
			
			if (furniture.heldObject.Value != null) return false;

			int rot = furniture.currentRotation.Value;
			
			Point this_pos = (furniture.TileLocation * 64f).ToPoint();
			this_pos.Y += collisions.get_bounding_box(this_pos, rot).Height;
			this_pos.Y -= source_rects[rot].Height * 4;
			Point rel_pos = (pos - this_pos) / new Point(4);

			Rectangle? slot_area = slots.get_slot(rel_pos, rot);
			if (slot_area == null) return false;

			Point item_pos = slot_area.Value.Location * new Point(4) + this_pos;
			Rectangle bounding_box = new(
				item_pos,
				slot_area.Value.Size * new Point(4)
			);

			StardewValley.Object obj_inst = (StardewValley.Object)who.ActiveItem.getOne();

			obj_inst.Location = furniture.Location;
			obj_inst.TileLocation = item_pos.ToVector2() / 64f;
			obj_inst.boundingBox.Value = bounding_box;

			furniture.heldObject.Value = obj_inst;

			who.reduceActiveItemByOne();
			Game1.currentLocation.playSound("woodyStep");

			return true;
		}

		public Item? remove_from_slot(Furniture furniture, Point pos, Farmer who)
		{
			if (furniture.heldObject.Value == null) return null;

			int rot = furniture.currentRotation.Value;

			Point this_pos = (furniture.TileLocation * 64f).ToPoint();
			this_pos.Y += collisions.get_bounding_box(this_pos, rot).Height;
			this_pos.Y -= source_rects[rot].Height * 4;
			Point rel_pos = (pos - this_pos) / new Point(4);

			Rectangle? slot_area = slots.get_slot(rel_pos, rot);
			if (slot_area == null) return null;

			StardewValley.Object value = furniture.heldObject.Value;
			furniture.heldObject.Value = null;
			if (who.addItemToInventoryBool(value))
			{
				value.performRemoveAction();
				Game1.playSound("coin");
				return value;
			}
			else
			{
				furniture.heldObject.Value = value;
			}

			return null;
		}

		// public void performObjectDropInAction(
		// 	Furniture furniture, ref bool result, Item dropInItem,
		// 	bool probe, Farmer who, bool returnFalseIfItemConsumed = false
		// )
		// {
		// 	GameLocation location = furniture.Location;
		// 	if (location == null)
		// 	{
		// 		result = false;
		// 		return;
		// 	}

		// 	if (dropInItem is not StardewValley.Object @object)
		// 	{
		// 		result = false;
		// 		return;
		// 	}

		// 	if (
		// 		is_table &&
		// 		furniture.heldObject.Value == null &&
		// 		!@object.bigCraftable.Value && !(@object is Wallpaper) &&
		// 		(@object is not Furniture item || (item.getTilesWide() == 1 && item.getTilesHigh() == 1)))
		// 	{
		// 		result = true;

		// 		if (probe) return;

		// 		furniture.heldObject.Value = (StardewValley.Object)@object.getOne();
		// 		furniture.heldObject.Value.Location = furniture.Location;
		// 		furniture.heldObject.Value.TileLocation = furniture.TileLocation;
		// 		furniture.heldObject.Value.boundingBox.X = furniture.boundingBox.X;
		// 		furniture.heldObject.Value.boundingBox.Y = furniture.boundingBox.Y;
		// 		furniture.heldObject.Value.performDropDownAction(who);
		// 		location.playSound("woodyStep");
		// 		if (who != null)
		// 		{
		// 			who.reduceActiveItemByOne();
		// 			if (returnFalseIfItemConsumed)
		// 			{
		// 				result = false;
		// 				return;
		// 			}
		// 		}

		// 		return;
		// 	}

		// 	result = false;
		// }
	}
}