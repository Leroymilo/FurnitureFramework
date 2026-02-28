using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{
	public class FurnitureFrameworkAPI : IFurnitureFrameworkAPI
	{

		public bool IsFF(Furniture furniture)
		{
			return Data.FPack.FPack.TryGetType(furniture, out _);
		}

		public bool TryGetScreenDepth(TV furniture, [MaybeNullWhen(false)] out float? depth, bool overlay = false)
		{
			depth = null;
			if (!furniture.modData.ContainsKey("FF")) return false;

			depth = Data.FType.FType.GetScreenDepth(furniture, overlay);
			return true;
		}

		public List<Tuple<Item, Point>> GetSlotItems(Furniture furniture)
		{
			List<Tuple<Item, Point>> result = new();

			Point furn_pos = new(furniture.boundingBox.Left, furniture.boundingBox.Bottom);

			if (Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type) && furniture.heldObject.Value is Chest chest)
			{
				string rot = type.GetRot(furniture);

				for (int i = 0; i < chest.Items.Count; i++)
				{
					Item item = chest.Items[i];
					if (item is null) continue;
					result.Add(new(item, furn_pos + type.Slots[rot][i].GetLocalPos(item)));
				}
			}

			else if (furniture.heldObject.Value is StardewValley.Object obj)
			{
				// reproducing vanilla positioning in draw
				Point pos = furn_pos + furniture.boundingBox.Center;
				if (obj is not Furniture) pos.Y += furniture.drawHeldObjectLow.Value ? 32 : -21;
				else pos.Y += furniture.drawHeldObjectLow.Value ? 16 : -16;
				
				result.Add(new(obj, pos));
			}

			return result;
		}

		public bool CanSlotHold(Furniture furniture, int index, Item item, Farmer? who = null)
		{
			if (Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type))
			{
				string rot = type.GetRot(furniture);
				if (index < 0 || index >= type.Slots[rot].Count) return false;
				return type.Slots[rot][index].CanHold(who, furniture, item);
			}

			else if (index == 0)
			{
				// vanilla check
				return furniture.IsTable() && item is StardewValley.Object obj && !obj.bigCraftable.Value && obj is not Wallpaper && (obj is not Furniture furn || (furn.getTilesWide() == 1 && furn.getTilesHigh() == 1));
			}

			return false;
		}

		public int GetSlotIndex(Furniture furniture, Point? pos, Farmer? who, [NotNull] ref Item? item)
		{
			if (Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type))
				return type.GetSlot(furniture, pos, who, ref item);

			else if (pos is null || furniture.boundingBox.Value.Contains(pos.Value))
			{
				// Placing in vanilla slot
				if (item is not null && furniture.heldObject.Value is null && CanSlotHold(furniture, 0, item, who))
					return 0;
				// Taking from vanilla slot
				if (item is null && furniture.heldObject.Value is not null)
				{
					item = furniture.heldObject.Value;
					return 0;
				}
			}

			item ??= new StardewValley.Object();
			return -1;
		}

		public bool PlaceInSlot(Furniture furniture, int index, Farmer? who, Item item, Action on_placed)
		{
			if (Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type))
				return type.PlaceInSlot(furniture, index, who, item, on_placed);
			
			if (index == 0 && item is StardewValley.Object obj && furniture.performObjectDropInAction(item, true, who))
			{
				// copied from Furniture.performObjectDropInAction
				furniture.heldObject.Value = (StardewValley.Object)obj.getOne();
				furniture.heldObject.Value.Location = furniture.Location;
				furniture.heldObject.Value.TileLocation = furniture.TileLocation;
				furniture.heldObject.Value.boundingBox.X = furniture.boundingBox.X;
				furniture.heldObject.Value.boundingBox.Y = furniture.boundingBox.Y;
				furniture.heldObject.Value.performDropDownAction(who);
				
				on_placed();
				return true;
			}

			return false;
		}

		public bool RemoveFromSlot(Furniture furniture, int index, Func<Item, bool> can_be_removed, Action on_removed, [MaybeNullWhen(false)] out Item item)
		{
			if (Data.FPack.FPack.TryGetType(furniture, out Data.FType.FType? type))
				return type.RemoveFromSlot(furniture, index, can_be_removed, on_removed, out item);

			if (index == 0 && furniture.heldObject.Value is StardewValley.Object obj)
			{
				// adapted Furniture.clicked
				if (can_be_removed(obj))
				{
					obj.performRemoveAction();
					item = obj;
					on_removed();
					furniture.heldObject.Value = null;
					return true;
				}
			}

			item = null;
			return false;
		}
	}
}