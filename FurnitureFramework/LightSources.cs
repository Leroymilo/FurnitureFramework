

using Microsoft.Xna.Framework;

namespace FurnitureFramework
{
	class LightSource
	{
		public Point position;
		public string texture = "FF.LightGlow.png";
		public Rectangle source_rect;
		public Color color = Color.White;
		public bool is_window = false;
	}
}