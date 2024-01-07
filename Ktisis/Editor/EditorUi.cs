using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Interface;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Windows;

namespace Ktisis.Editor;

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
			this._gui.GetOrCreate<Workspace>().Open();
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
}
