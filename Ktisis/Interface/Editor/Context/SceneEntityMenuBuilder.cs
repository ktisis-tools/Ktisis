using GLib.Popups.Context;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Editor.Types;
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
	}

	private void BuildEntityBaseBottom(ContextMenuBuilder menu) {
		if (this._entity is IAttachable attach && attach.IsAttached())
			menu.Separator().Action("Detach", () => this._ctx.Posing.Attachments.Detach(attach));

		menu.Separator().Action("Rename", () => this.Ui.OpenRenameEntity(this._entity));

		if (this._entity is IDeletable deletable)
			menu.Separator().Action("Delete", () => deletable.Delete());
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
			.Separator()
			.Group(sub => this.BuildActorIpcMenu(sub, actor))
			.Action("Edit appearance", this.OpenEditor)
			.Separator()
			.SubMenu("Import...", sub => {
				var builder = sub.Action("Character (.chara)", () => this.Ui.OpenCharaImport(actor))
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

	private void BuildActorIpcMenu(ContextMenuBuilder menu, ActorEntity actor) {
		if (this._ctx.Plugin.Ipc.IsPenumbraActive)
			menu.Action("Assign collection", () => this.Ui.OpenAssignCollection(actor));
		if (this._ctx.Plugin.Ipc.IsCustomizeActive)
			menu.Action("Assign C+ profile", () => this.Ui.OpenAssignCProfile(actor));
	}

	private void ImportMcdf(ActorEntity actor, string path) {
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, actor.Actor);
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
			.Action("Import preset (TODO)", () => { })
			.Action("Export preset (TODO)", () => { });
	}
}
