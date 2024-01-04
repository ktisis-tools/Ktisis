namespace Ktisis.Localization;

public class LocaleMetaData {

	public string TechnicalName { get; }

	/** English Display name */
	public string DisplayName { get; }
	/**
	* Localized Display name
	* (i.e. what the language calls itself)
	*/
	public string SelfName { get; }
	/**
	* List of maintainers for the locale data file.
	* `null` semantically represents "and others".
	*/
	public string?[] Maintainers { get; }

	internal LocaleMetaData(string technicalName, string displayName, string selfName, string?[] maintainers) {
		this.TechnicalName = technicalName;
		this.DisplayName = displayName;
		this.SelfName = selfName;
		this.Maintainers = maintainers;
	}
}
