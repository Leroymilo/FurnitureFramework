using System.Reflection;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FurnitureFramework.Data
{
	// Used in place of JsonConverter because there's no need to Write anything that is parsed in this namespace
	public abstract class ReadOnlyConverter<T> : JsonConverter<T>
	{
		public override bool CanWrite => false;

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
		{
			throw new NotImplementedException("Unnecessary because CanWrite is false. The type will skip the converter.");
		}
	}

	class Utils
	{
		public const string NOROT = "NoRot";
		public static readonly Point TILESIZE = new(64);

		public static JObject RemoveSpaces(JObject obj)
		{
			JObject new_obj = new();
			foreach (JProperty property in obj.Properties())
				new_obj.Add(property.Name.Replace(" ", null), property.Value);
			return new_obj;
		}

		public static List<string> GetSubDirections(Type type, JObject obj)
		// type is passed for the method to work on inherited classes
		{
			HashSet<string> result = new();

			// Check if all [Required] fields are in the obj
			foreach (string field_name in FType.Properties.TagAttribute.GetRequired(type))
			{
				if (!obj.ContainsKey(field_name)) return new() { };
				// If not return empty list to indicate that this might be a directional dict of DirListField
			}
			// Check if any of the [Directional] fields are present
			foreach (string field_name in FType.Properties.TagAttribute.GetDirectional(type))
			{
				JToken? field_token = obj.GetValue(field_name);
				if (field_token is not JObject field_obj) continue;
				// Must be a JObject (and not null) to be directional

				FieldInfo? field = type.GetField(field_name);
				if (field == null) continue;

				// Only 3 cases (for now): enum, Point or Rectangle
				if (field.FieldType.IsEnum || field_obj.PropertyValues().First() is JObject)
				// If the value is a JObject but represents an enum, then it's directional
				// If any value in the JObject is also a JObjext, then it's directional (Point or Rectangle)
				{
					foreach (JProperty prop in field_obj.Properties())
						result.Add(prop.Name);
				}
			}

			// Differentiates between invalid and no Directional Sub-Field
			if (result.Count == 0) result.Add(NOROT);
			return result.ToList();
		}
	}
}