using System.Collections.Generic;
using System.Linq;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Utility;

public class FolderEntity: SceneEntity, IVisibility {

	public FolderEntity(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.Folder;
		this.Name = "New Folder";
	}
	public bool Visible {
		get => this.RecurseVisible().All(vis => vis.Visible);
		set {
			foreach (var child in this.RecurseVisible())
				child.Visible = value;
		}
	}
	
	protected IEnumerable<IVisibility> RecurseVisible()
		=> this.Children.Where(child => child is IVisibility).Cast<IVisibility>();
}
