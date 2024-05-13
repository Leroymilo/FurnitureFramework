using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.Patches
{

	internal class FarmerPostfixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(Farmer);
		#pragma warning restore 0414

		internal static float getDrawLayer(
			float __result, Farmer __instance
		)
		{
			if (!__instance.IsSitting()) return __result;
			if (__instance.sittingFurniture is not Furniture furniture)
				return __result;

			try
			{
				ModEntry.f_cache.TryGetValue(
					furniture.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.GetSittingDepth(furniture, __instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getDrawLayer)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}
	}

}