using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{
	public class FurnitureFrameworkAPI : IFurnitureFrameworkAPI
	{

		public bool TryGetScreenDepth(TV furniture, [MaybeNullWhen(false)] out float? depth, bool overlay = false)
		{
			depth = null;
			if (!furniture.modData.ContainsKey("FF")) return false;

			depth = Data.FType.FType.GetScreenDepth(furniture, overlay);
			return true;
		}

		public bool CanSlotHold(Furniture furniture, int index, StardewValley.Object obj, Farmer? who = null)
		{
			if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type)) return false;
			string rot = type.GetRot(furniture);

			if (index < 0 || index >= type.Slots[rot].Count) return false;
			return type.Slots[rot][index].CanHold(who, furniture, obj);
		}

		public int GetSlotIndex(Furniture furniture, Point? pos, Farmer? who, [NotNull] ref StardewValley.Object? obj)
		{
			if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type)) {
				obj ??= new StardewValley.Object();
				return -1;
			}
			return type.GetSlot(furniture, pos, who, ref obj);
		}

		public bool PlaceInSlot(Furniture furniture, int index, Farmer? who, StardewValley.Object obj, Action on_placed)
		{
			if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type)) return false;
			return type.PlaceInSlot(furniture, index, who, obj, on_placed);
		}

		public bool RemoveFromSlot(Furniture furniture, int index, Func<StardewValley.Object, bool> can_be_removed, Action on_removed, [MaybeNullWhen(false)] out StardewValley.Object obj)
		{
			if (!Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type)) {
				obj = null;
				return false;
			}
			return type.RemoveFromSlot(furniture, index, can_be_removed, on_removed, out obj);
		}
	}
}