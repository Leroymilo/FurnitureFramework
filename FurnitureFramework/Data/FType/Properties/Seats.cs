using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace FurnitureFramework.Data.FType.Properties
{
	public enum SeatDir { Up, Right, Down, Left }

	[JsonConverter(typeof(FieldConverter<Seat>))]
	public class Seat : Field
	{
		[Required]
		[Directional]
		[JsonConverter(typeof(Vector2Converter))]
		public Vector2 Position;

		[Required]
		[Directional]
		public SeatDir PlayerDirection;

		public Depth? Depth;
		
		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			is_valid = true;
		}

		public float GetPlayerDepth(float top)
		{
			if (Depth is null) return -1;   // keep default depth
			else return Depth.GetValue(top);
		}
	}


	public class SeatList : List<Seat>
	{
		public void GetSeatPositions(Vector2 tile_pos, List<Vector2> result)
		{
			foreach (Seat seat in this)
			{
				result.Add(tile_pos + seat.Position);
			}
		}

		public int GetSittingDirection(int seat_index)
		{
			if (Count == 0) return -1;

			return (int)this[seat_index].PlayerDirection;
		}

		public float GetSittingDepth(int seat_index, float top)
		{
			if (Count == 0) return -1;
			
			return this[seat_index].GetPlayerDepth(top);
		}
	}
}