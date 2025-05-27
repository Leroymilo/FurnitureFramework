using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.Data.FType.Properties
{
	public enum LightType { Source, Glow }

	[JsonConverter(typeof(FieldConverter<Light>))]
	public class Light : Field
	{
		[Required]
		public LightType LightType;

		[Required]
		[Directional]
		public Rectangle SourceRect;

		[Required]
		[Directional]
		public Point Position;

		public string? SourceImage;

		public bool? Toggle;
		public bool? TimeBased;
		public float Radius = 2f;

		[JsonConverter(typeof(ColorConverter))]
		public Color Color = Color.White;


		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			// Inverting Light-Source Color to match the value to the visual result
			if (LightType == LightType.Source)
				Color = new(256 - Color.R, 265 - Color.G, 256 - Color.B, Color.A);
			is_valid = true;
		}

		static float GetWindowGlowDepth(Vector2 position)
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

		public void Draw(DrawData draw_data, bool is_on, bool is_dark)
		{
			if (SourceImage != null) draw_data.texture_path = SourceImage;

			if (Toggle != null) draw_data.is_on = (bool)Toggle && is_on;
			if (TimeBased != null) draw_data.is_dark = (bool)TimeBased && is_dark;

			draw_data.position += Position.ToVector2() * 4f;
			draw_data.source_rect = SourceRect;
			draw_data.color = Color;
			draw_data.origin = SourceRect.Size.ToVector2() / 2f;

			switch (LightType)
			{
				case LightType.Source:
					draw_data.depth = 0.9f;
					int quality = Game1.options.lightingQuality;
					draw_data.position *= 2f / quality;
					draw_data.scale = 2f * Radius / quality;
					break;
				case LightType.Glow:
					Vector2 global_pos = draw_data.position;
					global_pos += new Vector2(Game1.viewport.X, Game1.viewport.Y);
					draw_data.depth = global_pos.Y + 64f * GetWindowGlowDepth(global_pos);
					draw_data.depth /= 10000f;
					break;
			}

			draw_data.rect_offset = Point.Zero;
			draw_data.Draw();
		}

	}

	public class LightList : List<Light>
	{
		[JsonIgnore]
		readonly List<Light> Sources = new();
		[JsonIgnore]
		readonly List<Light> Glows = new();

		public void Validate(StreamingContext context)
		{
			foreach (Light light in this)
			{
				if (light.LightType == LightType.Source)
					Sources.Add(light);
				else Glows.Add(light);
			}
		}

		public void DrawSources(DrawData draw_data, bool is_on, bool is_dark)
		{
			foreach (Light light in Sources)
			{
				light.Draw(draw_data, is_on, is_dark);
			}
		}

		public void DrawGlows(DrawData draw_data, bool is_on, bool is_dark)
		{
			foreach (Light light in Glows)
			{
				light.Draw(draw_data, is_on, is_dark);
			}
		}

	}
}