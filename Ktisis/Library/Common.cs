using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace Ktisis.Library {
	internal class Common {
		// From SimpleTweaks - Thanks Caraxi
		internal static void OpenBrowser(string url)
			=> Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

		// Open assembly file
		internal static Stream GetAssemblyFile(string path) {
			var assembly = Assembly.GetExecutingAssembly();
			string assemblyName = assembly.GetName().Name!;

			path = $"{assemblyName}.{path}";

			Stream? stream = assembly.GetManifestResourceStream(path);
			if (stream == null)
				throw new FileNotFoundException(path);

			return stream;
		}
	}
}