using System.Reflection.Emit;
using System.Runtime.Versioning;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.FFHarmony.Patches
{
	[RequiresPreviewFeatures]
	internal class FarmerPostfixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly System.Type base_type = typeof(Farmer);
		#pragma warning restore 0414

		internal static float getDrawLayer(
			float __result, Farmer __instance
		)
		{
			if (!__instance.IsSitting()) return __result;
			if (__instance.sittingFurniture is not Furniture furniture)
				return __result;

			try
			{
				if (Pack.FurniturePack.try_get_type(furniture, out Type.FurnitureType? type))
					type.GetSittingDepth(furniture, __instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getDrawLayer)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}
	}

	[RequiresPreviewFeatures]
	internal class Game1Transpiler
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Transpiler;
		static readonly System.Type base_type = typeof(Game1);
		#pragma warning restore 0414

		#region DrawLighting

/*
Insert :

ldsfld class Microsoft.Xna.Framework.Graphics.SpriteBatch StardewValley.Game1::spriteBatch
call void Type.FurnitureType.draw_lighting(Microsoft.Xna.Framework.Graphics.SpriteBatch)

Before :

ldsfld class Microsoft.Xna.Framework.Graphics.SpriteBatch StardewValley.Game1::spriteBatch
callvirt instance void Microsoft.Xna.Framework.Graphics.SpriteBatch::End()
*/

		static IEnumerable<CodeInstruction> DrawLighting(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new()
			{
				new CodeInstruction(
					OpCodes.Ldsfld,
					AccessTools.DeclaredField(
						typeof(Game1),
						"spriteBatch"
					)
				),
				new CodeInstruction(
					OpCodes.Callvirt,
					AccessTools.DeclaredMethod(
						typeof(SpriteBatch),
						"End",
						new System.Type[] {}
					)
				)
			};
			List<CodeInstruction> to_write = new()
			{
				new CodeInstruction(
					OpCodes.Ldsfld,
					AccessTools.DeclaredField(
						typeof(Game1),
						"spriteBatch"
					)
				),
				new CodeInstruction(
					OpCodes.Call,
					AccessTools.DeclaredMethod(
						typeof(Type.FurnitureType),
						"draw_lighting",
						new System.Type[] {typeof(SpriteBatch) }
					)
				),
				new CodeInstruction(
					OpCodes.Ldsfld,
					AccessTools.DeclaredField(
						typeof(Game1),
						"spriteBatch"
					)
				),
				new CodeInstruction(
					OpCodes.Callvirt,
					AccessTools.DeclaredMethod(
						typeof(SpriteBatch),
						"End",
						Array.Empty<System.Type>()
					)
				)
			};

			return Transpiler.replace_instructions(instructions, to_replace, to_write);
		}

		#endregion
	}


	[RequiresPreviewFeatures]
	internal class UtilityTranspiler
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Transpiler;
		static readonly System.Type base_type = typeof(Utility);
		#pragma warning restore 0414

		#region canGrabSomethingFromHere
/* 	Replace :

ldfld class Netcode.NetRef`1<class StardewValley.Object> StardewValley.Object::heldObject
callvirt instance !0 class Netcode.NetFieldBase`2<class StardewValley.Object, class Netcode.NetRef`1<class StardewValley.Object>>::get_Value()

	With :

call check_held_object

*/

		static IEnumerable<CodeInstruction> canGrabSomethingFromHere(
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
					typeof(Type.FurnitureType),
					"has_held_object"
				))
			};

			return Transpiler.replace_instructions(instructions, to_replace, to_write);

		}

		#endregion
	}
}