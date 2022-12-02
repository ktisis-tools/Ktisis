using Ktisis.Data.Serialization.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ktisis.Data.Serialization {
	internal class JsonParser {
		internal static JsonSerializerOptions Options = new();

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
		}

		internal static JsonConverter<T> GetConverter<T>()
			=> (JsonConverter<T>)Options.GetConverter(typeof(T));

		internal static string Serialize(object obj)
			=> JsonSerializer.Serialize(obj, Options);

		internal static T? Deserialize<T>(string json) where T : notnull
			=> JsonSerializer.Deserialize<T>(json, Options);
	}
}