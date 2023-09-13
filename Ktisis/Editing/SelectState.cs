using System.Linq;
using System.Collections.Generic;

using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;

namespace Ktisis.Editing;

public enum SelectFlags {
	None,
	Ctrl
}

public delegate void OnSelectionChangedHandler(SelectState sender, SceneObject? item);

public class SelectState {
	// Constructor
	
	private readonly List<SceneObject> _selected = new();

	public void Update(SceneGraph? scene) {
		if (scene != null)
			scene.OnSceneObjectRemoved += OnSceneObjectRemoved;
		else
			this.Clear();
	}
	
	// Events

	public event OnSelectionChangedHandler? OnSelectionChanged;

	private void InvokeChange(SceneObject? item)
		=> this.OnSelectionChanged?.Invoke(this, item);

	private void OnSceneObjectRemoved(SceneGraph _scene, SceneObject item) {
		RemoveItem(item);
		InvokeChange(item);
	}

	// Item access
	
	public int Count => this._selected.Count;

	public IEnumerable<SceneObject> GetSelected()
		=> this._selected.AsReadOnly();

	public bool IsManipulable() => GetSelected()
		.Any(item => item is ITransform);
	
	// Item management
	
	private void Clear() {
		this._selected.Clear();
		InvokeChange(null);
	}

	private void AddItem(SceneObject item) {
		item.Flags |= ObjectFlags.Selected;
		this._selected.Remove(item);
		this._selected.Add(item);
	}

	private void RemoveItem(SceneObject item) {
		item.Flags &= ~ObjectFlags.Selected;
		this._selected.Remove(item);
	}

	private void RemoveAll() {
		this._selected.ForEach(item => item.Flags &= ~ObjectFlags.Selected);
		this._selected.Clear();
	}
	
	// Handler

	public void HandleClick(SceneObject item, SelectFlags flags) {
		var isSelect = item.IsSelected();
		var isMulti = this.Count > 1;
		
		// Ctrl modifier
		if (flags.HasFlag(SelectFlags.Ctrl)) {
			RemoveItem(item);
		} else {
			RemoveAll();
		}

		var add = !isSelect || isMulti;
		if (add) AddItem(item);
		
		InvokeChange(add ? item : null);
	}
}
