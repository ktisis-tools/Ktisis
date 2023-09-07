using System.Runtime.InteropServices;

namespace Ktisis.Common.Utility; 

public static class IoHelpers {
	[DllImport("user32.dll")]
	public extern static bool SetCursorPos(int x, int y);
}
