namespace HappyHomeDesigner
{
	public interface IHomeDesignerAPI
	{
		/// <summary>Register a mod which attaches catalogue shops to furniture items</summary>
		public void AddCatalogueProvider(ICatalogueProvider provider);

		/// <summary>Call this when a provider's catalogue list changes</summary>
		public void InvalidateProviderCache();

		public interface ICatalogueProvider
		{
			/// <summary>Returns a sequence of pairs, where the key is the qualified item ID and the value is the shop id associated with the catalogue item.</summary>
			IEnumerable<KeyValuePair<string, string>> GetCatalogues();
		}
	}
}