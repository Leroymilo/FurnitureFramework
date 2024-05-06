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
		public static Harmony? harmony;

		public static void patch()
		{
			if (harmony == null)
				throw new NullReferenceException("Harmony was not set");

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
					prefix: new(method, Priority.High)
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
			
			foreach (MethodInfo method in typeof(LocationTranspiler).GetMethods(
				BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public
			))
			{
				ModEntry.log($"Patching transpiler : {method.Name}", LogLevel.Trace);

				MethodInfo original = AccessTools.DeclaredMethod(
					typeof(GameLocation),
					method.Name
				);

				harmony.Patch(
					original: original,
					transpiler: new(method)
				);
			}
		}
	}
}