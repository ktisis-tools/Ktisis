using System.Text.Json;
using System.Text.Json.Serialization;

using Ktisis.Data.Serialization.Converters;

namespace Ktisis.Data.Serialization {
	internal class JsonParser {
		internal static JsonSerializerOptions Options = new();
		internal static JsonSerializerOptions DeserializeOptions;

		static JsonParser() {
			Options.WriteIndented = true;
			Options.PropertyNameCaseInsensitive = false;
			Options.AllowTrailingCommas = true;
			Options.ReadCommentHandling = JsonCommentHandling.Skip;
			Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

			Options.Converters.Add(new JsonStringEnumConverter());

			Options.Converters.Add(new QuaternionConverter());
			Options.Converters.Add(new Vector3Converter());
			Options.Converters.Add(new Vector4Converter());
			Options.Converters.Add(new TransformConverter());

			DeserializeOptions = new(Options);
			DeserializeOptions.Converters.Add(new JsonFileConverter());
		}

		internal static JsonConverter<T> GetConverter<T>()
			=> (JsonConverter<T>)Options.GetConverter(typeof(T));

		internal static string Serialize(object obj)
			=> JsonSerializer.Serialize(obj, Options);

		internal static T? Deserialize<T>(string json) where T : notnull
			=> JsonSerializer.Deserialize<T>(json, DeserializeOptions);
	}
}
