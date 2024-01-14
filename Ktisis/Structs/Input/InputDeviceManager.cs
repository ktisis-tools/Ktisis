using System.Runtime.InteropServices;

namespace Ktisis.Structs.Input;

[StructLayout(LayoutKind.Sequential)]
public struct InputDeviceManager {
	public unsafe void* Controller;
	public unsafe MouseDeviceData* Mouse;
	public unsafe KeyboardDeviceData* Keyboard;
}
