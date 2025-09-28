using System.Reflection;
using FurnitureFramework.Data.FPack;
using FurnitureFramework.Data.FType;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace FurnitureFramework.FFHarmony
{
	class CatalogueTabPatcher
	{
		public static void Patch(Harmony harmony)
		{
			Type? _c = typeof(ShopMenu).GetNestedType("<>c", BindingFlags.NonPublic);
			if (_c == null) return;

			for (int tab_idx = 1; tab_idx < 6; tab_idx++)
			{
				string method_name = $"<UseFurnitureCatalogueTabs>b__61_{tab_idx}";
				MethodInfo? method = _c.GetMethod(method_name, BindingFlags.Instance | BindingFlags.NonPublic);
				if (method == null) ModEntry.Log($"Could not find {_c.Name}.{method_name}!", StardewModdingAPI.LogLevel.Error);

				ModEntry.Log($"Patching {_c.Name}.{method_name}...");
				harmony.Patch(original:method, postfix:new(AccessTools.Method(typeof(CatalogueTabPatcher), "IsInTab")));
			}
		}

		static bool IsInTab(bool __result, ISalable item, MethodBase __originalMethod)
		{
			if (item is not Furniture furniture) return __result;
			if (!FPack.TryGetType(furniture, out FType? f_type)) return __result;
			return (int)f_type.FurnitureCatalogueTab == int.Parse(__originalMethod.Name.Last().ToString());
		}
	}
}