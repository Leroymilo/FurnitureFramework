using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{

	internal class FurniturePrefixes
	{
		#region draw

		internal static bool draw(
			Furniture __instance,
			SpriteBatch spriteBatch, int x, int y, float alpha = 1f
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				furniture_type.draw(__instance, spriteBatch, x, y, alpha);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(draw)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region rotate

		internal static bool rotate(
			Furniture __instance
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				furniture_type.rotate(__instance);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(rotate)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region clicked

		internal static bool clicked(
			Furniture __instance
		)
		{
			try
			{
				if (__instance.heldObject.Value is Chest)
					return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(clicked)}:\n{ex}", LogLevel.Error);
			}

			return true; // run original logic
		}

		#endregion
	}

	internal class FurniturePostfixes
	{
		#region GetFurnitureInstance

		internal static Furniture GetFurnitureInstance(
			Furniture __result,
			string itemId, Vector2? position = null
		)
		{
			if (!position.HasValue)
			{
				position = Vector2.Zero;
			}

			try
			{
				ModEntry.furniture.TryGetValue(
					__result.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type is not null)
				{
					switch (furniture_type.s_type)
					{
						case SpecialType.Dresser:
							return new StorageFurniture(itemId, position.Value);
						case SpecialType.TV:
							return new TV(itemId, position.Value);
					}
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetFurnitureInstance)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region updateRotation

		internal static void updateRotation(Furniture __instance)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.updateRotation(__instance
				);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateRotation)}:\n{ex}", LogLevel.Error);
			}
		}

		#endregion

		#region GetSeatPositions

		internal static List<Vector2> GetSeatPositions(
			List<Vector2> __result, Furniture __instance
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.GetSeatPositions(__instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetSeatPositions)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region GetSittingDirection

		internal static int GetSittingDirection(
			int __result, Furniture __instance
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.GetSittingDirection(__instance, Game1.player, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetSittingDirection)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region IntersectsForCollision

		internal static bool IntersectsForCollision(
			bool __result, Furniture __instance,
			Rectangle rect
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.IntersectsForCollision(__instance, rect, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(IntersectsForCollision)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region canBePlacedHere

		internal static bool canBePlacedHere(
			bool __result, Furniture __instance,
			GameLocation l, Vector2 tile,
			CollisionMask collisionMask = CollisionMask.All, bool showError = false
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.canBePlacedHere(__instance, l, tile, collisionMask, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(canBePlacedHere)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region AllowPlacementOnThisTile

		internal static bool AllowPlacementOnThisTile(
			bool __result, Furniture __instance,
			int tile_x, int tile_y
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.AllowPlacementOnThisTile(__instance, tile_x, tile_y, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(AllowPlacementOnThisTile)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region checkForAction

		internal static bool checkForAction(
			bool __result, Furniture __instance,
			Farmer who, bool justCheckingForActivity = false
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.checkForAction(__instance, who, justCheckingForActivity, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(checkForAction)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region updateWhenCurrentLocation

		internal static void updateWhenCurrentLocation(
			Furniture __instance
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.updateWhenCurrentLocation(__instance);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateWhenCurrentLocation)}:\n{ex}", LogLevel.Error);
			}
		}

		#endregion
	}

	// Other Fixes for Special Furniture

	internal class StorageFurniturePostFixes
	{
		internal static void updateWhenCurrentLocation(
			StorageFurniture __instance
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.updateWhenCurrentLocation(__instance);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateWhenCurrentLocation)}:\n{ex}", LogLevel.Error);
			}
		}
	}

	internal class TVPostFixes
	{
		internal static Vector2 getScreenPosition(
			Vector2 __result, TV __instance
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.getScreenPosition(__instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getScreenPosition)}:\n{ex}", LogLevel.Error);
			}
			
			return __result;
		}

		internal static float getScreenSizeModifier(
			float __result, TV __instance
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.getScreenSizeModifier(ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getScreenSizeModifier)}:\n{ex}", LogLevel.Error);
			}
			
			return __result;
		}
	}
}