using System.Numerics;

using ImGuiNET;
using Ktisis.ImGuizmo;

using Dalamud.Interface;

using Ktisis.Posing;
using Ktisis.Library;
using Ktisis.Library.Extensions;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.Components {
	public class TransformTable {
		public static Vector4 AxisColorX = new(1, 0.328f, 0.211f, 1);
		public static Vector4 AxisColorY = new(0.33f, 0.82f, 0, 1);
		public static Vector4 AxisColorZ = new(0, 0.33f, 1, 1);
		public static Vector4[] AxisColors => new Vector4[3] { AxisColorX, AxisColorY, AxisColorZ };

		public Operation Operations = Operation.UNIVERSAL;

		public TransformTable(Operation op = Operation.UNIVERSAL) {
			Operations = op;
		}

		public bool Draw(ref Transform trans) {
			var changed = false;

			Vector3 position = trans.Position;
			Vector3 euler = MathLib.ToEuler(trans.Rotation);
			Vector3 scale = trans.Scale;

			if (Operations.HasFlag(Operation.TRANSLATE)) {
				if (Draw(ref position, 0.0025f, "Pos")) {
					changed = true;
					trans.Position = position;
				}
				ImGui.SameLine();
				Icons.DrawIcon(FontAwesomeIcon.ArrowsAlt);
				Text.Tooltip("Position");
			}

			if (Operations.HasFlag(Operation.ROTATE)) {
				if (Draw(ref euler, 0.5f, "Rotate")) {
					changed = true;
					trans.Rotation = MathLib.ToQuaternion(euler);
				}
				ImGui.SameLine();
				Icons.DrawIcon(FontAwesomeIcon.Sync);
				Text.Tooltip("Rotation");
			}

			if (Operations.HasFlag(Operation.SCALE_U)) {
				if (Draw(ref scale, 0.0075f, "Scale")) {
					changed = true;
					trans.Scale = scale.ClampMin(0.001f);
				}
				ImGui.SameLine();
				Icons.DrawIcon(FontAwesomeIcon.ExpandAlt);
				Text.Tooltip("Scale");
			}

			return changed;
		}

		public bool Draw(ref Vector3 vec, float speed = 0.1f, string id = "") {
			ImGui.PushItemWidth(ImGui.GetFontSize() * 4.5f);
			var result = Numbers.ColoredDragFloat3($"TT{id}", ref vec, speed, AxisColors);
			ImGui.PopItemWidth();
			return result;
		}
	}
}