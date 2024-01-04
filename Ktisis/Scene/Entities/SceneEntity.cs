using System.Collections.Generic;
using System.Linq;

using Ktisis.Editor.Selection;
using Ktisis.Editor.Strategy;
using Ktisis.Editor.Strategy.Types;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities;

public abstract class SceneEntity : IComposite {
	protected readonly ISceneManager Scene;
	
	protected IEditEntity Strategy { get; init; }
	
	public string Name { get; set; } = string.Empty;
	public EntityType Type { get; protected init; }
	
	public IEditEntity GetEdit() => this.Strategy;

	public virtual bool IsValid => this.Scene.IsValid && this.Parent != null;
	
	protected SceneEntity(
		ISceneManager scene
	) {
		this.Strategy = new BaseEditor(); // TODO: Move this to builder?
		this.Scene = scene;
	}

	public virtual void Update() {
		if (!this.IsValid) return;
		
		foreach (var child in this.Children)
			child.Update();
	}
	
	// Selection

	private ISelectManager Selection => this.Scene.Context.Selection;
	
	public bool IsSelected => this.Selection.IsSelected(this);

	public void Select(SelectMode mode = SelectMode.Default)
		=> this.Selection.Select(this, mode);
	
	// IComposite
	
	private readonly List<SceneEntity> _children = new();

	public virtual SceneEntity? Parent { get; set; } = null;

	public virtual IEnumerable<SceneEntity> Children => this._children;
	protected List<SceneEntity> GetChildren() => this._children;

	public virtual bool Add(SceneEntity entity) {
		var exists = this._children.Contains(entity);
		if (exists) return false;
		this._children.Add(entity);
		entity.Parent?.Remove(entity);
		entity.Parent = this;
		return true;
	}

	public virtual bool Remove(SceneEntity entity) {
		var remove = this._children.Remove(entity);
		entity.Parent = null;
		return remove;
	}

	public virtual void Remove() {
		this.Parent?.Remove(this);
		this.Clear();
	}

	public virtual void Clear() {
		foreach (var child in this.Children.ToList())
			child.Remove();
	}

	public IEnumerable<SceneEntity> Recurse() {
		foreach (var child in this.Children) {
			yield return child;
			foreach (var reChild in child.Recurse())
				yield return reChild;
		}
	}

	public bool IsChildOf(SceneEntity entity) {
		var parent = this.Parent;
		var i = 0;
		while (parent != null && i++ < 1000) {
			if (parent == entity)
				return true;
			parent = parent.Parent;
		}
		return false;
	}
}
