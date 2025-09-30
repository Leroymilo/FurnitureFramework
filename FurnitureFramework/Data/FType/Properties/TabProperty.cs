using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.Menus;

namespace FurnitureFramework.Data.FType.Properties
{
	[JsonConverter(typeof(SpaceRemover<TabProperty>))]
	public class TabProperty
	{
		public string ID;
		public string HoverText = "";	// useless
		public string? Condition;
		public string SourceImage;
		public Rectangle SourceRect;

		public void AddTab(ShopMenu shop_menu, string mod_id, int idx)
		{
			shop_menu.tabButtons.Add(
				new(
					new Rectangle(0, 0, 64, 64), ModEntry.GetHelper().GameContent.Load<Texture2D>($"FF/{mod_id}/{SourceImage}"), SourceRect, 4f
				)
				{
					myID = 100000 + idx,
					upNeighborID = -99998,
					downNeighborID = -99998,
					rightNeighborID = 3546,
					Filter = salable => Condition == null || (salable is Item item && GameStateQuery.CheckConditions(Condition, inputItem:item)),
					hoverText = HoverText	// Does nothing
				}
			);
		}
	}
}