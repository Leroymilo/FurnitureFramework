

namespace FurnitureFramework.Data.FPack
{
	public partial class FPack
	{
		public int Format = 0;
		public Dictionary<string, FType.FType> Furniture = new();
		public Dictionary<string, IncludedPack> Included = new();
	}
}