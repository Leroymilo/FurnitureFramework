
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;


namespace FurnitureFramework
{

	class FurnitureType
	{
		string mod_id;

		public readonly string id;
		string display_name;
		string type;

		int rotations;
		List<string> rot_names = new();

		List<Point> bb_sizes = new();

		int price;
		int placement_rules;
		
		Texture2D texture;
		List<Rectangle> source_rects = new();
		
		Texture2D? front_texture = null;
		List<Rectangle> front_source_rects = new();

		bool exclude_from_random_sales;
		List<string> context_tags = new();

		// List<LightSource> light_sources = new();

		// TO ADD : torch fire positions, seats, placement spots

		bool is_rug = false;
		bool has_front = false;
		// bool can_be_toggled = false;
		// bool can_be_placed_on = false;
		bool is_mural = false;


		public FurnitureType(IContentPack pack, string id, JObject data)
		{
			#region base attributes

			mod_id = pack.Manifest.UniqueID;
			this.id = $"{mod_id}.{id}";
			display_name = JC.extract(data, "Display Name", "No Name");
			type = JC.extract(data, "Force Type", "other");
			price = JC.extract(data, "Price", 0);
			exclude_from_random_sales = JC.extract(data, "Exclude from Random Sales", false);

			placement_rules =
				+ 1 * JC.extract(data, "Indoors", 1)
				+ 2 * JC.extract(data, "Outdoors", 1)
				- 1;

			parse_rotations(data);

			#endregion

			#region textures & source rects

			string text_path = data.Value<string>("Texture")
				?? throw new InvalidDataException($"Missing Texture for Furniture {id}");
			texture = TextureManager.load(pack.ModContent, text_path);

			JToken? rect_token = data.GetValue("SourceRect");
			if (rect_token != null && rect_token.Type != JTokenType.Null)
				JC.get_directional_rectangles(rect_token, source_rects, rot_names);
			else if (rotations == 1)
				source_rects.Add(texture.Bounds);
			else
				throw new InvalidDataException($"Missing SourceRects for Furniture {id}.");

			string? front_text_path = data.Value<string?>("Front Texture");
			if (front_text_path != null)
				front_texture = TextureManager.load(pack.ModContent, front_text_path);

			JToken? front_rect_token = data.GetValue("Front SourceRect");
			if (front_rect_token != null && front_rect_token.Type != JTokenType.Null)
				JC.get_directional_rectangles(front_rect_token, front_source_rects, rot_names);

			if (front_texture != null || front_source_rects.Count > 0)
			{
				has_front = true;
				front_texture ??= texture;
				if (front_source_rects.Count == 0) front_source_rects = source_rects;
			}

			#endregion

			JToken size_token = data.GetValue("Bounding Box Size")
				?? throw new InvalidDataException($"Missing Bounding Box Size for Furniture {id}.");
			JC.get_directional_sizes(size_token, bb_sizes, rot_names);
			
			JToken? tag_token = data.GetValue("Context Tags");
			if (tag_token != null) JC.get_list_of_string(tag_token, context_tags);
		}

		public void parse_rotations(JObject data)
		{
			JToken? rot_token = data.GetValue("Rotations");
			if (rot_token == null || rot_token.Type == JTokenType.Null)
				throw new InvalidDataException($"Missing Rotations for Furniture {id}.");
			
			if (rot_token is JValue rot_value)
			{
				try
				{
					rotations = (int)rot_value;
				}
				catch
				{
					throw new InvalidDataException($"Invalid Rotations for Furniture {id}, should be a number or a list of names.");
				}

				switch (rotations)
				{
					case 1:
						return;
					case 2:
						rot_names.AddRange(new string[2]{"Horizontal", "Vertical"});
						return;
					case 4:
						rot_names.AddRange(new string[4]{"Down", "Right", "Up", "Left"});
						return;
				}

				throw new InvalidDataException($"Invalid Rotations for Furniture {id}, number can be 1, 2 or 4.");
			}

			if (rot_token is JArray rot_keys)
			{
				bool has_null_keys = false;
				foreach (string? key in rot_keys.Values<string>())
				{
					if (key == null) 
					{
						has_null_keys = true;
						continue;
					}
					rot_names.Add(key);
				}
				if (has_null_keys)
					ModEntry.log($"Invalid rotation(s) skipped in Furniture {id}.", LogLevel.Warn);
				
				rotations = rot_names.Count;
				return;
			}

			throw new InvalidDataException($"Invalid Rotations for Furniture {id}, should be a number or a list of names.");
		}

		public string get_string_data()
		{
			string result = display_name;
			result += $"/{type}";
			result += $"/{source_rects[0].Width/16} {source_rects[0].Height/16}";
			result += $"/{bb_sizes[0].X} {bb_sizes[0].X}";
			result += $"/{rotations}";
			result += $"/{price}";
			result += $"/{placement_rules}";
			result += $"/{display_name}";
			result += $"/0";
			result += $"/{id}";
			result += $"/{exclude_from_random_sales}";
			if (context_tags.Count > 0)
				result += $"/" + context_tags.Join(delimiter: " ");

			ModEntry.log(result);

			return result;
		}

		public Texture2D get_icon_texture()
		{
			return texture;
		}

		private Point get_bb_size(int rot)
		{
			if (bb_sizes.Count == 1)
				return bb_sizes[0];
			
			return get_bb_size(rot);
		}

		public void draw(Furniture furniture, SpriteBatch sprite_batch, int x, int y, float alpha)
		{
			int rot = furniture.currentRotation.Value;

			if (furniture.isTemporarilyInvisible) return;	// taken from game code, no idea what's this property

			SpriteEffects effects = SpriteEffects.None;
			Color color = Color.White * alpha;
			float depth;
			Vector2 position;

			// computing common depth :
			if (is_rug) depth = 2E-09f + furniture.TileLocation.Y;
			else
			{
				depth = furniture.boundingBox.Value.Bottom;
				if (is_mural) depth -= 48;
				else depth -= 8;
			}
			depth /= 10000f;

			// when the furniture is placed
			if (Furniture.isDrawingLocationFurniture)
			{
				position = new(
					furniture.boundingBox.X,
					furniture.boundingBox.Y - (source_rects[rot].Height * 4 - get_bb_size(rot).Y * 64)
				);
				if (furniture.shakeTimer > 0) {
					position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
				}
				position = Game1.GlobalToLocal(Game1.viewport, position);

				// // drawing a part of the furniture in front of the player
				// if (furniture.HasSittingFarmers())	// TODO : replace for custom seats support
				// {
				// 	depth = furniture.boundingBox.Value.Top + 16 / 10000f;
				// 	float front_depth = (furniture.boundingBox.Value.Bottom - 8) / 10000f;

				// 	if (has_front && front_texture != null && front_source_rects[rot].Right <= front_texture.Width && front_source_rects[rot].Bottom <= front_texture.Height)
				// 	{
				// 		sprite_batch.Draw(
				// 			front_texture, position, front_source_rects[rot],
				// 			color, 0f, Vector2.Zero, 4f, effects, front_depth
				// 		);
				// 	}
				// }

				sprite_batch.Draw(
					texture, position, source_rects[rot],
					color, 0f, Vector2.Zero, 4f, effects, depth
				);
			}

			// when the furniture follows the cursor
			else
			{
				position = new(
					64*x,
					64*y - (source_rects[rot].Height * 4 - get_bb_size(rot).Y * 64)
				);
				if (furniture.shakeTimer > 0) {
					position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
				}
				position = Game1.GlobalToLocal(Game1.viewport, position);

				sprite_batch.Draw(
					texture, position, source_rects[rot],
					color, 0f, Vector2.Zero, 4f, effects, depth
				);
			}

			// vanilla method

			// // CODE FOR ITEM ON TABLE
			// if (heldObject.Value != null)
			// {
			// 	if (heldObject.Value is Furniture furniture)
			// 	{
			// 		furniture.drawAtNonTileSpot(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - furniture.sourceRect.Height * 4 - (drawHeldObjectLow ? (-16) : 16))), (float)(boundingBox.Bottom - 7) / 10000f, alpha);
			// 	}
			// 	else
			// 	{
			// 		ParsedItemData dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
			// 		spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow ? 32 : 85))) + new Vector2(32f, 53f), Game1.shadowTexture.Bounds, Color.White * alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)boundingBox.Bottom / 10000f);
			// 		if (heldObject.Value is ColoredObject)
			// 		{
			// 			heldObject.Value.drawInMenu(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow ? 32 : 85))), 1f, 1f, (float)(boundingBox.Bottom + 1) / 10000f, StackDrawType.Hide, Color.White, drawShadow: false);
			// 		}
			// 		else
			// 		{
			// 			spriteBatch.Draw(dataOrErrorItem2.GetTexture(), Game1.GlobalToLocal(Game1.viewport, new Vector2(boundingBox.Center.X - 32, boundingBox.Center.Y - (drawHeldObjectLow ? 32 : 85))), dataOrErrorItem2.GetSourceRect(), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)(boundingBox.Bottom + 1) / 10000f);
			// 		}
			// 	}
			// }

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
				Vector2 draw_pos = new(furniture.boundingBox.X, furniture.boundingBox.Y - (source_rects[rot].Height * 4 - get_bb_size(rot).Y * 64));
				sprite_batch.DrawString(Game1.smallFont, furniture.QualifiedItemId, Game1.GlobalToLocal(Game1.viewport, draw_pos), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
			}

			ModEntry.print_debug = false;
		}
	}

}