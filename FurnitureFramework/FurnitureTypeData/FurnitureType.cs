using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Objects;

namespace FurnitureFramework
{

	using BedType = BedFurniture.BedType;

	enum SpecialType {
		None,
		Dresser,
		TV,
		Bed,
		FishTank,
		// RandomizedPlant
	}

	enum PlacementType {
		Normal,
		Rug,
		Mural
	}

	class FurnitureType
	{
		#region Fields

		public readonly string mod_id;
		public readonly string id;
		string display_name;
		string type;
		public readonly int price;
		bool exclude_from_random_sales;
		List<string> context_tags = new();
		int placement_rules;
		int rotations;
		bool can_be_toggled = false;
		bool time_based = false;
		
		// SeasonalTexture texture;
		DynaTexture texture;
		List<Rectangle> source_rects = new();
		public readonly Rectangle icon_rect = Rectangle.Empty;
		Layers layers;


		int frame_count = 0;
		int frame_length = 0;
		Point anim_offset = Point.Zero;
		bool is_animated = false;


		Collisions collisions;
		Seats seats;
		Slots slots;
		public readonly bool is_table = false;
		Sounds sounds;
		Particles particles;
		LightSources light_sources;

		PlacementType p_type = PlacementType.Normal;


		public readonly string? shop_id = null;
		public readonly List<string> shops = new();


		public readonly SpecialType s_type = SpecialType.None;


		Vector2 screen_position = Vector2.Zero;
		float screen_scale = 4;

		Point bed_spot = new(1);
		public readonly BedType bed_type = BedType.Double;
		Rectangle bed_area;

		Rectangle? fish_area = null;
		public readonly bool disable_fishtank_light = false;

		public readonly string? description;

		#endregion

		#region Parsing

		#region Makers

		public static void make_furniture(
			IContentPack pack, string id, JObject data,
			List<FurnitureType> list
		)
		{
			JToken? r_token = data.GetValue("Source Rect Offsets");

			#region Source Rect Variants Dict

			if (r_token is JObject r_dict)
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

			else if (r_token is JArray r_array)
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
			IContentPack pack, string id, JObject data,
			List<FurnitureType> list,
			Point rect_offset, string rect_var = ""
		)
		{
			JToken? t_token = data.GetValue("Source Image");

			#region Texture Variants Dict

			if (t_token is JObject t_dict)
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

					list.Add(new(
						pack, v_id, data,
						rect_offset, t_path.ToString(),
						rect_var, name
					));
					has_valid = true;
				}

				if (!has_valid)
					throw new InvalidDataException("Source Image dictionary has no valid path.");
			}

			#endregion

			#region Texture Variants Array

			else if (t_token is JArray t_array)
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

					list.Add(new(
						pack, v_id, data,
						rect_offset, t_path.ToString(),
						rect_var
					));
					has_valid = true;
				}

				if (!has_valid)
					throw new InvalidDataException("Source Image list has no valid path.");
			}

			#endregion

			#region Single Texture

			else if (t_token is JValue t_value)
			{
				if (t_value.Type != JTokenType.String)
					throw new InvalidDataException("Source Image is invalid, should be a string or a dictionary.");

				list.Add(new(
					pack, id, data,
					rect_offset, t_value.ToString(),
					rect_var
				));
			}

			#endregion
			
			else throw new InvalidDataException("Source Image is invalid, should be a string or a dictionary.");
		}

		#endregion

		public FurnitureType(
			IContentPack pack, string id, JObject data,
			Point rect_offset, string texture_path,
			string rect_var = "", string image_var = "")
		{
			JToken? token;

			#region attributes for Data/Furniture

			mod_id = pack.Manifest.UniqueID;
			this.id = id.Replace("{{ModID}}", mod_id, true, null);
			display_name = JsonParser.parse(data.GetValue("Display Name"), "No Name");
			display_name = display_name.Replace("{{ImageVariant}}", image_var, true, null);
			display_name = display_name.Replace("{{RectVariant}}", rect_var, true, null);
			type = JsonParser.parse(data.GetValue("Force Type"), "other");
			price = JsonParser.parse(data.GetValue("Price"), 0);
			exclude_from_random_sales = JsonParser.parse(data.GetValue("Exclude from Random Sales"), true);
			JsonParser.try_parse(data.GetValue("Context Tags"), ref context_tags);
			placement_rules = JsonParser.parse(data.GetValue("Placement Restriction"), 2);

			List<string> rot_names = parse_rotations(data.GetValue("Rotations"));

			#endregion

			#region textures & source rects

			texture = new(
				this, texture_path,
				JsonParser.parse(data.GetValue("Seasonal"), false),
				JsonParser.parse(data.GetValue("Weather Based"), false)
			);

			// texture = new(pack, texture_path, seasonal);

			token = data.GetValue("Source Rect");
			List<Rectangle?> n_source_rects = new();
			if (!JsonParser.try_parse(token, rot_names, ref n_source_rects))
				throw new InvalidDataException($"Missing or invalid Source Rectangles for Furniture {this.id}.");
			if (!JsonParser.try_rm_null(n_source_rects, ref source_rects))
				throw new InvalidDataException($"Missing directional Source Rectangles for Furniture {this.id}.");
			
			for (int i = 0; i < source_rects.Count; i++)
			{
				source_rects[i] = new(
					source_rects[i].Location + rect_offset,
					source_rects[i].Size
				);
			}

			token = data.GetValue("Icon Rect");
			if (!JsonParser.try_parse(token, ref icon_rect))
				icon_rect = source_rects[0];

			if (icon_rect.IsEmpty)
				icon_rect = source_rects[0];

			layers = Layers.make_layers(data.GetValue("Layers"), rot_names, texture, rect_offset);

			#endregion

			#region animation

			frame_count = JsonParser.parse(data.GetValue("Frame Count"), 0);
			frame_length = JsonParser.parse(data.GetValue("Frame Duration"), 0);
			token = data.GetValue("Animation Offset");
			if (token != null && !JsonParser.try_parse(token, ref anim_offset))
				ModEntry.log($"Invalid Animation Offset, ignoring animation");
			is_animated = frame_count > 0 && frame_length > 0;
			is_animated &= anim_offset.X + anim_offset.Y > 0;

			#endregion

			#region data in classes

			collisions = new(data.GetValue("Collisions"), rot_names);

			seats = Seats.make_seats(data.GetValue("Seats"), rot_names);

			slots = Slots.make_slots(data.GetValue("Slots"), rot_names);
			is_table = slots.has_slots;

			light_sources = new(this, data.GetValue("Light Sources"), rot_names);

			sounds = new(data.GetValue("Sounds"));

			particles = new(this, data.GetValue("Particles"));

			#endregion

			#region Shops

			shop_id = JsonParser.parse<string?>(data.GetValue("Shop Id"), null);
			if (shop_id is string)
				shop_id = shop_id.Replace("{{ModID}}", mod_id, true, null);
			
			JsonParser.try_parse(data.GetValue("Shows in Shops"), ref shops);
			for (int i = 0; i < shops.Count; i++)
				shops[i] = shops[i].Replace("{{ModID}}", mod_id, true, null);

			#endregion

			can_be_toggled = JsonParser.parse(data.GetValue("Toggle"), false);
			time_based = JsonParser.parse(data.GetValue("Time Based"), false);

			#region Placement Type

			p_type = Enum.Parse<PlacementType>(JsonParser.parse(data.GetValue("Placement Type"), "Normal"));
			if (!Enum.IsDefined(p_type)) {
				p_type = PlacementType.Normal;
				ModEntry.log($"Invalid Placement Type at {data.Path}, defaulting to Normal.", LogLevel.Warn);
			}

			if (p_type == PlacementType.Rug) type = "rug";
			if (p_type == PlacementType.Mural) type = "painting";

			#endregion

			#region Special Furniture

			s_type = Enum.Parse<SpecialType>(JsonParser.parse(data.GetValue("Special Type"), "None"));
			if (!Enum.IsDefined(s_type)) {
				s_type = SpecialType.None;
				ModEntry.log($"Invalid Special Type at {data.Path}, defaulting to None.", LogLevel.Warn);
			}

			JsonParser.try_parse(data.GetValue("Screen Position"), ref screen_position);
			screen_scale = JsonParser.parse(data.GetValue("Screen Scale"), 4f);

			JsonParser.try_parse(data.GetValue("Bed Spot"), ref bed_spot);

			bed_type = Enum.Parse<BedType>(JsonParser.parse(data.GetValue("Bed Type"), "Double"));
			if (!Enum.IsDefined(bed_type)) {
				bed_type = BedType.Double;
				ModEntry.log($"Invalid Bed Type at {data.Path}, defaulting to Double.", LogLevel.Warn);
			}

			if (JsonParser.try_parse(data.GetValue("Bed Area"), out bed_area))
			{
				ModEntry.log("The \"Bed Area\" field is deprecated, it will be used as \"Bed Area Pixel\" in the next Format version", LogLevel.Warn);
				bed_area = new Rectangle(
					bed_area.Location * new Point(64),
					bed_area.Size * new Point(64)
				);
			}
			else if (JsonParser.try_parse(data.GetValue("Bed Area Pixel"), out bed_area))
			{
				bed_area = new Rectangle(
					bed_area.Location * new Point(4),
					bed_area.Size * new Point(4)
				);
			}
			else
			{
				Point bed_size = collisions.get_size(0);
				Point area_size = new Point(
					Math.Max(64, bed_size.X - 64*2),
					Math.Max(64, bed_size.Y - 64*2)
				);
				bed_area = new Rectangle(
					(bed_size - area_size) / new Point(2),
					area_size
				);
			}
			// mirror_bed_area = JsonParser.parse(data.GetValue("Mirror Bed Area"), false);

			// TODO: replace "Bed Area" with "Bed Area Pixel" for simplification

			if (JsonParser.try_parse(data.GetValue("Fish Area"), out Rectangle read_fish_area))
			{
				fish_area = new Rectangle(
					read_fish_area.Location * new Point(4),
					read_fish_area.Size * new Point(4)
				);
			}
			disable_fishtank_light = JsonParser.parse(data.GetValue("Disable Fishtank Light"), false);


			#endregion
		
			description = JsonParser.parse<string?>(data.GetValue("Description"), null);
			if (description is not null)
			{
				description = description.Replace("{{ImageVariant}}", image_var, true, null);
				description = description.Replace("{{RectVariant}}", rect_var, true, null);
			}
		}

		public List<string> parse_rotations(JToken? token)
		{
			if (token == null || token.Type == JTokenType.Null)
				throw new InvalidDataException($"Missing or invalid Rotations for Furniture {id}.");
			
			#region Rotations number

			if (JsonParser.try_parse(token, ref rotations))
			{
				return rotations switch
				{
					1 => new(),
					2 => new() { "Horizontal", "Vertical" },
					4 => new() { "Down", "Right", "Up", "Left" },
					_ => throw new InvalidDataException($"Invalid Rotations for Furniture {id}, number can be 1, 2 or 4."),
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
					ModEntry.log($"Furniture {id} has no valid rotation key, fallback to \"Rotations\": 1", LogLevel.Warn);
				}

				return rot_names;
			}

			#endregion

			throw new InvalidDataException($"Invalid Rotations for Furniture {id}, should be a number or a list of names.");
		}

		#endregion

		public string get_string_data()
		{
			string result = display_name;
			result += $"/{type}";
			result += $"/{icon_rect.Width/16} {icon_rect.Height/16}";
			result += $"/-1";	// overwritten by updateRotation
			result += $"/4";	// overwritten by updateRotation
			result += $"/{price}";
			result += $"/{placement_rules}";
			result += $"/{display_name}";
			result += $"/0";
			result += $"/{id}";
			result += $"/{exclude_from_random_sales}";
			if (context_tags.Count > 0)
				result += $"/" + context_tags.Join(delimiter: " ");

			return result;
		}

		#region Rotation

		public void rotate(Furniture furniture)
		{
			int rot = furniture.currentRotation.Value;
			rot = (rot + 1) % rotations;
			if (rot < 0) rot = 0;

			furniture.currentRotation.Value = rot;
			furniture.updateRotation();
		}

		public void updateRotation(Furniture furniture)
		{
			int rot = furniture.currentRotation.Value;
			Point pos = furniture.TileLocation.ToPoint() * Collisions.tile_game_size;

			furniture.boundingBox.Value = collisions.get_bounding_box(pos, rot);
		}

		#endregion

		#region Drawing

		public Point get_source_rect_size(int rot)
		{
			return source_rects[rot].Size;
		}

		// for drawInMenu transpiler
		private static Rectangle get_icon_source_rect(Furniture furniture)
		{
			if (FurniturePack.try_get_type(furniture, out FurnitureType? type))
			{
				return type.icon_rect;
			}

			return ItemRegistry.GetDataOrErrorItem(furniture.QualifiedItemId).GetSourceRect();
		}

		public Texture2D get_texture()
		{
			return texture.get();
		}

		public void drawAtNonTileSpot(
			Furniture furniture, SpriteBatch sprite_batch,
			Vector2 position, float depth, float alpha = 1f
		)
		{
			int rot = furniture.currentRotation.Value;

			if (furniture.isTemporarilyInvisible) return;	// taken from game code, no idea what this property is

			SpriteEffects effects = SpriteEffects.None;
			Color color = Color.White * alpha;
			Rectangle source_rect = source_rects[rot].Clone();

			if (furniture.IsOn)
				source_rect.X += source_rect.Width;

			if (furniture.timeToTurnOnLights() && time_based)
				source_rect.Y += source_rect.Height;

			if (is_animated)
			{
				long time_ms = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				int frame = (int)(time_ms / frame_length) % frame_count;
				Point c_anim_offset = anim_offset * new Point(frame);
				source_rect.Location += c_anim_offset;
			}

			sprite_batch.Draw(
				texture.get(), position, source_rect,
				color, 0f, Vector2.Zero, 4f, effects, depth
			);
		}

		private void draw_fish_tank(FishTankFurniture furniture, SpriteBatch sprite_batch, float alpha)
		{
			// Code copied from FishTankFurniture.draw

			for (int i = 0; i < furniture.tankFish.Count; i++)
			{
				TankFish tankFish = furniture.tankFish[i];
				float num = Utility.Lerp(
					furniture.GetFishSortRegion().Y,
					furniture.GetFishSortRegion().X,
					tankFish.zPosition / 20f
				);
				num += 1E-07f * i;
				tankFish.Draw(sprite_batch, alpha, num);
			}

			for (int j = 0; j < furniture.floorDecorations.Count; j++)
			{
				KeyValuePair<Rectangle, Vector2>? pair = furniture.floorDecorations[j];
				if (pair is not null)
				{
					Vector2 pos = pair.Value.Value;
					Rectangle key = pair.Value.Key;
					float layerDepth = Utility.Lerp(
						furniture.GetFishSortRegion().Y,
						furniture.GetFishSortRegion().X,
						pos.Y / 20f
					) - 1E-06f;
					sprite_batch.Draw(
						furniture.GetAquariumTexture(),
						Game1.GlobalToLocal(
							new Vector2(
								furniture.GetTankBounds().Left + pos.X * 4f,
								furniture.GetTankBounds().Bottom - 4 - pos.Y * 4f
							)
						),
						key, Color.White * alpha, 0f,
						new Vector2(key.Width / 2, key.Height - 4),
						4f, SpriteEffects.None, layerDepth
					);
				}
			}

			foreach (Vector4 bubble in furniture.bubbles)
			{
				float layerDepth2 = Utility.Lerp(
					furniture.GetFishSortRegion().Y,
					furniture.GetFishSortRegion().X,
					bubble.Z / 20f
				) - 1E-06f;
				sprite_batch.Draw(
					furniture.GetAquariumTexture(),
					Game1.GlobalToLocal(
						new Vector2(
							furniture.GetTankBounds().Left + bubble.X,
							furniture.GetTankBounds().Bottom - 4 - bubble.Y - bubble.Z * 4f
						)
					),
					new Rectangle(0, 240, 16, 16),
					Color.White * alpha,
					0f, new Vector2(8f, 8f), 4f * bubble.W,
					SpriteEffects.None, layerDepth2
				);
			}
		}

		public void draw(Furniture furniture, SpriteBatch sprite_batch, int x, int y, float alpha)
		{
			int rot = furniture.currentRotation.Value;

			if (furniture.isTemporarilyInvisible) return;	// taken from game code, no idea what this property is

			if (furniture is FishTankFurniture fish_tank)
				draw_fish_tank(fish_tank, sprite_batch, alpha);

			SpriteEffects effects = SpriteEffects.None;
			Color color = Color.White * alpha;
			float depth;
			Vector2 position;
			Rectangle bounding_box = furniture.boundingBox.Value;
			Rectangle source_rect = source_rects[rot].Clone();

			if (furniture.IsOn)
				source_rect.X += source_rect.Width;

			if (furniture.timeToTurnOnLights() && time_based)
				source_rect.Y += source_rect.Height;

			Point c_anim_offset = Point.Zero;
			if (is_animated)
			{
				long time_ms = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
				int frame = (int)(time_ms / frame_length) % frame_count;
				c_anim_offset = anim_offset * new Point(frame);
				source_rect.Location += c_anim_offset;
			}

			// computing common depth
			if (p_type == PlacementType.Rug) depth = 2E-09f + furniture.TileLocation.Y;
			else
			{
				depth = bounding_box.Top;
				if (p_type == PlacementType.Mural)
					depth -= 32;
				else depth += 16;
			}
			depth /= 10000f;

			// when the furniture is placed
			if (Furniture.isDrawingLocationFurniture)
			{
				position = new(
					bounding_box.X,
					bounding_box.Y - (source_rect.Height * 4 - bounding_box.Height)
				);
			}

			// when the furniture follows the cursor
			else
			{
				position = new(
					64*x,
					64*y - (source_rect.Height * 4 - bounding_box.Height)
				);
			}

			if (furniture.shakeTimer > 0) {
				position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			position = Game1.GlobalToLocal(Game1.viewport, position);

			sprite_batch.Draw(
				texture.get(), position, source_rect,
				color, 0f, Vector2.Zero, 4f, effects, depth
			);

			if (Furniture.isDrawingLocationFurniture)
			{
				layers.draw(
					sprite_batch, color,
					position, bounding_box.Top,
					rot, furniture.IsOn,
					furniture.timeToTurnOnLights() && time_based,
					c_anim_offset
				);

				light_sources.draw_glows(
					sprite_batch,
					furniture.boundingBox.Value.Location.ToVector2(),
					rot, furniture.IsOn, furniture.timeToTurnOnLights()
				);
			}

			initialize_slots(furniture, rot);

			// draw held object
			if (furniture.heldObject.Value is Chest chest)
			{
				slots.draw(sprite_batch, chest.Items, rot, position, bounding_box.Top, alpha);
				// draw depending on heldObject own stored bounding box
			}

			// if ((bool)isOn && (int)furniture_type == 14)
			// {
			// 	// FIREPLACE FIRE
			// 	Rectangle boundingBoxAt = GetBoundingBoxAt(x, y);
			// 	spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 12, boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt.Bottom - 2) / 10000f);
				
			// 	spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32 - 4, boundingBox.Center.Y - 64)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 2047) + (double)(y * 98)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt.Bottom - 1) / 10000f);
			// }

			// else if ((bool)isOn && (int)furniture_type == 16)
			// {
			// 	// TORCHES FIRE
			// 	Rectangle boundingBoxAt2 = GetBoundingBoxAt(x, y);
			// 	spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 20, (float)boundingBox.Center.Y - 105.6f)), new Rectangle(276 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0) * 12, 1985, 12, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBoxAt2.Bottom - 2) / 10000f);
			// }

			// to keep :

			if (Game1.debugMode)
			{
				Vector2 draw_pos = new(bounding_box.X, bounding_box.Y - (source_rect.Height * 4 - bounding_box.Height));
				sprite_batch.DrawString(Game1.smallFont, furniture.QualifiedItemId, Game1.GlobalToLocal(Game1.viewport, draw_pos), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
			}
		}

		private void draw_lighting(Furniture furniture, SpriteBatch sprite_batch)
		{
			light_sources.draw_lights(
				sprite_batch,
				furniture.boundingBox.Value.Location.ToVector2(),
				furniture.currentRotation.Value,
				furniture.IsOn, furniture.timeToTurnOnLights()
			);

			// items in slots
			if (furniture.heldObject.Value is Chest chest)
			{
				foreach (Item item in chest.Items)
				{
					if (
						item is Furniture furn_item &&
						FurniturePack.try_get_type(furn_item, out FurnitureType? type)
					)
					{
						type.draw_lighting(furn_item, sprite_batch);
					}
				}
			}
		}

		#endregion

		#region Methods for Seats

		public void GetSeatPositions(Furniture furniture, ref List<Vector2> list)
		{
			// This is a postfix, it keeps the original seat positions.

			int cur_rot = furniture.currentRotation.Value;
			Vector2 tile_pos = furniture.boundingBox.Value.Location.ToVector2() / 64f;
			
			seats.get_seat_positions(cur_rot, tile_pos, list);
		}

		public void GetSittingDirection(Furniture furniture, Farmer who, ref int sit_dir)
		{
			int seat_index = furniture.sittingFarmers[who.UniqueMultiplayerID];
			int rot = furniture.currentRotation.Value;

			int new_sit_dir = seats.get_sitting_direction(rot, seat_index);
			if (new_sit_dir >= 0) sit_dir = new_sit_dir;
		}

		public void GetSittingDepth(Furniture furniture, Farmer who, ref float depth)
		{
			int seat_index = furniture.sittingFarmers[who.UniqueMultiplayerID];
			int rot = furniture.currentRotation.Value;

			float new_sit_depth = seats.get_sitting_depth(rot, seat_index, furniture.boundingBox.Top);
			if (new_sit_depth >= 0) depth = new_sit_depth;
		}

		#endregion

		#region Methods for Collisions

		public void IntersectsForCollision(Furniture furniture, Rectangle rect, ref bool collides)
		{
			if (!collides)
			{
				// not even in bounding box, or collision canceled
				return;
			}

			if (p_type == PlacementType.Rug)
			{
				collides = false;
				return;
			}
			
			int rot = furniture.currentRotation.Value;
			Point pos = furniture.boundingBox.Value.Location;
			collides = collisions.is_colliding(rect, pos, rot);
		}

		public void canBePlacedHere(
			Furniture furniture, GameLocation loc, Vector2 tile,
			CollisionMask collisionMask, ref bool result
		)
		{
			// don't change this part

			if (!loc.CanPlaceThisFurnitureHere(furniture))
			{
				result = false;
				return;
			}

			if (!furniture.isGroundFurniture())
			{
				tile.Y = furniture.GetModifiedWallTilePosition(loc, (int)tile.X, (int)tile.Y);
			}

			CollisionMask passable_ignored = CollisionMask.Buildings | CollisionMask.Flooring | CollisionMask.TerrainFeatures;
			if (furniture.isPassable())
			{
				passable_ignored |= CollisionMask.Characters | CollisionMask.Farmers;
			}

			collisionMask &= ~(CollisionMask.Furniture | CollisionMask.Objects);

			// Actual collision detection made by collisions

			if (!collisions.can_be_placed_here(furniture, loc, tile.ToPoint(), collisionMask, passable_ignored))
			{
				result = false;
				return;
			}

			if (p_type == PlacementType.Mural)
			{

				Point point = tile.ToPoint();

				if (loc is not DecoratableLocation dec_loc) 
				{
					result = false;
					return;
				}

				if (
					!((
						dec_loc.isTileOnWall(point.X, point.Y) &&
						dec_loc.GetWallTopY(point.X, point.Y) == point.Y
					) ||
					(
						dec_loc.isTileOnWall(point.X, point.Y - 1) &&
						dec_loc.GetWallTopY(point.X, point.Y) + 1 == point.Y
					))
				)
				{
					result = false;
					return;
				}
			}

			if (furniture.GetAdditionalFurniturePlacementStatus(loc, (int)tile.X * 64, (int)tile.Y * 64) != 0)
			{
				result = false;
				return;
			}
			
			result = true;
			return;
		}

		public void AllowPlacementOnThisTile(Furniture furniture, int x, int y, ref bool allow)
		{
			allow = !is_clicked(furniture, x * 64, y * 64);
		}

		#endregion

		#region Methods for Slots

		private void initialize_slots(Furniture furniture, int rot)
		{
			if (!is_table)
			{
				furniture.heldObject.Value = null;
				return;
			}

			int slots_count = slots.get_count(rot);

			if (furniture.heldObject.Value is Chest chest)
			{
				while (chest.Items.Count > slots_count)
				{
					Item item = chest.Items[slots_count];
					chest.Items.RemoveAt(slots_count);
					if (item is null) continue;
					Game1.createItemDebris(
						item,
						furniture.boundingBox.Center.ToVector2(),
						0
					);
				}
			}

			else
			{
				StardewValley.Object held = furniture.heldObject.Value;
				chest = new();
				chest.Items.Add(held);
				furniture.heldObject.Value = chest;
			}

			if (chest.Items.Count < slots_count)
			{
				chest.Items.AddRange(
					Enumerable.Repeat<Item?>(null,
						slots_count - chest.Items.Count
					).ToList()
				);
			}
		}

		private int get_slot(Furniture furniture, Point pos)
		{
			int rot = furniture.currentRotation.Value;

			Point this_pos = furniture.boundingBox.Value.Location;
			this_pos.Y += furniture.boundingBox.Value.Height;
			this_pos.Y -= source_rects[rot].Height * 4;
			Point rel_pos = (pos - this_pos) / new Point(4);

			return slots.get_slot(rel_pos, rot);
		}

		public bool place_in_slot(Furniture furniture, Point pos, Farmer who)
		{
			int rot = furniture.currentRotation.Value;
			
			initialize_slots(furniture, rot);

			if (who.ActiveItem is not StardewValley.Object) return false;
			// player is not holding an object
			
			if (furniture.heldObject.Value is not Chest chest) return false;
			// Furniture is not a proper initialized table

			int slot_index = get_slot(furniture, pos);
			if (slot_index < 0) return false;
			// No slot found at this pixel

			if (chest.Items[slot_index] is not null) return false;
			// Slot already occupied

			if (!slots.check_tags(rot, slot_index, furniture, who))
			{
				Game1.showRedMessage("This item cannot be placed here.");
				return false;
			}
			// held item doesn't have valid context tags

			StardewValley.Object obj_inst = (StardewValley.Object)who.ActiveItem.getOne();

			if (obj_inst is Furniture furn)
			{
				Point max_size = slots.get_max_size(rot, slot_index);
				Point size = furn.boundingBox.Value.Size / new Point(64);
				if (size.X > max_size.X || size.Y > max_size.Y)
					return false;
				// cannot place furniture larger than max_size
			}

			obj_inst.Location = furniture.Location;

			chest.Items[slot_index] = obj_inst;

			who.reduceActiveItemByOne();
			Game1.currentLocation.playSound("woodyStep");

			return true;
		}

		public bool remove_from_slot(Furniture furniture, Point pos, Farmer who)
		{
			int rot = furniture.currentRotation.Value;
			
			initialize_slots(furniture, rot);

			if (furniture.heldObject.Value is not Chest chest) return false;
			// Furniture is not a proper initialized table

			int slot_index = get_slot(furniture, pos);
			if (slot_index < 0) return false;
			// No slot found at this pixel

			Item item = chest.Items[slot_index];
			if (item is not StardewValley.Object obj) return false;
			// No Object in slot

			if (who.addItemToInventoryBool(obj))
			{
				obj.performRemoveAction();
				chest.Items[slot_index] = null;
				Game1.playSound("coin");
				return true;
			}

			return false;
		}

		// used in Furniture.canBeRemoved Transpiler
		public static bool has_held_object(Furniture furniture)
		{
			StardewValley.Object held_obj = furniture.heldObject.Value;
			if (held_obj == null) return false;

			if (held_obj is Chest chest)
			{
				foreach (Item? item in chest.Items)
				{
					if (item != null) return true;
				}

				return false;	// empty chest
			}

			return true;
		}

		#endregion

		#region Methods for Placement Type

		public void isGroundFurniture(ref bool is_ground_f)
		{
			is_ground_f = p_type != PlacementType.Mural;
		}

		public void isPassable(ref bool is_passable)
		{
			is_passable = p_type == PlacementType.Rug;
		}

		#endregion

		#region Methods for Special Furniture

		public void getScreenPosition(TV furniture, ref Vector2 position)
		{
			Rectangle bounding_box = furniture.boundingBox.Value;
			position = bounding_box.Location.ToVector2();
			position.Y += bounding_box.Height;
			position.Y -= source_rects[furniture.currentRotation.Value].Height * 4f;
			position += screen_position * 4f;
		}

		public void getScreenSizeModifier(ref float scale)
		{
			scale = screen_scale;
		}

		public void GetBedSpot(BedFurniture furniture, ref Point spot)
		{
			spot = furniture.TileLocation.ToPoint() + bed_spot;
		}

		public void DoesTileHaveProperty(BedFurniture furniture,
			int tile_x, int tile_y,
			string property_name, string layer_name,
			ref string property_value,
			ref bool result
		)
		{

			if (layer_name != "Back" || property_name != "TouchAction")
				return;

			result = false;
			return;
		}

		public void GetTankBounds(FishTankFurniture furniture, ref Rectangle result)
		{
			int rot = furniture.currentRotation.Value;
			Rectangle bounding_box = furniture.boundingBox.Value;
			Rectangle source_rect = source_rects[rot].Clone();

			Point position = new(
				bounding_box.X,
				bounding_box.Y + bounding_box.Height - source_rect.Height * 4
			);
			Point size = source_rect.Size * new Point(4);

			if (fish_area is null)
			{
				result = new Rectangle(
					position + new Point(4, 64),
					size - new Point(8, 92)
				);
			}

			else
			{
				result = new Rectangle(
					position + fish_area.Value.Location,
					fish_area.Value.Size
				);
			}
		}

		#endregion

		#region Methods for Transpilers

		public static bool is_clicked(Furniture furniture, int x, int y)
		{
			if (
				!FurniturePack.try_get_type(furniture, out FurnitureType? type)
				|| type.p_type == PlacementType.Rug
			)
			{
				return furniture.boundingBox.Value.Contains(x, y);
			}
			
			else
			{
				Rectangle rect = new(x, y, 1, 1);
				bool clicks = furniture.boundingBox.Value.Intersects(rect);
				type.IntersectsForCollision(furniture, rect, ref clicks);
				return clicks;
			}
		}

		public static bool is_clicked(Furniture furniture, Point pos)
		{
			return is_clicked(furniture, pos.X, pos.Y);
		}

		public static void draw_lighting(SpriteBatch sprite_batch)
		{
			foreach (Furniture furniture in Game1.currentLocation.furniture)
			{
				if (FurniturePack.try_get_type(furniture, out FurnitureType? type))
				{
					type.draw_lighting(furniture, sprite_batch);
				}
			}
		}

		#endregion

		public void updateWhenCurrentLocation(Furniture furniture)
		{

			// Updating particles
			long ms_time = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			particles.update_timer(furniture, ms_time);

			// Checking bed intersection
			if (s_type == SpecialType.Bed)
			{
				Rectangle bed_col = bed_area.Clone();
				bed_col.Location += furniture.boundingBox.Value.Location;
				GameLocation location = furniture.Location;
				bool contains = bed_col.Contains(Game1.player.GetBoundingBox());

				if (!furniture.modData.ContainsKey("FF.checked_bed_tile"))
				{
					furniture.modData["FF.checked_bed_tile"] = contains.ToString().ToLower();
				}

				if (contains)
				{
					if (furniture.modData["FF.checked_bed_tile"] != "true" &&
						!Game1.newDay && Game1.shouldTimePass() &&
						Game1.player.hasMoved && !Game1.player.passedOut
					)
					{
						furniture.modData["FF.checked_bed_tile"] = "true";
						location.createQuestionDialogue(
							Game1.content.LoadString("Strings\\Locations:FarmHouse_Bed_GoToSleep"),
							location.createYesNoResponses(), "Sleep", null
						);
						// Game1.drawObjectQuestionDialogue
					}
				}
				else
				{
					furniture.modData["FF.checked_bed_tile"] = "false";
				}
			}
		}

		public void checkForAction(Furniture furniture, Farmer who, bool justCheckingForActivity, ref bool had_action)
		{
			if (justCheckingForActivity) return;
			// had_action is already true from original method

			// Shop
			if (shop_id != null)
			{
				if (Utility.TryOpenShopMenu(shop_id, Game1.currentLocation))
					had_action = true;
			}

			// Toggle
			if (can_be_toggled)
			{
				furniture.IsOn = !furniture.IsOn;

				sounds.play(furniture.Location, furniture.IsOn);

				particles.burst(furniture);
			}
			else
			{
				sounds.play(furniture.Location);
			}

			// Seats
			if (seats.has_seats && !had_action)
			{
				int sit_count = furniture.GetSittingFarmerCount();
				who.BeginSitting(furniture);
				if (furniture.GetSittingFarmerCount() > sit_count)
					had_action = true;
			}
			
			// maybe add place in slot or remove from slot?
		}

		public void on_removed(Furniture furniture)
		{
			furniture.modData["FF.particle_timers"] = "[]";
			furniture.heldObject.Value = null;
		}

		public void on_placed(Furniture furniture)
		{
			particles.burst(furniture);

			int rot = furniture.currentRotation.Value;
			initialize_slots(furniture, rot);
		}
	}
}