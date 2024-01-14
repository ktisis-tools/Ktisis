using System.Linq;

using GLib.Popups.Context;

using Ktisis.Editor;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows.Pose;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Menus;

public interface IEntityMenuMediator {
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
						sb.Action("Character (.chara)", () => { })
							.Action("Pose file (.pose)", this.OpenPoseImport);
					})
					.SubMenu("Export...", sb => {
						sb.Action("Character (.chara)", () => { })
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
					.Action("Import preset", () => { })
					.Action("Export preset", () => { });
				break;
		}

		if (entity is IDeletable deletable) {
			cb.Separator()
				.Action("Delete", () => deletable.Delete());
		}

		return cb.Build($"##EntityContext_{cb.GetHashCode():X}");
	}
	
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
			ActorEntity _actor => (EntityPose?)_actor.Children.FirstOrDefault(child => child is EntityPose),
			_ => null
		};
		if (pose == null) return;
		mediator.ExportPose(new EntityPoseConverter(pose));
	}
}
