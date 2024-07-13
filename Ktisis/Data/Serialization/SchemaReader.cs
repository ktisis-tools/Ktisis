using Ktisis.Common.Utility;
using Ktisis.Data.Config.Sections;

namespace Ktisis.Data.Serialization;

public static class SchemaReader {
	// Categories
	
	private const string CategorySchemaPath = "Data.Schema.Categories.xml";

	public static CategoryConfig ReadCategories() {
		var stream = ResourceUtil.GetManifestResource(CategorySchemaPath);
		return CategoryReader.ReadStream(stream);
	}
}
