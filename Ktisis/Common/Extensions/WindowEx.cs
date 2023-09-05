using Dalamud.Interface.Windowing;

namespace Ktisis.Common.Extensions; 

public static class WindowEx {
	public static void Open(this Window window) => window.IsOpen = true;

	public static void Close(this Window window) => window.IsOpen = false;
}
