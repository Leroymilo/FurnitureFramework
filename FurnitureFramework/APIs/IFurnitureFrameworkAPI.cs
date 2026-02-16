

using System.Diagnostics.CodeAnalysis;
using StardewValley.Objects;

namespace FurnitureFramework
{
    /// <summary>The API lets other mods request information about a Furniture instance.
	/// More features can be added when other mods need them.</summary>
	public interface IFurnitureFrameworkAPI
	{
		/// <summary>
		/// Requests the depth at which the TV's screen should be drawn
		/// </summary>
		/// <param name="furniture">The TV instance</param>
		/// <param name="depth">The depth computed by FF if the instance is a FF TV</param>
		/// <param name="overlay">Whether or not to give the depth of the base screen or the overlay (default to `false`)</param>
		/// <returns>`true` if the TV instance is a FF TV, else `false` (and the depth out param is null)</returns>
		bool TryGetScreenDepth(TV furniture, [MaybeNullWhen(false)] out float? depth, bool overlay = false);
	}
}