using GLib.Popups.Context;

using Ktisis.Editor.Characters;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows.Import;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Menus;

public interface IEntityMenuMediator {
	public void ExportChara(EntityCharaConverter converter);
	public void ExportPose(EntityPoseConverter converter);

	public void OpenEditor<T>(T entity) where T : SceneEntity;
	public void OpenEditor<T, TA>(TA entity) where T : EntityEditWindow<TA> where TA : SceneEntity;
}

public class SceneEntityMenu(
	IEntityMenuMediator mediator,
	IEditorContext context,
	SceneEntity entity
) {
	public ContextMenu Build() {
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
					.Action("Edit appearance", () => mediator.OpenEditor(actor))
					.Separator()
					.SubMenu("Import...", sb => {
						sb.Action("Character (.chara)", this.OpenCharaImport)
							.Action("Pose file (.pose)", this.OpenPoseImport);
					})
					.SubMenu("Export...", sb => {
						sb.Action("Character (.chara)", this.OpenCharaExport)
							.Action("Pose file (.pose)", this.OpenPoseExport);
					});
				break;
			case EntityPose:
				cb.Separator()
					.Action("Import pose", this.OpenPoseImport)
					.Action("Export pose", this.OpenPoseExport);
				break;
			case LightEntity light:
				cb.Separator()
					.Action("Edit lighting", () => mediator.OpenEditor(light))
					.Separator()
					.Action("Import preset (TODO)", () => { })
					.Action("Export preset (TODO)", () => { });
				break;
		}

		if (entity is IAttachable attach && attach.IsAttached()) {
			cb.Separator()
				.Action("Detach", () => attach.Detach());
		}

		if (entity is IDeletable deletable) {
			cb.Separator()
				.Action("Delete", () => deletable.Delete());
		}

		return cb.Build($"##EntityContext_{cb.GetHashCode():X}");
	}
	
	// Chara

	private void OpenCharaImport() {
		if (entity is not ActorEntity actor) return;
		mediator.OpenEditor<CharaImportDialog, ActorEntity>(actor);
	}

	private void OpenCharaExport() {
		if (entity is not ActorEntity actor) return;
		mediator.ExportChara(new EntityCharaConverter(actor));
	}
	
	// Pose
	
	private void OpenPoseImport() {
		var actor = entity switch {
			ActorEntity _actor => _actor,
			EntityPose _pose => _pose.Parent as ActorEntity,
			_ => null
		};
		if (actor == null) return;
		mediator.OpenEditor<PoseImportDialog, ActorEntity>(actor);
	}

	private void OpenPoseExport() {
		var pose = entity switch {
			EntityPose _pose => _pose,
			ActorEntity _actor => _actor.Pose,
			_ => null
		};
		if (pose == null) return;
		mediator.ExportPose(new EntityPoseConverter(pose));
	}
}
