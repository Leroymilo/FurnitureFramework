using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework
{
	using SDColor = System.Drawing.Color;

	class Particles
	{
		#region ParticleData

		private class ParticleData
		{

			public readonly bool is_valid = false;
			public readonly string? error_msg;

			Texture2D texture;
			Rectangle source_rect;
			int emit_interval;

			Rectangle spawn_rect;
			List<float> depths = new();
			Vector2 base_speed = Vector2.Zero;

			List<float> rotations = new();
			List<float> rot_speeds = new();

			float scale;
			float scale_change;

			Color color;
			float alpha;
			float alpha_fade;

			int frame_count;
			int frame_length;
			int loop_count;
			bool hold_last;
			bool flicker;

			bool emit_when_on = false;
			bool emit_when_off = false;
			bool does_burst = true;

			List<long> particle_timers = new() {-1};

			#region ParticleData Parsing

			public ParticleData(IContentPack pack, JObject particle_obj)
			{
				error_msg = "Missing or Invalid Source Image path.";
				JToken? texture_token = particle_obj.GetValue("Source Image");
				if (texture_token is not null && texture_token.Type == JTokenType.String)
				{
					string? texture_path = (string?)texture_token;
					if (texture_path is not null)
					{
						texture = TextureManager.load(pack.ModContent, texture_path);
					}
				}
				if (texture == null) return;

				source_rect = texture.Bounds;
				JToken? rect_token = particle_obj.GetValue("Source Rect");
				if (rect_token is JObject)
				{
					try
					{
						source_rect = JC.extract_rect(rect_token);
					}
					catch (InvalidDataException)
					{
						source_rect = texture.Bounds;
					}
				}

				emit_interval = JC.extract(particle_obj, "Emission Interval", 500);

				error_msg = "Missing or invalid Spawn Rect field.";
				JToken? spawn_rect_token = particle_obj.GetValue("Spawn Rect");
				if (spawn_rect_token == null) return;
				try
				{
					spawn_rect = JC.extract_rect(spawn_rect_token);
				}
				catch (InvalidDataException)
				{
					return;
				}

				JToken? depths_token = particle_obj.GetValue("Depths");
				if (depths_token is JArray depths_arr)
				{
					foreach (JToken depth_token in depths_arr)
					{
						if (
							depth_token.Type != JTokenType.Float &&
							depth_token.Type != JTokenType.Integer
						) continue;
						depths.Add((float)depth_token);
					}
				}
				if (depths.Count == 0) depths.Add(0);

				JToken? speed_token = particle_obj.GetValue("Speed");
				if (speed_token is JObject speed_obj)
				{
					base_speed = JC.extract_position(speed_obj);
				}

				JToken? rots_token = particle_obj.GetValue("Rotations");
				if (rots_token is JArray rots_arr)
				{
					foreach (JToken rot_token in rots_arr)
					{
						if (
							rot_token.Type != JTokenType.Float &&
							rot_token.Type != JTokenType.Integer
						) continue;
						rotations.Add((float)rot_token);
					}
				}
				if (rotations.Count == 0) rotations.Add(0);

				JToken? rot_speeds_token = particle_obj.GetValue("Rotation Speeds");
				if (rot_speeds_token is JArray rot_speeds_arr)
				{
					foreach (JToken rot_speed_token in rot_speeds_arr)
					{
						if (
							rot_speed_token.Type != JTokenType.Float &&
							rot_speed_token.Type != JTokenType.Integer
						) continue;
						rot_speeds.Add((float)rot_speed_token);
					}
				}
				if (rot_speeds.Count == 0) rot_speeds.Add(0);

				scale = JC.extract(particle_obj, "Scale", 1f);
				scale_change = JC.extract(particle_obj, "Scale Change", 0f);

				string color_name = JC.extract(particle_obj, "Color", "White");
				SDColor c_color = SDColor.FromName(color_name);
				color = new(c_color.R, c_color.G, c_color.B);
				alpha = JC.extract(particle_obj, "Alpha", 1f);
				alpha_fade = JC.extract(particle_obj, "Alpha Fade", 0f);

				frame_count = JC.extract(particle_obj, "Frame Count", 1);
				frame_length = JC.extract(particle_obj, "Frame Duration", 1000);
				loop_count = JC.extract(particle_obj, "Loop Count", 1);
				hold_last = JC.extract(particle_obj, "Hold Last Frame", false);
				flicker = JC.extract(particle_obj, "Flicker", false);

				emit_when_on = JC.extract(particle_obj, "Emit When On", false);
				emit_when_off = JC.extract(particle_obj, "Emit When Off", false);
				does_burst = JC.extract(particle_obj, "Burst", false);

				is_valid = true;
			}

			#endregion

			#region ParticleData Methods

			public void free_timer(int index)
			{
				particle_timers[index] = -1;
			}

			public int initialize_timer()
			{
				for (int i = 1; i < particle_timers.Count; i++)
				{
					if (particle_timers[i] == -1)
					{
						// if timer was reset, use it
						return i;
					}
				}

				particle_timers.Add(0);
				return particle_timers.Count - 1;
			}

			public void update_timer(Furniture furniture, int timer_id, long time_ms)
			{
				if (time_ms - particle_timers[timer_id] > emit_interval)
				{
					make(furniture);
					particle_timers[timer_id] = time_ms;
				}
			}

			public void burst(Furniture furniture)
			{
				if (!does_burst) return;
				if (
					(!emit_when_on || !furniture.IsOn) &&
					(!emit_when_off || furniture.IsOn)
				) return;

				// placement/toggle burst
				for (float m = 1; m <= 6; m += 0.5f)
				{
					make(furniture, base_speed * m);
				}
			}

			public void make(Furniture furniture, Vector2? speed_ = null)
			{
				if (
					(!emit_when_on || !furniture.IsOn) &&
					(!emit_when_off || furniture.IsOn)
				) return;

				// Change magic values into parameters

				Vector2 speed;
				if (speed_ is null) speed = base_speed;
				else speed = speed_.Value;

				// for burst
				float new_alpha_fade;
				if (speed.Length() * base_speed.Length() > 0)
				{
					new_alpha_fade = alpha_fade * speed.Length() / base_speed.Length();
					new_alpha_fade -= speed.Length() > base_speed.Length() ? 0.002f: 0f;
				}
				else new_alpha_fade = alpha_fade;

				float depth = depths[Game1.random.Next(depths.Count)];
				depth = furniture.boundingBox.Bottom - depth * 64f;

				Vector2 position = furniture.TileLocation * 64f;
				position.Y += furniture.boundingBox.Value.Height;
				position.Y -= furniture.sourceRect.Height * 4f;
				position += spawn_rect.Location.ToVector2() * 4f;
				position += new Vector2(
					Game1.random.NextSingle(),
					Game1.random.NextSingle()
				) * spawn_rect.Size.ToVector2() * 4f;
				position -= source_rect.Size.ToVector2() * 2f;

				float rotation = rotations[Game1.random.Next(rotations.Count)];
				float rot_speed = rot_speeds[Game1.random.Next(rot_speeds.Count)];

				furniture.Location.temporarySprites.Add(
				new TemporaryAnimatedSprite()
				{
					texture = texture,
					sourceRect = source_rect,
					position = position,
					alpha = alpha,
					alphaFade = new_alpha_fade,
					color = color,
					motion = speed,
					// acceleration = Vector2.Zero,
					animationLength = frame_count,
					interval = frame_length,
					totalNumberOfLoops = loop_count,
					holdLastFrame = hold_last,
					flicker = flicker,
					layerDepth = depth / 10000f,
					scale = scale,
					scaleChange = scale_change,
					rotation = rotation,
					rotationChange = rot_speed
				});
			}

			#endregion

		}

		#endregion

		List<ParticleData> particle_list = new();

		#region Particles Parsing

		public Particles(IContentPack pack, JToken? parts_token)
		{
			if (parts_token is not JArray parts_arr) return;

			foreach (JToken part_token in parts_arr)
			{
				if (part_token is not JObject part_obj) continue;
				ParticleData new_part = new(pack, part_obj);
				if (!new_part.is_valid)
				{
					ModEntry.log($"Invalid Particle Data at {part_token.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{new_part.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping particle data");
					continue;
				}
				particle_list.Add(new_part);
			}
		}

		#endregion

		#region Particles Method

		public void update_timer(Furniture furniture, long time_ms)
		{
			int timer_id = furniture.lastNoteBlockSoundTime;
			if (timer_id == 0)
			{
				foreach (ParticleData particle in particle_list)
				{
					timer_id = particle.initialize_timer();
					// they should all return the same value
				}
				furniture.lastNoteBlockSoundTime = timer_id;
			}
			// Furniture.lastNoteBlockSoundTime is used to store an id instead of the timer itself.

			foreach (ParticleData particle in particle_list)
			{
				particle.update_timer(furniture, timer_id, time_ms);
			}
		}

		public void free_timers(Furniture furniture)
		{
			int index = furniture.lastNoteBlockSoundTime;
			foreach (ParticleData particle in particle_list)
			{
				particle.free_timer(index);
			}
		}

		public void burst(Furniture furniture)
		{
			foreach (ParticleData particle in particle_list)
			{
				particle.burst(furniture);
			}
		}

		#endregion
	}
	
}