using System.Text.Json;
using System.Text.Json.Serialization;

using Ktisis.Data.Json.Converters;

namespace Ktisis.Data.Json;

public class JsonFileSerializer {
	private readonly JsonSerializerOptions Options;
	private readonly JsonSerializerOptions DeserializeOptions;
	
	public JsonFileSerializer() {
		this.Options = new JsonSerializerOptions() {
			WriteIndented = true,
			PropertyNameCaseInsensitive = false,
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		
		this.Options.Converters.Add(new JsonStringEnumConverter());
		this.Options.Converters.Add(new QuaternionConverter(this));
		this.Options.Converters.Add(new Vector3Converter(this));
		this.Options.Converters.Add(new Vector4Converter(this));
		this.Options.Converters.Add(new TransformConverter(this));

		this.DeserializeOptions = new JsonSerializerOptions(this.Options);
		this.DeserializeOptions.Converters.Add(new JsonFileConverter());
	}
	
	public JsonConverter<T> GetConverter<T>()
		=> (JsonConverter<T>)this.Options.GetConverter(typeof(T));

	public string Serialize(object obj)
		=> JsonSerializer.Serialize(obj, this.Options);

	public T? Deserialize<T>(string json) where T : notnull
		=> JsonSerializer.Deserialize<T>(json, this.DeserializeOptions);
}
