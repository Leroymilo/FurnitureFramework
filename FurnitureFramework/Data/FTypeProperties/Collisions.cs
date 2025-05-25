using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace FurnitureFramework.Data
{
	[RequiresPreviewFeatures]
	public class Collisions : Field
	{
		[Required]
		public int Width = 0;
		[Required]
		public int Height = 0;
		public string? Map;

		[JsonIgnore]
		private List<Point> Tiles = new();

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			// Empty collision is invalid
			if (Width == 0 || Height == 0) return;

			if (Map != null)
			{
				string[] lines = Map.Split("/");
				if (lines.Length != Height) return; // Incorrect height of Map
				foreach (string line in lines)
					if (line.Length != Width) return;   // Incorrect width in line of Map

				// Populating Tiles
				for (int y = 0; y < Height; y++)
					for (int x = 0; x < Width; x++)
						if (lines[y][x] == 'X')
							Tiles.Add(new(x, y));
			}

			is_valid = true;
		}

		#region methods

		[JsonIgnore]
		public Point GameSize
		{
			get
			{
				return new Point(Width, Height) * Utils.TILESIZE;
			}
		}

		public Rectangle GetBoundingBox(Point this_game_pos)
		{
			return new(
				this_game_pos,
				new Point(Width, Height) * Utils.TILESIZE
			);
		}

		public bool IsColliding(Rectangle rect, Point this_game_pos)
		{
			Rectangle bounding_box = GetBoundingBox(this_game_pos);

			if (!bounding_box.Intersects(rect))
				return false; // bounding box does not intersect

			if (Map == null) return true;   // no custom collision map

			foreach (Point tile_pos in Tiles)
			{
				Rectangle tile_rect = new(
					this_game_pos + tile_pos * Utils.TILESIZE,
					Utils.TILESIZE
				);
				if (tile_rect.Intersects(rect))
					return true;    // collision map tile intersects
			}
			return false;   // no collision map tile intersects
		}

		public bool CanBePlacedHere(
			Furniture furniture, GameLocation loc, Point tile_pos,
			CollisionMask collisionMask, CollisionMask passable_ignored)
		{
			if (Map != null)
			{
				foreach (Point map_tile_pos in Tiles)
				{
					if (!IsTileFree(
						furniture,
						tile_pos + map_tile_pos, loc,
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
				for (int y = 0; y < Height; y++)
				{
					for (int x = 0; x < Width; x++)
					{
						if (CanPlaceOnTable(new Point(x, y) + tile_pos, loc))
						{
							return true;
						}

						if (!IsTileFree(
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

		private bool IsTileFree(
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

		private bool CanPlaceOnTable(Point tile, GameLocation loc)
		{
			if (Width > 1 || Height > 1)
				return false;

			Rectangle tile_rect = new Rectangle(
				tile * Utils.TILESIZE,
				Utils.TILESIZE
			);

			foreach (Furniture item in loc.furniture)
			{
				if (!item.modData.TryGetValue("FF", out string _))
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

		#endregion
	}
}