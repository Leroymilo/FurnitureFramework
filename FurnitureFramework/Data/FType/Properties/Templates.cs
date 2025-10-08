using System.Reflection;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley;

namespace FurnitureFramework.Data.FType.Properties
{
	using SDColor = System.Drawing.Color;

	#region Type Converters

	class Vector2Converter : ReadOnlyConverter<Vector2>
	{
		public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = JObject.Load(reader);
				JToken? X = obj.GetValue("X");
				JToken? Y = obj.GetValue("Y");
				if (X != null && Y != null)
				{
					return new(
						X.Value<float>(),
						Y.Value<float>()
					);
				}
			}

			ModEntry.Log($"Could not parse Vector2 from {reader.Value} at {reader.Path}.", StardewModdingAPI.LogLevel.Error);
			throw new InvalidDataException($"Could not parse Vector2 from {reader.Value} at {reader.Path}.");
		}
	}
	
	class ColorConverter : ReadOnlyConverter<Color>
	{
		public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.String)
			{
				string color_name = JToken.Load(reader).ToString();

				// From color code
				if (Utility.StringToColor(color_name) is Color color)
					return color;

				// From color name
				SDColor c_color = SDColor.FromName(color_name);
				return new(c_color.R, c_color.G, c_color.B);
			}
			return existingValue;
		}
	}

	#endregion

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

	public class Field
	{
		[JsonIgnore]
		public bool is_valid = false;

		public string? ID;  // Used by CP to patch elements in a List

		public static int Count = 0;
		public static string? CurrentDirKey = null;
		// The direction key that will be used to parse directional sub-fields
	}


	public class FieldConverter<T> : ReadOnlyConverter<T> where T : Field, new()
	{
		public override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			// Starts to read the data
			if (reader.TokenType == JsonToken.None) reader.Read();

			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = JObject.Load(reader);
				JObject new_obj = new();

				if (Field.CurrentDirKey == null)
				{
					// Non-directional Field (for FieldList)
					new_obj = Utils.RemoveSpaces(obj);
				}

				else
				{
					// Directional Field
					foreach (JProperty property in obj.Properties())
					{
						FieldInfo? field = objectType.GetField(property.Name);
						if (field == null) continue;

						DirectionalAttribute? tag = (DirectionalAttribute?)Attribute.GetCustomAttribute(field, typeof(DirectionalAttribute));
						if (tag != null && tag.Directional)
						{
							// Removing the layer of directionality if present
							if (property.Value is JObject prop_obj && Utils.IsSubfieldDirectional(prop_obj, field))
							{
								if (prop_obj.ContainsKey(Field.CurrentDirKey))
									new_obj.Add(property.Name, prop_obj.GetValue(Field.CurrentDirKey));
							}
							else new_obj.Add(property.Name, property.Value);
						}
						else new_obj.Add(property.Name, property.Value);
					}
				}

				T result = new();
				serializer.Populate(new_obj.CreateReader(), result);
				return result;
			}

			throw new InvalidDataException($"Could not parse {objectType} from {reader.Value} at {reader.Path}.");
		}
	}

	#endregion

	#region Field List

	/// <summary>
	/// Holds data for List of Fields
	/// </summary>

	public class FieldList<T> : List<T> where T : Field
	{
		public new void Add(T value)
		{
			value.ID ??= Count.ToString();  // Assigning default ID when omitted
			base.Add(value);
		}
	}

	public class FieldListConverter<ListT, T> : ReadOnlyConverter<ListT> where ListT : FieldList<T>, new() where T : Field, new()
	{
		public override ListT? ReadJson(JsonReader reader, Type objectType, ListT? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			ListT result = new();

			if (reader.TokenType == JsonToken.StartObject)
				// Parsing single T
				ReadField(Utils.RemoveSpaces(JObject.Load(reader)), ref result);

			else if (reader.TokenType == JsonToken.StartArray)
			{
				// Parsing array of T
				foreach (JToken token in JArray.Load(reader).Children())
				{
					if (token is not JObject obj) continue;
					ReadField(Utils.RemoveSpaces(obj), ref result);
				}
			}

			else
			{
				ModEntry.Log($"Could not parse List Field from {reader.Value} at {reader.Path}.", StardewModdingAPI.LogLevel.Error);
				throw new InvalidDataException($"Could not parse List Field from {reader.Value} at {reader.Path}.");
			}

			return result;
		}

		static void ReadField(JObject obj, ref ListT result)
		{
			T? value = obj.ToObject<T>();
			if (value == null || !value.is_valid) return;
			result.Add(value);
		}
	}

	#endregion

	#region Directional Field Dictionary

	/// <summary>
	/// Holds data of Directional Fields (1 value per direction)
	/// Only used for Collisions
	/// </summary>
	public class FieldDict<T> : Dictionary<string, T> where T : Field, new()
	{
		new public T this[string key]
		{
			get
			{
				T? result;
				if (!ContainsKey(key)) key = Utils.NOROT;
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


	class FieldDictConverter<T> : ReadOnlyConverter<FieldDict<T>> where T : Field, new()
	{
		/// <inheritdoc />
		public override FieldDict<T> ReadJson(JsonReader reader, Type objectType, FieldDict<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				existingValue ??= new();

				JObject obj = Utils.RemoveSpaces(JObject.Load(reader));

				List<string> sub_dirs = Utils.GetSubDirections(typeof(T), obj);

				if (sub_dirs.Count > 0)
				{
					// Parsing as a single T
					foreach (string key in sub_dirs)
						ReadField(obj, key, ref existingValue);
				}

				else
				{
					// Parsing as a directional T
					foreach (JProperty property in obj.Properties())
					{
						if (property.Value is JObject dir_obj)
							ReadField(Utils.RemoveSpaces(dir_obj), property.Name, ref existingValue);
					}
				}

				Field.CurrentDirKey = null;
				return existingValue;
			}

			ModEntry.Log($"Could not parse Directional Field from {reader.Value} at {reader.Path}.", StardewModdingAPI.LogLevel.Error);
			throw new InvalidDataException($"Could not parse Directional Field from {reader.Value} at {reader.Path}.");
		}

		static void ReadField(JObject obj, string dir_key, ref FieldDict<T> result)
		{
			Field.CurrentDirKey = dir_key;
			T? value = obj.ToObject<T>();
			if (value == null || !value.is_valid) return;
			result[dir_key] = value;
		}
	}

	#endregion

	#region Directional List Dictionary

	/// <summary>
	/// Holds data of Directional Lists (1 value per direction)
	/// </summary>

	public class FieldListDict<ListT, T> : Dictionary<string, ListT> where ListT : List<T>, new() where T : Field
	{
		new public ListT this[string key]
		{
			get
			{
				if (ContainsKey(key)) return base[key];
				return base[Utils.NOROT];
			}
			set { base[key] = value; }
		}

		public FieldListDict()
		{
			// Always have an empty List<T> to default to
			this[Utils.NOROT] = new();
		}

		public void Add(string key, T value)
		{
			if (!ContainsKey(key)) this[key] = new();
			value.ID ??= this[key].Count.ToString();    // Assigning default ID when omitted
			this[key].Add(value);
		}
	}


	class FieldListDictConverter<ListT, T> : ReadOnlyConverter<FieldListDict<ListT, T>> where ListT : List<T>, new() where T : Field, new()
	{
		public override FieldListDict<ListT, T>? ReadJson(JsonReader reader, Type objectType, FieldListDict<ListT, T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			FieldListDict<ListT, T> result = new();

			if (reader.TokenType == JsonToken.StartObject)
			{
				JObject obj = Utils.RemoveSpaces(JObject.Load(reader));

				// Ask T if the JObject can be parsed as a simple T
				List<string> sub_dirs = Utils.GetSubDirections(typeof(T), obj);

				if (sub_dirs.Count > 0)
				{
					// Parsing as a single T
					foreach (string key in sub_dirs)
						ReadField(obj, key, ref result);
				}

				else
				{
					foreach (JProperty property in obj.Properties())
					{
						if (property.Value is JObject dir_obj)
							// Parsing as a directional T
							ReadField(Utils.RemoveSpaces(dir_obj), property.Name, ref result);

						else if (property.Value is JArray dir_arr)
							// Parsing as a directional List<T>
							ReadArray(dir_arr, property.Name, ref result);
					}
				}
			}

			else if (reader.TokenType == JsonToken.StartArray)
				// Parsing as single List<T>
				ReadArray(JArray.Load(reader), Utils.NOROT, ref result);

			else
			{
				ModEntry.Log($"Could not parse Directional List from {reader.Value} at {reader.Path}.", StardewModdingAPI.LogLevel.Error);
				throw new InvalidDataException($"Could not parse Directional List from {reader.Value} at {reader.Path}.");
			}

			Field.CurrentDirKey = null;
			return result;
		}

		static void ReadArray(JArray array, string direction_key, ref FieldListDict<ListT, T> result)
		{
			foreach (JToken token in array.Children())
			{
				if (token is not JObject obj) continue;
				obj = Utils.RemoveSpaces(obj);

				List<string> sub_dirs;
				// Force single direction if one is given by a parent direction dictionary
				if (direction_key != Utils.NOROT) sub_dirs = new() { direction_key };
				// Getting direction keys from directional sub-fields
				else sub_dirs = Utils.GetSubDirections(typeof(T), obj);

				foreach (string dir_key in sub_dirs)
					ReadField(obj, dir_key, ref result);
			}
		}

		static void ReadField(JObject obj, string dir_key, ref FieldListDict<ListT, T> result)
		{
			Field.CurrentDirKey = dir_key;
			T? value = obj.ToObject<T>();
			if (value == null || !value.is_valid) return;
			result.Add(dir_key, value);
		}
	}

	#endregion

	#region Directional Struct

	public class DirStruct<T> : Dictionary<string, T> where T : struct
	{
		public DirStruct()
		{
			this[Utils.NOROT] = new();
		}

		new public T this[string key]
		{
			get
			{
				if (!ContainsKey(key) && ContainsKey(Utils.NOROT)) key = Utils.NOROT;
				if (ContainsKey(key)) return base[key];
				throw new KeyNotFoundException($"Missing value for direction {key} or {Utils.NOROT}");
			}
			set
			{
				base[key] = value;
			}
		}
	}

	class DirStructConverter<T> : ReadOnlyConverter<DirStruct<T>> where T : struct
	{
		public override DirStruct<T>? ReadJson(JsonReader reader, Type objectType, DirStruct<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				DirStruct<T> result = new();
				JObject obj = JObject.Load(reader);
				if (obj.First is JObject)
				{
					// Parsing directional
					foreach (JProperty property in obj.Properties())
					{
						if (property.Value is JObject prop_obj)
							result[property.Name] = prop_obj.ToObject<T>();
					}
				}
				else
				{
					// Parsing non-directional
					result[Utils.NOROT] = obj.ToObject<T>();
				}

				return result;
			}

			ModEntry.Log($"Could not parse Directional Point or Rectangle from {reader.Value} at {reader.Path}.", StardewModdingAPI.LogLevel.Error);
			throw new InvalidDataException($"Could not parse Directional Point or Rectangle from {reader.Value} at {reader.Path}.");
		}
	}

	#endregion
}