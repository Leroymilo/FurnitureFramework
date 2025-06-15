using System.Text.Json.Serialization;

namespace FurnitureFramework.Data.FPack
{
	public class IncludedPack
	{
		[JsonIgnore]
		public string Name;

		public string Path;
		public string Description = "";
		public bool Enabled = true;

		[JsonIgnore]
		public FPack Pack;
		[JsonIgnore]
		public string DataUID {get => Pack.DataUID;}
	}
}