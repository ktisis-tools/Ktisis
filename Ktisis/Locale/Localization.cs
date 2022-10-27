using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Dalamud.Logging;

namespace Ktisis.Localization {
	public static class Locale {
		public static UserLocale Loaded = UserLocale.None;
		public static JObject Strings = new();

		public static List<UserLocale> Languages = new() {
			UserLocale.English,
			UserLocale.German
		};

		public static UserLocale GetCurrent() {
			return Ktisis.Configuration.Localization;
		}

		public static string GetString(string handle) {
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

		public static string GetBoneName(string handle) {
			return Ktisis.Configuration.TranslateBones ? GetString(handle) : handle;
		}

		public static Stream GetLocaleFile(UserLocale lang) {
			Assembly assembly = Assembly.GetExecutingAssembly();
			string assemblyName = assembly.GetName().Name!;

			var path = $"{assemblyName}.Locale.i18n.{lang}.json";

			Stream? stream = assembly.GetManifestResourceStream(path);
			if (stream == null)
				throw new FileNotFoundException(path);

			return stream;
		}
	}

	public enum UserLocale {
		None = -1,
		// these don't exist yet
		English = 0,
		French = 1,
		German = 2,
		Japanese = 3
	}
}