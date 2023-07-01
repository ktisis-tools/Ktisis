using Ktisis.Core.Singletons;

namespace Ktisis.Data;

public class DataService : Service {
	// Schema

	public readonly SchemaReader Schema;

	// Ctor

	public DataService() {
		Schema = new SchemaReader();
	}
}
