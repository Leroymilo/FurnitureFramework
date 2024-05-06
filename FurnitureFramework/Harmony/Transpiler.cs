using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace FurnitureFramework
{
	#region helper

	static class Transpiler
	{

		static public bool are_equal(CodeInstruction a, CodeInstruction b, bool debug = false)
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

		static public List<int> find_start_indices(
			IEnumerable<CodeInstruction> original,
			IEnumerable<CodeInstruction> to_find,
			bool debug = false
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

					if (debug)
					{
						ModEntry.log($"original element at {i} :");
						ModEntry.log($"\topcode : {orig.opcode}");
						ModEntry.log($"\toperand : {orig.operand}");
						ModEntry.log($"to_find element at {j} :");
						ModEntry.log($"\topcode : {to_f.opcode}");
						ModEntry.log($"\toperand : {to_f.operand}");
					}

					if (!are_equal(orig, to_f, debug))
					{
						if (debug) ModEntry.log("Restart match");
						seq_matches = false;
						break;
					}
					else if (debug) ModEntry.log("Matching!");
				}
				if (seq_matches)
				{
					indices.Add(i-j);
					if (debug) ModEntry.log("Full Match found!", StardewModdingAPI.LogLevel.Info);
				}
			}

			return indices;
		}

		static public IEnumerable<CodeInstruction> replace_instructions(
			IEnumerable<CodeInstruction> instructions,
			List<CodeInstruction> to_replace,
			List<CodeInstruction> to_write,
			int match_limit = 1,
			bool debug = false
		)
		{
			List<int> start_indices = find_start_indices(instructions, to_replace, debug);

			if (start_indices.Count > match_limit)
			{
				start_indices.RemoveRange(match_limit, start_indices.Count - match_limit);
			}

			if (debug)
				ModEntry.log($"Transpiler found {start_indices.Count} instances to replace");

			int k = 0;

			List<CodeInstruction> new_inst = new();

			if (debug)
				ModEntry.log($"Starting to replace instructions");

			for (int i = 0; i < instructions.Count(); i++)
			{
				if (k < start_indices.Count && i == start_indices[k])
				{
					if (debug)
						ModEntry.log($"Replacing a set of instructions at index {i}");
					k++;
					i += to_replace.Count - 1;
					foreach (CodeInstruction instruction in to_write)
					{
						if (debug)
							ModEntry.log($"\t{instruction}");
						new_inst.Add(instruction);
					}
				}

				else
				{
					new_inst.Add(instructions.ElementAt(i));
				}
			}

			if (debug)
				ModEntry.log($"Finished replacing instructions");

			return new_inst;
		}
	}

	#endregion

	class LocationTranspiler
	{

		#region LowPriorityLeftClick
/* 	Replace :

ldfld class Netcode.NetRectangle StardewValley.Object::boundingBox  04000B6D 
callvirt instance !0 class Netcode.NetFieldBase`2<valuetype Microsoft.Xna.Framework.Rectangle, class Netcode.NetRectangle>::get_Value()  0A000379 
stloc.s 4
ldloca.s 4
ldarg.1
ldarg.2
call instance bool Microsoft.Xna.Framework.Rectangle::Contains(int32, int32)  0A0006CD 

	With :

ldarg.1
ldarg.2
ldc.i4.0
ldc.i4.0
newobj instance void Microsoft.Xna.Framework.Rectangle::.ctor(int32, int32, int32, int32)
callvirt instance bool StardewValley.Objects.Furniture::IntersectsForCollision(valuetype Microsoft.Xna.Framework.Rectangle)

*/

		public static IEnumerable<CodeInstruction> LowPriorityLeftClick(
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

			// ModEntry.log($"Transpiling GameLocation.LowPriorityLeftClick");
			return Transpiler.replace_instructions(instructions, to_replace, to_write);
		}

		#endregion

		#region checkAction
/* 	Replace :

ldfld class Netcode.NetRectangle StardewValley.Object::boundingBox
callvirt instance !0 class Netcode.NetFieldBase`2<valuetype Microsoft.Xna.Framework.Rectangle, class Netcode.NetRectangle>::get_Value()
stloc.s 12
ldloca.s 12
ldloc.2
ldfld float32 Microsoft.Xna.Framework.Vector2::X
ldc.r4 64
mul
conv.i4
ldloc.2
ldfld float32 Microsoft.Xna.Framework.Vector2::Y
ldc.r4 64
mul
conv.i4
call instance bool Microsoft.Xna.Framework.Rectangle::Contains(int32, int32)

	With :

ldloc.2
ldc.r4 64
call valuetype Microsoft.Xna.Framework.Vector2 Microsoft.Xna.Framework.Vector2::op_Multiply(valuetype Microsoft.Xna.Framework.Vector2, float32)
stloc.0
ldloca.s 0
call instance valuetype Microsoft.Xna.Framework.Point Microsoft.Xna.Framework.Vector2::ToPoint()
ldc.i4.1
ldc.i4.1
newobj instance void Microsoft.Xna.Framework.Point::.ctor(int32, int32)
newobj instance void Microsoft.Xna.Framework.Rectangle::.ctor(valuetype Microsoft.Xna.Framework.Point, valuetype Microsoft.Xna.Framework.Point)
callvirt instance bool ['Stardew Valley']StardewValley.Objects.Furniture::IntersectsForCollision(valuetype Microsoft.Xna.Framework.Rectangle)
*/

		public static IEnumerable<CodeInstruction> checkAction(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new()
			{
				new CodeInstruction(
					OpCodes.Ldfld,
					AccessTools.Field(typeof(StardewValley.Object), "boundingBox")
				),
				new CodeInstruction(
					OpCodes.Callvirt,
					AccessTools.Method(typeof(Netcode.NetRectangle), "get_Value")
				),
				new CodeInstruction(OpCodes.Stloc_S, 12),
				new CodeInstruction(OpCodes.Ldloca_S, 12),
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(
					OpCodes.Ldfld,
					AccessTools.Field(typeof(Vector2), "X")
				),
				new CodeInstruction(OpCodes.Ldc_R4, 64f),
				new CodeInstruction(OpCodes.Mul),
				new CodeInstruction(OpCodes.Conv_I4),
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(
					OpCodes.Ldfld,
					AccessTools.Field(typeof(Vector2), "Y")
				),
				new CodeInstruction(OpCodes.Ldc_R4, 64f),
				new CodeInstruction(OpCodes.Mul),
				new CodeInstruction(OpCodes.Conv_I4),
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
				new CodeInstruction(OpCodes.Ldc_R4, 64f),
				new CodeInstruction(
					OpCodes.Call,
					AccessTools.Method(
						typeof(Vector2),
						"op_Multiply",
						new Type[] {typeof(Vector2), typeof(float)}
					)
				),
				new CodeInstruction(OpCodes.Stloc_0),
				new CodeInstruction(OpCodes.Ldloca_S, 0),
				new CodeInstruction(
					OpCodes.Call,
					AccessTools.Method(typeof(Vector2), "ToPoint")
				),
				new CodeInstruction(OpCodes.Ldc_I4_1),
				new CodeInstruction(OpCodes.Ldc_I4_1),
				new CodeInstruction(
					OpCodes.Newobj,
					AccessTools.Constructor(
						typeof(Point),
						new Type[] {typeof(int), typeof(int)}
					)
				),
				new CodeInstruction(
					OpCodes.Newobj,
					AccessTools.Constructor(
						typeof(Rectangle),
						new Type[] {typeof(Point), typeof(Point)}
					)
				),
				new CodeInstruction(
					OpCodes.Callvirt,
					AccessTools.Method(typeof(Furniture), "IntersectsForCollision")
				)
			};

			// ModEntry.log($"Transpiling GameLocation.checkAction");
			return Transpiler.replace_instructions(instructions, to_replace, to_write);
		}

		#endregion
	}

	class FurnitureTranspiler
	{
		#region drawInMenu
/* 	Replace :

ldloc.0
ldc.i4.0
ldloca.s 2
initobj valuetype [System.Runtime]System.Nullable`1<int32>
ldloc.2
callvirt instance valuetype Microsoft.Xna.Framework.Rectangle StardewValley.ItemTypeDefinitions.ParsedItemData::GetSourceRect(int32, valuetype System.Nullable`1<int32>)

	With :

ldarg.0
call get_icon_source_rect

*/

		static private Rectangle get_icon_source_rect(Furniture furniture)
		{
			ModEntry.furniture.TryGetValue(
				furniture.ItemId,
				out FurnitureType? furniture_type
			);

			if (furniture_type != null) return furniture_type.icon_rect;

			return ItemRegistry.GetDataOrErrorItem(furniture.QualifiedItemId).GetSourceRect();
		}

		static public IEnumerable<CodeInstruction> drawInMenu(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new()
			{
				new(OpCodes.Ldloc_0),
				new(OpCodes.Ldc_I4_0),
				new(OpCodes.Ldloca_S, 2),
				new(OpCodes.Initobj, typeof(int?)),
				new(OpCodes.Ldloc_2),
				new(OpCodes.Callvirt, AccessTools.Method(
					typeof(ParsedItemData),
					"GetSourceRect"
				))
			};
			List<CodeInstruction> to_write = new()
			{
				new(OpCodes.Ldarg_0),
				new(OpCodes.Call, AccessTools.Method(
					typeof(FurnitureTranspiler),
					"get_icon_source_rect"
				))
			};

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 2);
		}

		#endregion

		#region canBeRemoved
/* 	Replace :

ldfld class Netcode.NetRef`1<class StardewValley.Object> StardewValley.Object::heldObject
callvirt instance !0 class Netcode.NetFieldBase`2<class StardewValley.Object, class Netcode.NetRef`1<class StardewValley.Object>>::get_Value()

	With :

call h

*/
		static private bool check_held_object(Furniture furniture)
		{
			StardewValley.Object held_obj = furniture.heldObject.Value;
			if (held_obj == null) return false;

			if (held_obj is Chest chest)
			{
				foreach (Item? item in chest.Items)
				{
					if (item != null) return true;
				}

				return false;	// empty chest
			}

			return true;
		}

		static public IEnumerable<CodeInstruction> canBeRemoved(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new()
			{
				new(OpCodes.Ldfld, AccessTools.Field(
					typeof(StardewValley.Object),
					"heldObject"
				)),
				new(OpCodes.Callvirt, AccessTools.Method(
					typeof(Netcode.NetRef<StardewValley.Object>),
					"get_Value"
				))
			};
			List<CodeInstruction> to_write = new()
			{
				new(OpCodes.Call, AccessTools.Method(
					typeof(FurnitureTranspiler),
					"check_held_object"
				))
			};

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 2);
		}

		#endregion
	}
}