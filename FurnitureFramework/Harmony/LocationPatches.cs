using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{

	internal class LocationPostfixes
	{
		internal static StardewValley.Object? getObjectAt(
			StardewValley.Object? __result, GameLocation __instance,
			int x, int y, bool ignorePassables = false
		)
		{
			if (__result is not Furniture furniture)
				return __result;
			// no hit or hit non-furniture object

			ModEntry.f_cache.TryGetValue(
				furniture.ItemId,
				out FurnitureType? furniture_type
			);
			if (furniture_type == null)
				return furniture;
			// vanilla furniture

			Rectangle pos_rect = new(x, y, 0, 0);

			bool collides = true;
			furniture_type.IntersectsForCollision(furniture, pos_rect, ref collides);
			if (collides)
				return furniture;
			// the custom furniture found is correct
			
			try
			{
				foreach (Furniture item in __instance.furniture)
				{
					if (!(ignorePassables && item.isPassable()) && item.IntersectsForCollision(pos_rect))
					{
						return item;
					}
				}

				Vector2 key = new Vector2(x / 64, y / 64);
				__result = null;
				__instance.objects.TryGetValue(key, out __result);
				if (__result != null && ignorePassables && __result.isPassable())
				{
					__result = null;
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getObjectAt)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		internal static bool isObjectAt(
			bool __result, GameLocation __instance,
			int x, int y
		)
		{
			if (!__result) return false;

			try
			{
				__result = false;
				Rectangle rect = new(x, y, 0, 0);
				foreach (Furniture furniture in __instance.furniture)
				{
					if (furniture.IntersectsForCollision(rect))
					{
						__result = true;
						break;
					}
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(isObjectAt)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}
	}
}