using FurnitureFramework.Data.FType.Properties;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;

namespace FurnitureFramework.Data.FType
{
	using SVObject = StardewValley.Object;
	
	public partial class FType
	{
		public static void SetModData(Furniture furniture)
		{
			if (FPack.FPack.TryGetType(furniture, out FType? type))
			{
				furniture.modData["FF"] = "true";
				furniture.modData["FF.SourceImage"] = type.Variants[furniture.ItemId].SourceImage;
				furniture.modData["FF.RectVariant"] = type.Variants[furniture.ItemId].RectVariant;
				furniture.modData["FF.particle_timers"] = "[]";
				type.InitializeSlots(furniture);

				if (furniture is StorageFurniture)
				{
					furniture.modData["FF.storage_open_state"] = false.ToString();
					furniture.modData["FF.storage_anim_start"] = 0.ToString();
				}
			}
		}

		public void loadDisplayName(Furniture furniture, ref string result)
		{
			result = Variants[furniture.ItemId].GetVariantString(DisplayName, FPack.FPack.ContentPacks[ModID]);
		}

		public void loadDescription(Furniture furniture, ref string result)
		{
			if (Description == null || Description.Length == 0) return;

			result = Variants[furniture.ItemId].GetVariantString(Description, FPack.FPack.ContentPacks[ModID]);
		}

		string GetRot(Furniture furniture)
		{
			// Just in case there is a corrupted furniture.
			if (furniture.currentRotation.Value >= Rotations.Count)
				furniture.currentRotation.Set(0);
			return Rotations[furniture.currentRotation.Value];
		}

		#region Rotation

		public void rotate(Furniture furniture)
		{
			int rot = furniture.currentRotation.Value;
			rot = (rot + 1) % Rotations.Count;
			if (rot < 0) rot = 0;

			furniture.currentRotation.Value = rot;
			furniture.updateRotation();
		}

		public void updateRotation(Furniture furniture)
		{
			string rot = GetRot(furniture);
			Point pos = furniture.TileLocation.ToPoint() * Data.Utils.TILESIZE;

			furniture.boundingBox.Value = Collisions[rot].GetBoundingBox(pos);
			furniture.sourceRect.Value = Layers[rot][0].SourceRect;
		}

		#endregion

		#region Methods for Seats

		public void GetSeatPositions(Furniture furniture, ref List<Vector2> list)
		{
			// This is a postfix, it keeps the original seat positions.

			string rot = GetRot(furniture);
			Vector2 tile_pos = furniture.boundingBox.Value.Location.ToVector2() / 64f;

			Seats[rot].GetSeatPositions(tile_pos, list);
		}

		public void GetSittingDirection(Furniture furniture, Farmer who, ref int sit_dir)
		{
			int seat_index = furniture.sittingFarmers[who.UniqueMultiplayerID];
			string rot = GetRot(furniture);

			int new_sit_dir = Seats[rot].GetSittingDirection(seat_index);
			if (new_sit_dir >= 0) sit_dir = new_sit_dir;
		}

		public void GetSittingDepth(Furniture furniture, Farmer who, ref float depth)
		{
			int seat_index = furniture.sittingFarmers[who.UniqueMultiplayerID];
			string rot = GetRot(furniture);

			float new_sit_depth = Seats[rot].GetSittingDepth(seat_index, furniture.boundingBox.Top);
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

			if (PlacementType == PlacementType.Rug)
			{
				collides = false;
				return;
			}

			string rot = GetRot(furniture);
			Point pos = furniture.boundingBox.Value.Location;
			collides = Collisions[rot].IsColliding(rect, pos);
		}

		public void canBePlacedHere(
			Furniture furniture, GameLocation loc, Vector2 tile,
			CollisionMask collisionMask, ref bool result
		)
		{
			// don't change this part

			if (!loc.CanPlaceThisFurnitureHere(furniture)) return;

			if (!furniture.isGroundFurniture())
			{
				tile.Y = furniture.GetModifiedWallTilePosition(loc, (int)tile.X, (int)tile.Y);
			}

			CollisionMask passable_ignored = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;
			if (furniture.isPassable()) passable_ignored |= CollisionMask.Characters | CollisionMask.Farmers;
			collisionMask &= ~(CollisionMask.Furniture | CollisionMask.Objects);

			// Actual collision detection made by Collisions

			string rot = GetRot(furniture);
			if (!Collisions[rot].CanBePlacedHere(furniture, loc, tile.ToPoint(), collisionMask, passable_ignored))
			{
				result = false;
				return;
			}

			if (PlacementType == PlacementType.Mural)
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

		public static void AllowPlacementOnThisTile(Furniture furniture, int x, int y, ref bool allow)
		{
			allow = !IsClicked(furniture, x * 64, y * 64);
		}

		#endregion

		#region Methods for Slots

		private void InitializeSlots(Furniture furniture, string rot = "")
		{
			if (rot == "") rot = GetRot(furniture);

			int slots_count = Slots[rot].Count;
			Point position = new(
				furniture.boundingBox.Left,
				furniture.boundingBox.Bottom
			);

			if (furniture.heldObject.Value is not Chest chest)
			{
				SVObject held = furniture.heldObject.Value;
				chest = new();
				chest.Items.Add(held);
				furniture.heldObject.Value = chest;

				if (slots_count > 0 && held != null)
					Slots[rot][0].SetBox(held, position);
			}

			while (chest.Items.Count > slots_count)
			{
				Item? item = chest.Items[slots_count];
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

		private Point GetRelPos(Furniture furniture, Point pos)
		{
			Point this_pos = furniture.boundingBox.Value.Location;
			this_pos.Y += furniture.boundingBox.Value.Height;
			return (pos - this_pos) / new Point(4);
		}

		public bool PlaceInSlot(Furniture furniture, Point pos, Farmer who, SVObject obj)
		{
			string rot = GetRot(furniture);

			if (furniture.heldObject.Value is not Chest chest) return false;
			// Furniture is not a proper initialized table

			int slot_index = Slots[rot].GetEmptySlot(GetRelPos(furniture, pos), chest, who, furniture, obj);
			if (slot_index < 0) return false;
			// No slot found at this pixel

			obj.Location = furniture.Location;
			Slots[rot][slot_index].SetBox(obj, new Point(
				furniture.boundingBox.Left,
				furniture.boundingBox.Bottom
			));
			chest.Items[slot_index] = obj;
			who.reduceActiveItemByOne();
			Game1.currentLocation.playSound("woodyStep");
			obj.performDropDownAction(who);

			return true;
		}

		public bool RemoveFromSlot(Furniture furniture, Point pos, Farmer who)
		{
			string rot = GetRot(furniture);

			if (furniture.heldObject.Value is not Chest chest) return false;
			// Furniture is not a proper initialized table

			int slot_index = Slots[rot].GetFilledSlot(GetRelPos(furniture, pos), chest, out SVObject? obj);
			if (slot_index < 0 || obj is null) return false;
			// No slot found at this pixel

			if (who.addItemToInventoryBool(obj))
			{
				obj.performRemoveAction();
				chest.Items[slot_index] = null;
				Game1.playSound("coin");
				return true;
			}

			return false;
		}

		public bool ActionInSlot(Furniture furniture, Point pos, Farmer who)
		{
			string rot = GetRot(furniture);

			if (furniture.heldObject.Value is not Chest chest) return false;
			// Furniture is not a proper initialized table

			int slot_index = Slots[rot].GetFilledSlot(GetRelPos(furniture, pos), chest, out SVObject? obj);
			if (slot_index < 0 || obj is not Furniture furn) return false;
			// No slot found at this pixel or item is not a Furniture

			return furn.checkForAction(who);
		}

		// used in Furniture.canBeRemoved Transpiler
		public static bool HasHeldObject(Furniture furniture)
		{
			SVObject held_obj = furniture.heldObject.Value;
			if (held_obj == null) return false;

			if (held_obj is Chest chest)
			{
				foreach (Item? item in chest.Items)
				{
					if (item != null)
						return true;
				}

				return false;   // empty chest
			}

			return true;
		}

		#endregion

		#region Methods for Placement Type

		public void isGroundFurniture(ref bool is_ground_f)
		{
			is_ground_f = PlacementType != PlacementType.Mural;
		}

		public void isPassable(ref bool is_passable)
		{
			is_passable = PlacementType == PlacementType.Rug;
		}

		#endregion

		#region Methods for Special Furniture

		#region TV
		public void getScreenPosition(TV furniture, ref Vector2 position)
		{
			Rectangle bounding_box = furniture.boundingBox.Value;
			position = bounding_box.Location.ToVector2();
			position.Y += bounding_box.Height;
			position += ScreenPosition[GetRot(furniture)].ToVector2() * 4f;
		}

		public void getScreenSizeModifier(ref float scale)
		{
			scale = ScreenScale;
		}

		#endregion

		public void GetBedSpot(BedFurniture furniture, ref Point spot)
		{
			spot = furniture.TileLocation.ToPoint() + BedSpot;
		}

		public void GetTankBounds(FishTankFurniture furniture, ref Rectangle result)
		{
			string rot = GetRot(furniture);
			Rectangle bounding_box = furniture.boundingBox.Value;
			Rectangle source_rect = Layers[rot][0].SourceRect;

			Point position = new(
				bounding_box.X,
				bounding_box.Y + bounding_box.Height
			);  // bottom left of the bounding box
			Point size = source_rect.Size * new Point(4);

			Rectangle area = FishArea[rot];

			if (area.IsEmpty)
			{
				position.Y -= source_rect.Height * 4;
				position += Layers[rot][0].DrawPos * new Point(4);
				// top left of the base layer

				result = new Rectangle(
					position + new Point(4, 64),
					size - new Point(8, 92)
				// offsets taken from vanilla code
				);
			}

			else
			{
				result = new Rectangle(
					position + area.Location * new Point(4),
					area.Size * new Point(4)
				);
			}
		}

		#region Storage

		public void setUpStoreForContext(ShopMenu shop_menu, ref bool _isStorageShop)
		{
			//shouldn't happen because shop_id must match FF/furniture_id to get here
			if (SpecialType != SpecialType.FFStorage) return;
			shop_menu.purchaseSound = null;
			shop_menu.purchaseRepeatSound = null;
			_isStorageShop = true;

			shop_menu.tabButtons = new();
			switch (StoragePreset)
			{
				case StoragePreset.Dresser:
					shop_menu.UseDresserTabs();
					return;
				case StoragePreset.Catalogue:
					shop_menu.UseCatalogueTabs();
					return;
				case StoragePreset.FurnitureCatalogue:
					shop_menu.UseFurnitureCatalogueTabs();
					return;
			}

			foreach ((TabProperty tab_prop, int idx) in StorageTabs.Select((value, index) => (value, index)))
				tab_prop.AddTab(shop_menu, ModID, idx);
			shop_menu.repositionTabs();
		}

		public bool highlightItemToSell(Item item)
		{
			if (SpecialType != SpecialType.FFStorage) return false;
			switch (StoragePreset)
			{
				case StoragePreset.Dresser:
					return new List<int>(){ -95, -100, -97, -96 }.Contains(item.Category);
				case StoragePreset.Catalogue:
					return item is Wallpaper;
				case StoragePreset.FurnitureCatalogue:
					return item is Furniture;
			}
			if (StorageCondition == null) return true;
			return GameStateQuery.CheckConditions(StorageCondition, inputItem:item);
		}
		
		#endregion

		#endregion

		#region Methods for Transpilers

		public static bool IsClicked(Furniture furniture, int x, int y)
		{
			if (
				!FPack.FPack.TryGetType(furniture, out FType? type)
				|| type.PlacementType == PlacementType.Rug
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

		public static bool IsClicked(Furniture furniture, Point pos)
		{
			return IsClicked(furniture, pos.X, pos.Y);
		}

		public static void DrawLighting(SpriteBatch sprite_batch)
		{
			foreach (Furniture furniture in Game1.currentLocation.furniture)
			{
				if (FPack.FPack.TryGetType(furniture, out FType? type))
				{
					type.DrawLights(furniture, sprite_batch);
				}

				else if (
					furniture.heldObject.Value is Furniture held_furn &&
					FPack.FPack.TryGetType(held_furn, out FType? held_type)
				)
				{
					// maybe move the held furniture bounding box in the middle?
					held_type.DrawLights(held_furn, sprite_batch);
				}
			}
		}

		public static float GetScreenDepth(Furniture furniture, bool overlay = false)
		{
			float depth;
			if (FPack.FPack.TryGetType(furniture, out FType? type))
			{
				depth = type.ScreenDepth[type.GetRot(furniture)].GetValue(furniture.GetBoundingBox().Top);
				depth = MathF.BitIncrement(depth);
			}
			else depth = (furniture.boundingBox.Bottom - 1) / 10000f + 1E-05f;
			if (overlay) depth = MathF.BitIncrement(depth);
			return depth;
		}

		#endregion

		public static void DayUpdate(Furniture furniture)
		{
			// Checking for the Santa easter-egg for multiple slots
			if (Game1.IsMasterGame && Game1.season == Season.Winter && Game1.dayOfMonth == 25 && furniture.heldObject.Value is Chest chest)
			{
				foreach ((Item item, int index) in chest.Items.Select((value, index) => (value, index)))
				{
					if (item.QualifiedItemId == "(O)223" && !Game1.player.mailReceived.Contains("CookiePresent_year" + Game1.year))
					{
						chest.Items[index] = ItemRegistry.Create<SVObject>("(O)MysteryBox");
						Game1.player.mailReceived.Add("CookiePresent_year" + Game1.year);
						continue;
					}
					else if (item.Category == -6 && !Game1.player.mailReceived.Contains("MilkPresent_year" + Game1.year))
					{
						chest.Items[index] = ItemRegistry.Create<SVObject>("(O)MysteryBox");
						Game1.player.mailReceived.Add("MilkPresent_year" + Game1.year);
						continue;
					}
				}
			}
		}

		public void updateWhenCurrentLocation(Furniture furniture)
		{
			string rot = GetRot(furniture);

			// Updating Particles
			long ms_time = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			Particles[rot].UpdateTimer(furniture, ms_time, ModID);

			// Checking bed intersection
			if (SpecialType == SpecialType.Bed)
			{
				// Computing the collisions of the "Do you want to go to bed?" dialogue.
				Rectangle bed_col;

				if (BedArea == null)
				{
					Point bed_size = Collisions[rot].GameSize;
					Point area_size = new(
						Math.Max(64, bed_size.X - 64 * 2),
						Math.Max(64, bed_size.Y - 64 * 2)
					);
					bed_col = new Rectangle(
						(bed_size - area_size) / new Point(2),
						area_size
					);
				}
				else
				{
					bed_col = new Rectangle(
						BedArea.Value.Location * new Point(4),
						BedArea.Value.Size * new Point(4)
					);
				}

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

			string rot = GetRot(furniture);

			// None of these actions are blocking other actions because I couldn't find a reason why they should.

			// Shop
			if (ShopId != null)
			{
				if (Utility.TryOpenShopMenu(ShopId, Game1.currentLocation))
					had_action = true;
			}

			// Toggle
			if (Toggle)
			{
				ToggleFurn(furniture);

				if (ModEntry.GetConfig().toggle_carry_to_slot &&
					furniture.heldObject.Value is Chest held_chest)
				{
					foreach (Item item in held_chest.Items)
					{
						if (item is Furniture furn && FPack.FPack.TryGetType(furn, out FType? f_type))
							f_type.ToggleFurn(furn);
					}
				}
				
				had_action = true;
			}
			else
			{
				had_action |= Sounds.Play(furniture.Location);
			}

			// Seats
			if (Seats[rot].Count > 0)
			{
				int sit_count = furniture.GetSittingFarmerCount();
				who.BeginSitting(furniture);
				if (furniture.GetSittingFarmerCount() > sit_count)
					had_action = true;
			}

			// maybe add place in slot or remove from slot?
		}

		public void ToggleFurn(Furniture furniture)
		{
			furniture.IsOn = !furniture.IsOn;
			Sounds.Play(furniture.Location, furniture.IsOn);
			Particles[GetRot(furniture)].Burst(furniture, ModID);

		}

		public static void OnRemoved(Furniture furniture)
		{
			furniture.modData["FF.particle_timers"] = "[]";
			furniture.heldObject.Value = null;
		}

		public void OnPlaced(Furniture furniture)
		{
			string rot = GetRot(furniture);
			Particles[rot].Burst(furniture, ModID);
		}
	}
}