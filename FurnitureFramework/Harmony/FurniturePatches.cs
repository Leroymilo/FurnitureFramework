using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace FurnitureFramework.FFHarmony.Patches
{
	#region Furniture

	internal class FurniturePrefixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Prefix;
		static readonly Type base_type = typeof(Furniture);
		#pragma warning restore 0414

		#region draw

		internal static bool draw(
			Furniture __instance,
			SpriteBatch spriteBatch, int x, int y, float alpha = 1f
		)
		{
			try
			{
				if (!Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					return true; // run original logic

				type.draw(__instance, spriteBatch, x, y, alpha);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(draw)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region drawAtNonTileSpot

		internal static bool drawAtNonTileSpot(
			Furniture __instance, SpriteBatch spriteBatch,
			Vector2 location, float layerDepth, float alpha = 1f
		)
		{
			try
			{
				if (!Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					return true; // run original logic

				type.drawAtNonTileSpot(__instance, spriteBatch, location, layerDepth, alpha);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(drawAtNonTileSpot)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region rotate

		internal static bool rotate(
			Furniture __instance
		)
		{
			try
			{
				if (!Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					return true; // run original logic

				type.rotate(__instance);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(rotate)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region updateRotation

		internal static bool updateRotation(Furniture __instance)
		{
			try
			{
				if (!Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					return true; // run original logic

				type.updateRotation(__instance);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateRotation)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion

		#region clicked

		internal static bool clicked(
			Furniture __instance
		)
		{
			try
			{
				if (__instance.heldObject.Value is Chest)
					return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(clicked)}:\n{ex}", LogLevel.Error);
			}

			return true; // run original logic
		}

		#endregion
	}

	class FurnitureTranspilers
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Transpiler;
		static readonly Type base_type = typeof(Furniture);
		#pragma warning restore 0414

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

		static IEnumerable<CodeInstruction> drawInMenu(
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
					typeof(Data.FType.FType),
					"GetIconSourceRect",
					new Type[] {typeof(Furniture)}
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

call check_held_object

*/

		static IEnumerable<CodeInstruction> canBeRemoved(
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
					typeof(Data.FType.FType),
					"HasHeldObject"
				))
			};

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 1);
		}

		#endregion

		#region GetOneCopyFrom
/* 	Replace :

ldarg.0
ldfld class Netcode.NetInt StardewValley.Objects.Furniture::rotations
callvirt instance !0 class Netcode.NetFieldBase`2<int32, class Netcode.NetInt>::get_Value()
ldc.i4.4
beq.s IL_009a
ldc.i4.2
br.s IL_009b
ldc.i4.1
sub
callvirt instance void class Netcode.NetFieldBase`2<int32, class Netcode.NetInt>::set_Value(!0)
ldarg.0
callvirt instance void StardewValley.Objects.Furniture::rotate()

	With :

callvirt instance void class Netcode.NetFieldBase`2<int32, class Netcode.NetInt>::set_Value(!0)
ldarg.0
callvirt instance void StardewValley.Objects.Furniture::updateRotation()

*/

		static IEnumerable<CodeInstruction> GetOneCopyFrom(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new()
			{
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldfld, AccessTools.Field(
					typeof(Furniture),
					"rotations"
				)),
				new(OpCodes.Callvirt, AccessTools.Method(
					typeof(Netcode.NetFieldBase<int, Netcode.NetInt>),
					"get_Value"
				)),
				new(OpCodes.Ldc_I4_4),
				new(OpCodes.Beq_S),
				new(OpCodes.Ldc_I4_2),
				new(OpCodes.Br_S),
				new(OpCodes.Ldc_I4_1),
				new(OpCodes.Sub),
				new(OpCodes.Callvirt, AccessTools.Method(
					typeof(Netcode.NetFieldBase<int, Netcode.NetInt>),
					"set_Value"
				)),
				new(OpCodes.Ldarg_0),
				new(OpCodes.Callvirt, AccessTools.Method(
					typeof(Furniture),
					"rotate"
				)),
			};
			List<CodeInstruction> to_write = new()
			{
				new(OpCodes.Callvirt, AccessTools.Method(
					typeof(Netcode.NetFieldBase<int, Netcode.NetInt>),
					"set_Value"
				)),
				new(OpCodes.Ldarg_0),
				new(OpCodes.Callvirt, AccessTools.Method(
					typeof(Furniture),
					"updateRotation"
				)),
			};

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 1);
		}

		#endregion
	}

	internal class FurniturePostfixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(Furniture);
		#pragma warning restore 0414

		#region GetFurnitureInstance

		internal static Furniture GetFurnitureInstance(
			Furniture __result,
			string itemId, Vector2? position = null
		)
		{
			if (!position.HasValue)
			{
				position = Vector2.Zero;
			}

			try
			{
				if (Pack.FurniturePack.try_get_type(__result, out Data.FType.FType? type))
				{
					switch (type.SpecialType)
					{
						case Data.FType.SpecialType.Dresser:
							__result = new StorageFurniture(itemId, position.Value);
							break;
						case Data.FType.SpecialType.TV:
							__result = new TV(itemId, position.Value);
							break;
						case Data.FType.SpecialType.Bed:
							__result = new BedFurniture(itemId, position.Value) { bedType = type.BedType };
							break;
						case Data.FType.SpecialType.FishTank:
							__result = new FishTankFurniture(itemId, position.Value);
							break;
					}

					type.SetModData(__result);
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetFurnitureInstance)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region GetSeatPositions

		internal static List<Vector2> GetSeatPositions(
			List<Vector2> __result, Furniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.GetSeatPositions(__instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetSeatPositions)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region GetSittingDirection

		internal static int GetSittingDirection(
			int __result, Furniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.GetSittingDirection(__instance, Game1.player, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetSittingDirection)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region IntersectsForCollision

		internal static bool IntersectsForCollision(
			bool __result, Furniture __instance,
			Rectangle rect
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.IntersectsForCollision(__instance, rect, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(IntersectsForCollision)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region canBePlacedHere

		internal static bool canBePlacedHere(
			bool __result, Furniture __instance,
			GameLocation l, Vector2 tile,
			CollisionMask collisionMask = CollisionMask.All
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.canBePlacedHere(__instance, l, tile, collisionMask, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(canBePlacedHere)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion²

		#region AllowPlacementOnThisTile

		internal static bool AllowPlacementOnThisTile(
			bool __result, Furniture __instance,
			int tile_x, int tile_y
		)
		{
			try
			{
				Data.FType.FType.AllowPlacementOnThisTile(__instance, tile_x, tile_y, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(AllowPlacementOnThisTile)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region checkForAction

		internal static bool checkForAction(
			bool __result, Furniture __instance,
			Farmer who, bool justCheckingForActivity = false
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.checkForAction(__instance, who, justCheckingForActivity, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(checkForAction)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region updateWhenCurrentLocation

		internal static void updateWhenCurrentLocation(
			Furniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.updateWhenCurrentLocation(__instance);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateWhenCurrentLocation)}:\n{ex}", LogLevel.Error);
			}
		}

		#endregion

		#region isGroundFurniture

		internal static bool isGroundFurniture(
			bool __result, Furniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.isGroundFurniture(ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(isGroundFurniture)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region isPassable

		internal static bool isPassable(
			bool __result, Furniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.isPassable(ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(isPassable)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region loadDescription

		internal static string loadDescription(
			string __result, Furniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.GetDescription(__instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(loadDescription)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region DayUpdate

		internal static void DayUpdate(
			Furniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					Data.FType.FType.DayUpdate(__instance);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(DayUpdate)}:\n{ex}", LogLevel.Error);
			}
		}

		#endregion
	
		#region addLights

		internal static void addLights(Furniture __instance)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
				{
					Data.FType.FType.addLights(__instance);

					if (type.DisableFishtankLight)
					{
						// copied from Furniture.removeLights
						string value = __instance.GenerateLightSourceId(__instance.TileLocation);
						for (int i = 0; i < __instance.getTilesWide(); i++)
						{
							__instance.Location.removeLightSource($"{value}_tile{i}");
						}

						__instance.lightSource = null;
					}
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(addLights)}:\n{ex}", LogLevel.Error);
			}
		}

		#endregion

	}

	#endregion

	// Other Fixes for Special Furniture

	#region StorageFurniture

	internal class StorageFurniturePostFixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(StorageFurniture);
		#pragma warning restore 0414

		#region updateWhenCurrentLocation

		internal static void updateWhenCurrentLocation(
			StorageFurniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.updateWhenCurrentLocation(__instance);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateWhenCurrentLocation)}:\n{ex}", LogLevel.Error);
			}
		}

		#endregion
	}

	#endregion

	#region TV

	internal class TVPostFixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(TV);
		#pragma warning restore 0414

		#region getScreenPosition

		internal static Vector2 getScreenPosition(
			Vector2 __result, TV __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.getScreenPosition(__instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getScreenPosition)}:\n{ex}", LogLevel.Error);
			}
			
			return __result;
		}

		#endregion

		#region getScreenSizeModifier

		internal static float getScreenSizeModifier(
			float __result, TV __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.getScreenSizeModifier(ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getScreenSizeModifier)}:\n{ex}", LogLevel.Error);
			}
			
			return __result;
		}

		#endregion
	}

	internal class TVTranspilers
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Transpiler;
		static readonly Type base_type = typeof(TV);
		#pragma warning restore

		static List<CodeInstruction> depth_replace = new()
		{
			new(OpCodes.Ldarg_0),
			new(OpCodes.Ldfld, AccessTools.Field(
				typeof(StardewValley.Object), "boundingBox"
			)),
			new(OpCodes.Callvirt, AccessTools.Method(
				typeof(Netcode.NetRectangle), "get_Bottom"
			)),
			new(OpCodes.Ldc_I4_1),
			new(OpCodes.Sub),
			new(OpCodes.Conv_R4),
			new(OpCodes.Ldc_R4, 10000f),
			new(OpCodes.Div),
			new(OpCodes.Ldc_R4, 1e-05f),
			new(OpCodes.Add)
		};

		static List<CodeInstruction> depth_write = new()
		{
			new(OpCodes.Ldarg_0),
			new(OpCodes.Ldc_I4_0),
			new(OpCodes.Call, AccessTools.Method(
				typeof(Data.FType.FType),
				"GetScreenDepth"
			))
		};

/*

Replaces (float)(boundingBox.Bottom - 1) / 10000f + 1E-05f

ldarg.0
ldfld class Netcode.NetRectangle StardewValley.Object::boundingBox
callvirt instance int32 Netcode.NetRectangle::get_Bottom()
ldc.i4.1
sub
conv.r4
ldc.r4 10000
div
ldc.r4 1E-05
add

With Data.FType.FType.GetScreenDepth(this, false)

ldarg.0
ldc.i4.0
call void Data.FType.FType::GetScreenDepth(Furniture, bool)

In selectChannel (x7) and proceedToNextScene (x6)

*/

		static IEnumerable<CodeInstruction> selectChannel(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new();
			to_replace.AddRange(depth_replace);
			List<CodeInstruction> to_write = new();
			to_write.AddRange(depth_write);

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 7);
		}

		static IEnumerable<CodeInstruction> proceedToNextScene(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new();
			to_replace.AddRange(depth_replace);
			List<CodeInstruction> to_write = new();
			to_write.AddRange(depth_write);

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 6);
		}

/*

Replaces (float)(boundingBox.Bottom - 1) / 10000f + 2E-05f

ldarg.0
ldfld class Netcode.NetRectangle StardewValley.Object::boundingBox
callvirt instance int32 Netcode.NetRectangle::get_Bottom()
ldc.i4.1
sub
conv.r4
ldc.r4 10000
div
ldc.r4 2E-05
add

With Data.FType.FType.GetScreenDepth(this, true)

ldarg.0
ldc.i4.1
call void Data.FType.FType::GetScreenDepth(Furniture, bool)

In setFortuneOverlay (x5) and setWeatherOverlay (x7)

*/

		static IEnumerable<CodeInstruction> setFortuneOverlay(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new();
			to_replace.AddRange(depth_replace);
			to_replace[8] = new(OpCodes.Ldc_R4, 2e-05f);
			List<CodeInstruction> to_write = new();
			to_write.AddRange(depth_write);
			to_write[1] = new(OpCodes.Ldc_I4_1);

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 5);
		}

		[TargetParamType(new[] {typeof(string)})]
		static IEnumerable<CodeInstruction> setWeatherOverlay(
			IEnumerable<CodeInstruction> instructions
		)
		{
			List<CodeInstruction> to_replace = new();
			to_replace.AddRange(depth_replace);
			to_replace[8] = new(OpCodes.Ldc_R4, 2e-05f);
			List<CodeInstruction> to_write = new();
			to_write.AddRange(depth_write);
			to_write[1] = new(OpCodes.Ldc_I4_1);

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 7);
		}
	}

	#endregion

	#region BedFurniture

	internal class BedFurniturePreFixes
	{
#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Prefix;
		static readonly Type base_type = typeof(BedFurniture);
#pragma warning restore 0414

		#region draw

		internal static bool draw(
			BedFurniture __instance,
			SpriteBatch spriteBatch, int x, int y, float alpha = 1f
		)
		{
			try
			{
				if (!Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					return true; // run original logic

				type.draw(__instance, spriteBatch, x, y, alpha);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(draw)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion
	}

	internal class BedFurniturePostFixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(BedFurniture);
		#pragma warning restore 0414

		#region IntersectsForCollision

		internal static bool IntersectsForCollision(
			bool __result, BedFurniture __instance,
			Rectangle rect
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
				{
					__result = __instance.GetBoundingBox().Intersects(rect);
					type.IntersectsForCollision(__instance, rect, ref __result);
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(IntersectsForCollision)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion
	
		#region GetBedSpot

		internal static Point GetBedSpot(
			Point __result, BedFurniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.GetBedSpot(__instance, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetBedSpot)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region DoesTileHaveProperty

		internal static bool DoesTileHaveProperty(
			bool __result, BedFurniture __instance,
			int tile_x, int tile_y,
			string property_name, string layer_name,
			ref string property_value
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					if (layer_name == "Back" && property_name == "TouchAction") __result = false;
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(DoesTileHaveProperty)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region canBeRemoved

		internal static bool canBeRemoved(
			bool __result, BedFurniture __instance
		)
		{
			try
			{
				if (Data.FType.FType.HasHeldObject(__instance)) return false;
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(canBeRemoved)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion
	}

	#endregion

	#region FishTankFurniture

	internal class FishTankFurniturePreFixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Prefix;
		static readonly Type base_type = typeof(FishTankFurniture);
		#pragma warning restore 0414

		#region draw

		internal static bool draw(
			FishTankFurniture __instance,
			SpriteBatch spriteBatch, int x, int y, float alpha = 1f
		)
		{
			try
			{
				if (!Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					return true; // run original logic

				type.draw(__instance, spriteBatch, x, y, alpha);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(draw)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		#endregion
	}

	internal class FishTankFurniturePostFixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(FishTankFurniture);
		#pragma warning restore 0414

		#region checkForAction

		internal static bool checkForAction(
			bool __result, FishTankFurniture __instance,
			Farmer who, bool justCheckingForActivity = false
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
					type.checkForAction(__instance, who, justCheckingForActivity, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(checkForAction)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion
	
		#region GetTankBounds

		internal static Rectangle GetTankBounds(
			Rectangle __result, FishTankFurniture __instance
		)
		{
			try
			{
				if (Pack.FurniturePack.try_get_type(__instance, out Data.FType.FType? type))
				{
					type.GetTankBounds(__instance, ref __result);
				}

			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetTankBounds)}:\n{ex}", LogLevel.Error);
			}

			__result.Width = Math.Max(1, __result.Width);
			__result.Height = Math.Max(1, __result.Height);

			return __result;
		}

		#endregion
	}

	#endregion
}