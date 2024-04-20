using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{

	#region draw

	internal class FurniturePatches
	{
		internal static bool draw_prefix(
			Furniture __instance,
			SpriteBatch spriteBatch, int x, int y, float alpha = 1f
		)
		{
			try
			{
				ModEntry.furniture.TryGetValue(
					__instance.ItemId,
					out CustomFurniture? custom_furniture
				);

				if (custom_furniture == null) return true; // run original logic

				custom_furniture.draw(__instance, spriteBatch, x, y, alpha);
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.log($"Failed in {nameof(draw_prefix)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		
		#endregion


	}

}