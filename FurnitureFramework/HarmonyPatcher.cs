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

			// fetching Furniture.GetSeatPositions MethodInfo
			original_method = typeof(Furniture)
				.GetMethods()
				.Where(x => x.Name == "GetSeatPositions")
				.Where(x => x.DeclaringType != null
					&& x.DeclaringType.Name == "Furniture")
				.FirstOrDefault();

			harmony.Patch(
				original: original_method,
				prefix: new HarmonyMethod(
					typeof(FurniturePatches),
					nameof(FurniturePatches.get_seat_positions_prefix)
				)
			);

			// fetching Furniture.GetSittingDirection MethodInfo
			original_method = typeof(Furniture)
				.GetMethods()
				.Where(x => x.Name == "GetSittingDirection")
				.Where(x => x.DeclaringType != null
					&& x.DeclaringType.Name == "Furniture")
				.FirstOrDefault();

			harmony.Patch(
				original: original_method,
				prefix: new HarmonyMethod(
					typeof(FurniturePatches),
					nameof(FurniturePatches.get_sitting_direction_prefix)
				)
			);

			// fetching Furniture.checkForAction MethodInfo
			original_method = typeof(Furniture)
				.GetMethods()
				.Where(x => x.Name == "checkForAction")
				.Where(x => x.DeclaringType != null
					&& x.DeclaringType.Name == "Furniture")
				.FirstOrDefault();

			harmony.Patch(
				original: original_method,
				prefix: new HarmonyMethod(
					typeof(FurniturePatches),
					nameof(FurniturePatches.check_for_action_prefix)
				)
			);
		}
	}
}