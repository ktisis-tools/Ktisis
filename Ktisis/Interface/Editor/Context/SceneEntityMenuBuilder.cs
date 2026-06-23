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
			menu.Action(Ktisis.Locale.Translate("workspace.entity_menu.base.select"), () => this._entity.Select(SelectMode.Multiple));
		else
			menu.Action(Ktisis.Locale.Translate("workspace.entity_menu.base.deselect"), this._entity.Unselect);
		if (this._entity.Children.Any())
			menu.Action(Ktisis.Locale.Translate("workspace.entity_menu.base.hierarchy"), () => {
				foreach (var entity in this._entity.Children.Where(entity => !entity.IsSelected))
					entity.Select(SelectMode.Multiple);
				if (!this._entity.IsSelected) this._entity.Select(SelectMode.Multiple);
			});

		if (this._entity.Root is ActorEntity actorEntity)
			menu.SubMenu(Ktisis.Locale.Translate("workspace.entity_menu.base.presets"), sub => {
				foreach (var (name, isEnabled) in actorEntity.GetPresets()) {
					sub.CheckableAction(name, isEnabled != PresetState.Disabled, () => actorEntity.TogglePreset(name));
				}

				sub.Separator()
					.Action(Ktisis.Locale.Translate("workspace.entity_menu.base.presets_save"), () => this.Ui.OpenSavePreset(actorEntity));
			});
	}

	private void BuildEntityBaseBottom(ContextMenuBuilder menu) {
		if (this._entity is IAttachable attach && attach.IsAttached())
			menu.Separator().Action(Ktisis.Locale.Translate("workspace.entity_menu.base.detach"), () => this._ctx.Posing.Attachments.Detach(attach));

		menu.Separator().Action(Ktisis.Locale.Translate("workspace.entity_menu.base.rename"), () => this.Ui.OpenRenameEntity(this._entity));

		if (this._entity is IDeletable deletable) {
			menu.Separator();
			if (this._entity is ActorEntity actor)
				menu.Action(Ktisis.Locale.Translate("workspace.entity_menu.base.duplicate"), () => this.DuplicateActor(actor));
			if (this._entity is LightEntity light)
				menu.Action(Ktisis.Locale.Translate("workspace.entity_menu.base.duplicate"), () => this.DuplicateLight(light));
			menu.Action(Ktisis.Locale.Translate("workspace.entity_menu.base.delete"), () => deletable.Delete());
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
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.target"), actor.Actor.SetGPoseTarget)
			.Separator()
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.edit"), this.OpenEditor)
			.Group(sub => this.BuildActorIpcMenu(sub, actor))
			.Separator()
			.SubMenu(Ktisis.Locale.Translate("workspace.entity_menu.actor.import"), sub => {
				var builder = sub.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.chara"), () => this.Ui.OpenCharaImport(actor))
					.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.npc"), () => this.Ui.OpenCharaImport(actor, true))
					.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.pose"), () => this.Ui.OpenPoseImport(actor));

				if (this._ctx.Plugin.Ipc.IsAnyMcdfActive && actor.GetHuman() != null) {
					builder.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.mcdf"), () => {
						this.Ui.OpenMcdfFile(path => this.ImportMcdf(actor, path));
					});
				}
			})
			.SubMenu(Ktisis.Locale.Translate("workspace.entity_menu.actor.export"), sub => {
				sub.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.chara"), () => this.Ui.OpenCharaExport(actor))
					.Action(Ktisis.Locale.Translate("workspace.entity_menu.actor.pose"), () => this.ExportPose(actor.Pose));
			});
	}

	private unsafe void BuildActorIpcMenu(ContextMenuBuilder menu, ActorEntity actor) {
		menu.SubMenu(Ktisis.Locale.Translate("workspace.entity_menu.ipc.submenu"), sub => {
			if (this._ctx.Plugin.Ipc.IsPenumbraActive) {
				sub.Action(Ktisis.Locale.Translate("workspace.entity_menu.ipc.penumbra.collection"), () => this.Ui.OpenAssignCollection(actor));
				sub.Action(Ktisis.Locale.Translate("workspace.entity_menu.ipc.penumbra.invisible_skin"), () => this._ctx.Characters.Mcdf.SetInvisibleSkin(actor));
			}
			if (this._ctx.Plugin.Ipc.IsGlamourerActive)
				sub.Action(Ktisis.Locale.Translate("workspace.entity_menu.ipc.glamourer.design"), () => this.Ui.OpenApplyDesign(actor));
			if (this._ctx.Plugin.Ipc.IsCustomizeActive)
				sub.Action(Ktisis.Locale.Translate("workspace.entity_menu.ipc.customize.profile"), () => this.Ui.OpenAssignCProfile(actor));
			if (this._ctx.Plugin.Ipc.IsAnyMcdfActive && actor.GetHuman() != null) {
				sub.Action(Ktisis.Locale.Translate("workspace.entity_menu.ipc.revert"), () => {
					this._ctx.Characters.Mcdf.Revert(actor.Actor);
					actor.AssignedProfile = null;
				});
			}
		});
	}

	private void ImportMcdf(ActorEntity actor, string path) {
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, actor.Actor);
	}

	private async void DuplicateActor(ActorEntity actor) {
		// pack actor into a temp charafile to apply to new actor after creation
		var file = await this._ctx.Characters.SaveCharaFile(actor);
		var dupe = await this._ctx.Scene.Factory.CreateActor()
			.WithAppearance(file)
			.Spawn();

		// copy glamourer state if applicable
		if (!this._ctx.Plugin.Ipc.IsGlamourerActive) return;
		var ipc = this._ctx.Plugin.Ipc.GetGlamourerIpc();
		ipc.CopyState(actor.Actor.ObjectIndex, dupe.Actor.ObjectIndex);
	}
	
	// Poses

	private void BuildPoseMenu(ContextMenuBuilder menu, EntityPose pose) {
		menu.Separator()
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.pose.import"), () => this.ImportPose(pose))
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.pose.export"), () => this.ExportPose(pose))
			.Separator()
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.pose.reference"), () => this._ctx.Posing.ApplyReferencePose(pose));
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
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.light.edit"), this.OpenEditor)
			.Separator()
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.light.import"), () => this.Ui.OpenLightFile((path, file) => this.ImportLight(light, file)))
			.Action(Ktisis.Locale.Translate("workspace.entity_menu.light.export"), () => this.Ui.OpenLightExport(light));
	}

	private async void ImportLight(LightEntity light, LightFile file) {
		await this._ctx.Scene.ApplyLightFile(light, file);
	}

	private async void DuplicateLight(LightEntity light) {
		var file = await this._ctx.Scene.SaveLightFile(light);
		var newLight = await this._ctx.Scene.Factory.CreateLight().Spawn();
		this.ImportLight(newLight, file);
	}
}
