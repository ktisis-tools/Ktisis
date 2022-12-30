using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Ktisis.Interface;

namespace Ktisis.Localization {
	public static class Locale {
		private static LocaleData Data = null!;

		public static List<UserLocale> Languages = new() {
			UserLocale.English,
			UserLocale.German
		};

		[Obsolete("Use `Translate({handle})` instead.")]
		public static string GetString(string handle) {
			return Translate(handle);
		}

		public static string Translate(string handle, Dictionary<string, string>? parameters = null) {
			return Data.Translate(handle, parameters);
		}

		public static bool HasTranslationFor(string handle) {
			return Data.HasTranslationFor(handle);
		}

		public static string GetBoneName(string handle) {
			return Ktisis.Configuration.TranslateBones ? GetString("bone." + handle) : handle;
		}
		public static string GetInputPurposeName(Input.Purpose purpose) {
			string regularPurposeString = $"config.input.keybind.purpose.{purpose}";
			if(HasTranslationFor(regularPurposeString))
				return GetString(regularPurposeString);

			bool isHold = (int)purpose >= Input.FirstCategoryPurposeHold && (int)purpose < Input.FirstCategoryPurposeToggle;
			bool isToggle = (int)purpose >= Input.FirstCategoryPurposeToggle;
			string actionHandle = isHold ? "config.input.keybind.purpose.generic.hold" : (isToggle ? "config.input.keybind.purpose.generic.toggle" : "config.input.keybind.purpose.generic.invalid");

			if (Input.PurposesCategories.TryGetValue(purpose, out var category))
				return GetString(actionHandle) + " " + GetString(category.Name);

			return regularPurposeString;
		}

		/* TODO: Remove this, and instead use the technical name as the identifier */
		public static void LoadLocale(UserLocale value) {
			LoadLocale(value switch {
				UserLocale.English => "en_US",
				UserLocale.French => "fr_FR",
				UserLocale.German => "de_DE",
				UserLocale.Japanese => "jp_JP",
				var _ => throw new ArgumentException("Unknown UserLocale", nameof(value))
			});
		}

		public static void LoadLocale(string technicalName) {
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (Data == null || Data.MetaData.TechnicalName != technicalName)
				Data = LocaleDataLoader.LoadData(technicalName);
		}
	}

	/* TODO: Remove this and instead scan for locale resources in the Assembly */
	public enum UserLocale {
		None = -1,
		// these don't exist yet
		English = 0,
		French = 1,
		German = 2,
		Japanese = 3
	}
}
