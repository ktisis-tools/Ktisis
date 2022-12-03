using System.Diagnostics;

namespace Ktisis.Helpers {
	internal static class Common {
		// From SimpleTweaks - Thanks Caraxi
		internal static void OpenBrowser(string url)
			=> Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
	}
}