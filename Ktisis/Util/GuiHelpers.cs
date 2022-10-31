using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using FFXIVClientStructs.Havok;

using Ktisis.Structs.Bones;

namespace Ktisis.Util
{
	internal class GuiHelpers {
		public static bool IconButtonHoldConfirm(FontAwesomeIcon icon, string tooltip, bool isHoldingKey) {
			if (!isHoldingKey) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().DisabledAlpha);
			bool accepting = ImGuiComponents.IconButton(icon);
			if (!isHoldingKey) ImGui.PopStyleVar();

			Tooltip(tooltip);

			return accepting && isHoldingKey;
		}

		public static bool IconButtonTooltip(FontAwesomeIcon icon, string tooltip) {
			bool accepting = ImGuiComponents.IconButton(icon);
			Tooltip(tooltip);
			return accepting;
		}

		public static void Icon(FontAwesomeIcon icon, bool enabled = true, Vector4? color = null) {
			string iconText = icon.ToIconString() ?? "";
			int num = 0;
			if (color.HasValue) {
				ImGui.PushStyleColor(ImGuiCol.Button, color.Value);
				num++;
			}

			ImGui.PushFont(UiBuilder.IconFont);
			if (enabled) ImGui.Text(iconText);
			else ImGui.TextDisabled(iconText);
			ImGui.PopFont();
			if (num > 0) {
				ImGui.PopStyleColor(num);
			}
		}
		public static Vector2 CalcIconSize(FontAwesomeIcon icon) {
			ImGui.PushFont(UiBuilder.IconFont);
			var size = ImGui.CalcTextSize(icon.ToIconString());
			ImGui.PopFont();
			return size;
		}
		public static void IconTooltip(FontAwesomeIcon icon, string tooltip, bool enabled = true, Vector4? color = null) {
			Icon(icon, enabled, color);
			Tooltip(tooltip);
		}


		public static void Tooltip(string text) {
			if (ImGui.IsItemHovered()) {
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}
		// Copy from Dalamud's ToggleButton but with colorizable circle
		public static bool ToggleButton(string id, ref bool v, Vector4 circleColor) {
			RangeAccessor<Vector4> colors = ImGui.GetStyle().Colors;
			Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
			ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
			float frameHeight = ImGui.GetFrameHeight();
			float num = frameHeight * 1.55f;
			float num2 = frameHeight * 0.5f;
			bool result = false;
			ImGui.InvisibleButton(id, new Vector2(num, frameHeight));
			if (ImGui.IsItemClicked()) {
				v = !v;
				result = true;
			}

			if (ImGui.IsItemHovered()) {
				windowDrawList.AddRectFilled(cursorScreenPos, new Vector2(cursorScreenPos.X + num, cursorScreenPos.Y + frameHeight), ImGui.GetColorU32((!v) ? colors[23] : new Vector4(0.78f, 0.78f, 0.78f, 1f)), frameHeight * 0.5f);
			} else {
				windowDrawList.AddRectFilled(cursorScreenPos, new Vector2(cursorScreenPos.X + num, cursorScreenPos.Y + frameHeight), ImGui.GetColorU32((!v) ? (colors[21] * 0.6f) : new Vector4(0.35f, 0.35f, 0.35f, 1f)), frameHeight * 0.5f);
			}

			windowDrawList.AddCircleFilled(new Vector2(cursorScreenPos.X + num2 + (float)(v ? 1 : 0) * (num - num2 * 2f), cursorScreenPos.Y + num2), num2 - 1.5f, ImGui.ColorConvertFloat4ToU32(circleColor));
			return result;
		}
		public static bool DrawBoneNode(Bone bone, ImGuiTreeNodeFlags flag, System.Action? executeIfClicked = null) {
			bool show = ImGui.TreeNodeEx(bone.UniqueId, flag, bone.LocaleName);

			var rectMin = ImGui.GetItemRectMin() + new Vector2(ImGui.GetTreeNodeToLabelSpacing(), 0);
			var rectMax = ImGui.GetItemRectMax();

			var mousePos = ImGui.GetMousePos();
			if (
				ImGui.IsMouseClicked(ImGuiMouseButton.Left)
				&& mousePos.X > rectMin.X && mousePos.X < rectMax.X
				&& mousePos.Y > rectMin.Y && mousePos.Y < rectMax.Y
			) {
				executeIfClicked?.Invoke();
			}
			return show;
		}

		public static void TextRight(string text, float offset = 0) {
			offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
			ImGui.TextUnformatted(text);
		}
		public static void TextCentered(string text) {
			var windowWidth = ImGui.GetWindowSize().X;
			var textWidth = ImGui.CalcTextSize(text).X;

			ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
			ImGui.Text(text);
		}

		public static void ButtonChangeOperation(OPERATION operation, FontAwesomeIcon icon) {
			var isCurrentOperation = Ktisis.Configuration.GizmoOp == operation;
			if (isCurrentOperation) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
			if (ImGuiComponents.IconButton(icon))
				Ktisis.Configuration.GizmoOp = operation;
			if (isCurrentOperation) ImGui.PopStyleColor();

			string help = "";
			if (isCurrentOperation)
				help += "Current gizmo operation is ";
			else
				help += "Change gizmo operation to ";

			if(operation == OPERATION.TRANSLATE) help += "Position";
			if(operation == OPERATION.ROTATE) help += "Rotation";
			if(operation == OPERATION.SCALE) help += "Scale";
			if(operation == OPERATION.UNIVERSAL) help += "Universal";

			Tooltip(help+".");
		}

		public static unsafe void AnimationControls(hkaDefaultAnimationControl* control) {
			var duration = control->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration;
			var durationLimit = duration - 0.05f;

			if (control->hkaAnimationControl.LocalTime >= durationLimit)
				control->hkaAnimationControl.LocalTime = 0f;

			ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.CalcTextSize("Speed").X + ImGui.GetFontSize() * .25f));
			ImGui.SliderFloat("Seek", ref control->hkaAnimationControl.LocalTime, 0, durationLimit);
			ImGui.SliderFloat("Speed", ref control->PlaybackSpeed, 0f, 0.999f);
			ImGui.PopItemWidth();
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
		public enum HoverPopupWindowFlags {
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
				Func<dynamic, bool, (bool, bool)> drawBeforeLine, // Parameters: dynamic item, bool isActive. Returns bool isSelected, bool Focus.
				Action<dynamic> onSelect,
				Action onClose,
				ref string inputSearch,
				string windowLabel = "",
				string listLabel = "",
				string searchBarLabel = "##search",
				string searchBarHint = "Search...",
				float minWidth = 400,
				int columns = 12
		) {
			PreviousOnClose ??= onClose;
			if (onClose != PreviousOnClose) {
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
			if (HoverPopupWindowIsBegan) {
				HoverPopupWindowFocus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();
				ImGui.PushItemWidth(minWidth);
				if (flags.HasFlag(HoverPopupWindowFlags.SearchBar))
					HoverPopupWindowSearchBarValidated = ImGui.InputTextWithHint(searchBarLabel, searchBarHint, ref inputSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);

				if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					ImGui.SetKeyboardFocusHere(flags.HasFlag(HoverPopupWindowFlags.SearchBar) ? -1 : 0); // TODO: verify the keyboarf focus behaviour when searchbar is disabled

				if (flags.HasFlag(HoverPopupWindowFlags.SelectorList))
					ImGui.BeginListBox(listLabel, new Vector2(-1, 300));
				// box has began

				if (flags.HasFlag(HoverPopupWindowFlags.Header)) {
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

				foreach (var i in enumerable) {
					bool selecting = false;
					bool isCurrentActive = HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey;

					var drawnLineTurpe = drawBeforeLine(i, isCurrentActive);
					HoverPopupWindowFocus |= ImGui.IsItemFocused();
					selecting |= drawnLineTurpe.Item1;
					HoverPopupWindowFocus |= drawnLineTurpe.Item2;

					if (!isOneSelected) {
						if (flags.HasFlag(HoverPopupWindowFlags.TwoDimenssion)) {
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) - 1 && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) + 1 && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) - HoverPopupWindowFastScrollLineJump && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey) + HoverPopupWindowFastScrollLineJump && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseLeft) && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey) - 1 && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey);
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseRight) && ColFromKey(HoverPopupWindowIndexKey) == ColFromKey(HoverPopupWindowLastSelectedItemKey) + 1 && RowFromKey(HoverPopupWindowIndexKey) == RowFromKey(HoverPopupWindowLastSelectedItemKey);
						} else {
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey - 1;
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey + 1;
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey - HoverPopupWindowFastScrollLineJump;
							selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && HoverPopupWindowIndexKey == HoverPopupWindowLastSelectedItemKey + HoverPopupWindowFastScrollLineJump;
						}
						selecting |= HoverPopupWindowSearchBarValidated;
					}

					if (selecting) {
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

				if ((!flags.HasFlag(HoverPopupWindowFlags.StayWhenLoseFocus) && !HoverPopupWindowFocus) || ImGui.IsKeyPressed(ImGuiKey.Escape)) {
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

		public static float CalcContrastRatio(uint background, uint foreground) {
			// https://github.com/ocornut/imgui/issues/3798
			float sa0 = ((background >> 24) & 0xFF);
			float sa1 = ((foreground >> 24) & 0xFF);
			float sr = 0.2126f / 255.0f;
			float sg = 0.7152f / 255.0f;
			float sb = 0.0722f / 255.0f;
			float contrastRatio =
				(sr * sa0 * ((background >> 16) & 0xFF) +
					sg * sa0 * ((background >> 8) & 0xFF) +
					sb * sa0 * ((background >> 0) & 0xFF) + 0.05f) /
				(sr * sa1 * ((foreground >> 16) & 0xFF) +
					sg * sa1 * ((foreground >> 8) & 0xFF) +
					sb * sa1 * ((foreground >> 0) & 0xFF) + 0.05f);
			if (contrastRatio < 1.0f)
				return 1.0f / contrastRatio;
			return contrastRatio;
		}
	}
}