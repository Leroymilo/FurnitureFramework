using HarmonyLib;
using StardewValley.Objects;
using System.Reflection;

namespace FurnitureFramework
{

	class HarmonyPatcher
	{
		public static Harmony harmony;

		public static void patch()
		{
			patch_furniture();
		}

		private static void patch_furniture()
		{
			// fetching Furniture.draw MethodInfo
			MethodInfo? original_method = typeof(Furniture)
				.GetMethods()
				.Where(x => x.Name == "draw")
				.Where(x => x.DeclaringType != null
					&& x.DeclaringType.Name == "Furniture")
				.FirstOrDefault();

			harmony.Patch(
				original: original_method,
				prefix: new HarmonyMethod(
					typeof(FurniturePatches),
					nameof(FurniturePatches.draw_prefix)
				)
			);
		}
	}
}