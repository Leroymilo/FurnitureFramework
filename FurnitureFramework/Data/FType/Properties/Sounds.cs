using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StardewValley;

namespace FurnitureFramework.Data.FType.Properties
{
	public enum SoundMode
	{
		on_turn_on,
		on_turn_off,
		on_click
	}

	[JsonConverter(typeof(FieldConverter<Sound>))]
	public class Sound : Field
	{
		public SoundMode Mode;
		public string Name = "";

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			if (Name == "" || !Game1.soundBank.Exists(Name))
			{
				ModEntry.Log($"Invalid Sound Name: \"{Name}\"", StardewModdingAPI.LogLevel.Error);
				throw new InvalidDataException($"Invalid Sound Name: \"{Name}\"");
			}
			else is_valid = true;
		}
	}

	[JsonConverter(typeof(FieldListConverter<Sound>))]
	public class SoundList : FieldList<Sound>
	{
		public bool Play(GameLocation location, bool? state = null)
		{
			bool played_sound = false;
			bool turn_on = state.HasValue && state.Value;
			bool turn_off = state.HasValue && !state.Value;

			foreach (Sound sound in this)
			{
				if (
					sound.Mode == SoundMode.on_click ||
					(sound.Mode == SoundMode.on_turn_on && turn_on) ||
					(sound.Mode == SoundMode.on_turn_off && turn_off)
				)
				{
					location.playSound(sound.Name);
					played_sound = true;
				}
			}
			return played_sound;
		}
	}
}