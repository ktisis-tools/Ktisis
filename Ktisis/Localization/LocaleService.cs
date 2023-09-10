using System;
using System.Collections.Generic;

namespace Ktisis.Localization;

public static class LocaleService {
	private static LocaleData Data = null!;

	public static List<UserLocale> Languages = new() {
		UserLocale.English
	};

	public static string Translate(string handle, Dictionary<string, string>? parameters = null) {
		return Data.Translate(handle, parameters);
	}

	public static bool HasTranslationFor(string handle) {
		return Data.HasTranslationFor(handle);
	}

	/* TODO: Remove this, and instead use the technical name as the identifier */
	public static void LoadLocale(UserLocale value) {
		LoadLocale(value switch {
			UserLocale.English => "en_US",
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
	English = 0
}
