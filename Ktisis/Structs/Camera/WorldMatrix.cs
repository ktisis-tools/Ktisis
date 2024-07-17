using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Utility;

namespace Ktisis.Structs.Camera;

[StructLayout(LayoutKind.Explicit, Size = 0x1FC)]
public struct WorldMatrix {
	[FieldOffset(0x1B4)] public Matrix4x4 Matrix;

	[FieldOffset(0x1F4)] public float Width;
	[FieldOffset(0x1F8)] public float Height;
	
	public bool WorldToScreenDepth(Vector3 v, out Vector3 screenPos) {
		var m = this.Matrix;

		var x = (m.M11 * v.X) + (m.M21 * v.Y) + (m.M31 * v.Z) + m.M41;
		var y = (m.M12 * v.X) + (m.M22 * v.Y) + (m.M32 * v.Z) + m.M42;
		var w = (m.M14 * v.X) + (m.M24 * v.Y) + (m.M34 * v.Z) + m.M44;

		var view = ImGuiHelpers.MainViewport;
		
		var camX = (view.Size.X / 2f);
		var camY = (view.Size.Y / 2f);
		screenPos = new Vector3(
			camX + (camX * x / w) + view.Pos.X,
			camY - (camY * y / w) + view.Pos.Y,
			w
		);

		return w > 0.001f;
	}
}
