using System.Runtime.InteropServices;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit, Size = 0x18)]
public struct SchedulerState {
	[FieldOffset(0)] public unsafe nint* __vfTable;
}
