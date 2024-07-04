using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Ktisis.Data.Files;

namespace Ktisis.Data.Serialization.Converters {
	public class JsonFileConverter : JsonConverter<JsonFile> {
		public override bool CanConvert(Type t) => t.BaseType == typeof(JsonFile);

		public override JsonFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			var result = (JsonFile)Activator.CreateInstance(typeToConvert)!;
			using var jsonDoc = JsonDocument.ParseValue(ref reader);

			foreach (var prop in typeToConvert.GetProperties()) {
				var isPresent = jsonDoc.RootElement.TryGetProperty(prop.Name, out var jsonValue);
				if (!isPresent) {
					var defVal = prop.GetCustomAttribute<DeserializerDefault>();
					if (defVal != null) prop.SetValue(result, defVal.Default);
					continue;
				}

				try {
					var value = jsonValue.Deserialize(prop.PropertyType, options);
					if (value != null) prop.SetValue(result, value);
				} catch {
					Ktisis.Log.Warning($"Failed to parse {prop.PropertyType.Name} value '{prop.Name}' (received {jsonValue.ValueKind} instead)");
				}
			}

			return result;
		}
		
		public override void Write(Utf8JsonWriter writer, JsonFile value, JsonSerializerOptions options) { }
	}
	
	public class DeserializerDefault : Attribute {
		public object Default;

		public DeserializerDefault(object value)
			=> Default = value;
	}
}