using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Ktisis.Helpers;
using System.Numerics;

namespace Ktisis.Util
{
	internal class GuiHelpers
	{
		public static bool IconButtonHoldConfirm(FontAwesomeIcon icon, string tooltip, bool isHoldingKey)
		{
			if (!isHoldingKey) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
			bool accepting = ImGuiComponents.IconButton(icon);
			if (!isHoldingKey) ImGui.PopStyleVar();

			Tooltip(tooltip);

			return accepting && isHoldingKey;
		}
		public static bool IconButtonTooltip(FontAwesomeIcon icon, string tooltip)
		{
			bool accepting = ImGuiComponents.IconButton(icon);
			Tooltip(tooltip);
			return accepting;
		}
		public static void Tooltip(string text)
		{
			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}

		}

		public static bool DragVec4intoVec3(string label, ref Vector4 vector4, float speed = 0.1f)
		{
			Vector3 vector3 = new(vector4.X, vector4.Y, vector4.Z);
			bool modified = ImGui.DragFloat3(label, ref vector3, speed);
			vector4.X = vector3.X;
			vector4.Y = vector3.Y;
			vector4.Z = vector3.Z;
			return modified;
		}
		public static bool DragQuatIntoEuler(string label, ref Quaternion quaternion, float speed = 0.1f) {
			Vector3 euler = MathHelpers.ToEuler(quaternion);
			bool modified = ImGui.DragFloat3(label, ref euler, speed);
			quaternion = MathHelpers.ToQuaternion(euler);
			return modified;
		}
	}
}
