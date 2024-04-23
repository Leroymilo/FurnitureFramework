using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;
using System.Reflection;

namespace FurnitureFramework
{

	class HarmonyPatcher
	{
		public static Harmony harmony;

		public static void patch()
		{
			foreach (MethodInfo method in typeof(Prefixes).GetMethods(
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

			foreach (MethodInfo method in typeof(Postfixes).GetMethods(
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
		}
	}
}