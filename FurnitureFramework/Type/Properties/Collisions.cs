using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace FurnitureFramework.Type.Properties
{
	[RequiresPreviewFeatures]
	class Collisions: IProperty<Collisions>
	{
		public static readonly Point tile_game_size = new(64);

		#region Collisions Parsing

		private static Collisions make_default()
		{
			JObject default_data = new()
			{
				{ "Height", 1 },
				{ "Width", 1 }
			};
			return new(default_data);
		}

		public static Collisions make(TypeInfo info, JToken? data, string rot_name, out string? error_msg)
		{
			error_msg = null;
			if (data == null)
			{
				error_msg = $"Missing \"Collisions\" field.";
				return make_default();
			}

			if (data is not JObject col_obj)
			{
				error_msg = $"Invalid \"Collisions\" field at {data.Path}: should be {JTokenType.Object}, not {data.Type}";
				return make_default();
			}

			Collisions result = new(col_obj);
			if (!result.is_valid)
			{
				JToken? dir_data = col_obj.GetValue(rot_name);
				if (dir_data is not JObject dir_col_obj)
				{
					// could not find a directional object with the given direction key
					error_msg = result.error_msg;
					result = make_default();
				}

				else
				{
					result = new(dir_col_obj);
				}
			}

			return result;
		}

		public readonly bool is_valid = false;
		public readonly string? error_msg;

		Point size = new();
		public readonly Point game_size;

		bool has_tiles = false;
		HashSet<Point> tiles = new();
		HashSet<Point> game_tiles = new();
		// in game coordinates (tile coordinates * 64)
		
		private Collisions(JObject data)
		{
			// Parsing required collision box size

			error_msg = "Missing Width in Collision Data.";
			JToken? w_token = data.GetValue("Width");
			if (w_token == null || w_token.Type != JTokenType.Integer) return;
			size.X = (int)w_token;

			error_msg = "Missing Height in Collision Data.";
			JToken? h_token = data.GetValue("Height");
			if (h_token == null || h_token.Type != JTokenType.Integer) return;
			size.Y = (int)h_token;

			is_valid = true;
			game_size = size * tile_game_size;

			parse_optional(data);
		}

		private void parse_optional(JObject data)
		{
			// Parsing optional custom collision map

			JToken? map_token = data.GetValue("Map");
			if (map_token == null || map_token.Type == JTokenType.None)
				return;
			
			if (map_token.Type != JTokenType.String)
			{
				ModEntry.log($"Invalid \"Map\" field at {map_token.Path}: should be {JTokenType.String}, not {map_token.Type}", LogLevel.Warn);
				ModEntry.log($"Ignoring Map", LogLevel.Warn);
				return;
			}

			string map_string = map_token.ToString();
			
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
						$"All lines of Map at {map_token.Path} must be as long as Width : {size.X}.",
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

			has_tiles = tiles.Count > 0;
		}

		#endregion

		#region Collisions Methods

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
			Furniture furniture, GameLocation loc, Point tile_pos,
			CollisionMask collisionMask, CollisionMask passable_ignored)
		{
			if (has_tiles)
			{
				foreach (Point tile in tiles)
				{
					if (!is_tile_free(
						furniture,
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
							furniture,
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
			Furniture furniture, Point tile, GameLocation loc,
			CollisionMask collisionMask, CollisionMask passable_ignored
		)
		{
			Vector2 v_tile = tile.ToVector2();
			Vector2 center = (v_tile + new Vector2(0.5f)) * 64;

			// checking for general map placeability
			if (!loc.isTilePlaceable(v_tile, furniture.isPassable()))
				return false;

			foreach (Furniture item in loc.furniture)
			{
				// obstructed by non-rug furniture
				if (
					!item.isPassable() &&
					item.GetBoundingBox().Contains(center) &&
					!item.AllowPlacementOnThisTile(tile.X, tile.Y)
				) return false;

				// cannot stack rugs
				if (
					item.isPassable() && furniture.isPassable() &&
					item.GetBoundingBox().Contains(center)
				) return false;
			}

			if (loc.objects.TryGetValue(v_tile, out var value) && value.isPassable() && furniture.isPassable())
				return false;

			if (loc.IsTileOccupiedBy(v_tile, collisionMask, passable_ignored))
				return false;

			if (!furniture.isGroundFurniture())
				return true;
			
			if (
				loc.terrainFeatures.ContainsKey(v_tile) &&
				loc.terrainFeatures[v_tile] is HoeDirt hoeDirt &&
				hoeDirt.crop != null
			) return false;

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
				if (!Pack.FurniturePack.try_get_type(item, out FurnitureType? _))
				{
					// vanilla furniture
					if (
						item.furniture_type.Value == 11 &&
						item.IntersectsForCollision(tile_rect) &&
						item.heldObject.Value == null
					) return true;
				}
			}

			return false;
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			ModEntry.log($"{indent}Widht: {size.X}", LogLevel.Debug);
			ModEntry.log($"{indent}Height: {size.Y}", LogLevel.Debug);
			if (has_tiles)
			{
				ModEntry.log($"{indent}Collision Tiles: [{string.Join(", ", tiles.Cast<string>())}]", LogLevel.Debug);
				ModEntry.log($"{indent}Collision Map:", LogLevel.Debug);
				for (int y = 0; y < size.Y; y++)
				{
					char[] line = new char[size.X];
					for (int x = 0; x < size.X; x++)
					{
						line[x] = tiles.Contains(new Point(x, y)) ? 'X': '.';
					}
					ModEntry.log($"{indent}\t{string.Concat(line)}", LogLevel.Debug);
				}
			}
			else
			{
				ModEntry.log($"{indent}No Collision Map", LogLevel.Debug);
			}
		}

		#endregion
	}
}