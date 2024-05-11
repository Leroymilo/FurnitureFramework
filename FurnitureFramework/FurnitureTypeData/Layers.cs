
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace FurnitureFramework
{
	class Layers
	{
		#region LayerData

		private class LayerData
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;

			Texture2D texture;
			Rectangle source_rect;

			Vector2 draw_pos = Vector2.Zero;
			readonly Depth depth;
			
			#region LayerData Parsing

			// When LayerData can have directional source rects
			public static List<LayerData?> make_layers(
				JObject layer_obj, Texture2D source_texture,
				Point rect_offset, List<string> rot_names
			)
			{
				List<LayerData?> result = new();
				JToken? rect_token = layer_obj.GetValue("Source Rect");

				Rectangle source_rect = Rectangle.Empty;
				if (JsonParser.try_parse(rect_token, ref source_rect))
				{
					source_rect.Location += rect_offset;
					LayerData layer = new(layer_obj, source_texture, source_rect);
					result.AddRange(Enumerable.Repeat(layer, rot_names.Count));
					return result;
				}

				List<Rectangle?> source_rects = new();
				if (JsonParser.try_parse(rect_token, rot_names, ref source_rects))
				{
					foreach (Rectangle? rect_ in source_rects)
					{
						if (rect_ is null) result.Add(null);
						else {
							Rectangle rect = rect_.Value;
							rect.Location += rect_offset;
							result.Add(new(layer_obj, source_texture, rect));
						}
					}
					return result;
				}

				throw new InvalidDataException("Missing or invalid Source Rect.");
			}
			
			// When LayerData cannot have directional source rects
			public static LayerData make_layer(
				JObject layer_obj, Texture2D source_texture, Point rect_offset
			)
			{
				Rectangle source_rect = Rectangle.Empty;
				if (JsonParser.try_parse(layer_obj.GetValue("Source Rect"), ref source_rect))
				{
					source_rect.Location += rect_offset;
					return new(layer_obj, source_texture, source_rect);
				}

				throw new InvalidDataException("Missing or invalid Source Rect.");
			}

			private LayerData(JObject layer_obj, Texture2D source_texture, Rectangle rect)
			{
				texture = source_texture;
				source_rect = rect;

				// Parsing optional layer draw position

				JToken? pos_token = layer_obj.GetValue("Draw Pos");
				JsonParser.try_parse(pos_token, ref draw_pos);
				draw_pos *= new Vector2(4);	// game rendering scale

				// Parsing optional layer depth

				try { depth = new(layer_obj.GetValue("Depth")); }
				catch (InvalidDataException) { depth = new(); }

				is_valid = true;
			}

			#endregion

			#region LayerData Methods

			public void draw(
				SpriteBatch sprite_batch, Color color,
				Vector2 texture_pos, float base_depth,
				bool is_on, Point c_anim_offset
			)
			{
				if (is_on)
					source_rect.X += source_rect.Width;
				source_rect.Location += c_anim_offset;

				sprite_batch.Draw(
					texture, texture_pos + draw_pos, source_rect,
					color, 0f, Vector2.Zero, 4f, SpriteEffects.None,
					depth.get_value(base_depth)
				);
			}

			#endregion
		}

		#endregion

		public bool has_layers {get; private set;} = false;
		List<List<LayerData>> layers = new();

		#region Layers Parsing

		public static Layers make_layers(JToken? token, List<string> rot_names, Texture2D texture, Point rect_offset)
		{
			int rot_count = 1;
			bool directional = false;
			if (rot_names.Count > 0)
			{
				rot_count = rot_names.Count;
				directional = true;
			}
			Layers result = new(rot_count);

			if (token is JArray layers_arr)
			{
				foreach (JToken layer_token in layers_arr)
				{
					if (layer_token is not JObject layer_obj) continue;
					if (directional)
						result.add_layers(layer_obj, texture, rect_offset, rot_names);
					else
						result.add_layer(layer_obj, texture, rect_offset);
				}
			}

			else if (token is JObject dir_layers_obj)
			{
				foreach ((string key, int rot) in rot_names.Select((value, index) => (value, index)))
				{
					JToken? dir_layers_tok = dir_layers_obj.GetValue(key);
					if (dir_layers_tok is not JArray dir_layers_arr) continue;

					foreach (JToken layer_token in dir_layers_arr)
					{
						if (layer_token is not JObject layer_obj) continue;
						result.add_layer(layer_obj, texture, rect_offset, rot);
					}
				}
			}

			return result;
		}

		private void add_layers(JObject layer_obj, Texture2D texture, Point rect_offset, List<string> rot_names)
		{
			List<LayerData?> list;
			try
			{
				list = LayerData.make_layers(layer_obj, texture, rect_offset, rot_names);
			}
			catch (InvalidDataException ex)
			{
				ModEntry.log($"Invalid layer at {layer_obj.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{ex.Message}", LogLevel.Warn);
				ModEntry.log("Skipping Layer.", LogLevel.Warn);
				return;
			}
			
			foreach ((LayerData? layer, int rot) in list.Select((value, index) => (value, index)))
			{
				if (layer is null) continue;
				if (!layer.is_valid)
				{
					ModEntry.log($"Invalid layer at {layer_obj.Path}->{rot_names[rot]}:", LogLevel.Warn);
					ModEntry.log($"\t{layer.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Layer.", LogLevel.Warn);
					continue;
				}
				layers[rot].Add(layer);
				has_layers = true;
			}
		}

		private void add_layer(JObject layer_obj, Texture2D texture, Point rect_offset, int? rot = null)
		{
			LayerData layer;
			try
			{
				layer = LayerData.make_layer(layer_obj, texture, rect_offset);
			}
			catch (InvalidDataException ex)
			{
				ModEntry.log($"Invalid layer at {layer_obj.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{ex.Message}", LogLevel.Warn);
				ModEntry.log("Skipping Layer.", LogLevel.Warn);
				return;
			}

			if (!layer.is_valid)
			{
				ModEntry.log($"Invalid layer at {layer_obj.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{layer.error_msg}", LogLevel.Warn);
				ModEntry.log("Skipping Layer.", LogLevel.Warn);
				return;
			}

			if (rot is null)
			{
				foreach (List<LayerData> layer_list in layers)
				{
					layer_list.Add(layer);
				}
			}
			else layers[rot.Value].Add(layer);

			has_layers = true;
		}

		private Layers(int rot_nb)
		{
			for (int i = 0; i < rot_nb; i++)
				layers.Add(new());
		}

		#endregion

		#region Layers Methods

		public void draw(
			SpriteBatch sprite_batch, Color color,
			Vector2 texture_pos, float base_depth,
			int rot, bool is_on, Point c_anim_offset
		)
		{
			if (!has_layers) return;

			foreach (LayerData layer in layers[rot])
			{
				layer.draw(sprite_batch, color, texture_pos, base_depth, is_on, c_anim_offset);
			}
		}

		#endregion
	}

}