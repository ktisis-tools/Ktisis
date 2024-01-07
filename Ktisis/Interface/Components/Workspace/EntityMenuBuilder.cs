using System.Linq;

using Dalamud.Plugin.Services;

using GLib.Popups.Context;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Windows.Actor;
using Ktisis.Interface.Windows.Pose;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class EntityMenuBuilder {
	private readonly GuiManager _gui;
	private readonly FileDialogManager _dialog;
	private readonly IFramework _framework;
	
	public EntityMenuBuilder(
		GuiManager gui,
		FileDialogManager dialog,
		IFramework framework
	) {
		this._gui = gui;
		this._dialog = dialog;
		this._framework = framework;
	}

	public ContextMenu Build(IEditorContext context, SceneEntity entity) {
		// TODO: Write a more modular factory for this.
		
		var cb = new ContextMenuBuilder();

		if (!entity.IsSelected)
			cb.Action("Select", () => context.Selection.Select(entity, SelectMode.Multiple));
		else
			cb.Action("Unselect", () => context.Selection.Unselect(entity));

		if (entity is IVisibility vis)
			cb.Action("Toggle display", () => vis.Toggle());
		
		switch (entity) {
			case ActorEntity actor:
				cb.Separator()
					.Action("Edit appearance", () => this.OpenEditorFor(context, entity))
					.Separator()
					.SubMenu("Import...", sb => {
						sb.Action("Character (.chara)", () => { })
							.Action("Pose file (.pose)", () => this.OpenPoseImport(context, actor));
					})
					.SubMenu("Export...", sb => {
						sb.Action("Character (.chara)", () => { })
							.Action("Pose file (.pose)", () => this.OpenPoseExport(actor));
					});
				break;
			case EntityPose:
				cb.Separator()
					.Action("Import pose", () => this.OpenPoseImport(context, entity))
					.Action("Export pose", () => this.OpenPoseExport(entity));
				break;
			case LightEntity:
				cb.Separator()
					.Action("Edit lighting", () => this.OpenEditorFor(context, entity))
					.Separator()
					.Action("Import preset", () => { })
					.Action("Export preset", () => { });
				break;
		}

		return cb.Build($"##EntityContext_{cb.GetHashCode():X}");
	}
	
	// Posing

	private void OpenPoseImport(IEditorContext context, SceneEntity entity) {
		var actor = entity switch {
			ActorEntity _actor => _actor,
			EntityPose _pose => _pose.Parent as ActorEntity,
			_ => null
		};
		if (actor == null) return;
		
		var window = this._gui.GetOrCreate<PoseImportDialog>(context);
		window.SetTarget(actor);
		window.Open();
	}

	private void OpenPoseExport(SceneEntity entity) {
		var pose = entity switch {
			EntityPose _pose => _pose,
			ActorEntity _actor => (EntityPose?)_actor.Children.FirstOrDefault(child => child is EntityPose),
			_ => null
		};
		if (pose == null) return;

		var converter = new EntityPoseConverter(pose);
		this._dialog.ExportPoseFile(converter);
	}
	
	// Editors

	private void OpenEditorFor(IEditorContext context, SceneEntity entity) {
		switch (entity) {
			case ActorEntity actor:
				var window = this._gui.GetOrCreate<ActorEditWindow>(context);
				window.Target = actor;
				window.Open();
				break;
			case LightEntity light:
				break;
		}
	}
}
