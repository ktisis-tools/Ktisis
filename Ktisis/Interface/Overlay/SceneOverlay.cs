using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Services;
using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;

namespace Ktisis.Interface.Overlay;

public class SceneOverlay {
	// Constructor

	private readonly CameraService _camera; // TODO: MOVE THIS

	private readonly SceneManager _scene;

	public SceneOverlay(CameraService _camera, SceneManager _scene) {
		this._camera = _camera;
		this._scene = _scene;
	}

	// Events

	public void SubscribeTo(GuiOverlay overlay) {
		overlay.OnOverlayDraw += OnOverlayDraw;
		if (overlay.Gizmo is Gizmo gizmo)
			gizmo.OnManipulate += OnManipulate;
	}

	// Draw UI

	private void OnOverlayDraw(GuiOverlay _overlay) {
		var scene = this._scene.Scene;
		if (scene is null) return;

		DrawItems(_overlay, scene.GetChildren());

		DrawSelected(_overlay);
	}

	private void DrawItems(GuiOverlay _overlay, IEnumerable<SceneObject> items) {
		foreach (var item in items)
			DrawItem(_overlay, item);
	}

	private void DrawItem(GuiOverlay _overlay, SceneObject item) {
		if (item is IManipulable world)
			DrawManipulable(_overlay, world);
	}

	// IManipulable

	private unsafe void DrawManipulable(GuiOverlay _overlay, IManipulable item) {
		var trans = item.GetTransform();
		if (trans is null) return;

		var cam = this._camera.GetSceneCamera();
		if (cam == null) return;

		// TEST CODE

		if (!cam->WorldToScreen(trans.Position, out var pos2d))
			return;

		var drawList = ImGui.GetWindowDrawList();
		drawList.AddCircleFilled(pos2d, 5f, 0xFFFFFFFF);
	}

	// Gizmo

	private IEnumerable<IManipulable>? GetSelected() => this._scene
		.SelectState?
		.GetSelected()
		.Where(item => item is IManipulable)
		.Cast<IManipulable>();

	private void DrawSelected(GuiOverlay _overlay) {
		var selected = GetSelected();
		if (selected is null) return;

		foreach (var item in selected) {
			var matrix = item.ComposeMatrix();
			if (matrix is null) continue;

			_overlay.Gizmo?.Manipulate(matrix.Value);

			break;
		}
	}

	private void OnManipulate(Gizmo gizmo) {
		var selected = GetSelected();
		if (selected is null) return;

		var result = gizmo.GetResult();
		Matrix4x4? delta = null;

		var isPrimary = true;
		foreach (var item in selected) {
			var matrix = item.ComposeMatrix();
			if (matrix is null) continue;

			if (isPrimary) {
				item.SetMatrix(result);
				isPrimary = false;
			} else {
				item.SetMatrix(gizmo.ApplyDelta(
					matrix.Value,
					delta ??= gizmo.GetDelta(),
					result
				));
			}
		}
	}
}
