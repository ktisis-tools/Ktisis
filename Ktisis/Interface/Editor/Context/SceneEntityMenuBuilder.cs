using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Dalamud.Bindings.ImGui;
using GLib.Popups.Context;

using Ktisis.Data.Files;
using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interface.Nodes;
using Ktisis.Interface.Widgets;
using Ktisis.Scene;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Editor.Context;

public class SceneEntityMenuBuilder {
	private readonly IEditorContext _ctx;
	private readonly SceneEntity _entity;

	private IEditorInterface Ui => this._ctx.Interface;

	public SceneEntityMenuBuilder(
		IEditorContext ctx,
		SceneEntity entity
	) {
		this._ctx = ctx;
		this._entity = entity;
	}

	public ContextMenu Create() {
		return new ContextMenuBuilder()
			.Group(this.BuildEntityBaseTop)
			.Group(this.BuildEntityType)
			.Group(this.BuildEntityBaseBottom)
			.Build($"EntityContextMenu_{this.GetHashCode():X}");
	}

	private void BuildEntityBaseTop(ContextMenuBuilder menu) {
		if (!this._entity.IsSelected)
			menu.Action("Select", () => this._entity.Select(SelectMode.Multiple));
		else
			menu.Action("Unselect", this._entity.Unselect);

		if (this._entity is IVisibility vis)
			menu.Action("Toggle display", () => vis.Toggle());

		if (this._entity.Root is ActorEntity actorEntity)
			menu.SubMenu("Presets...", sub => {
				foreach (var (name, isEnabled) in actorEntity.GetPresets()) {
					sub.CheckableAction(name, isEnabled != PresetState.Disabled, () => actorEntity.TogglePreset(name));
				}

				sub.Separator()
					.Action("Save New", () => this.Ui.OpenSavePreset(actorEntity));
			});
	}

	private void BuildEntityBaseBottom(ContextMenuBuilder menu) {
		if (this._entity is IAttachable attach && attach.IsAttached())
			menu.Separator().Action("Detach", () => this._ctx.Posing.Attachments.Detach(attach));

		menu.Separator().Action("Rename", () => this.Ui.OpenRenameEntity(this._entity));

		if (this._entity is IDeletable deletable) {
			menu.Separator();
			if (this._entity is ActorEntity actor)
				menu.Action("Duplicate", () => this.DuplicateActor(actor));
			menu.Action("Delete", () => deletable.Delete());
		}
	}
	
	// Entity types

	private void BuildEntityType(ContextMenuBuilder menu) {
		switch (this._entity) {
			case ActorEntity actor:
				this.BuildActorMenu(menu, actor);
				break;
			case EntityPose pose:
				this.BuildPoseMenu(menu, pose);
				break;
			case LightEntity light:
				this.BuildLightMenu(menu, light);
				break;
		}
	}

	private void OpenEditor() => this.Ui.OpenEditorFor(this._entity);
	
	// Actors

	private unsafe void BuildActorMenu(ContextMenuBuilder menu, ActorEntity actor) {
		menu.Separator()
			.Action("Target", actor.Actor.SetGPoseTarget)
			.Action($"{(actor.IsHidden ? "Unhide" : "Hide")} Actor", actor.ToggleHidden)
			.Separator()
			.Action("Edit appearance", this.OpenEditor)
			.Group(sub => this.BuildActorIpcMenu(sub, actor))
			.Separator()
			.SubMenu("Import...", sub => {
				var builder = sub.Action("Character (.chara)", () => this.Ui.OpenCharaImport(actor))
					.Action("NPC", () => this.Ui.OpenCharaImport(actor, true))
					.Action("Pose file (.pose)", () => this.Ui.OpenPoseImport(actor));
				
				if (this._ctx.Plugin.Ipc.IsAnyMcdfActive && actor.GetHuman() != null) {
					builder.Action("Mare data (.mcdf)", () => {
						this.Ui.OpenMcdfFile(path => this.ImportMcdf(actor, path));
					});
				}
			})
			.SubMenu("Export...", sub => {
				sub.Action("Character (.chara)", () => this.Ui.OpenCharaExport(actor))
					.Action("Pose file (.pose)", () => this.ExportPose(actor.Pose));
			});
	}

	private unsafe void BuildActorIpcMenu(ContextMenuBuilder menu, ActorEntity actor) {
		menu.SubMenu("IPC appearance", sub => {
			if (this._ctx.Plugin.Ipc.IsPenumbraActive)
				sub.Action("Penumbra: Assign collection", () => this.Ui.OpenAssignCollection(actor));
			if (this._ctx.Plugin.Ipc.IsGlamourerActive)
				sub.Action("Glamourer: Apply design", () => this.Ui.OpenApplyDesign(actor));
			if (this._ctx.Plugin.Ipc.IsCustomizeActive)
				sub.Action("Customize: Assign profile", () => this.Ui.OpenAssignCProfile(actor));
			if (this._ctx.Plugin.Ipc.IsAnyMcdfActive && actor.GetHuman() != null)
				sub.Action("Revert all IPC data", () => this._ctx.Characters.Mcdf.Revert(actor.Actor));
		});
	}

	private void ImportMcdf(ActorEntity actor, string path) {
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, actor.Actor);
	}

	private async void DuplicateActor(ActorEntity actor) {
		// pack actor into a temp charafile to apply to new actor after creation
		var file = await this._ctx.Characters.SaveCharaFile(actor);
		this._ctx.Scene.Factory.CreateActor()
			.WithAppearance(file)
			.Spawn();
	}
	
	// Poses

	private void BuildPoseMenu(ContextMenuBuilder menu, EntityPose pose) {
		menu.Separator()
			.Action("Import pose", () => this.ImportPose(pose))
			.Action("Export pose", () => this.ExportPose(pose))
			.Separator()
			.Action("Set to reference pose", () => this._ctx.Posing.ApplyReferencePose(pose));
	}

	private void ImportPose(EntityPose pose) {
		if (pose.Parent is ActorEntity actor)
			this.Ui.OpenPoseImport(actor);
	}
	
	private async void ExportPose(EntityPose? pose) {
		if (pose == null) return;
		await this.Ui.OpenPoseExport(pose);
	}
	
	// Lights

	private void BuildLightMenu(ContextMenuBuilder menu, LightEntity light) {
		menu.Separator()
			.Action("Edit lighting", this.OpenEditor)
			.Separator()
			.Action("Import preset", () => this.Ui.OpenLightFile((path, file) => this.ImportLight(light, file)))
			.Action("Export preset", () => this.Ui.OpenLightExport(light));
	}

	private async void ImportLight(LightEntity light, LightFile file) {
		await this._ctx.Scene.ApplyLightFile(light, file);
	}
}
