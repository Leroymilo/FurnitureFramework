using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.GameData.Shops;

namespace FurnitureFramework.FType
{
	partial class FurnitureType
	{
		#region Makers

		public static void make_furniture(
			IContentPack pack, string id, Data.FType data,
			List<FurnitureType> list
		)
		{
			switch (data.SourceRectOffsets.Count)
			{
				case 0:
					throw new InvalidDataException("No valid Source Rect Offset.");
				case 1:
					// No need to change the Furniture ID
					make_furniture(pack, id, data, list, data.SourceRectOffsets.First().Value);
					break;
				default:
					foreach (KeyValuePair<string, Point> elt in data.SourceRectOffsets)
					{
						string v_id = $"{id}_{elt.Key.ToLower()}";
						make_furniture(pack, v_id, data, list, elt.Value, elt.Key);
					}
					break;
			}
		}

		public static void make_furniture(
			IContentPack pack, string id, Data.FType data,
			List<FurnitureType> list,
			Point rect_offset, string rect_var = ""
		)
		{
			switch (data.SourceImage.Count)
			{
				case 0:
					throw new InvalidDataException("No valid Source Image.");
				case 1:
					// No need to change the Furniture ID
					TypeInfo info = new(pack, id, data, rect_var);
					list.Add(new(info, data, rect_offset, data.SourceImage.First().Value));
					break;
				default:
					foreach (KeyValuePair<string, string> elt in data.SourceImage)
					{
						string v_id = $"{id}_{elt.Key.ToLower()}";
						info = new(pack, v_id, data, rect_var, image_var: elt.Key);
						list.Add(new(info, data, rect_offset, elt.Value));
					}
					break;
			}
		}

		#endregion

		public FurnitureType(
			TypeInfo type_info, Data.FType data,
			Point rect_offset, string texture_path)
		{
			#region attributes for Data/Furniture

			info = type_info;
			type = data.ForceType;
			price = data.Price;
			exclude_from_random_sales = data.ExcludefromRandomSales;
			context_tags = data.ContextTags;
			placement_rules = data.PlacementRestriction;

			rotations = data.Rotations;

			#endregion

			#region textures & source rects

			texture = new(info, texture_path);

			layers = data.Layers;

			placing_layers = data.DrawLayersWhenPlacing;

			this.rect_offset = rect_offset;

			icon_rect = data.IconRect ?? layers[rotations[0]][0].SourceRect;
			icon_rect.Location += rect_offset;

			#endregion

			#region data in classes

			animation = data.Animation;
			placing_animate = data.AnimateWhenPlacing;

			collisions = data.Collisions;
			seats = new(info, data.Seats, rotations);
			slots = new(info, data.Slots, rotations);
			light_sources = new(info, data.Lights, rotations);
			sounds = new(data.Sounds);
			particles = new(info, data.Particles, rotations);

			#endregion

			#region Shops

			shop_id = data.ShopId;
			if (shop_id is string)
				shop_id = shop_id.Replace("[[ModID]]", info.mod_id, true, null);
			
			shops = data.ShowsinShop;
			for (int i = 0; i < shops.Count; i++)
				shops[i] = shops[i].Replace("[[ModID]]", info.mod_id, true, null);
			shops.Add("FF.debug_catalog");

			#endregion

			can_be_toggled = data.Toggle;
			time_based = data.TimeBased;

			#region Placement Type

			p_type = data.PlacementType;

			if (p_type == Data.PlacementType.Rug) type = "rug";
			if (p_type == Data.PlacementType.Mural) type = "painting";

			#endregion

			#region Special Furniture

			s_type = data.SpecialType;

			switch (s_type)
			{
				case Data.SpecialType.TV:
					screen_position = JsonParser.parse_dir(data.ScreenPosition, rotations, Vector2.Zero);
					screen_scale = data.ScreenScale;
					break;
				
				case Data.SpecialType.Bed:
					bed_type = data.BedType;

					JsonParser.try_parse(data.BedSpot, ref bed_spot);

					if (JsonParser.try_parse(data.BedArea, ref bed_area))
					{
						bed_area = new Rectangle(
							bed_area.Location * new Point(4),
							bed_area.Size * new Point(4)
						);
					}
					else
					{
						Point bed_size = collisions.First.GameSize;
						Point area_size = new Point(
							Math.Max(64, bed_size.X - 64*2),
							Math.Max(64, bed_size.Y - 64*2)
						);
						bed_area = new Rectangle(
							(bed_size - area_size) / new Point(2),
							area_size
						);
					}
					break;

				case Data.SpecialType.FishTank:
					fish_area = JsonParser.parse_dir(data.FishArea, rotations, Rectangle.Empty);
					disable_fishtank_light = data.DisableFishtankLight;
					break;
			}
			
			#endregion
		}

		public string get_string_data()
		{
			string result = info.display_name;
			result += $"/{type}";
			result += $"/{icon_rect.Width/16} {icon_rect.Height/16}";
			result += $"/-1";	// overwritten by updateRotation
			result += $"/4";	// overwritten by updateRotation
			result += $"/{price}";
			result += $"/{placement_rules}";
			result += $"/{info.display_name}";
			result += $"/0";
			result += $"/{texture.asset_name.Replace('/', '\\')}";	// for menu icon
			result += $"/{exclude_from_random_sales}";
			if (context_tags.Count > 0)
				result += $"/" + context_tags.Join(delimiter: " ");

			return result;
		}

		#region Data/Shops request

		private static bool has_shop_item(ShopData shop_data, string f_id)
		{
			foreach (ShopItemData shop_item_data in shop_data.Items)
			{
				if (shop_item_data.ItemId == $"(F){f_id}")
					return true;
			}
			return false;
		}

		private static void add_shop(IDictionary<string, ShopData> editor, string s_id)
		{
			if (editor.ContainsKey(s_id)) return;

			ShopData catalogue_shop_data = new()
			{
				CustomFields = new Dictionary<string, string>() {
					{"HappyHomeDesigner/Catalogue", "true"}
				},
				Owners = new List<ShopOwnerData>() { 
					new() {
						Name = "AnyOrNone",
						Dialogues = new() {}	// To remove default dialogue
					}
				}
			};
			editor[s_id] = catalogue_shop_data;
		}

		public void add_data_shop(IDictionary<string, ShopData> editor)
		{
			if (shop_id != null) add_shop(editor, shop_id);

			foreach (string s_id in shops)
			{
				add_shop(editor, s_id);

				if (!has_shop_item(editor[s_id], info.id))
				{
					ShopItemData shop_item_data = new()
					{
						Id = info.id,
						ItemId = $"(F){info.id}",
						// Price = types[f_id].price
					};

					editor[s_id].Items.Add(shop_item_data);
				}
			}
		}

		#endregion
	}
}