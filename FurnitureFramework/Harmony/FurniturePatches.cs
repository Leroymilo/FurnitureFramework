using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace FurnitureFramework.Patches
{
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				furniture_type.draw(__instance, spriteBatch, x, y, alpha);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				furniture_type.drawAtNonTileSpot(__instance, spriteBatch, location, layerDepth, alpha);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				furniture_type.rotate(__instance);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(rotate)}:\n{ex}", LogLevel.Error);
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
					typeof(FurnitureType),
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
					typeof(FurnitureType),
					"has_held_object"
				))
			};

			return Transpiler.replace_instructions(instructions, to_replace, to_write, 2);
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
				ModEntry.f_cache.TryGetValue(
					__result.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type is not null)
				{
					switch (furniture_type.s_type)
					{
						case SpecialType.Dresser:
							return new StorageFurniture(itemId, position.Value);
						case SpecialType.TV:
							return new TV(itemId, position.Value);
						case SpecialType.Bed:
							return new BedFurniture(itemId, position.Value);
					}
				}
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(GetFurnitureInstance)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion

		#region updateRotation

		internal static void updateRotation(Furniture __instance)
		{
			try
			{
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.updateRotation(__instance
				);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateRotation)}:\n{ex}", LogLevel.Error);
			}
		}

		#endregion

		#region GetSeatPositions

		internal static List<Vector2> GetSeatPositions(
			List<Vector2> __result, Furniture __instance
		)
		{
			try
			{
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.GetSeatPositions(__instance, ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.GetSittingDirection(__instance, Game1.player, ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.IntersectsForCollision(__instance, rect, ref __result);
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
			CollisionMask collisionMask = CollisionMask.All, bool showError = false
		)
		{
			try
			{
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.canBePlacedHere(__instance, l, tile, collisionMask, ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(canBePlacedHere)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion

		#region AllowPlacementOnThisTile

		internal static bool AllowPlacementOnThisTile(
			bool __result, Furniture __instance,
			int tile_x, int tile_y
		)
		{
			try
			{
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.AllowPlacementOnThisTile(__instance, tile_x, tile_y, ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.checkForAction(__instance, who, justCheckingForActivity, ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.updateWhenCurrentLocation(__instance);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.isGroundFurniture(ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.isPassable(ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(isPassable)}:\n{ex}", LogLevel.Error);
			}

			return __result;
		}

		#endregion
	}

	// Other Fixes for Special Furniture

	internal class StorageFurniturePostFixes
	{
		#pragma warning disable 0414
		static readonly PatchType patch_type = PatchType.Postfix;
		static readonly Type base_type = typeof(StorageFurniture);
		#pragma warning restore 0414

		internal static void updateWhenCurrentLocation(
			StorageFurniture __instance
		)
		{
			try
			{
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.updateWhenCurrentLocation(__instance);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(updateWhenCurrentLocation)}:\n{ex}", LogLevel.Error);
			}
		}
	}

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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.getScreenPosition(__instance, ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.getScreenSizeModifier(ref __result);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(getScreenSizeModifier)}:\n{ex}", LogLevel.Error);
			}
			
			return __result;
		}

		#endregion
	}

	// TODO for beds:
	// prefix draw						Done
	// postfix IntersectsForCollision	Done
	// postfix GetBedSpot

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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				if (furniture_type == null) return true; // run original logic

				furniture_type.draw(__instance, spriteBatch, x, y, alpha);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.IntersectsForCollision(__instance, rect, ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.GetBedSpot(__instance, ref __result);
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
				ModEntry.f_cache.TryGetValue(
					__instance.ItemId,
					out FurnitureType? furniture_type
				);

				furniture_type?.DoesTileHaveProperty(
					__instance, tile_x, tile_y,
					property_name, layer_name,
					ref property_value, ref __result
				);
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(DoesTileHaveProperty)}:\n{ex}", LogLevel.Error);
			}
			return __result;
		}

		#endregion
	}
}