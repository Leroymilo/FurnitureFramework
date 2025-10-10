using Newtonsoft.Json.Linq;
using StardewModdingAPI;


namespace FurnitureFramework.Data.FPack
{
	partial class FPack
	{
		#region ff_debug_print

		public static void DebugPrint(string _, string[] args)
		{
			if (args.Length == 0) PrintAll();
			else PrintSingle(args[0]);
		}

		private static void PrintAll()
		{
			foreach (string UID in ContentPacks.Keys)
				PrintSingle(UID);
		}

		private static void PrintSingle(string ID)
		{
			string data_UID = $"{ID}/{DEFAULT_PATH}";

			if (PacksData.TryGetValue(data_UID, out FPack? f_pack))
			{
				ModEntry.Log($"{data_UID}: {JObject.FromObject(f_pack)}");
				return;
			}

			if (TryGetType(ID, out FType.FType? f_type))
			{
				ModEntry.Log($"{ID}: {JObject.FromObject(f_type)}");
				return;
			}

			ModEntry.Log($"Pack or Furniture {ID} does not exist!", LogLevel.Warn);

		}

		#endregion

		#region ff_reload

		public static void Reload(string _, string[] args)
		{
			if (args.Length == 0) ReloadAll();
			else ReloadSingle(args[0]);
		}

		private static void ReloadAll()
		{
			foreach (string UID in ContentPacks.Keys)
				ReloadSingle(UID);
		}

		#endregion
	}
}