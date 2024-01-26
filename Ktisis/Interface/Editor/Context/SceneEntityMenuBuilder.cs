using GLib.Popups.Context;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interface.Windows.Import;
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
			menu.Separator().Action("Detach", attach.Detach);

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

	private void BuildActorMenu(ContextMenuBuilder menu, ActorEntity actor) {
		menu.Separator()
			.Group(sub => this.BuildActorIpcMenu(sub, actor))
			.Action("Edit appearance", this.OpenEditor)
			.Separator()
			.SubMenu("Import...", sub => {
				sub.Action("Character (.chara)", () => this.ImportChara(actor))
					.Action("Pose file (.pose)", () => this.ImportPose(actor));
			})
			.SubMenu("Export...", sub => {
				sub.Action("Character (.chara)", () => this.ExportChara(actor))
					.Action("Pose file (.pose)", () => this.ExportPose(actor.Pose));
			});
	}

	private void BuildActorIpcMenu(ContextMenuBuilder menu, ActorEntity actor) {
		if (!this._ctx.Plugin.Ipc.IsPenumbraActive) return;
		
		menu.Action("Assign collection", () => this.Ui.OpenAssignCollection(actor));
	}

	private void ImportChara(ActorEntity actor) => this.Ui.OpenEditor<CharaImportDialog, ActorEntity>(actor);
	private void ImportPose(ActorEntity pose) => this.Ui.OpenEditor<PoseImportDialog, ActorEntity>(pose);

	private async void ExportChara(ActorEntity actor) {
		var file = await this._ctx.Characters.SaveCharaFile(actor);
		this.Ui.ExportCharaFile(file);
	}
	
	// Poses

	private void BuildPoseMenu(ContextMenuBuilder menu, EntityPose pose) {
		menu.Separator()
			.Action("Import pose", () => this.ImportPose(pose))
			.Action("Export pose", () => this.ExportPose(pose));
	}

	private void ImportPose(EntityPose pose) {
		if (pose.Parent is ActorEntity actor)
			this.ImportPose(actor);
	}
	
	private async void ExportPose(EntityPose? pose) {
		if (pose == null) return;
		var file = await this._ctx.Posing.SavePoseFile(pose);
		this.Ui.ExportPoseFile(file);
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
