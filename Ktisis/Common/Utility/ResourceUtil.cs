using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ktisis.Common.Utility;

public static class ResourceUtil {
	public static Stream GetManifestResource(string path) {
		var assembly = Assembly.GetExecutingAssembly();
		var stream = assembly.GetManifestResourceStream(path);
		if (stream == null)
			throw new FileNotFoundException(path);
		return stream;
	}

	public static IEnumerable<string> GetResourcesInNamespace(string path) {
		var assembly = Assembly.GetExecutingAssembly();
		return assembly.GetManifestResourceNames()
			.Where(res => res.StartsWith(path));
	}
}
