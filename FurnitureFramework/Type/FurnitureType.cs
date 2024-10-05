using System.Runtime.Versioning;
using FurnitureFramework.Type.Properties;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Objects;

namespace FurnitureFramework.Type
{

	using BedType = BedFurniture.BedType;

	enum SpecialType {
		None,
		Dresser,
		TV,
		Bed,
		FishTank,
		// RandomizedPlant
	}

	enum PlacementType {
		Normal,
		Rug,
		Mural
	}

	struct TypeInfo
	{
		public readonly string mod_id;
		public readonly string id;
		public readonly string display_name;
		public readonly string? description;

		public TypeInfo(IContentPack pack, string id, JObject data, string rect_var = "", string image_var = "")
		{
			mod_id = pack.Manifest.UniqueID;
			this.id = id.Replace("{{ModID}}", mod_id, true, null);
			display_name = JsonParser.parse(data.GetValue("Display Name"), "No Name");
			display_name = display_name.Replace("{{ImageVariant}}", image_var, true, null);
			display_name = display_name.Replace("{{RectVariant}}", rect_var, true, null);
			description = JsonParser.parse<string?>(data.GetValue("Description"), null);
			if (description is not null)
			{
				description = description.Replace("{{ImageVariant}}", image_var, true, null);
				description = description.Replace("{{RectVariant}}", rect_var, true, null);
			}
		}
	}

	[RequiresPreviewFeatures]
	partial class FurnitureType
	{
		#region Fields

		public readonly TypeInfo info;
		string type;
		public readonly int price;
		bool exclude_from_random_sales;
		List<string> context_tags = new();
		int placement_rules;
		int rotations;
		bool can_be_toggled = false;
		bool time_based = false;
		
		DynaTexture texture;
		List<Rectangle> source_rects = new();
		Point rect_offset;
		public readonly Rectangle icon_rect = Rectangle.Empty;
		DirectionalStructure<LayerList> layers;


		int frame_count = 0;
		int frame_length = 0;
		Point anim_offset = Point.Zero;
		bool is_animated = false;


		DirectionalStructure<Collisions> collisions;
		DirectionalStructure<SeatList> seats;
		DirectionalStructure<SlotList> slots;
		SoundList sounds;
		DirectionalStructure<ParticlesList> particles;
		LightSources light_sources;

		PlacementType p_type = PlacementType.Normal;


		public readonly string? shop_id = null;
		public readonly List<string> shops = new();


		public readonly SpecialType s_type = SpecialType.None;


		Vector2 screen_position = Vector2.Zero;
		float screen_scale = 4;

		Point bed_spot = new(1);
		public readonly BedType bed_type = BedType.Double;
		Rectangle bed_area;

		Rectangle? fish_area = null;
		public readonly bool disable_fishtank_light = false;

		#endregion

		#region Rotation

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

			furniture.boundingBox.Value = collisions[rot].get_bounding_box(pos);
		}

		#endregion

		#region Drawing

		public Point get_source_rect_size(int rot)
		{
			return source_rects[rot].Size;
		}

		// for drawInMenu transpiler
		private static Rectangle get_icon_source_rect(Furniture furniture)
		{
			if (Pack.FurniturePack.try_get_type(furniture, out FurnitureType? type))
			{
				return type.icon_rect;
			}

			return ItemRegistry.GetDataOrErrorItem(furniture.QualifiedItemId).GetSourceRect();
		}

		public Texture2D get_texture()
		{
			return texture.get();
		}

		#endregion

		#region Methods for Seats

		public void GetSeatPositions(Furniture furniture, ref List<Vector2> list)
		{
			// This is a postfix, it keeps the original seat positions.

			int rot = furniture.currentRotation.Value;
			Vector2 tile_pos = furniture.boundingBox.Value.Location.ToVector2() / 64f;
			
			seats[rot].get_seat_positions(tile_pos, list);
		}

		public void GetSittingDirection(Furniture furniture, Farmer who, ref int sit_dir)
		{
			int seat_index = furniture.sittingFarmers[who.UniqueMultiplayerID];
			int rot = furniture.currentRotation.Value;

			int new_sit_dir = seats[rot].get_sitting_direction(seat_index);
			if (new_sit_dir >= 0) sit_dir = new_sit_dir;
		}

		public void GetSittingDepth(Furniture furniture, Farmer who, ref float depth)
		{
			int seat_index = furniture.sittingFarmers[who.UniqueMultiplayerID];
			int rot = furniture.currentRotation.Value;

			float new_sit_depth = seats[rot].get_sitting_depth(seat_index, furniture.boundingBox.Top);
			if (new_sit_depth >= 0) depth = new_sit_depth;
		}

		#endregion

		#region Methods for Collisions

		public void IntersectsForCollision(Furniture furniture, Rectangle rect, ref bool collides)
		{
			if (!collides)
			{
				// not even in bounding box, or collision canceled
				return;
			}

			if (p_type == PlacementType.Rug)
			{
				collides = false;
				return;
			}
			
			int rot = furniture.currentRotation.Value;
			Point pos = furniture.boundingBox.Value.Location;
			collides = collisions[rot].is_colliding(rect, pos);
		}

		public void canBePlacedHere(
			Furniture furniture, GameLocation loc, Vector2 tile,
			CollisionMask collisionMask, ref bool result
		)
		{
			// don't change this part

			if (!loc.CanPlaceThisFurnitureHere(furniture))
			{
				result = false;
				return;
			}

			if (!furniture.isGroundFurniture())
			{
				tile.Y = furniture.GetModifiedWallTilePosition(loc, (int)tile.X, (int)tile.Y);
			}

			CollisionMask passable_ignored = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;
			if (furniture.isPassable())
			{
				passable_ignored |= CollisionMask.Characters | CollisionMask.Farmers;
			}

			collisionMask &= ~(CollisionMask.Furniture | CollisionMask.Objects);

			// Actual collision detection made by collisions

			int rot = furniture.currentRotation.Value;
			if (!collisions[rot].can_be_placed_here(furniture, loc, tile.ToPoint(), collisionMask, passable_ignored))
			{
				result = false;
				return;
			}

			if (p_type == PlacementType.Mural)
			{

				Point point = tile.ToPoint();

				if (loc is not DecoratableLocation dec_loc) 
				{
					result = false;
					return;
				}

				if (
					!((
						dec_loc.isTileOnWall(point.X, point.Y) &&
						dec_loc.GetWallTopY(point.X, point.Y) == point.Y
					) ||
					(
						dec_loc.isTileOnWall(point.X, point.Y - 1) &&
						dec_loc.GetWallTopY(point.X, point.Y) + 1 == point.Y
					))
				)
				{
					result = false;
					return;
				}
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
			allow = !is_clicked(furniture, x * 64, y * 64);
		}

		#endregion

		#region Methods for Slots

		private void initialize_slots(Furniture furniture, int rot)
		{
			int slots_count = slots[rot].count;

			if (furniture.heldObject.Value is not Chest chest)
			{
				StardewValley.Object held = furniture.heldObject.Value;
				chest = new();
				chest.Items.Add(held);
				furniture.heldObject.Value = chest;
			}

			while (chest.Items.Count > slots_count)
			{
				Item item = chest.Items[slots_count];
				chest.Items.RemoveAt(slots_count);
				if (item is null) continue;
				Game1.createItemDebris(
					item,
					furniture.boundingBox.Center.ToVector2(),
					0
				);
			}

			if (chest.Items.Count < slots_count)
			{
				chest.Items.AddRange(
					Enumerable.Repeat<Item?>(null,
						slots_count - chest.Items.Count
					).ToList()
				);
			}
		}

		private int get_slot(Furniture furniture, Point pos)
		{
			int rot = furniture.currentRotation.Value;

			Point this_pos = furniture.boundingBox.Value.Location;
			this_pos.Y += furniture.boundingBox.Value.Height;
			this_pos.Y -= source_rects[rot].Height * 4;
			Point rel_pos = (pos - this_pos) / new Point(4);

			return slots[rot].get_slot(rel_pos);
		}

		public bool place_in_slot(Furniture furniture, StardewValley.Object obj, Point pos, Farmer who)
		{
			int rot = furniture.currentRotation.Value;
			
			initialize_slots(furniture, rot);
			
			if (furniture.heldObject.Value is not Chest chest) return false;
			// Furniture is not a proper initialized table

			int slot_index = get_slot(furniture, pos);
			if (slot_index < 0) return false;
			// No slot found at this pixel

			if (chest.Items[slot_index] is not null) return false;
			// Slot already occupied

			if (!slots[rot].can_hold(slot_index, obj, furniture, who))
			{
				Game1.showRedMessage("This item cannot be placed here.");
				return false;
			}
			// held item doesn't have valid context tags
			// or held furniture is too big

			obj.Location = furniture.Location;
			chest.Items[slot_index] = obj;
			who.reduceActiveItemByOne();
			Game1.currentLocation.playSound("woodyStep");

			return true;
		}

		public bool remove_from_slot(Furniture furniture, Point pos, Farmer who)
		{
			int rot = furniture.currentRotation.Value;
			
			initialize_slots(furniture, rot);

			if (furniture.heldObject.Value is not Chest chest) return false;
			// Furniture is not a proper initialized table

			int slot_index = get_slot(furniture, pos);
			if (slot_index < 0) return false;
			// No slot found at this pixel

			Item item = chest.Items[slot_index];
			if (item is not StardewValley.Object obj) return false;
			// No Object in slot

			if (who.addItemToInventoryBool(obj))
			{
				obj.performRemoveAction();
				chest.Items[slot_index] = null;
				Game1.playSound("coin");
				return true;
			}

			return false;
		}

		// used in Furniture.canBeRemoved Transpiler
		public static bool has_held_object(Furniture furniture)
		{
			StardewValley.Object held_obj = furniture.heldObject.Value;
			if (held_obj == null) return false;

			if (held_obj is Chest chest)
			{
				foreach (Item? item in chest.Items)
				{
					if (item != null) return true;
				}

				return false;	// empty chest
			}

			return true;
		}

		#endregion

		#region Methods for Placement Type

		public void isGroundFurniture(ref bool is_ground_f)
		{
			is_ground_f = p_type != PlacementType.Mural;
		}

		public void isPassable(ref bool is_passable)
		{
			is_passable = p_type == PlacementType.Rug;
		}

		#endregion

		#region Methods for Special Furniture

		public void getScreenPosition(TV furniture, ref Vector2 position)
		{
			Rectangle bounding_box = furniture.boundingBox.Value;
			position = bounding_box.Location.ToVector2();
			position.Y += bounding_box.Height;
			position.Y -= source_rects[furniture.currentRotation.Value].Height * 4f;
			position += screen_position * 4f;
		}

		public void getScreenSizeModifier(ref float scale)
		{
			scale = screen_scale;
		}

		public void GetBedSpot(BedFurniture furniture, ref Point spot)
		{
			spot = furniture.TileLocation.ToPoint() + bed_spot;
		}

		public void DoesTileHaveProperty(BedFurniture furniture,
			int tile_x, int tile_y,
			string property_name, string layer_name,
			ref string property_value,
			ref bool result
		)
		{

			if (layer_name != "Back" || property_name != "TouchAction")
				return;

			result = false;
			return;
		}

		public void GetTankBounds(FishTankFurniture furniture, ref Rectangle result)
		{
			int rot = furniture.currentRotation.Value;
			Rectangle bounding_box = furniture.boundingBox.Value;
			Rectangle source_rect = source_rects[rot].Clone();

			Point position = new(
				bounding_box.X,
				bounding_box.Y + bounding_box.Height - source_rect.Height * 4
			);
			Point size = source_rect.Size * new Point(4);

			if (fish_area is null)
			{
				result = new Rectangle(
					position + new Point(4, 64),
					size - new Point(8, 92)
				);
			}

			else
			{
				result = new Rectangle(
					position + fish_area.Value.Location,
					fish_area.Value.Size
				);
			}
		}

		#endregion

		#region Methods for Transpilers

		public static bool is_clicked(Furniture furniture, int x, int y)
		{
			if (
				!Pack.FurniturePack.try_get_type(furniture, out FurnitureType? type)
				|| type.p_type == PlacementType.Rug
			)
			{
				return furniture.boundingBox.Value.Contains(x, y);
			}
			
			else
			{
				Rectangle rect = new(x, y, 1, 1);
				bool clicks = furniture.boundingBox.Value.Intersects(rect);
				type.IntersectsForCollision(furniture, rect, ref clicks);
				return clicks;
			}
		}

		public static bool is_clicked(Furniture furniture, Point pos)
		{
			return is_clicked(furniture, pos.X, pos.Y);
		}

		public static void draw_lighting(SpriteBatch sprite_batch)
		{
			foreach (Furniture furniture in Game1.currentLocation.furniture)
			{
				if (Pack.FurniturePack.try_get_type(furniture, out FurnitureType? type))
				{
					type.draw_lighting(furniture, sprite_batch);
				}
			}
		}

		#endregion

		public void updateWhenCurrentLocation(Furniture furniture)
		{
			int rot = furniture.currentRotation.Value;

			// Updating particles
			long ms_time = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			particles[rot].update_timer(furniture, ms_time);

			// Checking bed intersection
			if (s_type == SpecialType.Bed)
			{
				Rectangle bed_col = bed_area.Clone();
				bed_col.Location += furniture.boundingBox.Value.Location;
				GameLocation location = furniture.Location;
				bool contains = bed_col.Contains(Game1.player.GetBoundingBox());

				if (!furniture.modData.ContainsKey("FF.checked_bed_tile"))
				{
					furniture.modData["FF.checked_bed_tile"] = contains.ToString().ToLower();
				}

				if (contains)
				{
					if (furniture.modData["FF.checked_bed_tile"] != "true" &&
						!Game1.newDay && Game1.shouldTimePass() &&
						Game1.player.hasMoved && !Game1.player.passedOut
					)
					{
						furniture.modData["FF.checked_bed_tile"] = "true";
						location.createQuestionDialogue(
							Game1.content.LoadString("Strings\\Locations:FarmHouse_Bed_GoToSleep"),
							location.createYesNoResponses(), "Sleep", null
						);
						// Game1.drawObjectQuestionDialogue
					}
				}
				else
				{
					furniture.modData["FF.checked_bed_tile"] = "false";
				}
			}
		}

		public void checkForAction(Furniture furniture, Farmer who, bool justCheckingForActivity, ref bool had_action)
		{
			if (justCheckingForActivity) return;
			// had_action is already true from original method

			int rot = furniture.currentRotation.Value;

			// Shop
			if (shop_id != null)
			{
				if (Utility.TryOpenShopMenu(shop_id, Game1.currentLocation))
					had_action = true;
			}

			// Toggle
			if (can_be_toggled)
			{
				furniture.IsOn = !furniture.IsOn;

				sounds.play(furniture.Location, furniture.IsOn);

				particles[rot].burst(furniture);
			}
			else
			{
				sounds.play(furniture.Location);
			}

			// Seats
			if (seats[rot].has_seats && !had_action)
			{
				int sit_count = furniture.GetSittingFarmerCount();
				who.BeginSitting(furniture);
				if (furniture.GetSittingFarmerCount() > sit_count)
					had_action = true;
			}
			
			// maybe add place in slot or remove from slot?
		}

		public void on_removed(Furniture furniture)
		{
			furniture.modData["FF.particle_timers"] = "[]";
			furniture.heldObject.Value = null;
		}

		public void on_placed(Furniture furniture)
		{
			int rot = furniture.currentRotation.Value;
			particles[rot].burst(furniture);
			initialize_slots(furniture, rot);
		}
	}
}