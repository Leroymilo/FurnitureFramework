using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.Patches
{

	internal class LocationPostfixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(GameLocation);
		#pragma warning restore 0414

		internal static StardewValley.Object? getObjectAt(
			StardewValley.Object? __result, GameLocation __instance,
			int x, int y, bool ignorePassables = false
		)
		{
			if (__result is not Furniture furniture)
				return __result;
			// no hit or hit non-furniture object

			ModEntry.f_cache.TryGetValue(
				furniture.ItemId,
				out FurnitureType? furniture_type
			);
			if (furniture_type == null)
				return furniture;
			// vanilla furniture

			Rectangle pos_rect = new(x, y, 0, 0);

			bool collides = true;
			furniture_type.IntersectsForCollision(furniture, pos_rect, ref collides);
			if (collides)
				return furniture;
			// the custom furniture found is correct
			
			try
			{
				foreach (Furniture item in __instance.furniture)
				{
					if (!(ignorePassables && item.isPassable()) && item.IntersectsForCollision(pos_rect))
					{
						return item;
					}
				}

				Vector2 key = new Vector2(x / 64, y / 64);
				__result = null;
				__instance.objects.TryGetValue(key, out __result);
				if (__result != null && ignorePassables && __result.isPassable())
				{
					__result = null;
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getObjectAt)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		internal static bool isObjectAt(
			bool __result, GameLocation __instance,
			int x, int y
		)
		{
			if (!__result) return false;

			try
			{
				__result = false;
				Rectangle rect = new(x, y, 0, 0);
				foreach (Furniture furniture in __instance.furniture)
				{
					if (furniture.IntersectsForCollision(rect))
					{
						__result = true;
						break;
					}
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(isObjectAt)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}
	}

	class LocationTranspilers
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Transpiler;
		static readonly Type base_type = typeof(GameLocation);
		#pragma warning restore 0414

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

		static IEnumerable<CodeInstruction> LowPriorityLeftClick(
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
}