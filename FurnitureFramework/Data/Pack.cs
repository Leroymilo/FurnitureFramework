using System.Runtime.Versioning;
using Newtonsoft.Json;

namespace FurnitureFramework.Data
{
	[RequiresPreviewFeatures]
	public sealed class Pack
	{
		public int Format = 0;
		public Dictionary<string, FType> Furniture = new();
		public Dictionary<string, IncludedPack> Included = new();
	}

	public class IncludedPack
	{
		public string Path;
		public string Description = "";
		public bool Enabled = true;
	}

	// Used in place of JsonConverter because there's no need to Write anything that is parsed in this namespace
	public abstract class JsonReadOnlyConv<T> : JsonConverter<T>
	{
		public override bool CanWrite => false;

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
		{
			throw new NotImplementedException("Unnecessary because CanWrite is false. The type will skip the converter.");
		}
	}
}