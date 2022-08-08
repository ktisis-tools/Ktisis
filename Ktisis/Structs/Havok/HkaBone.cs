using System;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Havok {
	[StructLayout(LayoutKind.Explicit, Size = 0x10)]
	public unsafe struct HkaBone {
		[FieldOffset(0)]
		char* NamePtr;

		public string? Name {
			get => Marshal.PtrToStringAnsi((IntPtr)NamePtr);
			set => throw new NotImplementedException();
		}
	}
}