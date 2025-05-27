using Microsoft.Xna.Framework;
using StardewValley.GameData.Shops;

namespace FurnitureFramework.Data.FType
{
	public partial class FType
	{
		public Dictionary<string, string> GetStringData()
		{
			Dictionary<string, string> result = new();
			foreach (Variant variant in Variants.Values)
			{
				result[variant.ID] = GetStringData(variant);
			}
			return result;
		}

		string GetStringData(Variant variant)
		{
			Rectangle icon_rect = GetIconSourceRect();
			icon_rect.Location += variant.Offset;

			string result = variant.DisplayName;
			result += $"/{ForceType}";
			result += $"/{icon_rect.Width / 16} {icon_rect.Height / 16}";
			result += $"/-1"; // overwritten by updateRotation
			result += $"/4";  // overwritten by updateRotation
			result += $"/{Price}";
			result += $"/{PlacementRestriction}";
			result += $"/{variant.DisplayName}";
			result += $"/0";
			result += $"/FF\\{ModID}\\{variant.SourceImage.Replace('/', '\\')}";    // for menu icon
			result += $"/{ExcludefromRandomSales}";
			if (ContextTags.Count > 0)
				result += $"/" + string.Join(" ", ContextTags);
			return result;
		}

		public Dictionary<string, List<ShopItemData>> GetShopItemData()
		{
			Dictionary<string, List<ShopItemData>> result = new();

			foreach (string s_id in ShowsinShops)
			{
				result[s_id] = new();
				foreach (string f_id in Variants.Keys)
					result[s_id].Add(new() { Id = f_id, ItemId = $"(F){f_id}" });
			}

			return result;
		}
	}
}