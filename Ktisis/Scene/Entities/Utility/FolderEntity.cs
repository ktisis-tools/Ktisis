using System.Collections.Generic;
using System.Linq;

using Ktisis.Common.Utility;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Utility;

public class FolderEntity: SceneEntity, IHideable, IDeletable {

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
			this._Hidden = value;
			foreach (var child in this.RecurseVisible()) 
				if(child.IsHidden != this._Hidden)
					child.ToggleHidden();
			foreach (var child in this.RecurseOverlays())
				child.Visible = !this._Hidden;
	
		}
	}

	public void Dissolve() {
		foreach (var child in Children.ToList()) {
			this.Scene.Add(child);
		}
		this.Remove();
		this.Scene.Refresh();
	}
	public bool Delete() {
		foreach (var child in Children.ToList().Where(child => child is IDeletable).Cast<IDeletable>()) {
			child.Delete();
		}
		foreach (var nondeletable in Children.ToList()) {
			this.Scene.Add(nondeletable);
		}
		this.Remove();
		return true;
	}

	public void ToggleHidden() => IsHidden = !IsHidden;

/*	public Transform? GetTransform() {
		Transform trs = new Transform();
		foreach (var c in Children.OfType<ITransform>()) {
			trs.Position += c.GetTransform()!.Position;
		}
		trs.Position.
		trs.Position /= this.Children.Count();
		return trs;
	}
	public void SetTransform(Transform trans) {
		Transform offset = this.GetTransform()!;
		offset.Position -= trans.Position;
		offset.Rotation -= trans.Rotation;
		offset.Scale -= trans.Scale;
		foreach (var c in Children.OfType<ITransform>()) {
			var local = c.GetTransform();
			local!.Position += offset.Position;
			local.Rotation += offset.Rotation;
			local.Scale += offset.Scale;
			c.SetTransform(local);
		}
	}*/

	protected IEnumerable<IHideable> RecurseVisible()
		=> this.Children.Where(child => child is IHideable).Cast<IHideable>();
	protected IEnumerable<OverlayEntity> RecurseOverlays()
		=> this.Children.Where(child => child is OverlayEntity).Cast<OverlayEntity>();
}
