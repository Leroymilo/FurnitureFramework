using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace FurnitureFramework
{
	class Collisions
	{
		public static readonly Point tile_game_size = new(64); 

		#region CollisionData

		private class CollisionData
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;

			Point size = new();
			Point game_size;

			bool has_tiles = false;
			HashSet<Point> tiles = new();
			HashSet<Point> game_tiles = new();
			// in game coordinates (tile coordinates * 64)

			public readonly bool is_passable = false;

			#region CollisionData Parsing

			public CollisionData(JObject col_object)
			{
				// Parsing required collision box size

				error_msg = "Missing Width in Collision Data.";
				JToken? w_token = col_object.GetValue("Width");
				if (w_token == null || w_token.Type != JTokenType.Integer) return;
				size.X = (int)w_token;

				error_msg = "Missing Height in Collision Data.";
				JToken? h_token = col_object.GetValue("Height");
				if (h_token == null || h_token.Type != JTokenType.Integer) return;
				size.Y = (int)h_token;

				is_valid = true;
				game_size = size * tile_game_size;

				// Parsing optional custom collision map

				JToken? map_token = col_object.GetValue("Map");
				if (map_token != null && map_token.Type == JTokenType.String)
				{
					string? map_string = (string?)map_token;
					if (map_string == null)
					{
						ModEntry.log(
							$"Map at {map_token.Path} must be a string.",
							LogLevel.Warn
						);
						ModEntry.log($"Ignoring Map.", LogLevel.Warn);
						return;
					}
					
					string[] map = map_string.Split('/');
					if (map.Length != size.Y)
					{
						ModEntry.log(
							$"Map at {map_token.Path} must have as many rows as Height : {size.Y}.",
							LogLevel.Warn
						);
						ModEntry.log($"Ignoring Map.", LogLevel.Warn);
						return;
					}

					for (int y = 0; y < size.Y; y++)
					{
						string map_line = map[y];
						if (map_line.Length != size.X)
						{
							ModEntry.log(
								$"All lines of Map at {map_token.Path} must be as long as Height : {size.X}.",
								LogLevel.Warn
							);
							ModEntry.log($"Ignoring Map.", LogLevel.Warn);
							return;
						}

						for (int x = 0; x < size.X; x++)
						{
							if (map[y][x] == 'X')
							{
								Point tile = new(x, y);
								tiles.Add(tile);
								game_tiles.Add(tile * tile_game_size);
							}
						}
					}

					has_tiles = true;
					if (tiles.Count < size.X * size.Y)
						is_passable = true;
					// to allow the player to un-sit properly
				}
			}

			#endregion

			#region CollisionData Methods

			public Rectangle get_bounding_box(Point this_game_pos)
			{
				return new(
					this_game_pos,
					game_size
				);
			}

			public bool is_colliding(Rectangle rect, Point this_game_pos)
			{
				Rectangle bounding_box = get_bounding_box(this_game_pos);

				if (!bounding_box.Intersects(rect))
					return false; // bounding box does not intersect

				if (!has_tiles) return true;	// no custom collision map				
				
				foreach (Point tile_game_pos in game_tiles)
				{
					Rectangle tile_rect = new(
						this_game_pos + tile_game_pos,
						tile_game_size
					);
					if (tile_rect.Intersects(rect))
					{
						return true;	// collision map tile intersects
					}
				}
				return false;	// no collision map tile intersects
			}

			public bool can_be_placed_here(
				GameLocation loc, Point tile_pos,
				CollisionMask collisionMask, CollisionMask passable_ignored)
			{
				if (has_tiles)
				{
					foreach (Point tile in tiles)
					{
						if (!is_tile_free(
							tile + tile_pos, loc,
							collisionMask, passable_ignored
						))
						{
							return false;
						}
					}
					return true;
				}

				else
				{
					for (int y = 0; y < size.Y; y++)
					{
						for (int x = 0; x < size.X; x++)
						{
							if (can_place_on_table(new Point(x, y) + tile_pos, loc))
							{
								return true;
							}

							if (!is_tile_free(
								new Point(x, y) + tile_pos, loc,
								collisionMask, passable_ignored
							))
							{
								return false;
							}
						}
					}
					return true;
				}
			}

			private bool is_tile_free(
				Point tile, GameLocation loc,
				CollisionMask collisionMask, CollisionMask passable_ignored
			)
			{
				Vector2 v_tile = tile.ToVector2();
				Vector2 center = (v_tile + new Vector2(0.5f)) * 64;

				// checking for general map placeability
				if (!loc.isTilePlaceable(v_tile, false))
				{
					return false;
				}

				foreach (Furniture item in loc.furniture)
				{
					// obstructed by non-rug furniture
					if (
						(item.furniture_type.Value != 12 /*|| furniture.furniture_type.Value == 12*/) &&
						item.GetBoundingBox().Contains(center) &&
						!item.AllowPlacementOnThisTile(tile.X, tile.Y)
					)
					{
						return false;
					}
				}

				if (loc.objects.TryGetValue(v_tile, out var value) /*&& (!value.isPassable() || !furniture.isPassable())*/)
				{
					return false;
				}

				/*if (!furniture.isGroundFurniture())
				{*/
					if (loc.IsTileOccupiedBy(v_tile, collisionMask, passable_ignored))
					{
						return false;
					}
				//}

				if (loc.IsTileBlockedBy(v_tile, collisionMask, passable_ignored))
				{
					return false;
				}

				if (
					loc.terrainFeatures.ContainsKey(v_tile) &&
					loc.terrainFeatures[v_tile] is HoeDirt hoeDirt &&
					hoeDirt.crop != null
				)
				{
					return false;
				}

				return true;
			}

			private bool can_place_on_table(Point tile, GameLocation loc)
			{
				if (size.X > 1 || size.Y > 1)
					return false;

				Rectangle tile_rect = new Rectangle(
					tile * tile_game_size,
					tile_game_size
				);

				foreach (Furniture item in loc.furniture)
				{
					ModEntry.furniture.TryGetValue(item.ItemId, out FurnitureType? f_type);
					if (f_type == null)
					{
						// vanilla furniture
						if (
							item.furniture_type.Value == 11 &&
							item.IntersectsForCollision(tile_rect) &&
							item.heldObject.Value == null
						)
							return true;
							
					}
					else
					{
						// custom furniture
						if (f_type.can_hold(tile))
							return true;
					}
				}

				return false;
			}

			#endregion
		}

		#endregion

		List<CollisionData> directional_collisions = new();
		CollisionData single_collision;
		bool is_directional = false;

		#region Collisions Parsing

		public Collisions(JToken? cols_token, List<string>? rotations, string id)
		{
			if (cols_token == null || cols_token is not JObject cols_obj)
				throw new InvalidDataException($"Missing Collisions for {id}.");
			
			// Case 1 : non-directional collisions
			single_collision = new CollisionData(cols_obj);
			if (single_collision.is_valid) return;

			// Case 2 : directional collisions
			if (rotations == null)
			{
				ModEntry.log(
					$"Invalid non-directional Collisions for non-directional Furniture {id}",
					LogLevel.Error
				);
				throw new InvalidDataException(single_collision.error_msg);
			}
			
			foreach (string rot_name in rotations)
			{
				JToken? col_token = cols_obj.GetValue(rot_name);
				if (col_token == null || col_token is not JObject col_obj)
				{
					string msg = $"Collisions for {id} are invalid: ";
					msg += "cannot parse as Single Collision nor as Directional Collisions";
					msg += $" (No Collision Data for rotation {rot_name}).";
					throw new InvalidDataException(msg);
				}
				else
				{
					CollisionData collision = new(col_obj);
					if (collision.is_valid)
					{
						directional_collisions.Add(collision);
					}
					else
					{
						ModEntry.log(
							$"Invalid Directional Collision for {id} -> {rot_name}.",
							LogLevel.Error
						);
						throw new InvalidDataException(collision.error_msg);
					}
				}
			}
			is_directional = true;
		}

		#endregion

		#region Collisions Methods

		public Rectangle get_bounding_box(Point this_game_pos, int this_rot = 0)
		{
			// Case 1 : non-directional collisions
			if (!is_directional)
			return single_collision.get_bounding_box(this_game_pos);

			// Case 2 : directional collisions
			return directional_collisions[this_rot].get_bounding_box(this_game_pos);
		}

		public bool is_colliding(Rectangle rect, Point this_game_pos, int this_rot = 0)
		{
			// Case 1 : non-directional collisions
			if (!is_directional)
			return single_collision.is_colliding(rect, this_game_pos);

			// Case 2 : directional collisions
			return directional_collisions[this_rot].is_colliding(rect, this_game_pos);
		}

		public bool can_be_placed_here(
			int rot, GameLocation loc, Point tile_pos,
			CollisionMask collisionMask, CollisionMask passable_ignored
		)
		{
			// Case 1 : non-directional collisions
			if (!is_directional)
			return single_collision.can_be_placed_here(loc, tile_pos, collisionMask, passable_ignored);

			// Case 2 : directional collisions
			return directional_collisions[rot].can_be_placed_here(loc, tile_pos, collisionMask, passable_ignored);
		}

		#endregion
	}
}