using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using System.Runtime.Versioning;

namespace FurnitureFramework.Type.Properties
{
	[RequiresPreviewFeatures]
	interface IProperty<T>
	{
		public abstract static T make(TypeInfo info, JToken? data, string rot_name, out string? error_msg);
		public abstract void debug_print(int indent_count);
	}


	[RequiresPreviewFeatures]
	class DirectionalStructure<T> where T: notnull, IProperty<T>
	{
		List<string> rot_names;
		JToken? data;
		TypeInfo info;
		List<T?> values = new();

		public DirectionalStructure(TypeInfo info, JToken? data, List<string> rot_names)
		{
			this.info = info;
			this.data = data;
			this.rot_names = rot_names;
			values.Capacity = rot_names.Count;
		}

		public T this[int i]
		{
			get {
				T? value = values[i];
				if (value is null)
				{
					value = T.make(info, data, rot_names[i], out string? error_msg);
					if (error_msg != null)
					{
						ModEntry.log($"Error in {info.id} of {info.mod_id}", LogLevel.Error);
						ModEntry.log($"\t{error_msg}", LogLevel.Error);
					}
					values[i] = value;
				}
				return value;
			}
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			ModEntry.log($"{indent}{typeof(T).Name}:", LogLevel.Debug);
			indent += '\t';
			if (rot_names.Count == 1)
			{
				this[0].debug_print(indent_count+1);
				return;
			}
			for (int i = 0; i < rot_names.Count; i++)
			{
				ModEntry.log($"{indent}{rot_names[i]}:", LogLevel.Debug);
				this[i].debug_print(indent_count+2);
			}
		}
	}
}