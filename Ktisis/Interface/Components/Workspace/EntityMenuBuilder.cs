using GLib.Popups.Context;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Strategy.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class EntityMenuBuilder {
	public EntityMenuBuilder(
		
	) {
		
	}

	public ContextMenu Build(IEditorContext context, SceneEntity entity) {
		// TODO: Write a more modular factory for this.
		
		var cb = new ContextMenuBuilder();

		if (!entity.IsSelected)
			cb.Action("Select", () => context.Selection.Select(entity, SelectMode.Multiple));
		else
			cb.Action("Unselect", () => context.Selection.Unselect(entity));

		if (entity.GetModify() is IVisibility vis)
			cb.Action("Toggle display", () => vis.Toggle());
		
		switch (entity) {
			case ActorEntity:
				cb.Separator().Action("Edit appearance", () => { });
				break;
			case LightEntity:
				cb.Separator().Action("Edit lighting", () => { });
				break;
		}

		if (entity is SkeletonNode or ActorEntity) {
			cb.Separator()
				.Action("Import pose", () => this.ImportPoseFor(entity))
				.Action("Export pose", () => this.ExportPoseFor(entity));
		}

		return cb.Build($"##EntityContext_{cb.GetHashCode():X}");
	}
	
	// Pose

	private void ImportPoseFor(SceneEntity entity) {
		
	}

	private void ExportPoseFor(SceneEntity entity) {
		
	}
}
