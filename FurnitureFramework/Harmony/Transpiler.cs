using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace FurnitureFramework.FFHarmony
{
	static class Transpiler
	{

		static public bool are_equal(CodeInstruction a, CodeInstruction b, int debug = 0)
		{
			if (a.IsStloc() && b.IsStloc())
			{
				return true;
			}
			
			if (a.opcode != b.opcode) return false;

			if (a.opcode == OpCodes.Callvirt)
			{
				if (a.operand is MethodInfo mi_a && b.operand is MethodInfo mi_b)
				{
					return mi_a.Name == mi_b.Name;
				}
			}

			// do not check operands for these OpCodes
			if (
				a.opcode == OpCodes.Beq_S || a.opcode == OpCodes.Br_S ||
				a.opcode == OpCodes.Ldloca || a.opcode == OpCodes.Ldloca_S
			)
			{
				return true;
			}

			return Equals(a.operand, b.operand);
		}

		static public List<int> find_start_indices(
			IEnumerable<CodeInstruction> original,
			IEnumerable<CodeInstruction> to_find,
			int debug = 0
		)
		{
			List<int> indices = new();
 
			for (int i = 0; i < 1 + original.Count() - to_find.Count(); i++)
			{
				bool seq_matches = true;
				int j;
				for (j = 0; j < to_find.Count(); j++, i++)
				{
					CodeInstruction orig = original.ElementAt(i);
					CodeInstruction to_f = to_find.ElementAt(j);

					if (debug > 1)
					{
						ModEntry.Log($"original[{i}]: \topcode: {orig.opcode}, \toperand: {orig.operand}", StardewModdingAPI.LogLevel.Trace);
						ModEntry.Log($"to_find[{j}]: \topcode: {to_f.opcode}, \toperand: {to_f.operand}", StardewModdingAPI.LogLevel.Trace);
					}

					if (!are_equal(orig, to_f, debug))
					{
						if (debug > 1 && j > 0) ModEntry.Log("Restart match", StardewModdingAPI.LogLevel.Trace);
						seq_matches = false;
						break;
					}
					else if (debug > 1) ModEntry.Log("Matching!");
				}
				if (seq_matches)
				{
					indices.Add(i-j);
					if (debug > 1) ModEntry.Log("Full Match found!", StardewModdingAPI.LogLevel.Info);
				}
			}

			return indices;
		}

		static public IEnumerable<CodeInstruction> replace_instructions(
			IEnumerable<CodeInstruction> instructions,
			List<CodeInstruction> to_replace,
			List<CodeInstruction> to_write,
			int match_limit = 1,
			int debug = 0
		)
		{
			List<int> start_indices = find_start_indices(instructions, to_replace, debug);

			if (start_indices.Count > match_limit)
			{
				start_indices.RemoveRange(match_limit, start_indices.Count - match_limit);
			}

			if (debug > 0)
				ModEntry.Log($"Transpiler found {start_indices.Count} instances to replace");

			int k = 0;

			List<CodeInstruction> new_inst = new();

			if (debug > 0)
				ModEntry.Log($"Starting to replace instructions");

			for (int i = 0; i < instructions.Count(); i++)
			{
				if (k < start_indices.Count && i == start_indices[k])
				{
					if (debug > 0)
						ModEntry.Log($"Replacing a set of instructions at index {i}");
					k++;
					i += to_replace.Count - 1;
					foreach (CodeInstruction instruction in to_write)
					{
						if (debug > 1)
							ModEntry.Log($"\t{instruction}");
						new_inst.Add(instruction);
					}
				}

				else
				{
					new_inst.Add(instructions.ElementAt(i));
				}
			}

			if (debug > 0)
				ModEntry.Log($"Finished replacing instructions");

			return new_inst;
		}
	}
}