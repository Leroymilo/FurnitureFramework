using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{

	internal class FurniturePatches
	{

		#region draw

		internal static bool draw_prefix(
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
				ModEntry.log($"Failed in {nameof(draw_prefix)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region GetSeatPositions

		internal static bool get_seat_positions_prefix(
			Furniture __instance, ref List<Vector2> __result,
			bool ignore_offsets = false // actually unused
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				__result = furniture_type.get_seat_positions(__instance);
				
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(get_seat_positions_prefix)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region GetSittingDirection

		internal static bool get_sitting_direction_prefix(
			Furniture __instance, ref int __result
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				__result = furniture_type.get_sitting_direction(__instance, Game1.player);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(get_sitting_direction_prefix)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region checkForAction

		internal static bool check_for_action_prefix(
			Furniture __instance, ref bool __result,
			Farmer who, bool justCheckingForActivity = false
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				__result = furniture_type.check_for_action(__instance, who, justCheckingForActivity);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(check_for_action_prefix)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion
	}

}