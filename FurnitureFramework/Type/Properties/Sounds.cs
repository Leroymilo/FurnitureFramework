using System.Runtime.Versioning;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;

namespace FurnitureFramework.FType.Properties
{
	[RequiresPreviewFeatures]
	class SoundList
	{
		private enum SoundMode {
			on_turn_on,
			on_turn_off,
			on_click
		}

		#region Sound Subclass

		private struct Sound
		{
			public readonly bool is_valid = false;
			public readonly string error_msg = "No error";

			public readonly SoundMode mode;
			public readonly string cue_name = "";

			public Sound(JObject sound_obj)
			{
				string mode_name = JsonParser.parse(sound_obj.GetValue("Mode"), "on_click");
				mode = Enum.Parse<SoundMode>(mode_name);
				if (!Enum.IsDefined(mode))
				{
					error_msg = "Invalid sound Mode.";
					return;
				}

				cue_name = JsonParser.parse(sound_obj.GetValue("Name"), "coin");
				if (!Game1.soundBank.Exists(cue_name))
				{
					error_msg = "Invalid sound Name.";
					return;
				}

				is_valid = true;
			}
		}

		#endregion

		List<Sound> list = new();

		#region SoundList Parsing

		public SoundList(JToken? sounds_token)
		{
			if (sounds_token is null || sounds_token.Type == JTokenType.Null) return;
			if (sounds_token is not JArray sounds_arr)
			{
				ModEntry.log(
					$"Sounds at {sounds_token.Path} is invalid, must be a list of SoundList.",
					LogLevel.Warn
				);
				return;
			}
			
			foreach (JToken sound_token in sounds_arr)
			{
				if (sound_token is JObject sound_obj)
				{
					Sound new_sound = new(sound_obj);
					if (!new_sound.is_valid)
					{
						ModEntry.log($"Invalid Sound at {sound_obj.Path}:", LogLevel.Warn);
						ModEntry.log($"\t{new_sound.error_msg}", LogLevel.Warn);
						ModEntry.log("Skipping sound.", LogLevel.Warn);
						continue;
					}
					list.Add(new_sound);
				}
			}
		}

		#endregion

		#region SoundList Methods

		public bool play(GameLocation location, bool? state = null)
		{
			bool played_sound = false;
			bool turn_on = state.HasValue && state.Value;
			bool turn_off = state.HasValue && !state.Value;
			foreach (Sound sound in list)
			{
				if (
					sound.mode == SoundMode.on_click ||
					(sound.mode == SoundMode.on_turn_on && turn_on) ||
					(sound.mode == SoundMode.on_turn_off && turn_off)
				)
				{
					location.playSound(sound.cue_name);
					played_sound = true;
					// ICue cue = Game1.soundBank.GetCue(sound.cue_name);
				}
			}
			return played_sound;
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			int index = 0;
			foreach (Sound sound in list)
			{
				ModEntry.log($"{indent}Sound {index}:", LogLevel.Debug);
				ModEntry.log($"{indent}\tMode: {sound.mode}", LogLevel.Debug);
				ModEntry.log($"{indent}\tCue: {sound.cue_name}", LogLevel.Debug);
				index ++;
			}
		}

		#endregion
	}
}