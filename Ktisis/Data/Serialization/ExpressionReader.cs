using System.IO;

using Ktisis.Data.Expressions;
using Ktisis.Data.Json;

namespace Ktisis.Data.Serialization;

public class ExpressionReader {
	private readonly JsonFileSerializer _json = new();
	
	private readonly ExpressionsSchema _schema = new();

	public void ReadEntry(ushort raceSexId, Stream stream) {
		var entry = this._json.Deserialize<ExpressionsSchemaFile>(stream);
		if (entry == null) {
			Ktisis.Log.Warning($"Failed to deserialize manifest for {raceSexId}!");
			return;
		}
		this._schema.AddEntry(raceSexId, entry);
	}
	
	public ExpressionsSchema GetResult() => this._schema;
}
