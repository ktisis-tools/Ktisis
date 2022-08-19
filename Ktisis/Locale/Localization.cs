using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Dalamud.Logging;

namespace Ktisis.Localization {
	public class Locale {
		private Ktisis Plugin;

		public UserLocale Loaded = UserLocale.None;
		public JObject Strings = new();

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
				Loaded = lang;

				try {
					var file = new StreamReader( GetLocaleFile(lang) );
					using (var reader = new JsonTextReader(file))
						Strings = (JObject)JToken.ReadFrom(reader);
				} catch {
					PluginLog.Error($"Failed to fetch localization: {lang}");
				}
			}

			return Strings.ContainsKey(handle) ? (string)Strings[handle]! : handle;
		}

		public string GetBoneName(string handle) {
			return Plugin.Configuration.TranslateBones ? GetString(handle) : handle;
		}

		public static Stream GetLocaleFile(UserLocale lang) {
			Assembly assembly = Assembly.GetExecutingAssembly();
			string assemblyName = assembly.GetName().Name!;

			var path = $"{assemblyName}.Locale.i18n.{Languages[lang]}.json";

			Stream? stream = assembly.GetManifestResourceStream(path);
			if (stream == null)
				throw new FileNotFoundException(path);

			return stream;
		}
	}

	public enum UserLocale {
		None = -1,
		// these don't exist yet
		En = 0,
		Fr = 1,
		De = 2,
		Jp = 3
	}
}