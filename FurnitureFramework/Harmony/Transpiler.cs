

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Objects;

// I gave up on this, this is a postfix now.

namespace FurnitureFramework
{
	static class Transpiler
	{

/* 	Replace :

ldfld class Netcode.NetRectangle StardewValley.Object::boundingBox  04000B6D 
callvirt instance !0 class Netcode.NetFieldBase`2<valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Rectangle, class Netcode.NetRectangle>::get_Value()  0A000379 
stloc.s 4
ldloca.s 4
ldarg.1
ldarg.2
call instance bool [MonoGame.Framework]Microsoft.Xna.Framework.Rectangle::Contains(int32, int32)  0A0006CD 

	With :

ldarg.1
ldarg.2
ldc.i4.0
ldc.i4.0
newobj instance void [MonoGame.Framework]Microsoft.Xna.Framework.Rectangle::.ctor(int32, int32, int32, int32)
callvirt instance bool StardewValley.Objects.Furniture::IntersectsForCollision(valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Rectangle)

*/

		static bool are_equal(CodeInstruction a, CodeInstruction b)
		{
			if (a.IsStloc() && b.IsStloc())
			{
				return true;
			}

			if (a.opcode != b.opcode) return false;

			if (a.opcode == OpCodes.Callvirt)
			{
				if (a.operand is MethodInfo mi_a && b.operand is MethodInfo mi_b)
				{
					return mi_a.Name == mi_b.Name;
				}
			}

			if (a.opcode == OpCodes.Ldloca || a.opcode == OpCodes.Ldloca_S)
			{
				return true;
			}

			return Equals(a.operand, b.operand);
		}

		static List<int> find_start_indices(
			IEnumerable<CodeInstruction> original,
			IEnumerable<CodeInstruction> to_find
		)
		{
			List<int> indices = new();
 
			for (int i = 0; i < 1 + original.Count() - to_find.Count(); i++)
			{
				bool seq_matches = true;
				int j;
				for (j = 0; j < to_find.Count(); j++, i++)
				{
					CodeInstruction orig = original.ElementAt(i);
					CodeInstruction to_f = to_find.ElementAt(j);

					// ModEntry.log($"original element at {i} :");
					// ModEntry.log($"\topcode : {orig.opcode}");
					// ModEntry.log($"\toperand : {orig.operand}");
					// ModEntry.log($"to_find element at {j} :");
					// ModEntry.log($"\topcode : {to_f.opcode}");
					// ModEntry.log($"\toperand : {to_f.operand}");

					if (are_equal(orig, to_f))
					{
						// ModEntry.log("Matching!");
					}
					else
					{
						seq_matches = false;
						break;
					}
				}
				if (seq_matches)
				{
					indices.Add(i-j);
				}
			}

			return indices;
		}

		public static IEnumerable<CodeInstruction> transpiler(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new()
			{
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(
					OpCodes.Ldfld,
					AccessTools.Field(typeof(StardewValley.Object), "boundingBox")
				),
				new CodeInstruction(
					OpCodes.Callvirt,
					AccessTools.Method(typeof(Netcode.NetRectangle), "get_Value")
				),
				new CodeInstruction(OpCodes.Stloc_S, 4),
				new CodeInstruction(OpCodes.Ldloca_S, 4),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(
					OpCodes.Call,
					AccessTools.Method(
						typeof(Rectangle),
						"Contains",
						new Type[] {typeof(int), typeof(int)}
					)
				)
			};
			List<CodeInstruction> to_write = new()
			{
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldc_I4_0),
				new CodeInstruction(OpCodes.Ldc_I4_0),
				new CodeInstruction(
					OpCodes.Newobj,
					AccessTools.Constructor(
						typeof(Rectangle),
						new Type[] {typeof(int), typeof(int), typeof(int), typeof(int)}
					)
				),
				new CodeInstruction(
					OpCodes.Callvirt,
					AccessTools.Method(typeof(Furniture), "IntersectsForCollision")
				)
			};

			
			List<int> start_indices = find_start_indices(instructions, to_replace);

			int k = 0;

			List<CodeInstruction> new_inst = new();

			for (int i = 0; i < instructions.Count(); i++)
			{
				if (k < start_indices.Count && i == start_indices[k])
				{
					// k++;
					i += to_replace.Count - 1;
					foreach (CodeInstruction instruction in to_write)
					{
						new_inst.Add(instruction);
					}
				}

				else
				{
					new_inst.Add(instructions.ElementAt(i));
				}
			}

			return new_inst;
		}
	}
}