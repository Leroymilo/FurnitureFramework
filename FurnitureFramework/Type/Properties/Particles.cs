using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace FurnitureFramework.Type.Properties
{
	[RequiresPreviewFeatures]
	class ParticlesList: IProperty<ParticlesList>
	{
		#region Particles Subclass

		private class Particles
		{

			#region Fields

			public readonly bool is_valid = false;
			public readonly string? error_msg;

			DynaTexture texture;
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

			#endregion

			#region Particles Parsing

			public Particles(TypeInfo info, JObject particle_obj, string rot_name)
			{
				error_msg = "Missing or Invalid Source Image path.";
				JToken? texture_token = particle_obj.GetValue("Source Image");
				if (texture_token is null || texture_token.Type != JTokenType.String)
					return;
				texture = new(info, texture_token.ToString());

				JToken? rect_token = particle_obj.GetValue("Source Rect");
				if (!JsonParser.try_parse(rect_token, ref source_rect))
					source_rect = Rectangle.Empty;

				emit_interval = JsonParser.parse(particle_obj.GetValue("Emission Interval"), 500);

				error_msg = "Missing or invalid Spawn Rect field.";
				JToken? spawn_rect_token = particle_obj.GetValue("Spawn Rect");
				if (!JsonParser.try_parse_dir(spawn_rect_token, rot_name, ref spawn_rect))
					return;

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

				JsonParser.try_parse(particle_obj.GetValue("Speed"), ref base_speed);

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

				scale = JsonParser.parse(particle_obj.GetValue("Scale"), 1f);
				scale_change = JsonParser.parse(particle_obj.GetValue("Scale Change"), 0f);

				color = JsonParser.parse_color(particle_obj.GetValue("Color"), Color.White);
				alpha = JsonParser.parse(particle_obj.GetValue("Alpha"), 1f);
				alpha_fade = JsonParser.parse(particle_obj.GetValue("Alpha Fade"), 0f);

				frame_count = JsonParser.parse(particle_obj.GetValue("Frame Count"), 1);
				frame_length = JsonParser.parse(particle_obj.GetValue("Frame Duration"), 1000);
				loop_count = JsonParser.parse(particle_obj.GetValue("Loop Count"), 1);
				hold_last = JsonParser.parse(particle_obj.GetValue("Hold Last Frame"), false);
				flicker = JsonParser.parse(particle_obj.GetValue("Flicker"), false);

				emit_when_on = JsonParser.parse(particle_obj.GetValue("Emit When On"), false);
				emit_when_off = JsonParser.parse(particle_obj.GetValue("Emit When Off"), false);
				does_burst = JsonParser.parse(particle_obj.GetValue("Burst"), false);

				is_valid = true;
			}

			#endregion

			#region Particles Methods

			public void update_timer(Furniture furniture, List<long> timers, int index, long time_ms)
			{
				if (
					(!emit_when_on || !furniture.IsOn) &&
					(!emit_when_off || furniture.IsOn)
				) return;

				if (time_ms - timers[index] > emit_interval)
				{
					make(furniture);
					timers[index] = time_ms;
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
				if (source_rect == Rectangle.Empty)
					source_rect = texture.get().Bounds;

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

				Point position = furniture.boundingBox.Value.Location;
				position.Y += furniture.boundingBox.Value.Height;
				position.Y -= furniture.sourceRect.Height * 4;
				position += spawn_rect.Location * new Point(4);
				position += new Point(
					(int)(Game1.random.NextSingle() * spawn_rect.Size.X * 4),
					(int)(Game1.random.NextSingle() * spawn_rect.Size.Y * 4)
				);
				position -= source_rect.Size * new Point(2);

				float rotation = rotations[Game1.random.Next(rotations.Count)];
				float rot_speed = rot_speeds[Game1.random.Next(rot_speeds.Count)];

				furniture.Location.temporarySprites.Add(
				new TemporaryAnimatedSprite()
				{
					texture = texture.get(),
					sourceRect = source_rect,
					position = position.ToVector2(),
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

			public void debug_print(int indent_count)
			{
				string indent = new('\t', indent_count);
				ModEntry.log($"{indent}TODO");
			}

			#endregion

		}

		#endregion

		#region Particles Parsing

		public static ParticlesList make_default(TypeInfo info, string rot_name)
		{
			return new(info, new JArray() /*empty array -> no particless*/, rot_name);
		}

		public static ParticlesList make(TypeInfo info, JToken? data, string rot_name, out string? error_msg)
		{
			error_msg = null;

			if (data == null || data.Type == JTokenType.None)
				return make_default(info, rot_name);
			
			if (data is JObject obj)
			{
				// single particles?
				Particles particles = new(info, obj, rot_name);
				if (particles.is_valid)
					return new(particles);

				// directional?
				JToken? dir_token = obj.GetValue(rot_name);
				if (dir_token is JObject dir_obj)
				{
					// directional single particles?
					Particles dir_particles = new(info, dir_obj, rot_name);
					if (dir_particles.is_valid)
						return new(dir_particles);
					
					// single particles was invalid
					ModEntry.log($"Could not parse a particles in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{particles.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Particles.", LogLevel.Warn);
				}

				else if (dir_token is JArray dir_arr)
				{
					// directional particless
					return new(info, dir_arr, rot_name);
				}

				else
				{
					// single particles was invalid
					ModEntry.log($"Could not parse a particles in {info.mod_id} at {data.Path}:", LogLevel.Warn);
					ModEntry.log($"\t{particles.error_msg}", LogLevel.Warn);
					ModEntry.log("Skipping Particles.", LogLevel.Warn);
				}
			}

			else if (data is JArray arr)
			{
				// list of particless
				return new(info, arr, rot_name);
			}

			// for all invalid cases

			error_msg = "Invalid Particle List definition, fallback to no Particles.";
			return make_default(info, rot_name);
		}

		List<Particles> list = new();

		private ParticlesList(Particles particles)
		{
			list.Add(particles);
		}

		private ParticlesList(TypeInfo info, JArray array, string rot_name)
		{
			foreach (JToken token in array)
			{
				if (token is not JObject obj2) continue;	// skips comments
				add_particles(info, obj2, rot_name);
			}
		}

		private void add_particles(TypeInfo info, JObject data, string rot_name)
		{
			Particles particles = new(info, data, rot_name);
			if (particles.is_valid)
				list.Add(particles);
			else
			{
				ModEntry.log($"Invalid Particles in {info.mod_id} at {data.Path}:", LogLevel.Warn);
				ModEntry.log($"\t{particles.error_msg}", LogLevel.Warn);
				ModEntry.log($"Skipping Particles.");
			}
		}

		#endregion

		#region ParticlesList Methods

		private List<long> parse_timers(Furniture furniture)
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

				if (timers_array.Count == list.Count)
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
				timers.AddRange(Enumerable.Repeat(0L, list.Count - timers.Count));
			}

			return timers;
		}

		private void save_timers(Furniture furniture, List<long> timers)
		{
			JArray timers_array = new();
			foreach (int timer in timers)
			{
				timers_array.Add(new JValue(timer));
			}

			furniture.modData["FF.particle_timers"] = timers_array.ToString();
		}

		public void update_timer(Furniture furniture, long time_ms)
		{
			List<long> timers = parse_timers(furniture);

			foreach ((Particles particle, int index) in list.Select((value, index) => (value, index)))
			{
				particle.update_timer(furniture, timers, index, time_ms);
			}

			save_timers(furniture, timers);
		}

		public void burst(Furniture furniture)
		{
			foreach (Particles particle in list)
			{
				particle.burst(furniture);
			}
		}

		public void debug_print(int indent_count)
		{
			string indent = new('\t', indent_count);
			int index = 0;
			foreach (Particles particles in list)
			{
				ModEntry.log($"{indent}Particles {index}:", LogLevel.Debug);
				particles.debug_print(indent_count + 1);
				index ++;
			}
		}

		#endregion
	}
	
}