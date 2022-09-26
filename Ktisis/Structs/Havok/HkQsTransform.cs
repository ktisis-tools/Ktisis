using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Havok;

[StructLayout(LayoutKind.Sequential)]
public struct hkQsTransform
{
	public Vector4 Translation;
	public Quaternion Rotation;
	public Vector4 Scale;

	public override string ToString()
	{
		return $"({Translation}), ({Rotation}), ({Scale})";
	}
}