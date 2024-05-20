using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace FurnitureFramework
{
	public sealed class ModConfig
	{
		public SButton slot_place_key {get; set;} = SButton.MouseRight;
		public SButton slot_take_key {get; set;} = SButton.MouseRight;

		public bool disable_AT_warning {get; set;} = false;

		public bool enable_slot_debug {get; set;} = false;
		public float slot_debug_alpha {get; set;} = 0.5f;

		public Color slot_debug_default_color {get; set;} = Color.DeepPink;
	}
}