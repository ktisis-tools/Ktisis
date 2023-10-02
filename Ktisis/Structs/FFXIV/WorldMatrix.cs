using Dalamud.Interface;

using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Utility;

namespace Ktisis.Structs.FFXIV {
	[StructLayout(LayoutKind.Explicit, Size = 0x1FC)]
	public partial struct WorldMatrix {
		[FieldOffset(0x1B4)] public Matrix4x4 Matrix;

		[FieldOffset(0x1F4)] public float Width;
		[FieldOffset(0x1F8)] public float Height;

		public bool WorldToScreen(Vector3 v, out Vector2 pos2d) {
			var result = WorldToScreenDepth(v, out Vector3 pos);
			pos2d = new Vector2(pos.X, pos.Y);
			return result;
		}

		public bool WorldToScreenDepth(Vector3 v, out Vector3 pos2d) {
			var m = Matrix;

			var windowPos = ImGuiHelpers.MainViewport.Pos;

			float x = (m.M11 * v.X) + (m.M21 * v.Y) + (m.M31 * v.Z) + m.M41;
			float y = (m.M12 * v.X) + (m.M22 * v.Y) + (m.M32 * v.Z) + m.M42;
			float w = (m.M14 * v.X) + (m.M24 * v.Y) + (m.M34 * v.Z) + m.M44;

			float camX = (Width / 2f);
			float camY = (Height / 2f);

			pos2d = new Vector3(
				camX + (camX * x / w) + windowPos.X,
				camY - (camY * y / w) + windowPos.Y,
				w
			);

			return w > 0.001f;
		}
	}
}
