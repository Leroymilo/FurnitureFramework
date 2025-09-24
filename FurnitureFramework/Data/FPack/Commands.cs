namespace FurnitureFramework.Data.FPack
{
	partial class FPack
	{
		#region ff_debug_print

		public static void DebugPrint(string _, string[] args)
		{
			ModEntry.Log("TODO", StardewModdingAPI.LogLevel.Warn);
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
			bool reloaded = false;
			foreach (string UID in ContentPacks.Keys)
				reloaded |= ReloadSingle(UID);
			
			if (reloaded) InvalidateGameData();
		}

		#endregion
	}
}