using Dalamud.Interface.Windowing;

namespace Ktisis.Extensions;

public static class WindowExtensions {
	public static void Open(this Window window) => window.IsOpen = true;
}
