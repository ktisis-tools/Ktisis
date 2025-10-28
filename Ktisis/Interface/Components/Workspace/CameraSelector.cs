using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;

namespace Ktisis.Interface.Components.Workspace;

public class CameraSelector {
	private readonly IEditorContext _ctx;

	private ICameraManager Cameras => this._ctx.Cameras;
	
	public CameraSelector(
		IEditorContext ctx
	) {
		this._ctx = ctx;
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
		if (ImGui.IsKeyDown(ImGuiKey.ModShift)) {
			using (ImRaii.Disabled(this.Cameras.GetCameras().Count() < 2 || this.Cameras.Current is EditorCamera { IsDefault: true }))
				if (Buttons.IconButtonTooltip(FontAwesomeIcon.Trash, "Delete camera"))
					this.Cameras.DeleteCurrent();
		} else {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.PencilAlt, "Edit camera"))
				this._ctx.Interface.OpenCameraWindow();
		}

		ImGui.SameLine(0, spacing);
		this.DrawFreecamToggle();
	}

	private void DrawFreecamToggle() {
		var isFreecam = this.Cameras.IsWorkCameraActive;
		using var bgCol = ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isFreecam);
		using var iconCol = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text).SetAlpha(0xCF), !isFreecam);
		
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
