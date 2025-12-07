using System.Collections.Generic;
using System.IO;
using System.Text;

using Ktisis.Data.Config.Props;

using Newtonsoft.Json;

namespace Ktisis.Data.Serialization;

public static class PropsReader {
	public static PropSchema ReadStream(Stream stream) {
		var schema = new PropSchema();
		schema.Props.AddRange(DeserializeProps(stream));
		return schema;
	}

	private static List<PropEntry> DeserializeProps(Stream stream) {
		var serializer = new JsonSerializer();
		var streamReader = new StreamReader(stream, new UTF8Encoding());
		var result = new List<PropEntry>();

		using (var reader = new JsonTextReader(streamReader)) {
			reader.CloseInput = false;
			reader.SupportMultipleContent = true;
			while (reader.Read()) {
				result.Add(serializer.Deserialize<PropEntry>(reader)!);
			}
		}

		return result;
	}
}
