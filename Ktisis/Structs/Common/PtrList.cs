using System.Runtime.InteropServices;

namespace Ktisis.Structs.Common;

[StructLayout(LayoutKind.Sequential, Size = 0x10)]
public struct PtrList<T> where T : unmanaged {
	public unsafe T** Pointers;
	public ushort Capacity;
	public ushort Length;
}
