using StardewModdingAPI;

namespace FurnitureFramework
{
	public sealed class ModConfig
	{
		public SButton slot_place_key {get; set;} = SButton.MouseRight;
		public SButton slot_take_key {get; set;} = SButton.MouseRight;

		public bool disable_AT_warning {get; set;} = false;
	}
}