using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Havok;

[StructLayout(LayoutKind.Explicit)]
public struct CcdIkConstraint {
	// The start bone in this chain
	[FieldOffset(0)] public short m_startBone;

	// The end bone in this chain
	[FieldOffset(2)] public short m_endBone;

	// The target position for the end bone, in model space
	[FieldOffset(16)] public Vector4 m_targetMS;
}
