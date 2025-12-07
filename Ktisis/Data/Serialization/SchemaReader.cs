using Ktisis.Common.Utility;
using Ktisis.Data.Config.Pose2D;
using Ktisis.Data.Config.Props;
using Ktisis.Data.Config.Sections;

namespace Ktisis.Data.Serialization;

public static class SchemaReader {
	// Categories
	
	private const string CategorySchemaPath = "Data.Schema.Categories.xml";

	public static CategoryConfig ReadCategories() {
		var stream = ResourceUtil.GetManifestResource(CategorySchemaPath);
		return CategoryReader.ReadStream(stream);
	}
	
	// Views

	private const string ViewSchemaPath = "Data.Schema.Views.xml";

	public static PoseViewSchema ReadPoseView() {
		var stream = ResourceUtil.GetManifestResource(ViewSchemaPath);
		return PoseViewReader.ReadStream(stream);
	}

	// Props

	private const string PropSchemaPath = "Data.Library.props.json";

	public static PropSchema ReadProps() {
		var stream = ResourceUtil.GetManifestResource(PropSchemaPath);
		return PropsReader.ReadStream(stream);
	}
}
