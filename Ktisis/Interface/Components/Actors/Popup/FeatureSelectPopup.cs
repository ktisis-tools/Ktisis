using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Ktisis.Editor.Characters.Make;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Actors.Popup;

public class FeatureSelectPopup {
	private readonly ITextureProvider _tex;

	public FeatureSelectPopup(ITextureProvider tex) {
		this._tex = tex;
	}
	
	private string PopupId => $"##FeatureSelect_{this.GetHashCode():X}";
	
	private MakeTypeFeature? Feature = null;

	private bool _isOpening;
	private bool _isOpen;

	public void Open(MakeTypeFeature feature) {
		this.Feature = feature;
		this._isOpening = true;
	}
	
	private const int MaxColumns = 6;
	private const int MaxRows = 3;
	private readonly static Vector2 ButtonSize = new(64, 64);

	public void Draw(ICustomizeEditor editor, ActorEntity actor) {
		if (this._isOpening) {
			this._isOpening = false;
			ImGui.OpenPopup(this.PopupId);
		}

		if (!ImGui.IsPopupOpen(this.PopupId)) return;
		
		var style = ImGui.GetStyle();
		ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(
			(ButtonSize.X + style.FramePadding.X * 2 + style.ItemSpacing.X) * MaxColumns + style.ItemSpacing.X + style.ScrollbarSize,
			(ButtonSize.Y + (style.FramePadding.X + style.ItemSpacing.Y) * 2 + UiBuilder.IconFont.FontSize) * MaxRows + style.WindowPadding.Y
		));
		
		using var _popup = ImRaii.Popup(this.PopupId, ImGuiWindowFlags.AlwaysAutoResize);
		if (!_popup.Success) {
			if (this._isOpen)
				this.OnClose();
			return;
		}
		this._isOpen = true;
		
		this.DrawParams(editor, actor);
	}

	private void DrawParams(ICustomizeEditor editor, ActorEntity actor) {
		if (this.Feature == null) return;
		
		using var _col = ImRaii.PushColor(ImGuiCol.Button, 0);

		var i = 0;
		foreach (var param in this.Feature.Params) {
			if (i++ % MaxColumns != 0 && i > 1) ImGui.SameLine();

			using var _group = ImRaii.Group();
			using var _id = ImRaii.PushId($"##Feature_{param.Value}_{i}");
			
			var icon = param.Graphic != 0 ? this._tex.GetIcon(param.Graphic) : null;
			icon ??= this._tex.GetIcon(61583);

			var totalSize = ButtonSize + ImGui.GetStyle().FramePadding * 2;
			
			bool button;
			if (icon != null)
				button = ImGui.ImageButton(icon.ImGuiHandle, ButtonSize);
			else
				button = ImGui.Button($"{param.Value}", totalSize);
			
			var label = param.Value.ToString();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (totalSize.X - ImGui.CalcTextSize(label).X) / 2);
			ImGui.Text(label);

			if (button)
				editor.SetCustomization(actor, this.Feature.Index, param.Value);
		}
	}

	private void OnClose() {
		this._isOpen = false;
		this.Feature = null;
	}
}
