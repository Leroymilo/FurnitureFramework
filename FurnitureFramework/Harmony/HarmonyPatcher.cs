using HarmonyLib;
using StardewModdingAPI;
using System.Reflection;

namespace FurnitureFramework.FFHarmony
{
	[AttributeUsage(AttributeTargets.Method)]
	public class TargetParamTypeAttribute : Attribute
	{
		public Type[] parameter_types;

		public TargetParamTypeAttribute(Type[] types)
		{
			parameter_types = types;
		}
	}

	namespace Patches
	{
		enum PatchType {
			Prefix,
			Postfix,
			Transpiler
		}
	}

	class HarmonyPatcher
	{
		public static void Patch(Harmony harmony)
		{
			var types = Assembly.GetExecutingAssembly()
				.GetTypes()
				.Where(
					t => t.Namespace is not null &&
					t.Namespace.StartsWith("FurnitureFramework.FFHarmony.Patches") &&
					!t.IsEnum
				);

			foreach (Type type in types)
			{
				#region Get Identification Fields

				var prop = type.GetField("patch_type",
					BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic
				);
				if (prop is null)
				{
					ModEntry.Log($"No patch_type in {type}", LogLevel.Trace);
					continue;
				}
				Patches.PatchType? patch_type = (Patches.PatchType?)prop.GetValue(null);
				if (patch_type is null)
				{
					ModEntry.Log($"patch_type is invalid {type}", LogLevel.Trace);
					continue;
				}

				prop = type.GetField("base_type",
					BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic
				);
				if (prop is null)
				{
					ModEntry.Log($"No base_type in {type}", LogLevel.Trace);
					continue;
				}
				Type? base_type = (Type?)prop.GetValue(null);
				if (base_type is null)
				{
					ModEntry.Log($"base_type is invalid in {type}", LogLevel.Trace);
					continue;
				}

				#endregion

				#region Patch

				foreach (MethodInfo method in type.GetMethods(
					BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic
				))
				{
					MethodInfo original;
					TargetParamTypeAttribute? attribute = method.GetCustomAttribute<TargetParamTypeAttribute>();
					if (patch_type == Patches.PatchType.Transpiler && attribute != null)
					{
						original = AccessTools.DeclaredMethod(
							base_type, method.Name, attribute.parameter_types
						);
					}
					else original = AccessTools.DeclaredMethod(base_type, method.Name);

					// Excludes Transpiler helper methods
					if (original == null) continue;

					ModEntry.Log($"Patching {patch_type} for {base_type.Name}.{method.Name}", LogLevel.Trace);

					HarmonyMethod? prefix = null, postfix = null, transpiler = null;

					switch (patch_type)
					{
						case Patches.PatchType.Prefix:
							prefix = new(method, Priority.High);
							break;
						case Patches.PatchType.Transpiler:
							transpiler = new(method);
							break;
						case Patches.PatchType.Postfix:
							postfix = new(method);
							break;
					}

					harmony.Patch(
						original: original,
						prefix: prefix,
						transpiler: transpiler,
						postfix: postfix
					);
				}

				#endregion
			}

			CatalogueTabPatcher.Patch(harmony);
		}
	}
}