using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Logging;
using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Scene;
using Ktisis.Scene.Objects;
using Ktisis.Editing;
using Ktisis.Editing.Modes;
using Ktisis.Interface.Overlay.Render;
using Ktisis.Common.Utility;
using Ktisis.Core;
using Ktisis.Core.Impl;
using Ktisis.Data.Config;
using Ktisis.Core.Services;
using Ktisis.ImGuizmo;

namespace Ktisis.Interface.Overlay;

[KtisisService]
public class GuiOverlay : IServiceInit {
	// Dependencies

	private readonly IServiceContainer _services;

	private readonly CameraService _camera;
	private readonly ConfigService _cfg;
	private readonly GPoseService _gpose;
	private readonly SceneManager _scene;
	private readonly EditorService _editor;
	
	private Gizmo? Gizmo;
	
	public readonly SelectionGui Selection;

	private ConfigFile Config => this._cfg.Config;

	// Constructor

	public GuiOverlay(
		IServiceContainer _services,
		CameraService _camera,
		ConfigService _cfg,
		GPoseService _gpose,
		SceneManager _scene,
		EditorService _editor
	) {
		this._services = _services;
        
		this._camera = _camera;
		this._cfg = _cfg;
		this._gpose = _gpose;
		this._scene = _scene;
		this._editor = _editor;

		this.Selection = new SelectionGui(_camera, _cfg);
		this.Selection.OnItemSelected += OnItemSelected;
	}

	public void Initialize() {
		if (Gizmo.Create(GizmoID.OverlayMain) is Gizmo gizmo) {
			this.Gizmo = gizmo;
			gizmo.Operation = Operation.ROTATE;
			gizmo.OnManipulate += OnManipulate;
			gizmo.OnDeactivate += OnDeactivate;
		} else {
			this._services.GetService<NotifyService>()?.Warning(
				"Failed to create gizmo. This may be due to version incompatibilities.\n" +
				"Please check your error log for more information."
			);
		}
		
		foreach (var (id, handler) in this._editor.GetHandlers()) {
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
		this._editor.Selection.HandleClick(item, flags);
	}

	private void OnManipulate(Gizmo gizmo) {
		if (!this._scene.IsActive) return;

		var target = this._editor.GetTransformTarget();
		if (target is not null)
			this._editor.Manipulate(target, gizmo.GetResult());
	}

	private void OnDeactivate(Gizmo _gizmo)
		=> this._editor.EndTransform();
	
	// Create overlay window

	public void Draw() {
		// TODO: Toggle

		if (!this.Config.Overlay_Visible || !this._gpose.IsInGPose) return;

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

		var editor = this._editor;
		if (editor.GetHandler() is ModeHandler handler)
			GetRenderer(this.Config.Editor_Mode)?.OnDraw(this, handler);

		this.Selection.Draw();

		if (editor.GetTransform() is Transform trans) {
			// TODO: Snapshot dummy object?
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
		if (this.Gizmo is null || !this._cfg.Config.Overlay_Gizmo) return;

		var view = this._camera.GetViewMatrix();
		var proj = this._camera.GetProjectionMatrix();
		if (view is Matrix4x4 viewMx && proj is Matrix4x4 projMx) {
			var size = ImGui.GetIO().DisplaySize;
			this.Gizmo.SetMatrix(viewMx, projMx);
			this.Gizmo.BeginFrame(Vector2.Zero, size);
			this.Gizmo.Mode = this.Config.Gizmo_Mode;
			this.Gizmo.Operation = this.Config.Gizmo_Op;
		}
	}

	private void EndFrame() {
		this.Gizmo?.EndFrame();
		ImGui.End();
	}
}
