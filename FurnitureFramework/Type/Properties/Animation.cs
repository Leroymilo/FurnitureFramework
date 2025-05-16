using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;

namespace FurnitureFramework.FType.Properties
{

	[RequiresPreviewFeatures]
	class Animation
	{
		public bool animates {get; private set;} = false;

		int frame_count = 0;
		List<int> end_times;	// ::)
		List<Point> offsets;

		public void parse(JToken? anim_token)
		{
			if (anim_token is not JObject data)
				return;

			frame_count = JsonParser.parse(data.GetValue("Frame Count"), 0);
			animates = frame_count > 0;
			if (!animates) return;
			
			end_times = new(frame_count);
			int last_end = 0;
			JToken? length_token = data.GetValue("Frame Duration");
			if (length_token is JArray length_array)
			{
				foreach (JToken token in length_array)
				{
					int length = 0;
					if (!JsonParser.try_parse(token, ref length))
						continue;
					last_end += length;
					end_times.Add(last_end);
				}

				if (end_times.Count != frame_count)
				{
					string msg = "Count of Frame Durations does not match Frame Count";
					msg += ", skipping animation";
					ModEntry.log(msg, LogLevel.Warn);
					animates = false;
					return;
				}
			}
			else
			{
				int length = 0;
				JsonParser.try_parse(length_token, ref length);

				if (length == 0)
				{
					animates = false;
					return;
				}

				end_times.Add(length);
				for (int i = 1; i < frame_count; i++)
					end_times.Add(end_times[i-1] + length);
			}

			animates &= end_times.Last() > 0;
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
					ModEntry.log(msg, LogLevel.Warn);
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
			int loop_time = (int)(time_ms % end_times.Last());
			int frame = end_times.BinarySearch(loop_time);
			if (frame < 0) frame = ~frame;
			return offsets[frame];
		}

		public void debug_print(int indent_count)
		{
			if (!animates) return;

			string indent = new('\t', indent_count);
			ModEntry.log($"{indent}Animation Data:", LogLevel.Debug);

			indent += '\t';
			ModEntry.log($"{indent}frame count: {frame_count}", LogLevel.Debug);
			ModEntry.log($"{indent}frame ends:", LogLevel.Debug);
			foreach (int end in end_times)
				ModEntry.log($"{indent}\t{end}ms", LogLevel.Debug);
			ModEntry.log($"{indent}offsets: ", LogLevel.Debug);
			foreach (Point offset in offsets)
				ModEntry.log($"{indent}\t{offset}", LogLevel.Debug);
		}
	}

}