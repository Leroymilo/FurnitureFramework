
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework.Graphics;
using System.Data;

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
			float depth = 0;
			
			#region LayerData Parsing

			public LayerData(JObject layer_obj, Texture2D source_texture)
			{
				texture = source_texture;

				// Parsing required layer source rectangle

				error_msg = "Missing or Invalid Source Rectangle.";
				JToken? rect_token = layer_obj.GetValue("Source Rect");
				if (rect_token == null || rect_token.Type == JTokenType.Null)
					return;
				try
				{
					source_rect = JC.extract_rect(rect_token);
				}
				catch (InvalidDataException)
				{
					return;
				}

				// Parsing optional layer draw position

				JToken? pos_token = layer_obj.GetValue("Draw Pos");
				if (pos_token != null && pos_token.Type != JTokenType.Null)
				{
					try
					{
						draw_pos = JC.extract_position(pos_token);
					}
					catch (InvalidDataException)
					{
						ModEntry.log(
							$"Invalid Draw Position at {pos_token.Path}, defaulting to (0, 0).",
							LogLevel.Warn
						);
					}
				}
				draw_pos *= new Vector2(4);	// game rendering scale

				// Parsing optional layer depth

				JToken? depth_token = layer_obj.GetValue("Depth");
				if (depth_token != null &&
					(depth_token.Type == JTokenType.Float ||
					depth_token.Type == JTokenType.Integer)
				)
				{
					depth = (float)depth_token;
				}

				is_valid = true;
			}

			#endregion

			#region LayerData Methods

			public void draw(
				SpriteBatch sprite_batch, Color color,
				Vector2 texture_pos, float base_depth
			)
			{
				sprite_batch.Draw(
					texture, texture_pos + draw_pos, source_rect,
					color, 0f, Vector2.Zero, 4f, SpriteEffects.None,
					(base_depth - depth * 64) / 10000f
				);
			}

			#endregion
		}

		#endregion

		public bool has_layers {get; private set;} = false;

		List<List<LayerData>> directional_layers = new();
		List<LayerData> singular_layers = new();
		bool is_directional = false;

		#region Layers Parsing

		public Layers(JToken? layers_token, List<string> rot_names, Texture2D texture)
		{
			if (layers_token == null || layers_token.Type == JTokenType.Null)
				return;	// No layers

			// Case 1 : non-directional layers

			if (layers_token is JArray layers_arr)
			{
				parse_layer_array(layers_arr, singular_layers, texture);
			}

			// Case 2 : directional layers

			else if (layers_token is JObject layers_obj)
			{
				foreach (string rot_name in rot_names)
				{
					List<LayerData> layer_list = new();

					JToken? layers_dir_token = layers_obj.GetValue(rot_name);
					if (layers_dir_token is JArray layers_dir_arr)
					{
						parse_layer_array(layers_dir_arr, layer_list, texture);
					}
					directional_layers.Add(layer_list);
				}

				is_directional = true;
			}
		}

		private void parse_layer_array(
			JArray layers_arr, List<LayerData> layer_list,
			Texture2D texture
		)
		{
			foreach (JToken layer_token in layers_arr.Children())
			{
				if (layer_token is not JObject layer_obj) continue;

				LayerData layer = new(layer_obj, texture);
				if (!layer.is_valid)
				{
					ModEntry.log($"Invalid Layer Data at {layer_token.Path}:", StardewModdingAPI.LogLevel.Warn);
					ModEntry.log($"\t{layer.error_msg}", StardewModdingAPI.LogLevel.Warn);
					ModEntry.log("Skipping Layer.", StardewModdingAPI.LogLevel.Warn);
					continue;
				}
				has_layers = true;
				layer_list.Add(layer);
			}
		}

		#endregion

		#region Layers Methods

		public void draw(
			SpriteBatch sprite_batch, Color color,
			Vector2 texture_pos, float base_depth, int rot
		)
		{
			if (!has_layers) return;

			List<LayerData> cur_layers;
			
			if (is_directional)
				cur_layers = directional_layers[rot];
			else
				cur_layers = singular_layers;

			foreach (LayerData layer in cur_layers)
			{
				layer.draw(sprite_batch, color, texture_pos, base_depth);
			}
		}

		#endregion
	}

}