using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.Data.FType.Properties
{
	[JsonConverter(typeof(FieldConverter<Particles>))]
	public class Particles : Field
	{
		[Required]
		string SourceImage = "";
		Rectangle SourceRect = Rectangle.Empty;
		int EmissionInterval = 500;

		[Required]
		[Directional]
		Rectangle SpawnRect;
		List<float> Depths = new();
		Vector2 Speed = Vector2.Zero;

		List<float> Rotations = new();
		List<float> RotationSpeeds = new();

		float Scale = 1f;
		float ScaleChange = 0f;

		[JsonConverter(typeof(ColorConverter))]
		Color Color = Color.White;
		float Alpha = 1f;
		float AlphaFade = 0f;

		int FrameCount = 0;
		int FrameDuration = 0;
		int LoopCount = 0;
		bool HoldLastFrame = true;
		bool Flicker = false;

		bool EmitWhenOn = false;
		bool EmitWhenOff = false;
		bool Burst = true;

		[OnDeserialized]
		private void Validate(StreamingContext context)
		{
			if (Depths.Count == 0) Depths.Add(0);
			if (Rotations.Count == 0) Rotations.Add(0);
			if (RotationSpeeds.Count == 0) RotationSpeeds.Add(0);
			is_valid = true;
		}

		public void UpdateTimer(Furniture furniture, List<long> timers, int index, long time_ms, string mod_id)
		{
			if (
				(!EmitWhenOn || !furniture.IsOn) &&
				(!EmitWhenOff || furniture.IsOn)
			) return;

			if (time_ms - timers[index] > EmissionInterval)
			{
				Make(furniture, mod_id);
				timers[index] = time_ms;
			}
		}

		public void BurstEmit(Furniture furniture, string mod_id)
		{
			if (!Burst) return;
			if (
				(!EmitWhenOn || !furniture.IsOn) &&
				(!EmitWhenOff || furniture.IsOn)
			) return;

			// placement/toggle burst
			for (float m = 1; m <= 6; m += 0.5f)
			{
				Make(furniture, mod_id, Speed * m);
			}
		}

		public void Make(Furniture furniture, string mod_id, Vector2? speed_ = null)
		{
			ModEntry.log("Making particle");

			Texture2D texture = ModEntry.get_helper().ModContent.Load<Texture2D>($"FF/{mod_id}/{SourceImage}");

			if (SourceRect == Rectangle.Empty)
				SourceRect = texture.Bounds;

			Vector2 speed;
			if (speed_ is null) speed = Speed;
			else speed = speed_.Value;

			// for burst
			float new_alpha_fade;
			if (speed.Length() * Speed.Length() > 0)
			{
				new_alpha_fade = AlphaFade * speed.Length() / Speed.Length();
				new_alpha_fade -= speed.Length() > Speed.Length() ? 0.002f : 0f;
			}
			else new_alpha_fade = AlphaFade;

			float depth = Depths[Game1.random.Next(Depths.Count)];
			depth = furniture.boundingBox.Bottom - depth * 64f;

			Point position = furniture.boundingBox.Value.Location;
			position.Y += furniture.boundingBox.Value.Height;
			// position.Y -= furniture.sourceRect.Height * 4;
			position += SpawnRect.Location * new Point(4);
			position += new Point(
				(int)(Game1.random.NextSingle() * SpawnRect.Size.X * 4),
				(int)(Game1.random.NextSingle() * SpawnRect.Size.Y * 4)
			);
			position -= SourceRect.Size * new Point(2);

			float rotation = Rotations[Game1.random.Next(Rotations.Count)];
			float rot_speed = RotationSpeeds[Game1.random.Next(RotationSpeeds.Count)];

			furniture.Location.temporarySprites.Add(
			new TemporaryAnimatedSprite()
			{
				texture = texture,
				sourceRect = SourceRect,
				position = position.ToVector2(),
				alpha = Alpha,
				alphaFade = new_alpha_fade,
				color = Color,
				motion = speed,
				// acceleration = Vector2.Zero,
				animationLength = FrameCount,
				interval = FrameDuration,
				totalNumberOfLoops = LoopCount,
				holdLastFrame = HoldLastFrame,
				flicker = Flicker,
				layerDepth = depth / 10000f,
				scale = Scale,
				scaleChange = ScaleChange,
				rotation = rotation,
				rotationChange = rot_speed
			});
		}
	}

	public class ParticleList : List<Particles>
	{
		private List<long> ParseTimers(Furniture furniture)
		{
			List<long> timers = new();
			bool valid_mod_data = true;

			if (furniture.modData.TryGetValue("FF.particle_timers", out string? timers_string))
			{
				JArray timers_array = new();

				try { timers_array = JArray.Parse(timers_string); }
				catch (JsonReaderException)
				{
					ModEntry.log("Invalid FF.particle_timer modData.", LogLevel.Trace);
					valid_mod_data = false;
				}

				if (timers_array.Count == Count)
				{
					foreach (JToken timer_token in timers_array)
					{
						if (timer_token.Type != JTokenType.Integer)
						{
							ModEntry.log("Invalid timer in FF.particle_timer modData.", LogLevel.Trace);
							valid_mod_data = false;
							break;
						}

						timers.Add((long)timer_token);
					}
				}
				
				else
				{
					valid_mod_data = false;
				}

			}
			else { valid_mod_data = false; }

			if (!valid_mod_data)
			{
				timers.AddRange(Enumerable.Repeat(0L, Count - timers.Count));
			}

			return timers;
		}

		private void SaveTimers(Furniture furniture, List<long> timers)
		{
			JArray timers_array = new();
			foreach (int timer in timers)
			{
				timers_array.Add(new JValue(timer));
			}

			furniture.modData["FF.particle_timers"] = timers_array.ToString();
		}

		public void UpdateTimer(Furniture furniture, long time_ms, string mod_id)
		{
			List<long> timers = ParseTimers(furniture);

			foreach ((Particles particle, int index) in this.Select((value, index) => (value, index)))
			{
				particle.UpdateTimer(furniture, timers, index, time_ms, mod_id);
			}

			SaveTimers(furniture, timers);
		}

		public void Burst(Furniture furniture, string mod_id)
		{
			foreach (Particles particle in this)
			{
				particle.BurstEmit(furniture, mod_id);
			}
		}
	}
}