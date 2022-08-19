namespace Ktisis.Localization {
	public class Locale {
		private Ktisis Plugin;

		public Locale(Ktisis plugin) {
			Plugin = plugin;
		}

		public UserLocale GetCurrent() {
			return Plugin.Configuration.Localization;
		}

		public string GetString(string handle) {
			return handle; // Not implemented
		}
	}

	public enum UserLocale {
		// these don't exist yet
		En = 0,
		De = 1,
		Jp = 2,
		Fr = 3
	}
}