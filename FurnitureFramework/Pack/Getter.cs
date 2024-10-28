

using FurnitureFramework.Type;

namespace FurnitureFramework.Pack
{
	partial class FurniturePack
	{
		private static string? get_type_source(string type_id)
		{
			if (!static_types.ContainsKey(type_id)) return null;
			return static_types[type_id].LastValue().First();
		}

		private FurnitureType get_type(string type_id)
		{
			
		}
	}
}