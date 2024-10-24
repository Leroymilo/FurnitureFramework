

namespace FurnitureFramework.Pack
{
	partial class FurniturePack
	{
		public static Dictionary<string, string> get_default_conflict_config()
		{
			Dictionary<string, string> result = new();

			foreach (string type_id in conflicts.Keys)
				result.Add(type_id, static_types[type_id]);

			return result;
		}

		private static int get_current_priority(string type_id)
		{
			if (static_types.ContainsKey(type_id))
				// Getting the pack currently patching type_id, and asking it the priority of the type.
				return packs[static_types[type_id]].types[type_id].info.priority;
			return -1;
		}
	}
}