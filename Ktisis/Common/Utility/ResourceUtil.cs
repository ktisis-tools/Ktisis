using System.IO;
using System.Reflection;

namespace Ktisis.Common.Utility;

public static class ResourceUtil {
	public static Stream GetManifestResource(string path) {
		var assembly = Assembly.GetExecutingAssembly();
		var name = assembly.GetName().Name!;
		path = $"{name}.{path}";

		var stream = assembly.GetManifestResourceStream(path);
		if (stream == null)
			throw new FileNotFoundException(path);
		return stream;
	}
}
