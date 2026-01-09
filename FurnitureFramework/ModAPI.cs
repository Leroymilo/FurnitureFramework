

using System.Diagnostics.CodeAnalysis;
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
	}
}