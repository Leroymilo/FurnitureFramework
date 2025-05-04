using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.Type.Properties
{
	[RequiresPreviewFeatures]
	class LightList: IProperty<LightList>
	{

		#region Light Subclass

		private enum LightType { Source, Glow }

		private class Light
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;
			public readonly LightType light_type;

			DynaTexture? texture = null;
			Rectangle source_rect;	// directional
			Point position;			// directional

			bool can_be_toggled;
			bool time_based;
			float radius;
			Color color;

			#region Light Parsing

			public Light(TypeInfo info, JObject data, string rot_name)
			{
				// Parsing Light Type
				error_msg = "Missing or Invalid Light Type";
				if (!JsonParser.try_parse_enum(data.GetValue("Light Type"), ref light_type))
					return;

				// Parsing Source Rectangle
				error_msg = "Missing or Invalid Source Rectangle";
				if (!JsonParser.try_parse_dir(data.GetValue("Source Rect"), rot_name, ref source_rect))
					return;
				
				// Parsing Position
				error_msg = "Missing or Invalid Position";
				if (!JsonParser.try_parse_dir(data.GetValue("Position"), rot_name, ref position))
					return;

				is_valid = true;

				parse_optional(info, data);
			}

			private void parse_optional(TypeInfo info, JObject data)
			{
				// Parsing Source Image
				string texture_path = "";
				if (JsonParser.try_parse(data.GetValue("Source Image"), ref texture_path))
					texture = new(info, texture_path);

				can_be_toggled = JsonParser.parse(data.GetValue("Toggle"), false);
				time_based = JsonParser.parse(data.GetValue("Time Based"), false);

				radius = JsonParser.parse(data.GetValue("Radius"), 2f);
				color = JsonParser.parse_color(data.GetValue("Color"), Color.White);
				if (light_type == LightType.Source)
					color = new(256 - color.R, 265 - color.G, 256 - color.B, color.A);
			}

			#endregion

			#region Light Methods

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

				else if (location is StardewValley.Locations.FarmHouse { upgradeLevel: >0 } farmHouse)
				{
					Vector2 vector2 = farmHouse.getKitchenStandingSpot().ToVector2() - tile_pos;
					if (vector2.Y == 3f && new float[] {-2f, -1f, 2f, 3f}.Contains(vector2.X))
						return 1.5f;
				}
				
				return 10f;
			}

			public void draw(DrawData draw_data, bool is_on, bool is_dark)
			{
				if (texture != null) draw_data.texture = texture.Value.get();

				if (can_be_toggled) draw_data.is_on = is_on;
				if (time_based) draw_data.is_dark = is_dark;

				draw_data.position += position.ToVector2() * 4f;
				draw_data.source_rect = source_rect;
				draw_data.color = color;
				draw_data.origin = source_rect.Size.ToVector2() / 2f;

				switch (light_type)
				{
					case LightType.Source:
						draw_data.depth = 0.9f;
						int quality = Game1.options.lightingQuality;
						draw_data.position *= 2f / quality;
						draw_data.scale = 2f * radius / quality;
						break;
					case LightType.Glow:
						Vector2 global_pos = draw_data.position;
						global_pos += new Vector2(Game1.viewport.X, Game1.viewport.Y);
						draw_data.depth = global_pos.Y + 64f * get_window_glow_depth(global_pos);
						draw_data.depth /= 10000f;
						break;
				}

				draw_data.rect_offset = Point.Zero;

				draw_data.draw();
			}

			public void debug_print(int indent_count)
			{
				string indent = new('\t', indent_count);

				if (texture is not null)
					ModEntry.log($"{indent}Texture Path: {texture.Value.asset_name}", LogLevel.Debug);
				ModEntry.log($"{indent}Source Rectangle: {source_rect}", LogLevel.Debug);
				ModEntry.log($"{indent}Position: {position}", LogLevel.Debug);

				ModEntry.log($"{indent}Toggleable: {can_be_toggled}", LogLevel.Debug);
				ModEntry.log($"{indent}Time Based: {time_based}", LogLevel.Debug);
				ModEntry.log($"{indent}Radius: {radius}", LogLevel.Debug);
				ModEntry.log($"{indent}Color: {color}", LogLevel.Debug);

			}

			#endregion
		}

		#endregion

		#region LightList Parsing

		public static LightList make_default(TypeInfo info, string rot_name)
		{
			return new(info, new JArray() /*empty array -> no lights*/, rot_name);
		}

		public static LightList make(TypeInfo info, JToken? data, string rot_name, out string? error_msg)
		{
			error_msg = null;

			if (data == null || data.Type == JTokenType.None)
				return make_default(info, rot_name);
			
			if (data is JObject obj)
			{
				// single light?
				Light light = new(info, obj, rot_name);
				if (light.is_valid)
					return new(light);

				// directional?
				JToken? dir_token = obj.GetValue(rot_name);
				if (dir_token is JObject dir_obj)
				{
					// directional single light?
					Light dir_light = new(info, dir_obj, rot_name);
					if (dir_light.is_valid)
						return new(dir_light);
					
					// single light was invalid
					ModEntry.log($"Could not parse a light in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{light.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Light.", LogLevel.Warn);
				}

				else if (dir_token is JArray dir_arr)
				{
					// directional lights
					return new(info, dir_arr, rot_name);
				}

				else
				{
					// single light was invalid
					ModEntry.log($"Could not parse a light in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{light.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Light.", LogLevel.Warn);
				}
			}

			else if (data is JArray arr)
			{
				// lights
				return new(info, arr, rot_name);
			}

			// for all invalid cases
			error_msg = "Invalid Light List definition, fallback to no Lights.";
			return make_default(info, rot_name);
		}

		List<Light> sources = new();
		List<Light> glows = new();
		public readonly bool is_valid = false;

		private LightList(Light light)
		{
			add_light(light);
			is_valid = true;
		}

		private LightList(TypeInfo info, JArray array, string rot_name)
		{
			foreach (JToken token in array)
			{
				if (token is not JObject obj2) continue;	// skips comments
				add_light(info, obj2, rot_name);
			}

			is_valid = true;
		}

		private void add_light(TypeInfo info, JObject data, string rot_name)
		{
			Light light = new(info, data, rot_name);
			if (light.is_valid)
				add_light(light);
			else
			{
				ModEntry.log($"Invalid Light in {info.mod_id} at {data.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{light.error_msg}", LogLevel.Warn);
				ModEntry.log($"Skipping Light.");
			}
		}

		private void add_light(Light light)
		{
			switch (light.light_type)
			{
				case LightType.Source:
					sources.Add(light);
					break;
				case LightType.Glow:
					glows.Add(light);
					break;
			}
		}

		#endregion
	
		#region LightList Methods

		public void draw_sources(DrawData draw_data, bool is_on, bool is_dark)
		{
			foreach (Light light in sources)
			{
				light.draw(draw_data, is_on, is_dark);
			}
		}

		public void draw_glows(DrawData draw_data, bool is_on, bool is_dark)
		{
			foreach (Light light in glows)
			{
				light.draw(draw_data, is_on, is_dark);
			}
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			int index = 0;
			foreach (Light source in sources)
			{
				ModEntry.log($"{indent}Light Source {index}:", LogLevel.Debug);
				source.debug_print(indent_count + 1);
				index ++;
			}

			index = 0;
			foreach (Light glow in glows)
			{
				ModEntry.log($"{indent}Light Glow {index}:", LogLevel.Debug);
				glow.debug_print(indent_count + 1);
				index ++;
			}
		}

		#endregion
	}
}