using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
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
				harmony.Patch(original:method, transpiler:new(AccessTools.Method(typeof(CatalogueTabPatcher), "SmartTranspiler")));
			}


			// method_name = $"<UseFurnitureCatalogueTabs>b__61_5";
			// method = _c.GetMethod(method_name, BindingFlags.Instance | BindingFlags.NonPublic);
			// if (method == null) ModEntry.Log($"Could not find {_c.Name}.{method_name}!", StardewModdingAPI.LogLevel.Error);

			// Func<Furniture, bool> is_ff_decor = (furniture) => {
			// 	if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? f_type)) return true;
			// 	return f_type.FurnitureCatalogueTab == Data.FType.CatalogueTab.Decor;
			// };

			// transpiler = (instructions) => {
			// 	List<CodeInstruction> to_replace = new()
			// 	{
			// 		new(OpCodes.Ret),
			// 		new(OpCodes.Ldc_I4_1),
			// 		new(OpCodes.Ret)
			// 	};
			// 	List<CodeInstruction> to_write = new()
			// 	{
			// 		new(OpCodes.Ret),
			// 		new(OpCodes.Ldloc_0),
			// 		new(OpCodes.Call, is_ff_decor.GetMethodInfo()),
			// 		new(OpCodes.Ret)
			// 	};

			// 	return Transpiler.ReplaceInstructions(instructions, to_replace, to_write);
			// };

			// harmony.Patch(method, transpiler:new(transpiler.GetMethodInfo()));
		}

		static int TabIdentifier(IEnumerable<CodeInstruction> instructions)
		{
			foreach (CodeInstruction inst in instructions)
			{
				if (Transpiler.are_equal(inst, new(OpCodes.Ldc_I4_4)))
					return 1;	// Table
				if (Transpiler.are_equal(inst, new(OpCodes.Ldc_I4_2)))
					return 2;	// Seat
				if (Transpiler.are_equal(inst, new(OpCodes.Ldc_I4_6)))
					return 3;	// Wall
				if (Transpiler.are_equal(inst, new(OpCodes.Ldc_I4_S, (sbyte)12)))
					return 4;	// Floor
				if (Transpiler.are_equal(inst, new(OpCodes.Ldc_I4_7)))
					return 5;	// Decor
			}
			return 0;
		}

		static IEnumerable<CodeInstruction> SmartTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			int tab_idx = TabIdentifier(instructions);
			ModEntry.Log($"patching for tab {tab_idx}");
			if (tab_idx == 0) return instructions;

			// replaces returning false (Ldc_I4_0)
			// with returning the result of "is_in_tab"

			List<CodeInstruction> to_replace = new()
			{
				new(OpCodes.Ret),
				new(OpCodes.Ldc_I4_0),
				new(OpCodes.Ret)
			};
			List<CodeInstruction> to_write = new()
			{
				new(OpCodes.Ret),
				new(OpCodes.Ldloc_0),
				new(OpCodes.Ldc_I4, tab_idx),
				new(OpCodes.Call, AccessTools.Method(typeof(CatalogueTabPatcher), "IsInTab")),
				new(OpCodes.Ret)
			};

			if (tab_idx == 5)
			{
				// For the Decor tab, replaces returning true (Ldc_I4_1), with returning the result of "is_ff_decor"
				to_replace[1] = new(OpCodes.Ldc_I4_1);
				to_write.RemoveAt(2);
				to_write[2] = new(OpCodes.Call, AccessTools.Method(typeof(CatalogueTabPatcher), "IsFFDecor"));
			}

			Dictionary<int, int> copy_labels = new(){ {1, 1} };

			return Transpiler.ReplaceInstructions(instructions, to_replace, to_write, 1, copy_labels:copy_labels, debug:1);
		}

		static bool IsInTab(Furniture furniture, int tab_idx)
		{
			if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? f_type)) return false;
			return (int)f_type.FurnitureCatalogueTab == tab_idx;
		}

		static bool IsFFDecor(Furniture furniture)
		{
			if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? f_type)) return true;
			return f_type.FurnitureCatalogueTab == Data.FType.CatalogueTab.Decor;
		}
	}
}