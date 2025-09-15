using System;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using GLib.Popups.ImFileDialog;

using Ktisis.Data.Files;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Editor.Context;
using Ktisis.Interface.Editor.Popup;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.Editors;
using Ktisis.Interface.Windows.Import;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Modules.Actors;

namespace Ktisis.Interface.Editor;

public class EditorInterface : IEditorInterface {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;

	private readonly GizmoManager _gizmo;
	
	public EditorInterface(
		IEditorContext ctx,
		GuiManager gui
	) {
		this._ctx = ctx;
		this._gui = gui;
		this._gizmo = new(ctx.Config);
	}
	
	// Scene ready

	public void Prepare() {
		if (this._ctx.Config.Editor.OpenOnEnterGPose)
			this._gui.GetOrCreate<WorkspaceWindow>(this._ctx).Open();

		this._ctx.Selection.Changed += this.OnSelectChanged;

		this._gizmo.Initialize();
		this._gui.GetOrCreate<OverlayWindow>(
			this._ctx,
			this._gizmo.Create(GizmoId.OverlayMain)
		).Open();
	}

	private void OnSelectChanged(ISelectManager sender) {
		if (!this._ctx.Config.Editor.ToggleEditorOnSelect) return;

		var open = sender.Count > 0;
		
		var editor = this._gui.Get<ObjectWindow>();
		if (editor == null) {
			if (open) this.OpenObjectEditor();
			return;
		}

		if (!this._ctx.Config.Editor.CloseEditorOnDeselect) return;
		editor.IsOpen = open;
	}
	
	// Window wrappers

	public void OpenConfigWindow() {
		if (this._ctx.Config.Editor.ToggleOpenWindows)
			this._gui.GetOrCreate<ConfigWindow>().Toggle();
		else {
			var _win = this._gui.GetOrCreate<ConfigWindow>();
			_win.Open();
			ImGui.SetWindowFocus(_win.WindowName);
		}
	}

	public void ToggleWorkspaceWindow() => this._gui.GetOrCreate<WorkspaceWindow>(this._ctx).Toggle();
	
	// Editor windows
	
	public void OpenCameraWindow() {
		if (this._ctx.Config.Editor.ToggleOpenWindows)
			this._gui.GetOrCreate<CameraWindow>(this._ctx).Toggle();
		else {
			var _win = this._gui.GetOrCreate<CameraWindow>(this._ctx);
			_win.Open();
			ImGui.SetWindowFocus(_win.WindowName);
		}
	}
	
	public void OpenEnvironmentWindow() {
		var scene = this._ctx.Scene;
		var module = scene.GetModule<EnvModule>();
		if (this._ctx.Config.Editor.ToggleOpenWindows)
			this._gui.GetOrCreate<EnvWindow>(scene, module).Toggle();
		else {
			var _win = this._gui.GetOrCreate<EnvWindow>(scene, module);
			_win.Open();
			ImGui.SetWindowFocus(_win.WindowName);
		}
	}

	public void OpenObjectEditor() {
		var gizmo = this._gizmo.Create(GizmoId.TransformEditor);
		if (this._ctx.Config.Editor.ToggleOpenWindows)
			this._gui.GetOrCreate<ObjectWindow>(this._ctx, new Gizmo2D(gizmo), this._gui).Toggle();
		else {
			var _win = this._gui.GetOrCreate<ObjectWindow>(this._ctx, new Gizmo2D(gizmo), this._gui);
			_win.Open();
			ImGui.SetWindowFocus(_win.WindowName);
		}
	}

	public void OpenObjectEditor(SceneEntity entity) {
		entity.Select(SelectMode.Force);
		this.OpenObjectEditor();
	}

	public void OpenPosingWindow() {
		if (this._ctx.Config.Editor.ToggleOpenWindows)
			this._gui.GetOrCreate<PosingWindow>(this._ctx, this._ctx.Locale).Toggle();
		else {
			var _win = this._gui.GetOrCreate<PosingWindow>(this._ctx, this._ctx.Locale);
			_win.Open();
			ImGui.SetWindowFocus(_win.WindowName);
		}
	}
	
	// Context menus

	public void OpenSceneCreateMenu() {
		var menu = new SceneCreateMenuBuilder(this._ctx);
		this._gui.AddPopup(menu.Create()).Open();
	}

	public void OpenSceneEntityMenu(SceneEntity entity) {
		var menu = new SceneEntityMenuBuilder(this._ctx, entity);
		this._gui.AddPopup(menu.Create()).Open();
	}

	public void OpenAssignCollection(ActorEntity entity) => this._gui.CreatePopup<ActorCollectionPopup>(this._ctx, entity).Open();

	public void OpenAssignCProfile(ActorEntity entity) => this._gui.CreatePopup<ActorCProfilePopup>(this._ctx, entity).Open();

	public void OpenOverworldActorList() => this._gui.CreatePopup<OverworldActorPopup>(this._ctx).Open();
	
	public void RefreshGposeActors() => this._ctx.Scene.GetModule<ActorModule>().RefreshGPoseActors();

	// Entity windows
	
	public void OpenRenameEntity(SceneEntity entity) => this._gui.CreatePopup<EntityRenameModal>(entity).Open();
	public void OpenSavePreset(ActorEntity entity) => this._gui.CreatePopup<PresetSaveModal>(entity).Open();
	
	public void OpenActorEditor(ActorEntity actor) {
		if (!this._ctx.Config.Editor.UseLegacyWindowBehavior)
			actor.Select(SelectMode.Force);
		this.OpenEditor<ActorWindow, ActorEntity>(actor);
	}
	
	public void OpenLightEditor(LightEntity light) {
		if (this._ctx.Config.Editor.UseLegacyLightEditor) {
			this.OpenEditor<LightWindow, LightEntity>(light);
			return;
		}
		this.OpenObjectEditor(light);
	}

	public void OpenEditor<T, TA>(TA entity) where T : EntityEditWindow<TA> where TA : SceneEntity {
		var editor = this._gui.GetOrCreate<T>(this._ctx);
		editor.SetTarget(entity);

		if (this._ctx.Config.Editor.ToggleOpenWindows)
			editor.Toggle();
		else {
			editor.Open();
			ImGui.SetWindowFocus(editor.WindowName);
		}
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
	
	// import/export wrappers

	public void OpenCharaImport(ActorEntity actor) => this.OpenEditor<CharaImportDialog, ActorEntity>(actor);

	public async Task OpenCharaExport(ActorEntity actor) {
		var file = await this._ctx.Characters.SaveCharaFile(actor);
		this.ExportCharaFile(file);
	}

	public void OpenPoseImport(ActorEntity actor) => this.OpenEditor<PoseImportDialog, ActorEntity>(actor);

	public async Task OpenPoseExport(EntityPose pose) {
		var file = await this._ctx.Posing.SavePoseFile(pose);
		this.ExportPoseFile(file);
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

	private readonly static FileDialogOptions McdfFileOptions = new() {
		Filters = "MCDF Files{.mcdf}",
		Extension = ".mcdf"
	};
	
	public void OpenCharaFile(Action<string, CharaFile> handler)
		=> this._gui.FileDialogs.OpenFile("Open Chara File", handler, CharaFileOptions);

	public void OpenPoseFile(Action<string, PoseFile> handler) {
		this._gui.FileDialogs.OpenFile<PoseFile>("Open Pose File", (path, file) => {
			file.ConvertLegacyBones();
			handler.Invoke(path, file);
		}, PoseFileOptions);
	}
	
	public void OpenMcdfFile(Action<string> handler) {
		this._gui.FileDialogs.OpenFile("Open MCDF File", handler, McdfFileOptions);
	}

	public void OpenReferenceImages(Action<string> handler) {
		this._gui.FileDialogs.OpenImage("image", handler);
	}

	public void ExportCharaFile(CharaFile file)
		=> this._gui.FileDialogs.SaveFile("Export Chara File", file, CharaFileOptions);
	
	public void ExportPoseFile(PoseFile file)
		=> this._gui.FileDialogs.SaveFile("Export Pose File", file, PoseFileOptions);
}
