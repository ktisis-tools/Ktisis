#nullable  enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

using Ktisis.Data.Json;

namespace Ktisis.Localization;

public class LocaleDataLoader {

	/* Being lenient for backwards compatibility, but you will get an annoyed look if you put in C/C++-style comments in your JSON. */
	private readonly static JsonReaderOptions readerOptions = new() {
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	private Stream GetResourceStream(string technicalName) {
		/* TODO: Type-scope once we get the namespaces sorted out */
		Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
			typeof(LocaleDataLoader),
			"Data." + technicalName + ".json"
		);
		if (stream == null)
			throw new Exception($"Cannot find data file '{technicalName}'");
		return stream;
	}

	public LocaleMetaData LoadMeta(string technicalName) {
		using Stream stream = GetResourceStream(technicalName);
		var reader = new BlockBufferJsonReader(stream, stackalloc byte[4096], readerOptions);

		reader.Read();
		if (reader.Reader.TokenType != JsonTokenType.StartObject)
			throw new Exception($"Locale Data file '{technicalName}.json' does not contain a top-level object.");

		while (reader.Read()) {
			switch (reader.Reader.TokenType) {
				case JsonTokenType.PropertyName:
					if (reader.Reader.GetString() == "$meta") {
						return ReadMetaObject(technicalName, ref reader);
					}
					reader.SkipIt();
					break;
				case JsonTokenType.EndObject:
					throw new Exception($"Locale Data file '{technicalName}' is is missing the top-level '$meta' object.");
				default:
					Debug.Assert(false, "Should not reach this point.");
					throw new Exception("Should not reach this point.");
			}
		}
		throw new Exception($"Locale Data file '{technicalName}.json' is missing its meta data (top-level '$meta' key not found)");
	}
	private LocaleMetaData ReadMetaObject(string technicalName, ref BlockBufferJsonReader reader) {
		reader.Read();
		if(reader.Reader.TokenType != JsonTokenType.StartObject)
			throw new Exception($"Locale Data file '{technicalName}.json' has a non-object at the top-level '$meta' key.");

		string? displayName = null;
		string? selfName = null;
		string?[]? maintainers = null;

		while(true) {
			reader.Reader.Read();
			switch(reader.Reader.TokenType) {
				case JsonTokenType.PropertyName:
					string propertyName = reader.Reader.GetString()!;
					reader.Read();
					switch(propertyName) {
						case "__comment":
							break;
						case "displayName":
							if(reader.Reader.TokenType != JsonTokenType.String)
								throw new Exception($"Locale data file '{technicalName}.json' has an invalid '%.$meta.displayName' value (not a string).");
							displayName = reader.Reader.GetString();
							break;
						case "selfName":
							if(reader.Reader.TokenType != JsonTokenType.String)
								throw new Exception($"Locale data file '{technicalName}.json' has an invalid '%.$meta.selfName' value (not a string).");
							selfName = reader.Reader.GetString();
							break;
						case "maintainers":
							if(reader.Reader.TokenType != JsonTokenType.StartArray)
								throw new Exception($"Locale data file '{technicalName}.json' has an invalid '%.$meta.maintainers' value (not an array).");
							List<string?> collectMaintainers = new List<string?>();
							int i = 0;
							while(reader.Read()) {
								switch(reader.Reader.TokenType) {
									case JsonTokenType.Null:
										collectMaintainers.Add(null);
										break;
									case JsonTokenType.String:
										collectMaintainers.Add(reader.Reader.GetString());
										break;
									case JsonTokenType.EndArray:
										goto endArray;
									default:
										throw new Exception(
											$"Locale data file '{technicalName}' has an invalid value at '%.$meta.maintainers.{i}' (not a string or null).");
								}

								i++;
							}

							endArray:
							maintainers = collectMaintainers.ToArray();
							break;
						default:
							Logger.Warning($"Locale data file '{technicalName}.json' has unknown meta key at '%.$meta.{reader.Reader.GetString()}'");
							reader.SkipIt();
							break;
					}

					break;
				case JsonTokenType.EndObject:
					goto done;
			}
		}

		done:
		if(displayName == null)
			throw new Exception($"Locale data file '{technicalName}.json' is missing the '%.$meta.displayName' value.");
		if(selfName == null)
			throw new Exception($"Locale data file '{technicalName}.json' is missing the '%.$meta.selfName' value.");
		maintainers ??= new string?[] { null };

		return new LocaleMetaData(technicalName, displayName, selfName, maintainers);
	}

	public LocaleData LoadData(string technicalName) => _LoadData(technicalName, null);

	public LocaleData LoadData(LocaleMetaData metaData) => _LoadData(metaData.TechnicalName, metaData);

	private LocaleData _LoadData(string technicalName, LocaleMetaData? meta) {
		using Stream stream = GetResourceStream(technicalName);
		var reader = new BlockBufferJsonReader(stream, stackalloc byte[4096], readerOptions);

		reader.Read();
		if (reader.Reader.TokenType != JsonTokenType.StartObject)
			throw new Exception($"Locale Data file '{technicalName}' does not contain a top-level object.");

		Dictionary<string, string> translationData = new();

		Stack<string> keyStack = new();
		string? currentKey = null;

		int metaCount = 0;

		while (reader.Read()) {
			switch (reader.Reader.TokenType) {
				case JsonTokenType.PropertyName:
					if (keyStack.Count == 0 && reader.Reader.GetString() == "$meta") {
						metaCount++;
						if (meta == null)
							meta = ReadMetaObject(technicalName, ref reader);
						else
							reader.SkipIt();
					} else if(reader.Reader.GetString() == "__comment") {
						reader.SkipIt();
					} else {
						keyStack.TryPeek(out string? prevKey);
						if (prevKey != null) {
							currentKey = prevKey + "." + reader.Reader.GetString();
						} else {
							currentKey = reader.Reader.GetString();
						}
					}
					break;
				case JsonTokenType.String:
					translationData.Add(currentKey!, reader.Reader.GetString()!);
					break;
				case JsonTokenType.StartObject:
					keyStack.Push(currentKey!);
					break;
				case JsonTokenType.EndObject:
					if (keyStack.TryPop(out string? _)) /* non-top-level object */
						break;
					goto done;
				case JsonTokenType.StartArray:
					WarnUnsupported(technicalName, "array", currentKey!);
					reader.SkipIt();
					break;
				case JsonTokenType.True:
				case JsonTokenType.False:
					WarnUnsupported(technicalName, "boolean", currentKey!);
					break;
				case JsonTokenType.Number:
					WarnUnsupported(technicalName, "number", currentKey!);
					break;
				case JsonTokenType.Null:
					WarnUnsupported(technicalName, "null", currentKey!);
					break;
			}
		}

		done:
		switch(metaCount) {
			case 0:
				throw new Exception($"Locale Data file '{technicalName}.json' is is missing the top-level '$meta' object.");
			case > 1:
				Logger.Warning($"Locale Data file '{technicalName}.json' has {{0}} top-level '$meta' objects?!", metaCount);
				break;
		}

		translationData.TrimExcess();

		return new LocaleData(meta!, translationData);
	}

	private void WarnUnsupported(string technicalName, string elementType, string currentKey) {
		Logger.Warning("Locale Data File '{0}.json' has an unsupported {1} at '%.{2}'.", technicalName, elementType, currentKey);
	}

}
