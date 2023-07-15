using System;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using Ktisis.Interface;
using Ktisis.Scenes.Objects.Impl;

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

	// Object

	public virtual List<SceneObject> Children { get; init; } = new();

	public bool Selected;
	public int SortPriority { get; init; } = 0;

	// Constructor

	protected SceneObject() {
		Name = GetType().Name;
		UiId = Gui.GenerateId(this);
	}

	// Object methods

	internal virtual void Update()
		=> Children.ForEach(UpdateItem);

	private void UpdateItem(SceneObject item) {
		try {
			item.Update();
		} catch (Exception e) {
			PluginLog.Error($"Error while updating object state for '{item.Name}':\n{e}");
		}
	}

	public void Remove() {
		Parent?.RemoveChild(this);
	}

	// Selection

	public virtual void Select() {
		Selected = true;
	}

	public virtual void Unselect() {
		Selected = false;
	}

	public virtual void SetSelected(bool select) {
		Selected = select;
	}

	public void ToggleSelected() {
		SetSelected(!Selected);
	}

	// Children

	public void AddChild(SceneObject child) {
		child.Parent = this;
		Children.Add(child);
	}

	public void RemoveChild(SceneObject child) {
		child.Parent = null;
		Children.Remove(child);
	}

	public void AddToParent(SceneObject parent) {
		Parent?.RemoveChild(this);
		parent.AddChild(this);
	}

	public void SortChildren() => Children
		.Sort((a, b) => a.SortPriority - b.SortPriority);
}
