using System;
using System.Collections.Generic;

using Dalamud.Logging;

using Ktisis.Common.Extensions;
using Ktisis.Data.Config.Display;
using Ktisis.Scene.Impl;

namespace Ktisis.Scene.Objects;

// Object flags

[Flags]
public enum ObjectFlags {
	None = 0,
	Removed = 1,
	Selected = 2
}

// Used to define wrappers around objects that may be added to the workspace tree.

public abstract class SceneObject : ITreeNode, IParentable<SceneObject> {
	// Data

	public virtual string Name { get; set; }

	public string UiId { get; init; }

	public virtual ItemType ItemType { get; init; } = ItemType.Default;

	// Object children

	protected readonly List<SceneObject> Children = new();

	// Constructor

	protected SceneObject() {
		this.Name = GetType().Name;
		this.UiId = this.GenerateId();
	}

	// Object methods

	public virtual void Update(SceneContext ctx) {
		if (this.Flags.HasFlag(ObjectFlags.Removed)) {
			if (this.Parent is not null)
				this.SetParent(null);
			return;
		}

		this.Children.ForEach(item => {
			try {
				item.Update(ctx);
			} catch (Exception e) {
				PluginLog.Error($"Error while updating object state for '{item.Name}':\n{e}");
			}
		});
	}

	// Flags

	public ObjectFlags Flags = ObjectFlags.None;

	public bool HasFlag(ObjectFlags flag) => this.Flags.HasFlag(flag);

	public bool IsRemoved() => HasFlag(ObjectFlags.Removed);
	public bool IsSelected() => HasFlag(ObjectFlags.Selected);

	// IParentable

	public SceneObject? Parent { get; private set; }

	public int Count => this.Children.Count;

	public void AddChild(SceneObject child) {
		child.Parent = this;
		this.Children.Add(child);
	}

	public void RemoveChild(SceneObject child) {
		child.Parent = null;
		this.Children.Remove(child);
	}

	public void SetParent(SceneObject? parent) {
		this.Parent?.RemoveChild(this);
		parent?.AddChild(this);
	}

	public IReadOnlyList<SceneObject> GetChildren()
		=> this.Children.AsReadOnly();
}
