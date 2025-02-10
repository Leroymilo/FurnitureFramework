using StardewModdingAPI;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System.Runtime.Versioning;

namespace FurnitureFramework.Type.Properties
{
	#pragma warning disable 0649

	[RequiresPreviewFeatures]
	class LayerList: IProperty<LayerList>
	{

		#region Layer Subclass

		private class Layer
		{
			bool is_base;

			public readonly bool is_valid = false;
			public readonly string? error_msg;

			public readonly Rectangle source_rect;

			public readonly Vector2 draw_pos = Vector2.Zero;
			Depth depth = new(0, 1000);

			#region Layer Parsing

			public Layer(JObject data, string rot_name, bool is_base)
			{
				this.is_base = is_base;

				// Parsing directional Source Rectangle
				error_msg = "Missing or Invalid Source Rectangle";
				if (!JsonParser.try_parse_dir(data.GetValue("Source Rect"), rot_name, ref source_rect))
					return;

				is_valid = true;

				// Parsing directional layer draw position

				JsonParser.try_parse_dir(data.GetValue("Draw Pos"), rot_name, ref draw_pos);
				draw_pos *= 4f;	// game rendering scale

				// Parsing layer depth

				if (is_base) depth = new(0, 0);
				try { depth = new(data.GetValue("Depth")); }
				catch (InvalidDataException) { }
			}

			#endregion

			#region Layer Methods

			public void draw(DrawData draw_data, float top, bool ignore_depth = false)
			{
				draw_data.source_rect = source_rect;
				draw_data.position += draw_pos;
				draw_data.position.Y -= source_rect.Height * 4;

				if (!ignore_depth) draw_data.depth = depth.get_value(top);

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

			if (data == null || data.Type == JTokenType.None)
				return make_default(info, rot_name);
			
			if (data is JObject obj)
			{
				// single layer?
				Layer layer = new(obj, rot_name, true);
				if (layer.is_valid)
					return new(layer);

				// directional?
				JToken? dir_token = obj.GetValue(rot_name);
				if (dir_token is JObject dir_obj)
				{
					// directional single layer?
					Layer dir_layer = new(dir_obj, rot_name, true);
					if (dir_layer.is_valid)
						return new(dir_layer);
					
					// single directional layer was invalid
					ModEntry.log($"Could not parse a directional layer in {info.mod_id} at {dir_obj.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{dir_layer.error_msg}", LogLevel.Warn);
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
			error_msg = "Invalid Layer List definition, fallback to no Layers.";
			return make_default(info, rot_name);
		}

		List<Layer> list = new();
		public bool has_layer { get => list.Count > 0; }

		private LayerList(Layer layer)
		{
			list.Add(layer);
		}

		private LayerList(TypeInfo info, JArray array, string rot_name)
		{
			int i = 0;
			foreach (JToken token in array)
			{
				if (token is not JObject obj2) continue;	// skips comments
				add_layer(info, obj2, rot_name, i);
				i++;
			}
		}

		private void add_layer(TypeInfo info, JObject data, string rot_name, int index)
		{
			Layer layer = new(data, rot_name, index == 0);
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

		public void draw_one(DrawData draw_data, float top, bool ignore_depth = false)
		{
			list[0].draw(draw_data, top, ignore_depth);
		}

		public void draw_all(DrawData draw_data, float top)
		{
			foreach (Layer layer in list)
				layer.draw(draw_data, top);
		}

		public Rectangle get_source_rect()
		{
			return list[0].source_rect;
		}

		public Vector2 get_draw_offset()
		{
			return list[0].draw_pos;
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