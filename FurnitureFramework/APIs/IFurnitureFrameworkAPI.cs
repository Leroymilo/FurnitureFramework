

using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{
    /// <summary>The API lets other mods request information about a Furniture instance.
	/// More features can be added when other mods need them.</summary>
	public interface IFurnitureFrameworkAPI
	{
		/// <summary>
		/// Asks if a Furniture is handled by FF
		/// </summary>
		/// <param name="furniture">The Furniture to check</param>
		/// <returns>`true` if a furniture pack created/edited this furniture, else `false`</returns>
		bool IsFF(Furniture furniture);

		/// <summary>
		/// Requests the depth at which the TV's screen should be drawn
		/// </summary>
		/// <param name="furniture">The TV instance</param>
		/// <param name="depth">The depth computed by FF if the instance is a FF TV</param>
		/// <param name="overlay">Whether or not to give the depth of the base screen or the overlay (default to `false`)</param>
		/// <returns>`true` if the TV instance is a FF TV, else `false` (and the `depth` out param is null)</returns>
		bool TryGetScreenDepth(TV furniture, [MaybeNullWhen(false)] out float? depth, bool overlay = false);

		/// <summary>
		/// Requests a list representing the contents of slots and the position of their bottom center when not empty
		/// </summary>
		/// <param name="furniture">The furniture holding the items</param>
		/// <returns>A `List<Tuple<Item?, Vector2>>` containing the items (or null) and their position (bottom center) (garbage value for empty slots)</returns>
		List<Tuple<Item?, Vector2>> GetSlotItems(Furniture furniture);

		/// <summary>
		/// Checks if the given slot can hold the given item (depending on size and condition)
		/// </summary>
		/// <param name="furn">The furniture instance to check</param>
		/// <param name="index">The index of the slot to check</param>
		/// <param name="item">The item to check</param>
		/// <param name="who">The player holding the item</param>
		/// <returns>`true` if `item` can be held in slot `index` of `furn`, else `false`</returns>
		bool CanSlotHold(Furniture furn, int index, Item item, Farmer? who = null);

		/// <summary>
		/// Searches for the first valid pointed filled slot if item is null, or the empty slot otherwise
		/// </summary>
		/// <param name="furniture">The furniture instance in which to search</param>
		/// <param name="pos">The position of the cursor to check against the slot's area, cursor position ignored if null</param>
		/// <param name="who">The player who is placing/taking the item</param>
		/// <param name="item">The item being placed, searching for an empty slot if null</param>
		/// <returns>The index of the first valid slot, `-1` if no slot is found (if `null` is passed in `item`, a default object is assigned)</returns>
		int GetSlotIndex(Furniture furniture, Point? pos, Farmer? who, [NotNull] ref Item? item);

		/// <summary>
		/// Places the given item in the given slot if possible
		/// </summary>
		/// <param name="furniture">The furniture instance in which to place the item</param>
		/// <param name="index">The index of the slot in which to place the item</param>
		/// <param name="who">The player who is placing the item</param>
		/// <param name="item">The item being placed</param>
		/// <param name="on_placed">The handle of a function to call if the item is succesfully placed (usually `who.reduceActiveItemByOne` and a sound)</param>
		/// <returns>`true` if the item was succesfully placed, else `false`</returns>
		bool PlaceInSlot(Furniture furniture, int index, Farmer? who, Item item, Action on_placed);

		/// <summary>
		/// Removes the item from the given slot if any
		/// </summary>
		/// <param name="furniture">The furniture instance from which the item is removed</param>
		/// <param name="index">The index of the slot from which the item is removed</param>
		/// <param name="can_be_removed">The handle of a function taking the removed item as an argument and returning a boolean, the item is removed only if it returns `true` (usually `who.addItemToInventoryBool`)</param>
		/// <param name="on_removed">The handle of a function to call if the item is succesfully removed (usually a sound)</param>
		/// <param name="item">The removed item (may be `null` if not removed succesfully)</param>
		/// <returns>`true` if the item was succesfully removed, else `false`</returns>
		bool RemoveFromSlot(Furniture furniture, int index, Func<Item, bool> can_be_removed, Action on_removed, [MaybeNullWhen(false)] out Item item);
	}
}