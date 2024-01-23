using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Editor;
using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.Editors;
using Ktisis.Scene.Modules;

namespace Ktisis.Interface;

[Singleton]
public class EditorUi {
	private readonly ConfigManager _cfg;
	private readonly GizmoManager _gizmo;
	private readonly GuiManager _gui;
	
	public EditorUi(
		ConfigManager cfg,
		GizmoManager gizmo,
		GuiManager gui
	) {
		this._cfg = cfg;
		this._gizmo = gizmo;
		this._gui = gui;
	}

	public void Initialize() {
		this._gizmo.Initialize();
	}

	public void HandleWorkspace(bool state) {
		if (state && this._cfg.Config.Editor.OpenOnEnterGPose)
			this._gui.GetOrCreate<WorkspaceWindow>().Open();
	}

	public void OpenOverlay(IEditorContext context) {
		var gizmo = this._gizmo.Create(GizmoId.OverlayMain);
		if (this._gui.Get<OverlayWindow>() is {} overlay)
			overlay.Close();
		this._gui.GetOrCreate<OverlayWindow>(context, gizmo).Open();
	}

	public void OpenTransformWindow(IEditorContext context) {
		var gizmo = this._gizmo.Create(GizmoId.TransformEditor);
		this._gui.GetOrCreate<TransformWindow>(context, new Gizmo2D(gizmo)).Open();
	}

	public void OpenEnvironmentWindow(IEditorContext context) {
		var scene = context.Scene;
		var module = scene.GetModule<EnvModule>();
		this._gui.GetOrCreate<EnvWindow>(scene, module).Open();
	}

	public void OpenCameraWindow(IEditorContext context) {
		this._gui.GetOrCreate<CameraWindow>(context).Open();
	}
}
