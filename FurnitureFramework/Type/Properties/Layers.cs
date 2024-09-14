
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System.Runtime.Versioning;

namespace FurnitureFramework.Type.Properties
{
	[RequiresPreviewFeatures]
	class LayerList: IProperty<LayerList>
	{

		#region Layer Subclass

		private class Layer
		{
			public readonly bool is_valid = false;
			public readonly string? error_msg;

			Rectangle source_rect;

			Vector2 draw_pos = Vector2.Zero;
			Depth depth = new(0, 1000);

			#region Layer Parsing

			public Layer(JObject data, string rot_name)
			{
				// Parsing Required Source Rectangle
				JToken? rect_token = data.GetValue("Source Rect");
				if (!JsonParser.try_parse(rect_token, out source_rect))
				{
					error_msg = "Missing or Invalid Source Rectangle";

					// Directional?
					if (rect_token is not JObject rect_obj) return;

					JToken? dir_rect_token = rect_obj.GetValue(rot_name);
					if (!JsonParser.try_parse(dir_rect_token, out source_rect))
						return;
				}

				is_valid = true;

				parse_optional(data);
			}

			private void parse_optional(JObject data)
			{
				// Parsing optional layer draw position

				JToken? pos_token = data.GetValue("Draw Pos");
				JsonParser.try_parse(pos_token, ref draw_pos);
				draw_pos *= 4f;	// game rendering scale

				// Parsing optional layer depth

				try { depth = new(data.GetValue("Depth")); }
				catch (InvalidDataException) { }
			}

			#endregion

			#region Layer Methods

			public void draw(DrawData draw_data, float top)
			{
				draw_data.source_rect = source_rect;
				draw_data.position += draw_pos;
				draw_data.depth = depth.get_value(top);

				draw_data.draw();
			}

			public void debug_print(int indent_count)
			{
				string indent = new('\t', indent_count);
				ModEntry.log($"{indent}Source Rectangle: {source_rect}", LogLevel.Debug);
				ModEntry.log($"{indent}Draw Position: {(draw_pos/4f).ToPoint()}", LogLevel.Debug);
				depth.debug_print(indent_count);
			}

			#endregion
		}

		#endregion

		#region LayerList Parsing

		public static LayerList make_default(TypeInfo info, string rot_name)
		{
			return new(info, new JArray() /*empty array -> no layers*/, rot_name);
		}

		public static LayerList make(TypeInfo info, JToken? data, string rot_name, out string? error_msg)
		{
			error_msg = null;
			
			if (data is JObject obj)
			{
				// single layer?
				Layer layer = new(obj, rot_name);
				if (layer.is_valid)
					return new(layer);

				// directional?
				JToken? dir_token = obj.GetValue(rot_name);
				if (dir_token is JObject dir_obj)
				{
					// directional single layer?
					Layer dir_layer = new(dir_obj, rot_name);
					if (dir_layer.is_valid)
						return new(dir_layer);
					
					// single layer was invalid
					ModEntry.log($"Could not parse a layer in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{layer.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Layer.", LogLevel.Warn);
				}

				else if (dir_token is JArray dir_arr)
				{
					// directional layers
					return new(info, dir_arr, rot_name);
				}

				else
				{
					// single layer was invalid
					ModEntry.log($"Could not parse a layer in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{layer.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Layer.", LogLevel.Warn);
				}
			}

			else if (data is JArray arr)
			{
				// list of layers
				return new(info, arr, rot_name);
			}

			// for all invalid cases
			return make_default(info, rot_name);
		}

		List<Layer> list = new();
		public readonly bool is_valid = false;
		public readonly string? error_msg;

		private LayerList(Layer layer)
		{
			list.Add(layer);
			is_valid = true;
		}

		private LayerList(TypeInfo info, JArray array, string rot_name)
		{
			foreach (JToken token in array)
			{
				if (token is not JObject obj2) continue;	// skips comments
				add_layer(info, obj2, rot_name);
			}

			is_valid = true;
		}

		private void add_layer(TypeInfo info, JObject data, string rot_name)
		{
			Layer layer = new(data, rot_name);
			if (layer.is_valid)
				list.Add(layer);
			else
			{
				ModEntry.log($"Invalid Layer in {info.mod_id} at {data.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{layer.error_msg}", LogLevel.Warn);
				ModEntry.log($"Skipping Layer.");
			}
		}

		#endregion

		#region LayerList Methods

		public void draw(DrawData draw_data, float top)
		{
			foreach (Layer layer in list)
				layer.draw(draw_data, top);
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			int index = 0;
			foreach (Layer layer in list)
			{
				ModEntry.log($"{indent}Layer {index}:", LogLevel.Debug);
				layer.debug_print(indent_count + 1);
				index ++;
			}
		}

		#endregion
	}
}