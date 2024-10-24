

namespace FurnitureFramework.Pack
{
	partial class FurniturePack
	{
		public static void update_conflict_config()
		{
			Dictionary<string, string> config_choices = ModEntry.get_config().conflicts_choices;
			HashSet<string> added_choices = new();

			foreach (string type_id in conflicts.Keys)
			{
				added_choices.Add(type_id);
				if (config_choices.ContainsKey(type_id))
				{
					// apply config choices
					static_types[type_id] = config_choices[type_id];
				}
				else
				{
					// register default config choice
					config_choices[type_id] = static_types[type_id];
				}
			}

			foreach (string type_id in config_choices.Keys)
			{
				if (!added_choices.Contains(type_id))
					config_choices.Remove(type_id);
			}
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