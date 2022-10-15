using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Helpers;
using Ktisis.Structs;
using Ktisis.Structs.Actor;
using System.Collections.Generic;
using System;
using System.Linq;
using FFXIVClientStructs.Havok;

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
		
		public static unsafe void AnimationControls(hkaDefaultAnimationControl* control)
		{
			var duration = control->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration;
			var durationLimit = duration - 0.05f;
			
			if (control->hkaAnimationControl.LocalTime >= durationLimit)
				control->hkaAnimationControl.LocalTime = 0f;
        
			ImGui.SliderFloat("Seek", ref control->hkaAnimationControl.LocalTime, 0, durationLimit);
			ImGui.SliderFloat("Speed", ref control->PlaybackSpeed, 0f, 0.999f);
		}





		// HoverPopupWindow Method
		// Constants
		private const ImGuiKey KeyBindBrowseUp = ImGuiKey.UpArrow;
		private const ImGuiKey KeyBindBrowseDown = ImGuiKey.DownArrow;
		private const ImGuiKey KeyBindBrowseUpFast = ImGuiKey.PageUp;
		private const ImGuiKey KeyBindBrowseDownFast = ImGuiKey.PageDown;
		private const int HoverPopupWindowFastScrollLineJump = 8; // number of lines on the screen?

		// Properties
		private static Vector2 HoverPopupWindowSelectPos = Vector2.Zero;
		private static bool HoverPopupWindowIsBegan = false;
		private static bool HoverPopupWindowFocus = false;
		private static bool HoverPopupWindowSearchBarValidated = false;
		private static int HoverPopupWindowLastSelectedItemKey = 0;


		public enum HoverPopupWindowFlags
		{
			None,
			SelectorList,
			SearchBar,
		}

		public static void HoverPopupWindow(
			HoverPopupWindowFlags flags,
			IEnumerable<dynamic> enumerable,
			Func<dynamic, bool> drawBeforeLine,
			Func<dynamic, string> lineLabel,
			Func<dynamic, bool> drawAfterLine,
			Action<dynamic> onSelect,
			Action onClose,
			ref string InputSearch,
			string windowLabel = "",
			string listLabel = "",
			string searchBarLabel = "##search",
			string searchBarHint = "Search..."
			)
		{
			if (BeginHoverPopupWindow(flags, ref InputSearch, windowLabel, listLabel, searchBarLabel, searchBarHint))
			{
				if (flags.HasFlag(HoverPopupWindowFlags.SearchBar))
					if (InputSearch.Length > 0)
					{
						var inputSearch = InputSearch;
						enumerable = enumerable.Where(s => lineLabel(s).Contains(inputSearch, StringComparison.OrdinalIgnoreCase));
					}

				LoopHoverPopupWindow(flags, enumerable, drawBeforeLine, drawAfterLine, onSelect, lineLabel);
			}
			EndHoverPopupWindow(flags, onClose);
		}



		private static bool BeginHoverPopupWindow(HoverPopupWindowFlags flags, ref string InputSearch, string windowLabel, string listLabel, string searchBarLabel, string searchBarHint)
		{
			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			if(HoverPopupWindowSelectPos == Vector2.Zero) HoverPopupWindowSelectPos = ImGui.GetMousePos();
			ImGui.SetNextWindowPos(HoverPopupWindowSelectPos);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			HoverPopupWindowIsBegan = ImGui.Begin(windowLabel, ImGuiWindowFlags.NoDecoration);
			if (HoverPopupWindowIsBegan)
			{

				HoverPopupWindowFocus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();
				ImGui.PushItemWidth(400);
				if (flags.HasFlag(HoverPopupWindowFlags.SearchBar))
					HoverPopupWindowSearchBarValidated = ImGui.InputTextWithHint(searchBarLabel, searchBarHint, ref InputSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);

				if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					ImGui.SetKeyboardFocusHere(flags.HasFlag(HoverPopupWindowFlags.SearchBar) ? -1 : 0); // TODO: verify the keyboarf focus behaviour when searchbar is disabled

				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					ImGui.BeginListBox(listLabel, new Vector2(-1, 300));

			}
			return HoverPopupWindowIsBegan;
		}
		private static void EndHoverPopupWindow(HoverPopupWindowFlags flags, Action onClose)
		{
			if (HoverPopupWindowIsBegan)
			{
				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					ImGui.EndListBox();
				HoverPopupWindowFocus |= ImGui.IsItemActive();
				ImGui.PopItemWidth();

				if (!HoverPopupWindowFocus || ImGui.IsKeyPressed(ImGuiKey.Escape))
				{
					onClose();
					HoverPopupWindowSelectPos = Vector2.Zero;
				}
			}

			ImGui.End();
		}
		private static void LoopHoverPopupWindow(HoverPopupWindowFlags flags, IEnumerable<dynamic> enumerable, Func<dynamic, bool> drawBeforeLine, Func<dynamic, bool> drawAfterLine, Action<dynamic> onSelect, Func<dynamic, string> lineLabel)
		{
			if (!HoverPopupWindowIsBegan) return;

			int indexKey = 0;
			bool isOneSelected = false; // allows one selection per foreach
			if (HoverPopupWindowLastSelectedItemKey >= enumerable.Count()) HoverPopupWindowLastSelectedItemKey = enumerable.Count() - 1;

			foreach (var i in enumerable)
			{
				bool selecting = false;

				selecting |= drawBeforeLine(i);
				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					selecting |= ImGui.Selectable(lineLabel(i), indexKey == HoverPopupWindowLastSelectedItemKey);
				HoverPopupWindowFocus |= ImGui.IsItemFocused();
				selecting |= drawAfterLine(i);

				if (!isOneSelected)
				{
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && indexKey == HoverPopupWindowLastSelectedItemKey - 1;
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && indexKey == HoverPopupWindowLastSelectedItemKey + 1;
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && indexKey == HoverPopupWindowLastSelectedItemKey - HoverPopupWindowFastScrollLineJump;
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && indexKey == HoverPopupWindowLastSelectedItemKey + HoverPopupWindowFastScrollLineJump;
					selecting |= HoverPopupWindowSearchBarValidated;
				}

				if (selecting)
				{
					if (ImGui.IsKeyPressed(KeyBindBrowseUp) || ImGui.IsKeyPressed(KeyBindBrowseDown) || ImGui.IsKeyPressed(KeyBindBrowseUpFast) || ImGui.IsKeyPressed(KeyBindBrowseDownFast))
						ImGui.SetScrollY(ImGui.GetCursorPosY() - (ImGui.GetWindowHeight() / 2));

					onSelect(i);
					HoverPopupWindowLastSelectedItemKey = indexKey;
					isOneSelected = true;
				}
				HoverPopupWindowFocus |= ImGui.IsItemFocused();
				indexKey++;
			}
		}

	}
}
