using System.Collections.Generic;

using Dalamud.Interface;

using Ktisis.Interface;
using Ktisis.Interface.Items;

namespace Ktisis.Scenes.Objects; 

// Used to define wrappers around objects that may be added to the workspace tree.

public abstract class SceneObject : ITreeNode {
	// Data
	
	public string Name { get; set; }

	public SceneObject? Parent { get; set; }
	
	// Tree node properties
	// TODO: This probably doesn't belong here, revisit it later.
	
	public string UiId { get; set; }
	public virtual uint Color { get; init; } = 0xFFFFFFFF;
	public virtual FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.None;

	public int SortPriority { get; init; } = 0;

	// Object

	public virtual List<SceneObject>? Children { get; init; } = new();

	// Constructor

	protected SceneObject() {
		Name = GetType().Name;
		UiId = Gui.GenerateId(this);
	}
	
	// Children

	public void AddChild(SceneObject child) {
		if (Children == null) return;
		child.Parent = this;
		Children.Add(child);
	}

	public void RemoveChild(SceneObject child) {
		child.Parent = null;
		Children?.Remove(child);
	}

	public void ParentTo(SceneObject parent) {
		Parent?.RemoveChild(this);
		parent.AddChild(this);
	}

	public void SortChildren()
		=> Children?.Sort((a, b) => a.SortPriority - b.SortPriority);

	// Update

	internal virtual void Update()
		=> Children?.ForEach(child => child.Update());
}