using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;

namespace FurnitureFramework.Type.Properties
{

	[RequiresPreviewFeatures]
	class Animation
	{
		public bool animates {get; private set;} = false;

		int frame_count = 0;
		int frame_length = 0;
		List<Point> offsets;

		public void parse(JToken? anim_token)
		{
			if (anim_token is not JObject data)
				return;

			frame_count = JsonParser.parse(data.GetValue("Frame Count"), 0);
			frame_length = JsonParser.parse(data.GetValue("Frame Duration"), 0);

			animates = frame_count > 0 && frame_length > 0;
			if (!animates) return;

			offsets = new(frame_count);
			JToken? offset_token = data.GetValue("Offset");
			if (offset_token is JArray offset_array)
			{
				foreach (JToken token in offset_array)
				{
					Point offset = Point.Zero;
					if (!JsonParser.try_parse(token, ref offset))
						continue;
					offsets.Add(offset);
				}

				if (offsets.Count != frame_count)
				{
					string msg = "Count of Animation Offset does not match Frame Count";
					msg += ", skipping animation";
					ModEntry.log(msg, StardewModdingAPI.LogLevel.Warn);
					animates = false;
					return;
				}
			}
			else
			{
				Point offset = Point.Zero;
				JsonParser.try_parse(offset_token, ref offset);

				if (offset.ToVector2().Length() == 0)
				{
					animates = false;
					return;
				}

				for (int i = 0; i < frame_count; i++)
					offsets.Add(offset * new Point(i));
			}
		}

		public Point get_offset()
		{
			if (!animates) return Point.Zero;

			long time_ms = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			int frame = (int)(time_ms / frame_length) % frame_count;
			return offsets[frame];
		}

		public void debug_print(int indent_count)
		{
			if (!animates) return;

			string indent = new('\t', indent_count);
			ModEntry.log($"{indent}Animation Data:", LogLevel.Debug);

			indent += '\t';
			ModEntry.log($"{indent}frame count: {frame_count}", LogLevel.Debug);
			ModEntry.log($"{indent}frame length: {frame_length}", LogLevel.Debug);
			ModEntry.log($"{indent}offsets: ");
			
			indent += '\t';
			foreach (Point offset in offsets)
				ModEntry.log($"{indent}{offset}", LogLevel.Debug);
		}
	}

}