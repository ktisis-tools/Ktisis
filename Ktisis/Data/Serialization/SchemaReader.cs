using Ktisis.Common.Utility;
using Ktisis.Data.Config.Gobos;
using Ktisis.Data.Config.Pose2D;
using Ktisis.Data.Config.Props;
using Ktisis.Data.Config.Sections;
using Ktisis.Data.Expressions;

namespace Ktisis.Data.Serialization;

public static class SchemaReader {
	// Categories
	
	private const string CategorySchemaPath = "Ktisis.Data.Schema.Categories.xml";

	public static CategoryConfig ReadCategories() {
		var stream = ResourceUtil.GetManifestResource(CategorySchemaPath);
		return CategoryReader.ReadStream(stream);
	}
	
	// Views

	private const string ViewSchemaPath = "Ktisis.Data.Schema.Views.xml";

	public static PoseViewSchema ReadPoseView() {
		var stream = ResourceUtil.GetManifestResource(ViewSchemaPath);
		return PoseViewReader.ReadStream(stream);
	}
	
	// Gobos

	private const string GoboSchemaPath = "Ktisis.Data.Library.gobos.csv";

	public static GoboSchema ReadGobos() {
		var stream = ResourceUtil.GetManifestResource(GoboSchemaPath);
		return GoboReader.ReadStream(stream);
	}

	// Props

	private const string PropSchemaPath = "Ktisis.Data.Library.props.json";

	public static PropSchema ReadProps() {
		var stream = ResourceUtil.GetManifestResource(PropSchemaPath);
		return PropsReader.ReadStream(stream);
	}
	
	// Expressions

	private const string ExpressionsSchemaPath = "Ktisis.Data.Library.Expressions";

	public static ExpressionsSchema ReadExpressions() {
		var reader = new ExpressionReader();

		var paths = ResourceUtil.GetResourcesInNamespace(ExpressionsSchemaPath);
		foreach (var path in paths) {
			var name = path[(ExpressionsSchemaPath.Length + 1)..];
			if (!ushort.TryParse(name.Split("_")[0], out var id))
				continue;
			var stream = ResourceUtil.GetManifestResource(path);
			reader.ReadEntry(id, stream);
		}
		
		return reader.GetResult();
	}
}
