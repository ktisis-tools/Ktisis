using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Context;

namespace Ktisis.Interface.Components.Workspace;

public class CameraSelector {
	private readonly IEditorContext _context;

	private ICameraManager Cameras => this._context.Cameras;
	
	public CameraSelector(
		IEditorContext context
	) {
		this._context = context;
	}

	public void Draw() {
		using var _ = ImRaii.PushId("##CameraSelect");
		
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (Buttons.CalcSize() + spacing) * 3);
		this.DrawSelector();
		
		ImGui.SameLine(0, spacing);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Plus, "Create new camera"))
			this.Cameras.Create();

		ImGui.SameLine(0, spacing);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.PencilAlt, "Edit camera"))
			this._context.Interface.OpenCameraWindow();
		
		ImGui.SameLine(0, spacing);
		this.DrawFreecamToggle();
	}

	private void DrawFreecamToggle() {
		var isFreecam = this.Cameras.IsWorkCameraActive;
		using var _bgCol = ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isFreecam);
		using var _iconCol = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text).SetAlpha(0xCF), !isFreecam);
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Camera, "Toggle work camera"))
			this.Cameras.ToggleWorkCameraMode();
	}
	
	// Selector

	private bool _isOpen;
	private float _lastScroll;

	private void DrawSelector() {
		using var _ = ImRaii.Disabled(this.Cameras.IsWorkCameraActive);
        
		var current = this.Cameras.Current;
		var combo = ImGui.BeginCombo("##CameraSelectList", current?.Name ?? "INVALID");
		if (combo) {
			// Restore last scroll position
			if (!this._isOpen && this._lastScroll > 0.0f)
				ImGui.SetScrollY(this._lastScroll);
			// Display cameras
			foreach (var camera in this.Cameras.GetCameras()) {
				if (ImGui.Selectable(camera.Name, camera == current))
					this.Cameras.SetCurrent(camera);
			}
			// Save last scroll position
			this._lastScroll = ImGui.GetScrollY();
			// End combo
			ImGui.EndCombo();
		}
		this._isOpen = combo;
	}
}
