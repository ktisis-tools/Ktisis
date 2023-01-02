using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface.Widgets {
	internal class Numbers {
		internal static bool ColoredDragFloat(string label, ref float val, float speed, Vector4 color = default, float borderSize = 1f) {
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, borderSize);
			ImGui.PushStyleColor(ImGuiCol.Border, color);
			var result = ImGui.DragFloat(label, ref val, speed, 0, 0, "%.3f");
			ImGui.PopStyleColor();
			ImGui.PopStyleVar();
			return result;
		}

		internal static bool ColoredDragFloat3(string id, ref Vector3 vec, float speed, Vector4[] colors, float borderSize = 1f) {
			if (colors.Length < 3)
				return false;

			var result = false;
			result |= ColoredDragFloat($"##{id}X", ref vec.X, speed, colors[0], borderSize);
			ImGui.SameLine();
			result |= ColoredDragFloat($"##{id}Y", ref vec.Y, speed, colors[1], borderSize);
			ImGui.SameLine();
			result |= ColoredDragFloat($"##{id}Z", ref vec.Z, speed, colors[2], borderSize);
			return result;
		}
	}
}