using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace FurnitureFramework
{
	class LightSources
	{

		enum LightMode {
			always_on,
			when_on,
			when_off,
			when_dark_out,
			when_bright_out
		}

		#region LightSourceData

		private class LightSourceData
		{
			public readonly bool is_valid = false;
			public readonly string error_msg = "";

			Texture2D texture;
			Rectangle source_rect;
			List<Point?> positions = new();
			float radius = 2f;
			Color color;
			LightMode mode = LightMode.when_bright_out;

			public readonly bool is_glow = false;


			#region LightSourceData Parsing

			public LightSourceData(IContentPack pack, JObject light_obj, List<string> rot_names)
			{
				JToken? token = light_obj.GetValue("Position");
				Point single_pos = new();
				if (JsonParser.try_parse(token, ref single_pos))
				{
					// Single Position

					positions.AddRange(
						Enumerable.Repeat(
							(Point?)single_pos,
							Math.Max(rot_names.Count, 1)
						)
					);
				}
				else
				{
					// Directional Position

					if (rot_names.Count == 0 || token is not JObject pos_obj)
					{
						error_msg = "Missing or Invalid non-directional position of Light Source for non-directional Furniture.";
						return;
					}

					bool has_dir = false;

					foreach (string key in rot_names)
					{
						token = pos_obj.GetValue(key);
						Point dir_pos = new();
						if (JsonParser.try_parse(token, ref dir_pos))
						{
							has_dir = true;
							positions.Add(dir_pos);
						}
						else positions.Add(null);
					}

					if (!has_dir)
					{
						error_msg = "Missing or Invalid position.";
						return;
					}
				}

				token = light_obj.GetValue("Source Image");
				string image_path = JsonParser.parse(token, "FF/light_glows/window.png");
				texture = TextureManager.load(pack.ModContent, image_path);

				token = light_obj.GetValue("Source Rect");
				if (token is not JObject || !JsonParser.try_parse(token, ref source_rect))
					source_rect = texture.Bounds;

				color = JsonParser.parse_color(light_obj.GetValue("Color"), "White");

				token = light_obj.GetValue("Mode");
				if (token is JValue && token.Type == JTokenType.String)
				{
					LightMode parsed_mode = Enum.Parse<LightMode>(token.ToString());
					if (Enum.IsDefined(parsed_mode))
					{
						mode = parsed_mode;
					}
				}

				token = light_obj.GetValue("Radius");
				radius = JsonParser.parse(token, 2f);

				token = light_obj.GetValue("Is Glow");
				is_glow = JsonParser.parse(token, false);

				is_valid = true;
			}

			#endregion

			#region LightSourceData Methods

			private bool should_turn_on(bool is_on, bool is_dark)
			{
				switch (mode)
				{
					case LightMode.always_on: return true;
					case LightMode.when_on: return is_on;
					case LightMode.when_off: return !is_on;
					case LightMode.when_dark_out: return is_dark;
					case LightMode.when_bright_out: return !is_dark;
					default: return false;
				}
			}

			private float get_window_glow_depth(Vector2 position)
			{
				GameLocation location = Game1.currentLocation;
				Vector2 tile_pos = Vector2.Round(position / 64f);
				tile_pos.Y += 1;

				Furniture furnitureAt = location.GetFurnitureAt(
					tile_pos + new Vector2(0, 2)
				);

				if (furnitureAt != null && furnitureAt.sourceRect.Height / 16 - furnitureAt.getTilesHigh() > 1)
				{
					return 2.5f;
				}

				else if (location is FarmHouse { upgradeLevel: >0 } farmHouse)
				{
					Vector2 vector2 = farmHouse.getKitchenStandingSpot().ToVector2() - tile_pos;
					if (vector2.Y == 3f && new float[] {-2f, -1f, 2f, 3f}.Contains(vector2.X))
						return 1.5f;
				}
				
				return 10f;
			}

			public void draw(SpriteBatch sprite_batch, Vector2 position, int rot, bool is_on, bool is_dark)
			{
				if (!should_turn_on(is_on, is_dark)) return;

				Point? pxl_pos = positions[rot];
				if (pxl_pos is null) return;

				int quality = Game1.options.lightingQuality;

				Vector2 pos = Game1.GlobalToLocal(Game1.viewport, position);
				pos += pxl_pos.Value.ToVector2() * 4f;
				float scale = 4f;
				float depth = 0.9f;

				if (is_glow)
				{
					float depth_offset = get_window_glow_depth(position);
					depth = position.Y + 64f * depth_offset;
					depth /= 10000f;
				}
				else
				{
					pos *= 2f / quality;
					scale = 2f * radius / quality;
				}

				sprite_batch.Draw(
					texture, pos, source_rect,
					color, 0f, source_rect.Size.ToVector2() / 2f, scale,
					SpriteEffects.None, depth
				);
			}

			#endregion
		}

		#endregion

		List<LightSourceData> light_sources = new();
		List<LightSourceData> light_glows = new();

		#region LightSources Parsing

		public LightSources(IContentPack pack, JToken? token, List<string> rot_names)
		{
			if (token is not JArray light_arr) 
			{
				if (token is not null)
					ModEntry.log($"{token.Path} is not an array!");
				return;
			}

			foreach (JToken light_token in light_arr.Children())
			{
				if (light_token is not JObject light_obj)
					continue;

				LightSourceData light = new(pack, light_obj, rot_names);

				if (!light.is_valid)
				{
					ModEntry.log($"Invalid Light Source definition at {light_obj.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{light.error_msg}", LogLevel.Warn);
					ModEntry.log($"Skipping Light Source.", LogLevel.Warn);
					continue;
				}

				if (light.is_glow)
					light_glows.Add(light);
				else light_sources.Add(light);
			}
		}

		#endregion

		#region LightSources Methods

		public void draw_lights(SpriteBatch sprite_batch, Vector2 position, int rot, bool is_on, bool is_dark)
		{
			foreach (LightSourceData light in light_sources)
			{
				light.draw(sprite_batch, position, rot, is_on, is_dark);
			}
		}

		public void draw_glows(SpriteBatch sprite_batch, Vector2 position, int rot, bool is_on, bool is_dark)
		{
			foreach (LightSourceData light in light_glows)
			{
				light.draw(sprite_batch, position, rot, is_on, is_dark);
			}
		}

		#endregion

	}
}