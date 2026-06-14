using System.IO;
using System.Text;

using Ktisis.Editor.Expressions.Data;

using Newtonsoft.Json;

namespace Ktisis.Data.Serialization;

public static class ActionUnitReader {
	public static ActionUnitCatalog ReadStream(Stream stream) {
		using var streamReader = new StreamReader(stream, new UTF8Encoding());
		var serializer = new JsonSerializer();
		using var reader = new JsonTextReader(streamReader);
		return serializer.Deserialize<ActionUnitCatalog>(reader) ?? new ActionUnitCatalog();
	}
}
