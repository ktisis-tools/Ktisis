using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Context;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class CameraSelector {
	private readonly EditorUi _ui;
	
	public CameraSelector(
		EditorUi ui
	) {
		this._ui = ui;
	}

	public void Draw(IEditorContext context) {
		using var _id = ImRaii.PushId("##CameraSelect");
		
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (Buttons.CalcSize() + spacing) * 3);
		this.DrawSelector(context.Cameras);
		
		ImGui.SameLine(0, spacing);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Plus, "Create new camera"))
			context.Cameras.Create();

		ImGui.SameLine(0, spacing);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.PencilAlt, "Edit camera"))
			this._ui.OpenCameraWindow(context);
		
		ImGui.SameLine(0, spacing);
		this.DrawFreecamToggle(context);
	}

	private void DrawFreecamToggle(IEditorContext context) {
		var isFreecam = context.Cameras.IsWorkCameraActive;
		using var _bgCol = ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isFreecam);
		using var _iconCol = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text).SetAlpha(0xCF), !isFreecam);
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Camera, "Toggle work camera"))
			context.Cameras.ToggleWorkCameraMode();
	}
	
	// Selector

	private bool _isOpen;
	private float _lastScroll;

	private void DrawSelector(ICameraManager manager) {
		var current = manager.Current;

		var combo = ImGui.BeginCombo("##CameraSelectList", current?.Name ?? "INVALID");
		if (combo) {
			// Restore last scroll position
			if (!this._isOpen && this._lastScroll > 0.0f)
				ImGui.SetScrollY(this._lastScroll);
			// Display cameras
			foreach (var camera in manager.GetCameras()) {
				if (ImGui.Selectable(camera.Name, camera == current))
					manager.SetCurrent(camera);
			}
			// Save last scroll position
			this._lastScroll = ImGui.GetScrollY();
			// End combo
			ImGui.EndCombo();
		}
		this._isOpen = combo;
	}
}
