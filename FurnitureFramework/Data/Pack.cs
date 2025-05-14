

using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	public sealed class Pack
	{
		public int Format = 0;
		public Dictionary<string, JObject> Furniture = new();
		public Dictionary<string, IncludedPack> Included = new();
	}

	public class IncludedPack
	{
		public string Path;
		public string Description = "";
		public bool Enabled = true;
	}
}