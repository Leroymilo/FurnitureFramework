using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace FurnitureFramework.Data.FType.Properties
{
	[JsonConverter(typeof(FieldConverter<Layer>))]
	public class Layer : Field
	{
		[Required]
		[Directional]
		public Rectangle SourceRect;

		[Directional]
		public Point DrawPos = Point.Zero;

		public Depth Depth = new() { is_default = true };

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			is_valid = true;
		}

		public void Draw(DrawData draw_data, float top, bool ignore_depth = false)
		{
			draw_data.source_rect = SourceRect;
			draw_data.position += DrawPos.ToVector2() * 4f;
			draw_data.position.Y -= SourceRect.Height * 4;

			if (!ignore_depth) draw_data.depth = Depth.GetValue(top);

			draw_data.Draw();
		}
	}

	public class LayerList : List<Layer>
	{
		public void DrawAll(DrawData draw_data, float top)
		{
			foreach (Layer layer in this)
				layer.Draw(draw_data, top);
		}
	}
}