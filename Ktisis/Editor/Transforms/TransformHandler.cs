using System.Linq;
using System.Numerics;

using Ktisis.Common.Utility;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Actions.Types;
using Ktisis.Editor.Selection;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Transforms;

public interface ITransformHandler {
	public ITransformTarget? Target { get; }
	
	public ITransformMemento Begin(ITransformTarget target);
}

public interface ITransformMemento : IMemento {
	public ITransformMemento Save();
	
	public void SetTransform(Transform transform);
	public void SetMatrix(Matrix4x4 matrix);
	
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
		
		var selectTargets = TransformResolver.GetCorrelatingBones(selected, yieldDefault: true)
			.Where(entity => entity is ITransform)
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

		public TransformMemento(
			TransformHandler handler,
			ITransformTarget target
		) {
			this._handler = handler;
			this.Target = target;
		}

		public ITransformMemento Save() {
			this.Initial = this.Target.GetTransform();
			return this;
		}

		public void SetTransform(Transform transform) => this.Target.SetTransform(transform);

		public void SetMatrix(Matrix4x4 matrix) => this.Target.SetMatrix(matrix);

		public void Restore() {
			if (this.Initial != null)
				this.Target.SetTransform(this.Initial);
		}

		public void Apply() {
			if (this.Final != null)
				this.Target.SetTransform(this.Final);
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
