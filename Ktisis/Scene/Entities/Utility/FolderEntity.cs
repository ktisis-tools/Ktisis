using System.Collections.Generic;
using System.Linq;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Utility;

public class FolderEntity: SceneEntity, IHideable {

	private bool _Hidden = false;
	public FolderEntity(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.Folder;
		this.Name = "New Folder";
	}
	public bool IsHidden {
		get => this._Hidden;
		set {
			foreach (var child in this.RecurseVisible())
				child.IsHidden = value;
			foreach (var child in this.RecurseOverlays())
				child.Visible = !value;
			this._Hidden = value;
		}
	}

	public void ToggleHidden() => IsHidden = !IsHidden;
	
	protected IEnumerable<IHideable> RecurseVisible()
		=> this.Children.Where(child => child is IHideable).Cast<IHideable>();
	protected IEnumerable<OverlayEntity> RecurseOverlays()
		=> this.Children.Where(child => child is OverlayEntity).Cast<OverlayEntity>();
}
