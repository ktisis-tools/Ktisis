using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Actions;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Actions;
using Ktisis.Localization;

namespace Ktisis.Interface.Components.Config;

// Uses Caraxi's item hotkeys as reference - thank you
// https://github.com/Caraxi/SimpleTweaksPlugin/blob/257ca7cf4105784abf0af720654dac4345f4a619/Tweaks/Tooltips/ItemHotkeys.cs#L116

[Transient]
public class ActionKeybindEditor {
	private readonly ActionService _actions;
	private readonly LocaleManager _locale;
	
	public ActionKeybindEditor(
		ActionService actions,
		LocaleManager locale
	) {
		this._actions = actions;
		this._locale = locale;
	}
	
	// State

	private readonly List<KeyAction> Actions = new();
	
	public void Setup() {
		var actions = this._actions.GetBindable();
		this.Actions.Clear();
		this.Actions.AddRange(actions);
		this.SetEditing(null);
	}
	
	// Draw

	private readonly static Vector2 CellPadding = new(8, 8);

	public void Draw() {
		using var pad = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		using var frame = ImRaii.Child("##CfgStyleFrame", ImGui.GetContentRegionAvail(), false);
		if (!frame.Success) return;
		
		using var tablePad = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero);
		using var table = ImRaii.Table("##KeyActionTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders);
		if (!table.Success) return;

		if (!ImGui.IsWindowFocused())
			this.SetEditing(null);

		ImGui.TableSetupColumn("Keys");
		ImGui.TableSetupColumn("Action");
		
		foreach (var action in this.Actions)
			this.DrawAction(action);
	}

	private void DrawAction(KeyAction action) {
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		this.DrawKeybind(action.GetKeybind());
		ImGui.TableNextColumn();
		
		var name = this._locale.Translate($"actions.{action.GetName()}");
		var cursor = ImGui.GetCursorPos() + CellPadding;
		ImGui.SetCursorPos(cursor);
		ImGui.Text(name);
	}

	private void DrawKeybind(ActionKeybind keybind) {
		using var _ = ImRaii.PushId($"Keybind_{keybind.GetHashCode():X}");
		
		using var round = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0.0f);

		var isActive = this.Editing == keybind;
		var activeColor = ImGui.GetColorU32(ImGuiCol.FrameBgActive);
		using var color = ImRaii.PushColor(ImGuiCol.Button, isActive ? activeColor : 0);
		using var colorActive = ImRaii.PushColor(ImGuiCol.ButtonActive, activeColor);
		using var colorHover = ImRaii.PushColor(ImGuiCol.ButtonHovered, isActive ? activeColor: ImGui.GetColorU32(ImGuiCol.FrameBgHovered));

		var width = ImGui.GetColumnWidth();
		var size = new Vector2(width, ImGui.GetFrameHeightWithSpacing()) + CellPadding;
		if (isActive) {
			var spacing = ImGui.GetStyle().ItemSpacing;
			ImGui.SetCursorPos(ImGui.GetCursorPos() + CellPadding);
			ImGui.SetNextItemWidth(width - CellPadding.X - spacing.X);
			this.EditKeybind(keybind);
			ImGui.Dummy(CellPadding - spacing);
		} else if (ImGui.Button(keybind.Combo.GetShortcutString(), size)) {
			this.SetEditing(keybind);
		}

		if (this.Editing != null && this.Editing != keybind && ImGui.IsItemFocused())
			this.SetEditing(null);
	}
	
	// Editing

	private ActionKeybind? Editing;
	private KeyCombo? KeyCombo;
	private readonly List<VirtualKey> KeysHandled = new();

	private void SetEditing(ActionKeybind? keybind) {
		this.FinishEdit();
		this.Editing = keybind;
		this.KeyCombo = null;
		this.KeysHandled.Clear();
	}

	private void FinishEdit() {
		if (this.Editing == null || this.KeyCombo == null) return;
		if (this.KeyCombo.Key != VirtualKey.NO_KEY)
			this.Editing.Combo = this.KeyCombo;
		Ktisis.Log.Info($"Applying edit ({this.KeyCombo.GetShortcutString()})");
	}

	private void EditKeybind(ActionKeybind keybind) {
		using var _ = ImRaii.PushId(keybind.GetHashCode());
		using var bg = ImRaii.PushColor(ImGuiCol.TextSelectedBg, 0);

		this.KeyCombo ??= new KeyCombo();

		var keys = KeyHelpers.GetKeysDown().Except(this.KeysHandled).ToList();
		this.KeysHandled.AddRange(keys);
		foreach (var key in keys) {
			if (key == VirtualKey.RETURN) {
				this.SetEditing(null);
				return;
			}
			
			if (this.KeyCombo.Key == VirtualKey.NO_KEY) {
				this.KeyCombo.Key = key;
			} else if (KeyHelpers.IsModifierKey(key) && !KeyHelpers.IsModifierKey(this.KeyCombo.Key)) {
				this.KeyCombo.AddModifier(key);
			} else {
				var prev = this.KeyCombo.Key;
				this.KeyCombo.Key = key;
				this.KeyCombo.AddModifier(prev);
			}
		}
		
		var text = this.KeyCombo.GetShortcutString();
		ImGui.InputText("##EditKeybind", ref text, 256, ImGuiInputTextFlags.ReadOnly & ~ImGuiInputTextFlags.AutoSelectAll);
		ImGui.SetKeyboardFocusHere(-1);
	}
}
