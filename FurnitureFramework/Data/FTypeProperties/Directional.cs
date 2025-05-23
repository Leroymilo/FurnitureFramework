using System.Reflection;
using System.Runtime.Versioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	#region Attributes

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class TagAttribute : Attribute
	{
		public bool Required;
		public bool Directional;

		public TagAttribute(bool required, bool directional)
		{
			Required = required;
			Directional = directional;
		}

		public static List<string> GetRequired(Type type)
		{
			List<string> result = new();

			foreach (FieldInfo field in type.GetFields())
			{
				RequiredAttribute? tag = (RequiredAttribute?)GetCustomAttribute(field, typeof(RequiredAttribute));
				if (tag != null && tag.Required) result.Add(field.Name);
			}

			return result;
		}

		public static List<string> GetDirectional(Type type)
		{
			List<string> result = new();

			foreach (FieldInfo field in type.GetFields())
			{
				DirectionalAttribute? tag = (DirectionalAttribute?)GetCustomAttribute(field, typeof(DirectionalAttribute));
				if (tag != null && tag.Directional) result.Add(field.Name);
			}

			return result;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class RequiredAttribute : TagAttribute
	{
		public RequiredAttribute() : base(true, false) { }
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class DirectionalAttribute : TagAttribute
	{
		public DirectionalAttribute() : base(false, true) { }
	}

	#endregion

	#region Directional Field

	/// <summary>
	/// The base class for values inside a DirFieldDict or DirListDict
	/// </summary>
	public class DirField
	{
		public const string NOROT = "NoRot";

		[JsonIgnore]
		public bool is_valid = false;

		public static string? CurrentDirKey = null;
		// The direction key that will be used to parse directional sub-fields

		public static List<string> GetSubDirections(Type type, JObject obj)
		// type is passed for the method to work on inherited classes
		{
			List<string> result = new();

			// Check if all [Required] fields are in the obj
			foreach (string field_name in TagAttribute.GetRequired(type))
			{
				if (!obj.ContainsKey(field_name)) return new() { };
				// If not return empty list to indicate that this might be a directional dict of DirListField
			}

			// Check if any of the [Directional] fields are present
			foreach (string field_name in TagAttribute.GetDirectional(type))
			{
				JToken? field_token = obj.GetValue(field_name);
				if (field_token is not JObject field_obj) continue;
				// Must be a JObject (and not null) to be directional

				FieldInfo? field = type.GetField(field_name);
				if (field == null) continue;

				// Only 3 cases (for now): enum, Point or Rectangle

				if (field.FieldType.IsEnum || field_obj.First is JObject)
				// If the value is a JObject but represents an enum, then it's directional
				// If any value in the JObject is also a JObjext, then it's directional
				{
					foreach (JProperty prop in field_obj.Properties())
						result.Add(prop.Name);
				}
			}

			// Differentiates between invalid and no Directional Sub-Field
			if (result.Count == 0) result.Add(NOROT);
			return result;
		}
	}

	[RequiresPreviewFeatures]
	public class DirFieldConverter<T> : ReadOnlyConverter<T> where T : DirField, new()
	{
		public override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (DirField.CurrentDirKey == null)
				throw new InvalidDataException("Trying to parse Directional Field with no directional key specified.");

			// Starts to read the data
			if (reader.TokenType == JsonToken.None) reader.Read();

			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = JObject.Load(reader);
				JObject new_obj = new();

				foreach (JProperty property in obj.Properties())
				{
					FieldInfo? field = objectType.GetField(property.Name);
					if (field == null) continue;

					DirectionalAttribute? tag = (DirectionalAttribute?)Attribute.GetCustomAttribute(field, typeof(DirectionalAttribute));
					if (tag != null && tag.Directional)
					{
						// Removing the layer of directionality if present
						if (property.Value is JObject prop_obj && (field.FieldType.IsEnum || prop_obj.First is JObject))
						{
							if (prop_obj.ContainsKey(DirField.CurrentDirKey))
								new_obj.Add(property.Name, prop_obj.GetValue(DirField.CurrentDirKey));
						}
						else new_obj.Add(property.Name, property.Value);
					}
					else new_obj.Add(property.Name, property.Value);
				}

				T result = new();
				serializer.Populate(new_obj.CreateReader(), result);
				return result;
			}

			throw new InvalidDataException($"Could not parse {objectType} from {reader.Value} at {reader.Path}.");
		}
	}

	#endregion

	#region Directional Field Dictionary

	/// <summary>
	/// Holds data of Directional Fields (1 value per direction)
	/// Only used for Collisions
	/// </summary>
	public class DirFieldDict<T> : Dictionary<string, T> where T : DirField, new()
	{
		// Only required because BedArea is not directional
		public T First
		{
			get
			{
				foreach (T value in Values)
					if (value.is_valid) return value;
				if (ContainsKey(DirField.NOROT) && this[DirField.NOROT].is_valid) return this[DirField.NOROT];
				throw new InvalidOperationException("Unique value invalid");
			}
		}

		new public T this[string key]
		{
			get
			{
				T? result;
				if (!ContainsKey(key)) key = DirField.NOROT;
				if (ContainsKey(key))
				{
					result = base[key];
					if (result.is_valid) return result;
				}
				throw new KeyNotFoundException("Key not found and Unique value invalid");
			}
			set
			{
				base[key] = value;
			}
		}
	}

	[RequiresPreviewFeatures]
	class DirFieldDictConverter<T> : ReadOnlyConverter<DirFieldDict<T>> where T : DirField, new()
	{
		/// <inheritdoc />
		public override DirFieldDict<T> ReadJson(JsonReader reader, Type objectType, DirFieldDict<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				DirFieldDict<T> result = new();

				JObject obj = JObject.Load(reader);

				List<string> sub_dirs = DirField.GetSubDirections(typeof(T), obj);

				if (sub_dirs.Count > 0)
				{
					// Parsing as a single T
					foreach (string key in sub_dirs)
						ReadField(obj, key, ref result, serializer);
				}

				else
				{
					// Parsing as a directional T
					foreach (JProperty property in obj.Properties())
					{
						if (property.Value is JObject dir_obj)
							ReadField(dir_obj, property.Name, ref result, serializer);
					}
				}

				DirField.CurrentDirKey = null;

				return result;
			}

			throw new InvalidDataException($"Could not parse Directional Field from {reader.Value} at {reader.Path}.");
		}

		static void ReadField(JObject obj, string dir_key, ref DirFieldDict<T> result, JsonSerializer serializer)
		{
			DirField.CurrentDirKey = dir_key;
			DirFieldConverter<T> converter = new();
			T? value = converter.ReadJson(obj.CreateReader(), typeof(T), null, false, serializer);
			if (value == null || !value.is_valid) return;
			result[dir_key] = value;
		}
	}

	#endregion

	#region Directional List Dictionary

	/// <summary>
	/// Holds data of Directional Lists (1 value per direction)
	/// </summary>
	public class DirListDict<ListT, T> : Dictionary<string, ListT> where ListT : List<T>, new() where T : DirField
	{
		new public ListT this[string key]
		{
			get
			{
				if (ContainsKey(key)) return base[key];
				return base[DirField.NOROT];
			}
			set { base[key] = value; }
		}

		public DirListDict()
		{
			// Always have an empty List<T> to default to
			this[DirField.NOROT] = new();
		}

		public void Add(string key, T value)
		{
			if (!ContainsKey(key)) this[key] = new();
			this[key].Add(value);
		}
	}

	[RequiresPreviewFeatures]
	class DirListDictConverter<ListT, T> : ReadOnlyConverter<DirListDict<ListT, T>> where ListT : List<T>, new() where T : DirField, new()
	{
		public override DirListDict<ListT, T>? ReadJson(JsonReader reader, Type objectType, DirListDict<ListT, T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			DirListDict<ListT, T> result = new();

			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = JObject.Load(reader);

				// Ask T if the JObject can be parsed as a simple T
				List<string> sub_dirs = DirField.GetSubDirections(typeof(T), obj);

				if (sub_dirs.Count > 0)
				{
					// Parsing as a single T
					foreach (string key in sub_dirs)
						ReadField(obj, key, ref result, serializer);
				}

				else
				{
					foreach (JProperty property in obj.Properties())
					{
						if (property.Value is JObject dir_obj)
							// Parsing as a directional T
							ReadField(dir_obj, property.Name, ref result, serializer);

						else if (property.Value is JArray dir_arr)
							// Parsing as a directional List<T>
							ReadArray(dir_arr, property.Name, ref result, serializer);
					}
				}

				DirField.CurrentDirKey = null;

				return result;
			}
			else if (reader.TokenType == JsonToken.StartArray)
				// Parsing as single List<T>
				ReadArray(JArray.Load(reader), DirField.NOROT, ref result, serializer);

			throw new InvalidDataException($"Could not parse Directional List from {reader.Value} at {reader.Path}.");
		}

		static void ReadArray(JArray array, string direction_key, ref DirListDict<ListT, T> result, JsonSerializer serializer)
		{
			foreach (JToken token in array.Children())
			{
				if (token is not JObject obj) continue;

				List<string> sub_dirs;
				// Force single direction if one is given by a parent direction dictionary
				if (direction_key != DirField.NOROT) sub_dirs = new() { direction_key };
				// Getting direction keys from directional sub-fields
				else sub_dirs = DirField.GetSubDirections(typeof(T), obj);

				foreach (string dir_key in sub_dirs)
					ReadField(obj, dir_key, ref result, serializer);
			}
		}

		static void ReadField(JObject obj, string dir_key, ref DirListDict<ListT, T> result, JsonSerializer serializer)
		{
			DirField.CurrentDirKey = dir_key;
			DirFieldConverter<T> converter = new();
			T? value = converter.ReadJson(obj.CreateReader(), typeof(T), null, false, serializer);
			if (value == null || !value.is_valid) return;
			result.Add(dir_key, value);
		}
	}

	#endregion
}