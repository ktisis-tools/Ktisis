using System.Runtime.InteropServices;

namespace Ktisis.Structs.Common;

[StructLayout(LayoutKind.Sequential)]
public struct ObjectUnion {
	public unsafe nint** __vfTable;
	public nint Data;

	public nint GetObjectPointer() => (this.Data & 1) != 0 ? this.Data & ~7 : nint.Zero;
	public short GetObjectIndex() => (this.Data & 4) != 0 ? (short)(this.Data >> 3) : (short)(-1);
}
