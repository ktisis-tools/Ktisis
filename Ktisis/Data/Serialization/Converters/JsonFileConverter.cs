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

				var value = JsonSerializer.Deserialize(jsonValue, prop.PropertyType, options);
				prop.SetValue(result, value);
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