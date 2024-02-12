using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Characters.Types;

namespace Ktisis.Interface.Components.Chara.Popup;

public class ParamColorSelectPopup {
	private string PopupId => $"##ColorSelect_{this.GetHashCode():X}";

	private bool _isOpening;
	private bool _isOpen;
	
	private CustomizeIndex Index = 0;
	
	private bool IsAlpha;
	private Vector4[] Colors = [];
	
	public void Open(CustomizeIndex index, uint[] colors) {
		this._isOpening = true;
		
		this.Index = index;

		this.IsAlpha = colors.Length == 0x80;
		this.Colors = colors.Take(this.IsAlpha ? 0x60 : colors.Length)
			.Select(ImGui.ColorConvertU32ToFloat4)
			.ToArray();
	}

	public void Draw(ICustomizeEditor editor) {
		if (this._isOpening) {
			this._isOpening = false;
			ImGui.OpenPopup(this.PopupId);
		}

		if (!ImGui.IsPopupOpen(this.PopupId)) return;
		
		using var _popup = ImRaii.Popup(this.PopupId, ImGuiWindowFlags.AlwaysAutoResize);
		if (!_popup.Success) {
			if (this._isOpen)
				this.OnClose();
			return;
		}
		this._isOpen = true;

		var current = editor.GetCustomization(this.Index);
		if (this.IsAlpha) {
			this.DrawAlphaToggle(editor, current);
			ImGui.Spacing();
		}
		this.DrawColorInput(editor, current);
		ImGui.Spacing();
		this.DrawColorTable(editor, current);
	}

	private void DrawColorInput(ICustomizeEditor editor, byte current) {
		ImGui.SetNextItemWidth(ImGui.GetFrameHeight() * 8);
		var intValue = current & ~0x80;
		if (ImGui.InputInt($"##Input_{this.Index}", ref intValue))
			this.SetColor(editor, current, (byte)intValue);
	}

	private void DrawColorTable(ICustomizeEditor editor, byte current) {
		using var _space = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
		using var _round = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
		
		for (var i = 0; i < this.Colors.Length; i++) {
			if (i % 8 != 0) ImGui.SameLine();

			var color = this.Colors[i];
			if (ImGui.ColorButton($"{i}##{this.Index}", color, ImGuiColorEditFlags.DisplayMask | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar))
				this.SetColor(editor, current, (byte)i);
		}
	}

	private void DrawAlphaToggle(ICustomizeEditor editor, byte current) {
		var active = (current & 0x80) != 0;
		if (ImGui.Checkbox("Transparency", ref active))
			editor.SetCustomization(this.Index, (byte)(current ^ 0x80));
	}

	private void SetColor(ICustomizeEditor editor, byte current, byte value) {
		if (this.IsAlpha)
			value |= (byte)(current & 0x80);
		if (this.Index is CustomizeIndex.EyeColor)
			editor.SetEyeColor(value);
		else
			editor.SetCustomization(this.Index, value);
	}

	private void OnClose() {
		this.Colors = [];
	}
}
