using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.FFHarmony.Patches
{

	internal class LocationPostfixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(GameLocation);
		#pragma warning restore 0414

		#region getObjectAt

		internal static StardewValley.Object? getObjectAt(
			StardewValley.Object? __result, GameLocation __instance,
			int x, int y, bool ignorePassables = false
		)
		{
			if (__result is not Furniture furniture)
				return __result;
			// no hit or hit non-furniture object
			
			if (Data.FType.FF3Type.IsClicked(furniture, x, y))
				return furniture;
			// the custom furniture found is correct
			
			try
			{
				foreach (Furniture item in __instance.furniture)
				{
					if (!(ignorePassables && item.isPassable()) && Data.FType.FF3Type.IsClicked(item, x, y))
					{
						return item;
					}
				}

				Vector2 key = new(x / 64, y / 64);
				__result = null;
				__instance.objects.TryGetValue(key, out __result);
				if (__result != null && ignorePassables && __result.isPassable())
				{
					__result = null;
				}
			}
			catch (Exception ex)
			{
				ModEntry.Log($"Failed in {nameof(getObjectAt)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region isObjectAt

		internal static bool isObjectAt(
			bool __result, GameLocation __instance,
			int x, int y
		)
		{
			if (!__result) return false;

			try
			{
				foreach (Furniture furniture in __instance.furniture)
				{
					if (Data.FType.FF3Type.IsClicked(furniture, x, y))
					{
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				ModEntry.Log($"Failed in {nameof(isObjectAt)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion
	}

	class LocationTranspilers
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Transpiler;
		static readonly Type base_type = typeof(GameLocation);
		#pragma warning restore 0414

		#region LowPriorityLeftClick
/* 	Replace :

ldloc.2
ldfld class Netcode.NetRectangle StardewValley.Object::boundingBox  04000B6D 
callvirt instance !0 class Netcode.NetFieldBase`2<valuetype Microsoft.Xna.Framework.Rectangle, class Netcode.NetRectangle>::get_Value()  0A000379 
stloc.s 4
ldloca.s 4
ldarg.1
ldarg.2
call instance bool Microsoft.Xna.Framework.Rectangle::Contains(int32, int32)  0A0006CD 

	With :

ldloc.2
ldarg.1
ldarg.2
call bool Data.FType.FType::is_clicked(StardewValley.Furniture, int, int)

And Replace :

ldfld class Netcode.NetRef`1<class StardewValley.Object> StardewValley.Object::heldObject
callvirt instance !0 class Netcode.NetFieldBase`2<class StardewValley.Object, class Netcode.NetRef`1<class StardewValley.Object>>::get_Value()

	With :

call Data.FType.FType::HasHeldObject
*/

		static IEnumerable<CodeInstruction> LowPriorityLeftClick(
			IEnumerable<CodeInstruction> instructions
		)
		{
			// replace bounding box contains with custom is_clicked
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
				new CodeInstruction(
					OpCodes.Call,
					AccessTools.Method(
						typeof(Data.FType.FF3Type),
						"IsClicked",
						new Type[] {typeof(Furniture), typeof(int), typeof(int)}
					)
				)
			};

			instructions = Transpiler.ReplaceInstructions(instructions, to_replace, to_write);

			// replace heldObject != null with custom has_held_object
			to_replace = new()
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
			to_write = new()
			{
				new(OpCodes.Call, AccessTools.Method(
					typeof(Data.FType.FF3Type),
					"HasHeldObject"
				))
			};

			return Transpiler.ReplaceInstructions(instructions, to_replace, to_write);
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
stloc.s 20
ldloca.s 20
call instance valuetype Microsoft.Xna.Framework.Point Microsoft.Xna.Framework.Vector2::ToPoint()
call bool FurnitureFramework.Type.FurnitureType::is_clicked(StardewValley.Furniture, Microsoft.Xna.Framework.Point)

*/

		static IEnumerable<CodeInstruction> checkAction(
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
				new CodeInstruction(OpCodes.Stloc_S, 20),
				new CodeInstruction(OpCodes.Ldloca_S, 20),
				new CodeInstruction(
					OpCodes.Call,
					AccessTools.Method(typeof(Vector2), "ToPoint")
				),
				new CodeInstruction(
					OpCodes.Call,
					AccessTools.Method(
						typeof(Data.FType.FF3Type),
						"IsClicked",
						new Type[] {typeof(Furniture), typeof(Point) }
					)
				)
			};

			return Transpiler.ReplaceInstructions(instructions, to_replace, to_write);
		}

		#endregion
	}
}