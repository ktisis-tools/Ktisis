using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Logging;
using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Core;
using Ktisis.Scene;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Editing;
using Ktisis.Scene.Editing.Modes;
using Ktisis.Interface.Overlay.Render;
using Ktisis.Common.Utility;
using Ktisis.Services;
using Ktisis.ImGuizmo;

namespace Ktisis.Interface.Overlay;

public class GuiOverlay {
	// Dependencies

	private readonly CameraService _camera;
	private readonly GPoseService _gpose;
	private readonly SceneManager _scene;

	private readonly Gizmo? Gizmo;

	public readonly SelectionGui Selection;

	// State

	public bool Visible = true;

	// Constructor

	public GuiOverlay(
		IServiceContainer _services,
		CameraService _camera,
		GPoseService _gpose,
		SceneManager _scene,
		NotifyService _notify
	) {
		this._camera = _camera;
		this._gpose = _gpose;
		this._scene = _scene;

		if (Gizmo.Create(GizmoID.OverlayMain) is Gizmo gizmo) {
			this.Gizmo = gizmo;
			gizmo.Operation = Operation.ROTATE;
			gizmo.OnManipulate += OnManipulate;
		} else {
			_notify.Warning(
				"Failed to create gizmo. This may be due to version incompatibilities.\n" +
				"Please check your error log for more information."
			);
		}

		this.Selection = _services.Inject<SelectionGui>();
		this.Selection.OnItemSelected += OnItemSelected;

		foreach (var (id, handler) in _scene.Editor.GetHandlers()) {
			if (handler.GetRenderer() is Type type)
				AddRenderer(id, type);
		}
	}

	// Object mode renderers

	private readonly Dictionary<EditMode, RendererBase> Renderers = new();

	private void AddRenderer(EditMode id, Type type) {
		if (type.BaseType != typeof(RendererBase))
			throw new Exception($"Attempted to register invalid type as renderer: {type}");

		var inst = (RendererBase)Activator.CreateInstance(type)!;
		this.Renderers.Add(id, inst);
	}

	private RendererBase? GetRenderer(EditMode id) => this.Renderers
		.TryGetValue(id, out var result) ? result : null;

	// Events

	private void OnItemSelected(SceneObject item) {
		var flags = GuiHelpers.GetSelectFlags();
		this._scene.Editor.Selection.HandleClick(item, flags);
	}

	private void OnManipulate(Gizmo gizmo) {
		if (!this._scene.IsActive) return;

		var target = this._scene.Editor.GetHandler()?
			.GetTransformTarget();

		if (target is not null)
			this._scene.Editor.Manipulate(target, gizmo.GetResult(), gizmo.GetDelta());
	}

	// Create overlay window

	public void Draw() {
		// TODO: Toggle

		if (!this.Visible || !this._gpose.IsInGPose) return;

		try {
			if (BeginFrame())
				BeginGizmo();
			else return;

			try {
				DrawScene();
			} catch (Exception err) {
				PluginLog.Error($"Error while drawing overlay:\n{err}");
			}
		} finally {
			EndFrame();
		}
	}

	private bool BeginFrame() {
		const ImGuiWindowFlags flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs;

		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

		var io = ImGui.GetIO();
		ImGui.SetNextWindowSize(io.DisplaySize);
		ImGui.SetNextWindowPos(Vector2.Zero);

		ImGuiHelpers.ForceNextWindowMainViewport();

		var begin = ImGui.Begin("Ktisis Overlay", flags);
		ImGui.PopStyleVar();
		return begin;
	}

	// Draw scene

	private void DrawScene() {
		if (!this._scene.IsActive) return;

		var editor = this._scene.Editor;
		if (editor.GetHandler() is ModeHandler handler)
			GetRenderer(editor.CurrentMode)?.OnDraw(this, handler);

		this.Selection.Draw();

		if (editor.GetTransform() is Transform trans) {
			var matrix = trans.ComposeMatrix();
			this.Gizmo?.Manipulate(matrix);
		}
	}

	// Draw line

	public unsafe void DrawLine(ImDrawListPtr drawList, Vector3 fromPos, Vector3 toPos) {
		var camera = this._camera.GetSceneCamera();
		if (camera == null) return;

		if (!camera->WorldToScreen(fromPos, out var fromPos2d)) return;
		if (!camera->WorldToScreen(toPos, out var toPos2d)) return;

		drawList.AddLine(fromPos2d, toPos2d, 0xFFFFFFFF);
	}

	// Gizmo

	private void BeginGizmo() {
		if (this.Gizmo is null) return;

		var view = this._camera.GetViewMatrix();
		var proj = this._camera.GetProjectionMatrix();
		if (view is Matrix4x4 viewMx && proj is Matrix4x4 projMx ) {
			var size = ImGui.GetIO().DisplaySize;
			this.Gizmo.SetMatrix(viewMx, projMx);
			this.Gizmo.BeginFrame(Vector2.Zero, size);
			this.Gizmo.Mode = this._scene.Editor.TransformMode;
		}
	}

	private void EndFrame() {
		this.Gizmo?.EndFrame();
		ImGui.End();
	}
}
