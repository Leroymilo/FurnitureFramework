using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
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

			Type? _c = typeof(ShopMenu).GetNestedType("<>c", BindingFlags.NonPublic);
			if (_c == null) return;

			for (int tab_idx = 1; tab_idx < 6; tab_idx++)
			{
				string method_name = $"<UseFurnitureCatalogueTabs>b__61_{tab_idx}";
				MethodInfo? method = _c.GetMethod(method_name, BindingFlags.Instance | BindingFlags.NonPublic);
				if (method == null) ModEntry.Log($"Could not find {_c.Name}.{method_name}!", LogLevel.Error);

				ModEntry.Log($"Patching Postfix for {typeof(ShopMenu).Name}.{_c.Name}.{method_name}", LogLevel.Trace);
				harmony.Patch(original:method, postfix:new(AccessTools.Method(typeof(HarmonyPatcher), "IsInTab")));
			}
		}

		static List<string> CategoryTags = new() {
			"ff_category_table", "ff_category_seat",
			"ff_category_wall", "ff_category_floor",
			"ff_category_decor"
		};

		static bool IsInTab(bool __result, ISalable item, MethodBase __originalMethod)
		{
			if (item is not Furniture furniture) return __result;
			if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? _)) return __result;
			return furniture.HasContextTag(CategoryTags[int.Parse(__originalMethod.Name.Last().ToString()) - 1]);
		}
	}
}