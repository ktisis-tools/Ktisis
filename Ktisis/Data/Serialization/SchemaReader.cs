using Ktisis.Common.Utility;
using Ktisis.Data.Config.Pose2D;
using Ktisis.Data.Config.Props;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Expressions.Data;

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

	// Action Units (facial expressions), per race+gender+clan variant key
	// (e.g. "Miqote_Feminine_SeekerOfTheSun"). Returns null if no catalog is
	// embedded for that key.

	public static ActionUnitCatalog? ReadActionUnits(string key) {
		try {
			using var stream = ResourceUtil.GetManifestResource($"Data.Library.Expressions.{key}.json");
			return ActionUnitReader.ReadStream(stream);
		} catch (System.IO.FileNotFoundException) {
			return null;
		}
	}
}
