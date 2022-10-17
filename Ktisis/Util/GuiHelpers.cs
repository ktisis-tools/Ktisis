using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

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

		// HoverPopupWindow Method
		// Constants
		private const ImGuiKey KeyBindBrowseUp = ImGuiKey.UpArrow;
		private const ImGuiKey KeyBindBrowseDown = ImGuiKey.DownArrow;
		private const ImGuiKey KeyBindBrowseLeft = ImGuiKey.LeftArrow;
		private const ImGuiKey KeyBindBrowseRight = ImGuiKey.RightArrow;
		private const ImGuiKey KeyBindBrowseUpFast = ImGuiKey.PageUp;
		private const ImGuiKey KeyBindBrowseDownFast = ImGuiKey.PageDown;
		private const int HoverPopupWindowFastScrollLineJump = 8; // number of lines on the screen?

		// Properties
		private static Vector2 HoverPopupWindowSelectPos = Vector2.Zero;
		private static bool HoverPopupWindowIsBegan = false;
		private static bool HoverPopupWindowFocus = false;
		private static bool HoverPopupWindowSearchBarValidated = false;
		public static int HoverPopupWindowLastSelectedItemKey = 0;
		public static int HoverPopupWindowColumns = 1;
		private static Action? PreviousOnClose;
		public static int HoverPopupWindowIndexKey = 0;
		public static dynamic? HoverPopupWindowItemForHeader = null;

		[Flags]
		public enum HoverPopupWindowFlags
		{
			None = 0,
			SelectorList = 1,
			SearchBar = 2,
			Grabbable = 4,
			TwoDimenssion = 8,
			Header = 16,
			StayWhenLoseFocus = 32, // TODO: make it instanciable so we can have multiple
		}
		private static int RowFromKey(int key) => (int)Math.Floor((double)(key / HoverPopupWindowColumns));
		private static int ColFromKey(int key) => key % HoverPopupWindowColumns;
		private static int KeyFromRowCol(int row, int col) => (row * HoverPopupWindowColumns) + col;

		public static void HoverPopupWindow(
				HoverPopupWindowFlags flags,
				IEnumerable<dynamic> enumerable,
				Func<IEnumerable<dynamic>, string, IEnumerable<dynamic>> filter,
				Action<dynamic> header,
				Func<dynamic, bool, (bool,bool)> drawBeforeLine, // Parameters: dynamic item, bool isActive. Returns bool isSelected, bool Focus.
				Action<dynamic> onSelect,
				Action onClose,
				ref string inputSearch,
				string windowLabel = "",
				string listLabel = "",
				string searchBarLabel = "##search",
				string searchBarHint = "Search...",
				float minWidth = 400,
				int columns = 12
		)
		{
			PreviousOnClose ??= onClose;
			if (onClose != PreviousOnClose)
			{
				// for StayWhenLoseFocus, close
				PreviousOnClose();
				PreviousOnClose = onClose;
				HoverPopupWindowSelectPos = Vector2.Zero;
			}
			HoverPopupWindowColumns = columns;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			bool isNewPop = HoverPopupWindowSelectPos == Vector2.Zero;
			if (isNewPop) HoverPopupWindowSelectPos = ImGui.GetMousePos();
			if (!flags.HasFlag(HoverPopupWindowFlags.Grabbable) || isNewPop)
				ImGui.SetNextWindowPos(HoverPopupWindowSelectPos);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None;
			if (!flags.HasFlag(HoverPopupWindowFlags.Grabbable))
				windowFlags |= ImGuiWindowFlags.NoDecoration;

			HoverPopupWindowIsBegan = ImGui.Begin(windowLabel, windowFlags);
			if (HoverPopupWindowIsBegan)
			{
				HoverPopupWindowFocus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();
				ImGui.PushItemWidth(minWidth);
				if (flags.HasFlag(HoverPopupWindowFlags.SearchBar))
					HoverPopupWindowSearchBarValidated = ImGui.InputTextWithHint(searchBarLabel, searchBarHint, ref inputSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);

				if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					ImGui.SetKeyboardFocusHere(flags.HasFlag(HoverPopupWindowFlags.SearchBar) ? -1 : 0); // TODO: verify the keyboarf focus behaviour when searchbar is disabled

				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					ImGui.BeginListBox(listLabel, new Vector2(-1, 300));
				// box has began

				if (flags.HasFlag(HoverPopupWindowFlags.Header))
				{
					if (HoverPopupWindowItemForHeader != null)
						header(HoverPopupWindowItemForHeader);
					else
						ImGui.Text("");
				}

				if (flags.HasFlag(HoverPopupWindowFlags.SearchBar)) {
					if (inputSearch.Length > 0) {
						var inputSearch2 = inputSearch;
						enumerable = filter(enumerable, inputSearch2);
					}
				}

				HoverPopupWindowIndexKey = 0;
				bool isOneSelected = false; // allows one selection per foreach
				if (!flags.HasFlag(HoverPopupWindowFlags.TwoDimenssion))
					if (HoverPopupWindowLastSelectedItemKey >= enumerable.Count()) HoverPopupWindowLastSelectedItemKey = enumerable.Count() - 1;

				foreach (var i in enumerable)
				{
					bool selecting = false;
					bool isCurrentActive = HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey;

					var drawnLineTurpe = drawBeforeLine(i, isCurrentActive);
					HoverPopupWindowFocus |= ImGui.IsItemFocused();
					selecting |= drawnLineTurpe.Item1;
					HoverPopupWindowFocus |= drawnLineTurpe.Item2;

					if (!isOneSelected)
					{
						if (flags.HasFlag(HoverPopupWindowFlags.TwoDimenssion))
						{
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) - 1 && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) + 1 && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) - HoverPopupWindowFastScrollLineJump && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) + HoverPopupWindowFastScrollLineJump && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseLeft) && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey) - 1 && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseRight) && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey) + 1 && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey);
						}
						else
						{
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey - 1;
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey + 1;
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey - HoverPopupWindowFastScrollLineJump;
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey + HoverPopupWindowFastScrollLineJump;
						}
						selecting |= HoverPopupWindowSearchBarValidated;
					}

					if (selecting)
					{
						if (ImGui.IsKeyPressed(KeyBindBrowseUp) || ImGui.IsKeyPressed(KeyBindBrowseDown) || ImGui.IsKeyPressed(KeyBindBrowseUpFast) || ImGui.IsKeyPressed(KeyBindBrowseDownFast))
							ImGui.SetScrollY(ImGui.GetCursorPosY() - (ImGui.GetWindowHeight() / 2));

						onSelect(i);
						// assigning cache vars
						HoverPopupWindowLastSelectedItemKey = HoverPopupWindowIndexKey;
						isOneSelected = true;
						HoverPopupWindowItemForHeader = i;
					}
					HoverPopupWindowFocus |= ImGui.IsItemFocused();
					HoverPopupWindowIndexKey++;
				}


				// box has ended
				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					ImGui.EndListBox();
				HoverPopupWindowFocus |= ImGui.IsItemActive();
				ImGui.PopItemWidth();

				if ((!flags.HasFlag(HoverPopupWindowFlags.StayWhenLoseFocus) && !HoverPopupWindowFocus) || ImGui.IsKeyPressed(ImGuiKey.Escape))
				{
					onClose();

					// cleaning cache vars
					PreviousOnClose = null;
					HoverPopupWindowSelectPos = Vector2.Zero;
					HoverPopupWindowIndexKey = 0;
					HoverPopupWindowItemForHeader = null;
				}
			}

			ImGui.End();
		}
	}
}