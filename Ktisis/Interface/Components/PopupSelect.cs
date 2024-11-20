using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

namespace Ktisis.Interface.Components {
	public static class PopupSelect {

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
		private static bool HoverPopupWindowFocus = false;
		private static bool HoverPopupWindowSearchBarValidated = false;
		public static int HoverPopupWindowLastSelectedItemKey = 0;
		public static int HoverPopupWindowColumns = 1;
		private static Action? PreviousOnClose;
		public static int HoverPopupWindowIndexKey = 0;
		public static object? HoverPopupWindowItemForHeader = null;

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


		public static void HoverPopupWindow<T>(
		HoverPopupWindowFlags flags,
		IEnumerable<T> enumerable,
		Func<IEnumerable<T>, string, IEnumerable<T>> filter,
		Func<T, bool, (bool, bool)> drawLine,
		Action<T> onSelect,
		Action onClose,
		ref string inputSearch,
		string windowLabel = "",
		string listLabel = "",
		string searchBarLabel = "##search",
		string searchBarHint = "Search...",
		float minWidth = 400,
		int columns = 12
		) => HoverPopupWindow(flags, enumerable, filter, (_) => { }, drawLine, onSelect, onClose, ref inputSearch, windowLabel, listLabel, searchBarLabel, searchBarHint, minWidth, columns);

		public static void HoverPopupWindow<T>(
				HoverPopupWindowFlags flags,
				IEnumerable<T> enumerable,
				Func<IEnumerable<T>, string, IEnumerable<T>> filter,
				Action<object> header,
				Func<T, bool, (bool, bool)> drawBeforeLine, // Parameters: T item, bool isActive. Returns bool isSelected, bool Focus.
				Action<T> onSelect,
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

			if (ImGui.Begin(windowLabel, windowFlags)) {
				HoverPopupWindowFocus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();
				ImGui.PushItemWidth(minWidth);
				if (flags.HasFlag(HoverPopupWindowFlags.SearchBar))
					HoverPopupWindowSearchBarValidated = ImGui.InputTextWithHint(searchBarLabel, searchBarHint, ref inputSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);
				ImGui.PopItemWidth();
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

	}
}
