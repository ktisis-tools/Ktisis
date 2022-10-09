using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Helpers;
using Ktisis.Structs;
using Ktisis.Structs.Actor;

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
		public static bool DrawBoneNode(string? str_id, ImGuiTreeNodeFlags flag, string fmt, System.Action? executeIfClicked = null)
		{
			bool show = ImGui.TreeNodeEx(str_id, flag, fmt);

			var rectMin = ImGui.GetItemRectMin() + new Vector2(ImGui.GetTreeNodeToLabelSpacing(), 0);
			var rectMax = ImGui.GetItemRectMax();

			var mousePos = ImGui.GetMousePos();
			if (
				ImGui.IsMouseClicked(ImGuiMouseButton.Left)
				&& mousePos.X > rectMin.X && mousePos.X < rectMax.X
				&& mousePos.Y > rectMin.Y && mousePos.Y < rectMax.Y
			)
			{
				executeIfClicked?.Invoke();
			}
			return show;
		}
		public static unsafe bool CoordinatesTable(ActorModel* actorModel, System.Action? doAfter = null)
		{
			bool active = false;
			active |= ImGui.DragFloat3("Position", ref actorModel->Position, 0.005f);
			active |= GuiHelpers.DragQuatIntoEuler("Rotation", ref actorModel->Rotation, 0.1f);
			active |= ImGui.DragFloat3("Scale", ref actorModel->Scale, 0.005f);

			doAfter?.Invoke();
			return active;
		}
		public static bool CoordinatesTable(Transform transform, System.Action? doAfter = null)
		{
			bool active = false;
			active |= GuiHelpers.DragVec4intoVec3("Position", ref transform.Position, 0.0001f);
			active |= GuiHelpers.DragQuatIntoEuler("Rotation", ref transform.Rotation, 0.1f);
			active |= GuiHelpers.DragVec4intoVec3("Scale", ref transform.Scale, 0.01f);

			doAfter?.Invoke();
			return active;
		}

		public static void TextCentered(string text)
		{
			var windowWidth = ImGui.GetWindowSize().X;
			var textWidth = ImGui.CalcTextSize(text).X;

			ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
			ImGui.Text(text);
		}
	}
}
