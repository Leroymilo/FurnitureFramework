using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Reflection;

namespace FurnitureFramework
{

	// TO PATCH :
	// Object.drawPlacementBounds to draw only squares with collision.

	class HarmonyPatcher
	{
		public static Harmony harmony;

		public static void patch()
		{
			foreach (MethodInfo method in typeof(FurniturePrefixes).GetMethods(
				BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic
			))
			{
				ModEntry.log($"Patching prefix : {method.Name}", LogLevel.Trace);

				MethodInfo original = AccessTools.DeclaredMethod(
					typeof(Furniture),
					method.Name
				);

				harmony.Patch(
					original: original,
					prefix: new(method)
				);
			}

			foreach (MethodInfo method in typeof(FurniturePostfixes).GetMethods(
				BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic
			))
			{
				ModEntry.log($"Patching postfix : {method.Name}", LogLevel.Trace);

				MethodInfo original = AccessTools.DeclaredMethod(
					typeof(Furniture),
					method.Name
				);

				harmony.Patch(
					original: original,
					postfix: new(method)
				);
			}

			foreach (MethodInfo method in typeof(LocationPostfixes).GetMethods(
				BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic
			))
			{
				ModEntry.log($"Patching postfix : {method.Name}", LogLevel.Trace);

				MethodInfo original = AccessTools.DeclaredMethod(
					typeof(GameLocation),
					method.Name
				);

				harmony.Patch(
					original: original,
					postfix: new(method)
				);
			}

			ModEntry.log("Patching transpiler", LogLevel.Warn);

			harmony.Patch(
				original: AccessTools.DeclaredMethod(
					typeof(GameLocation),
					"LowPriorityLeftClick"
				),
				transpiler: new HarmonyMethod(
					AccessTools.Method(typeof(Transpiler), "transpiler")
				)
			);
		}
	}
}