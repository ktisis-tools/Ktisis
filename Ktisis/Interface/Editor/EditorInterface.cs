using System;

using GLib.Popups.ImFileDialog;

using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Editor.Context;
using Ktisis.Interface.Editor.Popup;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.Editors;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Modules;

namespace Ktisis.Interface.Editor;

public class EditorInterface : IEditorInterface {
	private readonly IContextMediator _mediator;
	private readonly GuiManager _gui;

	private readonly GizmoManager _gizmo;

	private IEditorContext Context => this._mediator.Context;
	
	public EditorInterface(
		IContextMediator mediator,
		GuiManager gui
	) {
		this._mediator = mediator;
		this._gui = gui;
		this._gizmo = new(mediator.Config);
	}
	
	// Scene ready

	public void Prepare() {
		if (this.Context.Config.Editor.OpenOnEnterGPose)
			this._gui.GetOrCreate<WorkspaceWindow>(this.Context).Open();

		this._gizmo.Initialize();
		this._gui.GetOrCreate<OverlayWindow>(
			this.Context,
			this._gizmo.Create(GizmoId.OverlayMain)
		).Open();
	}
	
	// Window wrappers

	public void OpenConfigWindow() => this._gui.GetOrCreate<ConfigWindow>().Open();
	
	// Editor windows
	
	public void OpenCameraWindow() => this._gui.GetOrCreate<CameraWindow>(this.Context).Open();
	
	public void OpenEnvironmentWindow() {
		var scene = this.Context.Scene;
		var module = scene.GetModule<EnvModule>();
		this._gui.GetOrCreate<EnvWindow>(scene, module).Open();
	}

	public void OpenTransformWindow() {
		var gizmo = this._gizmo.Create(GizmoId.TransformEditor);
		this._gui.GetOrCreate<TransformWindow>(this.Context, new Gizmo2D(gizmo)).Open();
	}
	
	// Context menus

	public void OpenSceneCreateMenu() {
		var menu = new SceneCreateMenuBuilder(this, this.Context);
		this._gui.AddPopup(menu.Create()).Open();
	}

	public void OpenSceneEntityMenu(SceneEntity entity) {
		var menu = new SceneEntityMenuBuilder(this, this.Context, entity);
		this._gui.AddPopup(menu.Create()).Open();
	}

	public void OpenAssignCollection(ActorEntity entity) => this._gui.CreatePopup<ActorCollectionPopup>(this.Context, entity).Open();

	public void OpenOverworldActorList() => this._gui.CreatePopup<OverworldActorPopup>(this.Context).Open();
	
	// Entity windows

	public void OpenActorEditor(ActorEntity actor) => this.OpenEditor<ActorWindow, ActorEntity>(actor);
	public void OpenLightEditor(LightEntity light) => this.OpenEditor<LightWindow, LightEntity>(light);

	public void OpenEditor<T, TA>(TA entity) where T : EntityEditWindow<TA> where TA : SceneEntity {
		var editor = this._gui.GetOrCreate<T>(this.Context);
		editor.SetTarget(entity);
		editor.Open();
	}
    
	public void OpenEditorFor(SceneEntity entity) {
		switch (entity) {
			case ActorEntity actor:
				this.OpenActorEditor(actor);
				break;
			case LightEntity light:
				this.OpenLightEditor(light);
				break;
		}
	}
	
	// Import/export dialogs
	
	private readonly static FileDialogOptions CharaFileOptions = new() {
		Filters = "Character Files{.chara}",
		Extension = ".chara"
	};

	private readonly static FileDialogOptions PoseFileOptions = new() {
		Filters = "Pose Files{.pose}",
		Extension = ".pose"
	};
	
	public void OpenCharaFile(Action<string, CharaFile> handler)
		=> this._gui.FileDialogs.OpenFile("Open Chara File", handler, CharaFileOptions);

	public void OpenPoseFile(Action<string, PoseFile> handler)
		=> this._gui.FileDialogs.OpenFile("Open Pose File", handler, PoseFileOptions);

	public void ExportCharaFile(CharaFile file)
		=> this._gui.FileDialogs.SaveFile("Export Chara File", file, CharaFileOptions);
	
	public void ExportPoseFile(PoseFile file)
		=> this._gui.FileDialogs.SaveFile("Export Pose File", file, PoseFileOptions);
}
