using System;
using System.Collections.Generic;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Events;
using Ktisis.Scene.Entities;

namespace Ktisis.Editor.Selection;

public enum SelectMode {
	Default,
	Multiple
}

public delegate void SelectChangedHandler(ISelectManager sender);

public interface ISelectManager {
	public event SelectChangedHandler Changed;
	
	public void Update();
	
	public int Count { get; }
	
	public IEnumerable<SceneEntity> GetSelected();

	public bool IsSelected(SceneEntity entity);

	public void Select(SceneEntity entity, SelectMode mode = SelectMode.Default);
	public void Unselect(SceneEntity entity);
	
	public void Clear();
}

public class SelectManager : ISelectManager {
	private readonly IEditorContext _context;

	private readonly Event<Action<ISelectManager>> _changed = new();
	public event SelectChangedHandler Changed {
		add => this._changed.Add(value.Invoke);
		remove => this._changed.Remove(value.Invoke);
	}

	private readonly List<SceneEntity> Selected = new();
	
	public SelectManager(
		IEditorContext context
	) {
		this._context = context;
	}
	
	// Update handler

	public void Update() {
		var remove = this.Selected.RemoveAll(item => !item.IsValid);
		if (remove > 0) this.InvokeChanged();
	}
	
	// Selection

	public int Count => this.Selected.Count;
	
	public IEnumerable<SceneEntity> GetSelected() => this.Selected.AsReadOnly();

	public bool IsSelected(SceneEntity entity)
		=> this.Selected.Contains(entity);

	public void Select(SceneEntity entity) {
		this.Selected.Remove(entity);
		this.Selected.Add(entity);
		this.InvokeChanged();
	}

	public void Select(SceneEntity entity, SelectMode mode) {
		var isSelect = this.IsSelected(entity);
		var isMulti = this.Count > 1;
		var modeMulti = mode == SelectMode.Multiple;
		if (!modeMulti)
			this.Selected.Clear();

		if (!isSelect || !modeMulti && isMulti)
			this.Selected.Add(entity);
		else
			this.Selected.Remove(entity);
		
		this.InvokeChanged();
	}

	public void Unselect(SceneEntity entity) {
		if (this.Selected.Remove(entity))
			this.InvokeChanged();
	}

	public void Clear() {
		this.Selected.Clear();
		this.InvokeChanged();
	}
	
	// Event invocation

	private void InvokeChanged() {
		try {
			this._changed.Invoke(this);
		} catch (Exception err) {
			Ktisis.Log.Error(err.ToString());
		}
	}
}
