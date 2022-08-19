using System.Collections.Generic;

using Newtonsoft;

namespace Ktisis.Localization {
	public class Locale {
		private Ktisis Plugin;

		public UserLocale Loaded = UserLocale.None;
		public Dictionary<string, string> Dict = new();

		public static Dictionary<UserLocale, string> Languages = new() {
			[UserLocale.En] = "English"/*,
			[UserLocale.Fr] = "French",
			[UserLocale.De] = "German",
			[UserLocale.Jp] = "Japanese"*/
		};

		public Locale(Ktisis plugin) {
			Plugin = plugin;
		}

		public UserLocale GetCurrent() {
			return Plugin.Configuration.Localization;
		}

		public string GetString(string handle) {
			var lang = GetCurrent();
			if (lang != Loaded) {

			}

			return handle; // Not implemented
		}
	}

	public enum UserLocale {
		None = -1,
		// these don't exist yet
		En = 0,
		De = 1,
		Jp = 2,
		Fr = 3
	}
}