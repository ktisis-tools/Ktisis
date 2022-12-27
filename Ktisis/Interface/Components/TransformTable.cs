using System.Numerics;

using ImGuiNET;

using ImGuizmoNET;

using Ktisis.Interface.Library;

namespace Ktisis.Interface.Components {
	public class TransformTable {
		public static Vector4 AxisColorX = new(1, 0.328f, 0.211f, 1);
		public static Vector4 AxisColorY = new(0.33f, 0.82f, 0, 1);
		public static Vector4 AxisColorZ = new(0, 0.33f, 1, 1);
		public static Vector4[] AxisColors => new Vector4[3] { AxisColorX, AxisColorY, AxisColorZ };

		public OPERATION Operations = OPERATION.UNIVERSAL;

		public TransformTable(OPERATION op = OPERATION.UNIVERSAL) {
			Operations = op;
		}

		public void Draw(Matrix4x4 matrix) {
			ImGui.PushItemWidth(ImGui.GetFontSize() * 4.5f);

			var _ = new Vector3();

			if (Operations.HasFlag(OPERATION.TRANSLATE)) {
				Draw(ref _, "TTPos", false);
			}

			if (Operations.HasFlag(OPERATION.ROTATE)) {
				Draw(ref _, "TTRot", false);
			}

			if (Operations.HasFlag(OPERATION.SCALEU)) {
				Draw(ref _, "TTScale", false);
			}

			ImGui.PopItemWidth();
		}

		public void Draw(ref Vector3 vec, string id = "TT", bool pop = true) {
			if (pop) ImGui.PushItemWidth(ImGui.GetFontSize() * 4.5f);
			Numbers.ColoredDragFloat3(id, ref vec, 1f, AxisColors);
			if (pop) ImGui.PopItemWidth();
		}
	}
}