using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley;

namespace FurnitureFramework.Data.FType.Properties
{
	[JsonConverter(typeof(SpaceRemover<Animation>))]
	public class Animation
	{
		[JsonIgnore]
		public bool Animates { get; private set; } = false;

		public int FrameCount = 0;

		[JsonConverter(typeof(ValueListConverter<int>))]
		public List<int> FrameDuration = new();

		[JsonConverter(typeof(ValueListConverter<Point>))]
		public List<Point> Offset = new();

		[JsonIgnore]
		private readonly List<int> EndTimes = new();    // ::)

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			if (FrameCount == 0 || FrameDuration.Count == 0 || Offset.Count == 0) return;

			// Builds a list of frame durations from a single value
			if (FrameDuration.Count == 1)
				FrameDuration = Enumerable.Repeat(FrameDuration.First(), FrameCount).ToList();

			// Builds a list of offsets from a single value
			if (Offset.Count == 1)
			{
				// If Offset == (0, 0), then all frames are on the same rectangle, negating the animation.
				if (Offset.First() == Point.Zero)
				{
					ModEntry.Log("Offset is null");
					return;
				}
				Point delta = Offset.First();
				Offset.Clear();

				for (int i = 0; i < FrameCount; i++)
					Offset.Add(delta * new Point(i));
			}

			if (FrameDuration.Count != FrameCount || Offset.Count != FrameCount)
				throw new InvalidDataException("Length of Frame Duration array or Offset array does not match Frame Count");

			// Builds the EndTimes list from FrameDuration
			int frame_end = 0;
			foreach (int duration in FrameDuration)
			{
				frame_end += duration;
				EndTimes.Add(frame_end);
			}

			Animates = true;
		}

		public Point GetOffsetLoop()
		{
			if (!Animates) return Point.Zero;

			long time_ms = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
			int loop_time = (int)(time_ms % EndTimes.Last());
			int frame = EndTimes.BinarySearch(loop_time);
			if (frame < 0) frame = ~frame;
			return Offset[frame];
		}

		public Point GetOffsetOnce(long start_time)
		{
			if (!Animates) return Point.Zero;

			long time_ms = (long)Game1.currentGameTime.TotalGameTime.TotalMilliseconds - start_time;
			if (time_ms >= EndTimes.Last()) return Offset.Last();
			int frame = EndTimes.BinarySearch((int)time_ms);
			if (frame < 0) frame = ~frame;
			return Offset[frame];
		}

		public Animation Reverse()
		{
			Animation reversed = new()
			{
				FrameCount = FrameCount,
				FrameDuration = FrameDuration.Reverse<int>().ToList(),
				Offset = Offset.Reverse<Point>().ToList()
			};

			// Builds the EndTimes list from FrameDuration
			int frame_end = 0;
			foreach (int duration in reversed.FrameDuration)
			{
				frame_end += duration;
				reversed.EndTimes.Add(frame_end);
			}

			reversed.Animates = true;

			return reversed;
		}
	}

	/// <summary>
	/// Parses either an array of values or a single value used to generate an array
	/// </summary>
	public class ValueListConverter<T> : ReadOnlyConverter<List<T>>
	{
		public override List<T>? ReadJson(JsonReader reader, Type objectType, List<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (typeof(T) == typeof(int) && reader.TokenType == JsonToken.Integer)
			{
				T? value = JToken.Load(reader).Value<T>() ??
					throw new InvalidDataException($"Could not parse Frame Duration from {reader.Value} at {reader.Path}.");
				return new() { value };
			}
			else if (typeof(T) == typeof(Point) && reader.TokenType == JsonToken.StartObject)
			{
				T? value = JObject.Load(reader).ToObject<T>() ??
					throw new InvalidDataException($"Could not parse Offset from {reader.Value} at {reader.Path}.");
				return new() { value };
			}
			else if (reader.TokenType == JsonToken.StartArray)
				return JArray.Load(reader).ToObject<List<T>>();
			
			throw new InvalidDataException($"Could not parse Frame Duration or Offset from {reader.Value} at {reader.Path}.");
		}
	}
}