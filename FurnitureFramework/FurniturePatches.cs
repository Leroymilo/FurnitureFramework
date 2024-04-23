using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{

	internal class Prefixes
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

	}

	internal class Postfixes
	{

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

	}

}