using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;

namespace FurnitureFramework.Data
{
	[RequiresPreviewFeatures]
	public class Layer : Field
	{
		[Required]
		[Directional]
		public Rectangle SourceRect;

		[Directional]
		public Point DrawPos = Point.Zero;

		public Depth Depth = new() {is_default = true};

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			is_valid = true;
		}

		public void Draw(FurnitureFramework.FType.DrawData draw_data, float top, bool ignore_depth = false)
		{
			draw_data.source_rect = SourceRect;
			draw_data.position += DrawPos.ToVector2() * 4f;
			draw_data.position.Y -= SourceRect.Height * 4;

			if (!ignore_depth) draw_data.depth = Depth.GetValue(top);

			draw_data.draw();
		}
	}

	[RequiresPreviewFeatures]
	public class LayerList : List<Layer>
	{
		public void DrawAll(FurnitureFramework.FType.DrawData draw_data, float top)
		{
			foreach (Layer layer in this)
				layer.Draw(draw_data, top);
		}
	}
}