using HarmonyLib;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley.GameData.Shops;
using StardewValley.Objects;

namespace FurnitureFramework.FType
{
	using BedType = BedFurniture.BedType;

	partial class FurnitureType
	{
		#region Makers

		public static void make_furniture(
			IContentPack pack, string id, Data.FType data,
			List<FurnitureType> list
		)
		{
			#region Source Rect Variants Dict

			if (data.SourceRectOffsets is JObject r_dict)
			{
				bool has_valid = false;

				foreach ((string name, JToken? rect_token) in r_dict)
				{
					Point offset = new();
					if (!JsonParser.try_parse(rect_token, ref offset))
					{
						ModEntry.log(
							$"Invalid Source Rect Offset at {r_dict.Path}: {name}, skipping variant.",
							LogLevel.Warn
						);
						continue;
					}

					string v_id = $"{id}_{name.ToLower()}";

					make_furniture(
						pack, v_id, data, list, 
						offset, name
					);
					has_valid = true;
				}

				if (!has_valid)
					throw new InvalidDataException("Source Rect Offsets dictionary has no valid value.");
			}

			#endregion

			#region Source Rect Variants List 

			else if (data.SourceRectOffsets is JArray r_array)
			{
				bool has_valid = false;

				foreach ((JToken rect_token, int i) in r_array.Children().Select((value, index) => (value, index)))
				{
					Point offset = new();
					if (!JsonParser.try_parse(rect_token, ref offset))
					{
						ModEntry.log(
							$"Invalid Source Rect Offset at {rect_token.Path}, skipping variant.",
							LogLevel.Warn
						);
						continue;
					}

					string v_id = $"{id}_{i}";

					make_furniture(
						pack, v_id, data, list, 
						offset
					);
					has_valid = true;
				}

				if (!has_valid)
					throw new InvalidDataException("Source Rect Offsets list has no valid value.");
			}

			#endregion

			// Single Source Rect
			else make_furniture(pack, id, data, list, Point.Zero);

		}

		public static void make_furniture(
			IContentPack pack, string id, Data.FType data,
			List<FurnitureType> list,
			Point rect_offset, string rect_var = ""
		)
		{
			#region Texture Variants Dict

			if (data.SourceImage is JObject t_dict)
			{
				bool has_valid = false;

				foreach ((string name, JToken? t_path) in t_dict)
				{
					if(t_path is null || t_path.Type != JTokenType.String)
					{
						ModEntry.log(
							$"Could not read Source Image path at {t_dict.Path}: {name}, skipping variant.",
							LogLevel.Warn
						);
						continue;
					}

					string v_id = $"{id}_{name.ToLower()}";

					TypeInfo info = new(pack, v_id, data, rect_var, image_var: name);
					list.Add(new(info, data, rect_offset, t_path.ToString()));
					has_valid = true;
				}

				if (!has_valid)
					throw new InvalidDataException("Source Image dictionary has no valid path.");
			}

			#endregion

			#region Texture Variants Array

			else if (data.SourceImage is JArray t_array)
			{
				bool has_valid = false;

				foreach ((JToken t_path, int i) in t_array.Children().Select((value, index) => (value, index)))
				{
					if(t_path.Type != JTokenType.String)
					{
						ModEntry.log(
							$"Could not read Source Image path at {t_path.Path}, skipping variant.",
							LogLevel.Warn
						);
						continue;
					}

					string v_id = $"{id}_{i}";

					TypeInfo info = new(pack, v_id, data, rect_var);
					list.Add(new(info, data, rect_offset, t_path.ToString()));
					has_valid = true;
				}

				if (!has_valid)
					throw new InvalidDataException("Source Image list has no valid path.");
			}

			#endregion

			#region Single Texture

			else if (data.SourceImage is JValue t_value)
			{
				if (t_value.Type != JTokenType.String)
					throw new InvalidDataException("Source Image is invalid, should be a string or a dictionary.");

				TypeInfo info = new(pack, id, data, rect_var);
				list.Add(new(info, data, rect_offset, t_value.ToString()));
			}

			#endregion
			
			else throw new InvalidDataException("Source Image is invalid, should be a string or a dictionary.");
		}

		#endregion

		public FurnitureType(
			TypeInfo type_info, Data.FType data,
			Point rect_offset, string texture_path)
		{
			JToken? token;

			#region attributes for Data/Furniture

			info = type_info;
			type = data.ForceType;
			price = data.Price;
			exclude_from_random_sales = data.ExcludefromRandomSales;
			context_tags = data.ContextTags;
			placement_rules = data.PlacementRestriction;

			List<string> rot_names = data.Rotations.rot_names;

			#endregion

			#region textures & source rects

			texture = new(info, texture_path);

			layers = new(info, data.Layers, rot_names);
			for (int i = 0; i < rotations; i++)
			{
				if (!layers[i].has_layer)
					throw new InvalidDataException($"Need at least one Layer for each rotation.");
			}

			placing_layers = data.DrawLayersWhenPlacing;

			this.rect_offset = rect_offset;

			token = data.IconRect;
			if (!JsonParser.try_parse(token, ref icon_rect) || icon_rect.IsEmpty)
				icon_rect = layers[0].get_source_rect();
			
			icon_rect.Location += rect_offset;

			#endregion

			#region data in classes

			animation.parse(data.Animation);
			placing_animate = data.AnimateWhenPlacing;

			collisions = new(info, data.Collisions, rot_names);
			seats = new(info, data.Seats, rot_names);
			slots = new(info, data.Slots, rot_names);
			light_sources = new(info, data.Lights, rot_names);
			sounds = new(data.Sounds);
			particles = new(info, data.Particles, rot_names);

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

			p_type = Enum.Parse<PlacementType>(data.PlacementType);
			if (!Enum.IsDefined(p_type)) {
				p_type = PlacementType.Normal;
				ModEntry.log($"Invalid Placement Type for {info.id}, defaulting to Normal.", LogLevel.Warn);
			}

			if (p_type == PlacementType.Rug) type = "rug";
			if (p_type == PlacementType.Mural) type = "painting";

			#endregion

			#region Special Furniture

			s_type = Enum.Parse<SpecialType>(data.SpecialType);
			if (!Enum.IsDefined(s_type)) {
				s_type = SpecialType.None;
				ModEntry.log($"Invalid Special Type for {info.id}, defaulting to None.", LogLevel.Warn);
			}

			switch (s_type)
			{
				case SpecialType.TV:
					screen_position = JsonParser.parse_dir(data.ScreenPosition, rot_names, Vector2.Zero);
					screen_scale = data.ScreenScale;
					break;
				
				case SpecialType.Bed:
					bed_type = Enum.Parse<BedType>(data.BedType);
					if (!Enum.IsDefined(bed_type)) {
						bed_type = BedType.Double;
						ModEntry.log($"Invalid Bed Type for {info.id}, defaulting to Double.", LogLevel.Warn);
					}

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
						Point bed_size = collisions[0].game_size;
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

				case SpecialType.FishTank:
					fish_area = JsonParser.parse_dir(data.FishArea, rot_names, Rectangle.Empty);
					disable_fishtank_light = data.DisableFishtankLight;
					break;
			}
			
			#endregion
		}

		public List<string> parse_rotations(JToken? token)
		{
			if (token == null || token.Type == JTokenType.Null)
				throw new InvalidDataException($"Missing or invalid Rotations for Furniture {info.id}.");
			
			#region Rotations number

			if (JsonParser.try_parse(token, ref rotations))
			{
				return rotations switch
				{
					1 => new() { "NoRot" },
					2 => new() { "Horizontal", "Vertical" },
					4 => new() { "Down", "Right", "Up", "Left" },
					_ => throw new InvalidDataException($"Invalid Rotations for Furniture {info.id}: number can be 1, 2 or 4."),
				};
			}

			#endregion

			#region Rotations list

			List<string> rot_names = new();

			if (JsonParser.try_parse(token, ref rot_names))
			{
				rotations = rot_names.Count;

				if (rotations == 0)
				{
					rotations = 1;
					rot_names = new() { "NoRot" };
					ModEntry.log($"Furniture {info.id} has no valid rotation key, fallback to \"Rotations\": 1", LogLevel.Warn);
				}

				return rot_names;
			}

			#endregion

			throw new InvalidDataException($"Invalid Rotations for Furniture {info.id}, should be a number or a list of names.");
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