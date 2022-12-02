using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Dalamud.Logging;

using Ktisis.Interface;

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
		public static string GetInputPurposeName(Input.Purpose purpose) {
			string regularPurposeString = $"Keyboard_Action_{purpose}";
			if(Strings.ContainsKey(regularPurposeString))
				return GetString(regularPurposeString);

			bool isHold = (int)purpose >= Input.FirstCategoryPurposeHold && (int)purpose < Input.FirstCategoryPurposeToggle;
			bool isToggle = (int)purpose >= Input.FirstCategoryPurposeToggle;
			string actionHandle = isHold ? "Input_Generic_Hold" : (isToggle ? "Input_Generic_Toggle" : "Input_Generic_Not_Applicable");

			if (Input.PurposesCategories.TryGetValue(purpose, out var category))
				return GetString(actionHandle) + " " + GetString(category.Name);

			return regularPurposeString;
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