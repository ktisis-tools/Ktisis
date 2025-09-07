using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Ktisis.Editor.Selection;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities;

public abstract class SceneEntity : IComposite {
	protected readonly ISceneManager Scene;
	
	public string Name { get; set; } = string.Empty;
	public EntityType Type { get; protected init; }

	public virtual bool IsValid => this.Scene.IsValid && this.Parent != null;
	public SceneEntity Root => this.Parent is null or { Type: EntityType.Invalid } ? this : this.Parent.Root;
	
	protected SceneEntity(
		ISceneManager scene
	) {
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

	public void Select(SelectMode mode = SelectMode.Default) => this.Selection.Select(this, mode);
	public void Unselect() => this.Selection.Unselect(this);
	
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
	
	//Presetting
	protected void ToggleView(ImmutableHashSet<string> names, bool newState) {
		if (this is BoneNode node && names.Contains(node.Info.Name)) {
			node.Visible = newState;
			((IVisibility) node).Toggle();
		}
	
		foreach (var bone in this.Recurse().OfType<BoneNode>()) {
			if (names.Contains(bone.Info.Name)) {
				bone.Visible = newState;
			}
		}
	}

	protected ImmutableHashSet<string> GetEnabledBones() {
		HashSet<string> bones = new(128);

		foreach (var bone in this.Recurse().OfType<BoneNode>()) {
			if (bone.Visible)
				bones.Add(bone.Info.Name);
		}
		
		return bones.ToImmutableHashSet();
	}
}
