using Dalamud.Interface.Windowing;

namespace Ktisis.Common.Extensions;

public static class WindowExtensions {
	public static void Open(this Window window) => window.IsOpen = true;
}
