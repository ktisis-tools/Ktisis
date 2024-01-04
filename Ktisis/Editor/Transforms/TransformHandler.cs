using System.Collections.Generic;
using System.Linq;

using Ktisis.Common.Utility;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Actions.Types;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Strategy.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Transforms;

public interface ITransformHandler {
	public ITransformTarget? Target { get; }
	
	public ITransformMemento Begin(ITransformTarget target);
}

public interface ITransformMemento : IMemento {
	public ITransformMemento Save();
	
	public ITransformMemento SetTransform(Transform transform);
	
	public void Dispatch();
}

public class TransformHandler : ITransformHandler {
	private readonly IActionManager _action;
	private readonly ISelectManager _select;

	public TransformHandler(
		IActionManager action,
		ISelectManager select
	) {
		this._action = action;
		this._select = select;
		select.Changed += this.OnSelectionChanged;
	}
	
	// Transform target
	
	public ITransformTarget? Target { get; private set; }

	private void OnSelectionChanged(ISelectManager sender) {
		var selected = this._select.GetSelected()
			.Where(entity => entity is { IsValid: true });
		
		var selectTargets = TransformResolver.GetCorrelatingBones(selected, true)
			.Where(entity => entity.Edit() is ITransform)
			.ToList();

		if (selectTargets.Count == 0) {
			this.Target = null;
			return;
		}

		var target = selectTargets.FirstOrDefault();
		if (target is BoneNode)
			target = TransformResolver.GetPoseTarget(selectTargets);

		this.Target = new TransformTarget(target, selectTargets);
	}
	
	// Transformation

	public ITransformMemento Begin(ITransformTarget target) {
		return new TransformMemento(
			this,
			target
		).Save();
	}

	private class TransformMemento : ITransformMemento {
		private readonly TransformHandler _handler;

		private readonly ITransformTarget Target;

		private Transform? Initial;
		private Transform? Final;

		//private readonly Dictionary<SceneEntity, Transform> Initial = new();
		//private readonly Dictionary<SceneEntity, Transform> Final = new();

		public TransformMemento(
			TransformHandler handler,
			ITransformTarget target
		) {
			this._handler = handler;
			this.Target = target;
		}

		public ITransformMemento Save() {
			//this.SaveMap(this.Initial);
			this.Initial = this.Target.GetTransform();
			return this;
		}

		public ITransformMemento SetTransform(Transform transform) {
			this.Final = transform;
			this.Target.SetTransform(transform);
			return this;
		}

		public void Restore() {
			if (this.Initial != null)
				this.SetTransform(this.Initial);
		}

		public void Apply() {
			if (this.Final != null)
				this.SetTransform(this.Final);
		}

		private void SaveMap(Dictionary<SceneEntity, Transform> map) {
			map.Clear();
			foreach (var entity in this.Target.Targets) {
				if (!entity.IsValid || entity.Edit() is not ITransform target) continue;
				var transform = target.GetTransform();
				if (transform != null)
					map.Add(entity, transform);
			}
		}

		private static void ApplyMap(Dictionary<SceneEntity, Transform> map) {
			foreach (var (entity, transform) in map) {
				if (entity.IsValid && entity.Edit() is ITransform target)
					target.SetTransform(transform);
			}
		}

		private bool IsDispatch;

		public void Dispatch() {
			if (this.IsDispatch) return;
			this.IsDispatch = true;
			this.Final = this.Target.GetTransform();
			this._handler._action.History.Add(this);
		}
	}
}
